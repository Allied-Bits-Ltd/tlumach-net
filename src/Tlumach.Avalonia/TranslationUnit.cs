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

        public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
            : base(translationManager, translationConfiguration, key)
        {
            string value = InternalGetValueAsText(TranslationManager.CurrentCulture);
            _value = new BehaviorSubject<string>(value);
            TranslationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources.
                TranslationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
                _value.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Update listeners with the string value obtained for the new culture
            _value.OnNext(InternalGetValueAsText(args.Culture));
        }
    }
}
