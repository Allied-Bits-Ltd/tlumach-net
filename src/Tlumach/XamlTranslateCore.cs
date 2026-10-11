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
    /// An internal class used in XAML integrations: provides the current text of a translation unit in the <see cref="Value"/> property, which is updated when
    /// the culture of the unit's translation manager changes and when the unit fires <see cref="TranslationUnit.OnChange"/>.
    /// <para>The events of the unit and of the manager reference this object weakly (see <see cref="WeakUnitSubscription{TOwner}"/>), because the generated classes keep
    /// them in static properties. The binding of the target property, which uses this object as its source, keeps it alive as long as the target.</para>
    /// </summary>
    internal class XamlTranslateCore : INotifyPropertyChanged
    {
        private readonly Action<Action> _postToUi;
        private TranslationUnit? _unit;
        private WeakUnitSubscription<XamlTranslateCore>? _subscription;

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
                _subscription?.Unsubscribe();
                _subscription = null;

                _unit = value;

                // Subscribe new. A culture change notifies the binding only when the text differs; OnChange (NotifyPlaceholdersUpdated) always does.
                if (_unit is not null)
                    _subscription = new WeakUnitSubscription<XamlTranslateCore>(_unit, this, static (core, cultureChanged) => core.SetValueFromUnit(forceNotify: !cultureChanged));

                // Set current value
                SetValueFromUnit(forceNotify: true);
            }
        }

        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set
            {
                if (string.Equals(_value, value, StringComparison.Ordinal))
                    return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
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
