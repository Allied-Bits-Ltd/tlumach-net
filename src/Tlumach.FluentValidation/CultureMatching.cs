// <copyright file="CultureMatching.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.FluentValidation;

/// <summary>
/// Compares cultures by their language for the fallback rules of <see cref="TlumachLanguageManager"/>.
/// </summary>
internal static class CultureMatching
{
    /// <summary>
    /// Returns the name of the topmost neutral culture of the given culture: "en" for "en-GB", "zh-Hant" for "zh-Hant-TW", and an empty string for the invariant culture.
    /// </summary>
    /// <param name="culture">The culture to get the name of the topmost neutral culture of.</param>
    /// <returns>The name of the topmost neutral culture.</returns>
    public static string NeutralName(CultureInfo culture)
    {
        CultureInfo current = culture;
        while (!current.IsNeutralCulture && current.Parent.Name.Length != 0)
            current = current.Parent;

        return current.Name;
    }

    /// <summary>
    /// Tells whether the culture is English or a culture of English, for which the built-in messages of FluentValidation are the English ones.
    /// </summary>
    /// <param name="culture">The culture to check.</param>
    /// <returns><see langword="true"/> if the culture is English.</returns>
    public static bool IsEnglish(CultureInfo culture)
        => NeutralName(culture).Equals("en", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the culture of the default file of the translation manager, or <see langword="null"/> when the locale of the default file is unknown or not a valid culture name.
    /// </summary>
    /// <param name="manager">The translation manager, whose default file is examined.</param>
    /// <returns>The culture of the default file, or <see langword="null"/>.</returns>
    public static CultureInfo? DefaultFileCulture(TranslationManager manager)
    {
        string? locale = manager.DefaultConfiguration?.DefaultFileLocale;
        if (string.IsNullOrEmpty(locale))
            return null;

        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Tells whether the default file of the translation manager is written in the language of the given culture, so that its text is the text of that culture.
    /// </summary>
    /// <param name="manager">The translation manager, whose default file is examined.</param>
    /// <param name="culture">The culture to compare with the culture of the default file.</param>
    /// <returns><see langword="true"/> if the default file and the culture share the language.</returns>
    public static bool DefaultFileSharesLanguage(TranslationManager manager, CultureInfo culture)
    {
        CultureInfo? defaultCulture = DefaultFileCulture(manager);
        if (defaultCulture is null)
            return false;

        string language = NeutralName(culture);
        return language.Length != 0 && language.Equals(NeutralName(defaultCulture), StringComparison.OrdinalIgnoreCase);
    }
}
