// <copyright file="ITlumachMessageBinding.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.DataAnnotations
{
    /// <summary>
    /// The route through which a localized validation message is read.
    /// <para>An implementation is immutable and holds no text, so a change of the culture and a reload of a translation are picked up on the next call.</para>
    /// </summary>
    internal interface ITlumachMessageBinding
    {
        /// <summary>
        /// Returns the culture, for which the message is read.
        /// </summary>
        /// <param name="cultureSource">The culture source that the attribute asks for.</param>
        /// <returns>The effective culture.</returns>
        CultureInfo GetCulture(TranslationCultureSource cultureSource);

        /// <summary>
        /// Returns the message template without processing the placeholders it contains.
        /// </summary>
        /// <param name="cultureSource">The culture source that the attribute asks for.</param>
        /// <returns>The template, or an empty string when there is no text.</returns>
        string GetTemplate(TranslationCultureSource cultureSource);
    }
}
