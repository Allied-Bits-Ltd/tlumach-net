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

using System.Globalization;
using System.Reactive.Subjects;
using System.Reflection;

using Tlumach.Base;

namespace Tlumach.Avalonia
{
    public class TranslationUnit : BaseTranslationUnit, IDisposable
    {
        private readonly BehaviorSubject<string> _value;

        public IObservable<string> Value => _value;

        public string CurrentValue => _value.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationUnit"/> class.
        /// <para>This constructor is used to create a constant translation unit, i.e., the one whose text does not come from a translation file but is fixed.</para>
        /// </summary>
        /// <param name="constantValue">The string value to return.</param>
        /// <param name="translationConfiguration">A reference to an instance of <seealso cref="TranslationConfiguration"/>. If <paramref name="containsPlaceholders"/> is <see langword="true"/>, this configuration's TextProcessingMode is used to process the <paramref name="constantValue"/>.</param>
        /// <param name="containsPlaceholders">Specifies whether <paramref name="constantValue"/> contains placeholders and should be processed accordingly.</param>
        public TranslationUnit(string constantValue, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
            : base(constantValue, translationConfiguration, containsPlaceholders)
        {
            _value = new BehaviorSubject<string>(constantValue);
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

        /// <summary>
        /// Notifies XAML bindings that they need to request a new value and update the controls.
        /// </summary>
        public override void NotifyPlaceholdersUpdated()
        {
            if (_constantValue is null)
            {
                // Update listeners with the new string value
                _value.OnNext(GetValue(TranslationManager.CurrentCulture));
            }
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Update listeners with the string value obtained for the new culture
            _value.OnNext(GetValue(args.Culture));
        }
    }
}
