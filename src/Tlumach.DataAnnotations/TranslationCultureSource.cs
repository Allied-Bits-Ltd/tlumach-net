// <copyright file="TranslationCultureSource.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.DataAnnotations
{
    /// <summary>
    /// Selects the culture, for which a localized validation message is read.
    /// </summary>
    public enum TranslationCultureSource
    {
        /// <summary>
        /// Defer to <see cref="TlumachValidationDefaults.CultureSource"/>. This is the value that an attribute has when the application does not set the property explicitly.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use <see cref="Tlumach.TranslationManager.CurrentCulture"/> of the translation manager that provides the message. This is consistent with <see cref="Tlumach.TranslationUnit.CurrentValue"/> and with the rest of Tlumach.
        /// </summary>
        TranslationManager = 1,

        /// <summary>
        /// Use the ambient <see cref="System.Globalization.CultureInfo.CurrentCulture"/>, which is what a request localization pipeline of a web application sets for the duration of a request.
        /// </summary>
        Ambient = 2,
    }
}
