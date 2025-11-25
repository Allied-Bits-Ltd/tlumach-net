// <copyright file="TranslationUnit.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel;

using Tlumach.Base;

namespace Tlumach.UWP
{
    public sealed class TranslationUnit : BaseTranslationUnit, INotifyPropertyChanged
    {
        private string? _currentValue;

        public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
            : base(translationManager, translationConfiguration, key)
        {
            // Subscribe for culture changes
            TranslationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
        }

        public string CurrentValue
        {
            get
            {
                if (_currentValue is null)
                {
                    // Initial text
                    _currentValue = InternalGetValueAsText(TranslationManager.CurrentCulture);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentValue)));
                }

                return _currentValue;
            }

            private set
            {
                if (string.Equals(_currentValue, value, StringComparison.Ordinal))
                    return;

                _currentValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentValue)));
            }
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Re-translate and notify
            CurrentValue = InternalGetValueAsText(args.Culture);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
