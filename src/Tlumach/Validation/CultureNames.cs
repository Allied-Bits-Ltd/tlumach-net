// <copyright file="CultureNames.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
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

namespace Tlumach.Validation;

/// <summary>
/// Decides whether a name taken from a file name or a configuration is a culture.
/// </summary>
internal static class CultureNames
{
    /// <summary>
    /// The names of the cultures known to the operating system. With ICU, <see cref="CultureInfo"/> accepts any well-formed name (so "Strings_backup.json" would look like the "backup" translation of "Strings.json"), while the list of predefined cultures is the same on every platform.
    /// </summary>
    private static readonly HashSet<string> KnownNames = new(
        CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name).Where(n => n.Length > 0),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the culture with the given name when the name is the name of a predefined culture.
    /// </summary>
    /// <param name="name">The name to check, such as "de" or "de-AT".</param>
    /// <param name="culture">The culture, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="name"/> names a predefined culture.</returns>
    public static bool TryGetCulture(string? name, out CultureInfo? culture)
    {
        culture = null;

        if (name is null || string.IsNullOrWhiteSpace(name) || !KnownNames.Contains(name.Trim()))
            return false;

        try
        {
            culture = new CultureInfo(name.Trim());
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
