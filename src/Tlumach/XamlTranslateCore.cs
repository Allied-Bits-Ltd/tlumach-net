// <copyright file="XamlTranslateCore.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tlumach
{
    /// <summary>
    /// An internal class used in XAML integrations.
    /// </summary>
    public class XamlTranslateCore : INotifyPropertyChanged
    {
        private readonly Action<Action> _postToUi;
        private TranslationUnit? _unit;
        private TranslationManager? _translationManager;

        public XamlTranslateCore(Action<Action> postToUi)
        {
            _postToUi = postToUi ?? throw new ArgumentNullException(nameof(postToUi));
        }

        public TranslationUnit? Unit
        {
            get => _unit;
            set
            {
                if (ReferenceEquals(_unit, value)) return;

                // Unsubscribe old
                if (_translationManager is not null)
                    _translationManager.OnCultureChanged += OnCultureChanged;

                if (_unit is not null)
                    _unit.OnChange -= TranslationUnit_OnChange;

                _unit = value;

                if (_unit is not null)
                    _unit.OnChange += TranslationUnit_OnChange;

                // Subscribe new
                _translationManager = _unit?.TranslationManager;
                if (_translationManager is not null)
                    _translationManager.OnCultureChanged += OnCultureChanged;

                // Set current value
                SetValueFromUnit(forceNotify: true);
            }
        }

        private void TranslationUnit_OnChange(object? sender, EventArgs e)
        {
            SetValueFromUnit(forceNotify: true);
        }

        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            private set
            {
                if (string.Equals(_value, value, StringComparison.Ordinal))
                    return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
        {
            SetValueFromUnit(forceNotify: false);
        }

        private void SetValueFromUnit(bool forceNotify)
        {
            var newText = _unit?.CurrentValue ?? string.Empty;

            _postToUi(() =>
            {
                if (forceNotify)
                {
                    _value = newText;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
                else
                {
                    Value = newText;
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
