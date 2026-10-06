// <copyright file="TranslationEntrySource.cs" company="Allied Bits Ltd.">
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

namespace Tlumach
{
    /// <summary>
    /// Tells where <see cref="TranslationManager.GetValueWithSource(Tlumach.Base.TranslationConfiguration, string, System.Globalization.CultureInfo, out TranslationEntrySource)"/> found the returned entry.
    /// </summary>
    public enum TranslationEntrySource
    {
        /// <summary>
        /// No translation contains the key. The returned entry is empty unless a handler of <see cref="TranslationManager.OnTranslationValueNotFound"/> supplied a value.
        /// </summary>
        NotFound = 0,

        /// <summary>
        /// The translation of a requested culture contains the key, or a handler of <see cref="TranslationManager.OnTranslationValueNeeded"/> supplied the value.
        /// </summary>
        Culture = 1,

        /// <summary>
        /// The translation of the basic culture of a requested culture contains the key, for example the translation for "de-DE" when "de-AT" was requested.
        /// </summary>
        BasicCulture = 2,

        /// <summary>
        /// The value comes from the default translation. This is also the source when the requested culture is the locale of the default file.
        /// </summary>
        DefaultTranslation = 3,
    }
}
