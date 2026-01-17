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
using System.Diagnostics;
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.UWP
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TranslationUnit : BaseTranslationUnit, INotifyPropertyChanged, IDisposable
    {
        private string? _currentValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationUnit"/> class.
        /// <para>For internal use. This constructor is used by <seealso cref="UntranslatedUnit"/>.</para>
        /// </summary>
        /// <param name="translationManager">The translation manager to which the unit is bound.</param>
        /// <param name="translationConfiguration">The translation configuration used to create the unit.</param>
        /// <param name="containsPlaceholders">An indicator of whether the unit contains placeholders.</param>
        protected TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
            : base(translationManager, translationConfiguration, containsPlaceholders)
        {
            if (TranslationManager != TranslationManager.Empty)
                TranslationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
        }

        public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key, bool containsPlaceholders)
            : base(translationManager, translationConfiguration, key, containsPlaceholders)
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
                    _currentValue = GetValue(TranslationManager == TranslationManager.Empty ? CultureInfo.CurrentCulture : TranslationManager.CurrentCulture);
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

        public override string ToString() => CurrentValue ?? string.Empty;

        public static implicit operator string(TranslationUnit unit)
            => unit?.ToString() ?? string.Empty;

        private string DebuggerDisplay() => $"Translation unit '{Key}': '{CurrentValue ?? "(No value)"}'";

        /// <summary>
        /// Notifies XAML bindings that they need to request a new value and update the controls.
        /// </summary>
        public override void NotifyPlaceholdersUpdated()
        {
            // Update listeners with the new string value
            CurrentValue = GetValue(TranslationManager.CurrentCulture);
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Re-translate and notify
            CurrentValue = GetValue(args.Culture);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources.
#pragma warning disable S1066 // Mergeable "if" statements should be combined
                if (TranslationManager != TranslationManager.Empty)
                    TranslationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
#pragma warning restore S1066 // Mergeable "if" statements should be combined
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
