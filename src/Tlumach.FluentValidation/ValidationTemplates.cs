// <copyright file="ValidationTemplates.cs" company="Allied Bits Ltd.">
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

using FluentValidation;
using FluentValidation.Resources;

namespace Tlumach.FluentValidation;

/// <summary>
/// Reads the raw templates of rule-level messages and names at validation time.
/// </summary>
internal static class ValidationTemplates
{
    private const string NoTextFormat = "The Tlumach translation key '{0}' provides no text for the culture '{1}'.";

    /// <summary>
    /// Returns the culture for a rule-level message. It is the same culture that the language manager uses for the built-in messages, so that the messages of one validation
    /// agree: the rules of <see cref="TlumachLanguageManager"/> when it is installed, otherwise <c>LanguageManager.Culture</c> or <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    /// <returns>The culture to read the text for.</returns>
    public static CultureInfo CurrentCulture()
    {
        ILanguageManager languageManager = ValidatorOptions.Global.LanguageManager;

        return languageManager is TlumachLanguageManager tlumach
            ? tlumach.ResolveCulture()
            : languageManager.Culture ?? CultureInfo.CurrentUICulture;
    }

    public static string Read(BaseTranslationUnit unit)
    {
        CultureInfo culture = CurrentCulture();
        string template = unit.GetValueAsTemplate(culture);

        return string.IsNullOrEmpty(template) ? throw NoText(unit.Key, culture) : template;
    }

    public static string Read(TranslationManager manager, string key)
    {
        CultureInfo culture = CurrentCulture();
        string? template = manager.GetValue(key, culture).Text;

        return string.IsNullOrEmpty(template) ? throw NoText(key, culture) : template;
    }

    private static InvalidOperationException NoText(string key, CultureInfo culture)
        => new(string.Format(CultureInfo.CurrentCulture, NoTextFormat, key, culture.Name));
}
