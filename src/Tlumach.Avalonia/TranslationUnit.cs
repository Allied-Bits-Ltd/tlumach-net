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

using System.Diagnostics;
using System.Reactive.Subjects;

using Tlumach.Base;

namespace Tlumach.Avalonia
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TranslationUnit : BaseTranslationUnit, IDisposable
    {
        protected readonly BehaviorSubject<string> _value;

        public IObservable<string> Value => _value;

        public string CurrentValue => _value.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationUnit"/> class.
        /// <para>For internal use. This constructor is used by <seealso cref="UntranslatedUnit"/>.</para>
        /// </summary>
        /// <param name="translationManager">The translation manager to which the unit is bound.</param>
        /// <param name="translationConfiguration">The translation configuration used to create the unit.</param>
        /// <param name="containsPlaceholders">An indicator of whether the unit contains placeholders.</param>
        protected TranslationUnit(string sourceValue, TranslationManager translationManager, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
            : base(translationManager, translationConfiguration, containsPlaceholders)
        {
            _value = new BehaviorSubject<string>(sourceValue);

            if (TranslationManager != TranslationManager.Empty)
                TranslationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
        }

        public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key, bool containsPlaceholders)
            : base(translationManager, translationConfiguration, key, containsPlaceholders)
        {
            string value = GetValue(TranslationManager.CurrentCulture);
            _value = new BehaviorSubject<string>(value);
            TranslationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources.
                if (TranslationManager != TranslationManager.Empty)
                    TranslationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
                _value.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString() => CurrentValue ?? string.Empty;

        public static implicit operator string(TranslationUnit unit)
            => unit?.ToString() ?? string.Empty;

        private string DebuggerDisplay() => $"Translation unit '{Key}': '{CurrentValue ?? "(No value)"}'";

        /// <summary>
        /// Notifies XAML bindings that they need to request a new value and update the controls.
        /// </summary>
        public override void NotifyPlaceholdersUpdated()
        {
            _value.OnNext(GetValue(TranslationManager.CurrentCulture));
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Update listeners with the string value obtained for the new culture
            _value.OnNext(GetValue(args.Culture));
        }
    }
}
