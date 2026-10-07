// <copyright file="TlumachCultureOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Web;

/// <summary>
/// The cultures that a web application supports, and the path of the endpoint that stores the culture chosen by the user.
/// <para>Shared by the Blazor, the ASP.NET Core hosting, and the MVC and Razor Pages integrations. Register an instance with
/// <see cref="TlumachWebServiceCollectionExtensions.AddTlumachCultures"/> or, in a Blazor application, with <c>AddTlumachBlazor</c>.</para>
/// </summary>
public class TlumachCultureOptions
{
    /// <summary>
    /// The default value of <see cref="CultureEndpoint"/>.
    /// </summary>
    public const string DefaultCultureEndpoint = "/tlumach/culture";

    // The names of all cultures known to the runtime, built once on first use (Lazy<T> is thread-safe by default).
    private static readonly Lazy<HashSet<string>> PredefinedCultureNames = new(
        () => new HashSet<string>(CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name), StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets or sets the cultures that the user may choose. When the list is empty, any culture is accepted.
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; set; } = [];

    /// <summary>
    /// Gets or sets the culture used when the culture of the request or of the browser is not supported.
    /// <para>When not set, the first supported culture is used.</para>
    /// </summary>
    public CultureInfo? DefaultCulture { get; set; }

    /// <summary>
    /// Gets or sets the path of the endpoint that stores the culture in a cookie, relative to the base path of the application.
    /// </summary>
    public string CultureEndpoint { get; set; } = DefaultCultureEndpoint;

    /// <summary>
    /// Finds the supported culture that serves the given culture: the culture itself, then the closest of its parent cultures, then a culture of the same language.
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    /// <returns>The matching supported culture, the requested culture itself when <see cref="SupportedCultures"/> is empty, or <see langword="null"/> if no supported culture matches.</returns>
    public CultureInfo? FindSupportedCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (SupportedCultures.Count == 0)
            return culture;

        for (CultureInfo? current = culture; current is not null; current = current.Name.Length == 0 ? null : current.Parent)
        {
            CultureInfo? supported = FindSupportedCultureByExactName(current.Name);
            if (supported is not null)
                return supported;
        }

        return FindSupportedCultureByLanguage(culture.TwoLetterISOLanguageName);
    }

    /// <summary>
    /// Finds the supported culture that serves the culture with the given name.
    /// <para>When the name is the name of a predefined culture (one listed by <see cref="CultureInfo.GetCultures(CultureTypes)"/> with <see cref="CultureTypes.AllCultures"/>,
    /// compared ignoring case), that culture is obtained with <see cref="CultureInfo.GetCultureInfo(string)"/> and matched by <see cref="FindSupportedCulture(CultureInfo)"/>,
    /// so both overloads give the same result. When <see cref="SupportedCultures"/> is empty, the predefined culture is returned.</para>
    /// <para>Any other name is matched with string operations only, and no culture is created for it: the culture itself, then the closest culture whose name is a prefix
    /// of the name (subtags are removed from the end one at a time), then a culture of the same language. When <see cref="SupportedCultures"/> is empty, such names are rejected.
    /// Because the set of predefined names is finite and no other culture is ever created, arbitrary names (for example, from a request) do not grow the process-wide culture cache,
    /// in which the runtime keeps the data of every created culture for the lifetime of the process.</para>
    /// <para>On ICU-based runtimes, some legacy region names (for example, zh-TW, zh-CN, sr-RS) are not predefined cultures, so they are matched as strings, that is, by language rather than by script. List the cultures you support explicitly (for example, zh-Hant-TW) if that matters.</para>
    /// <para>An empty name or a name that consists of white space only yields <see langword="null"/>.</para>
    /// </summary>
    /// <param name="cultureName">The name of the requested culture, for example "de-AT".</param>
    /// <returns>The matching supported culture, the predefined culture with the given name when <see cref="SupportedCultures"/> is empty, or <see langword="null"/>
    /// if no supported culture matches (or, when <see cref="SupportedCultures"/> is empty, if no predefined culture has the given name).</returns>
    public CultureInfo? FindSupportedCulture(string cultureName)
    {
        ArgumentNullException.ThrowIfNull(cultureName);

        if (string.IsNullOrWhiteSpace(cultureName))
            return null;

        // TryGetValue returns the name as the runtime lists it, so the cache of CultureInfo.GetCultureInfo gets one entry per predefined culture, whatever the case of the request.
        if (PredefinedCultureNames.Value.TryGetValue(cultureName, out string? predefinedName))
        {
            CultureInfo? predefined = null;
            try
            {
                predefined = CultureInfo.GetCultureInfo(predefinedName);
            }
            catch (CultureNotFoundException)
            {
                // Not expected for a listed name; fall through to the matching by name.
            }

            if (predefined is not null)
                return FindSupportedCulture(predefined);
        }

        if (SupportedCultures.Count == 0)
            return null;

        for (string current = cultureName; current.Length > 0;)
        {
            CultureInfo? supported = FindSupportedCultureByExactName(current);
            if (supported is not null)
                return supported;

            int separator = current.LastIndexOf('-');
            current = separator < 0 ? string.Empty : current[..separator];
        }

        int languageEnd = cultureName.IndexOf('-', StringComparison.Ordinal);
        string language = languageEnd < 0 ? cultureName : cultureName[..languageEnd];
        return language.Length == 0 ? null : FindSupportedCultureByLanguage(language);
    }

    /// <summary>
    /// Picks the culture to start with: the supported culture that serves <paramref name="current"/>, else <see cref="DefaultCulture"/>, else the first supported culture.
    /// </summary>
    /// <param name="current">The culture of the request or of the browser.</param>
    /// <returns>The culture to use.</returns>
    public CultureInfo ResolveInitialCulture(CultureInfo current)
    {
        ArgumentNullException.ThrowIfNull(current);

        CultureInfo? resolved = FindSupportedCulture(current);
        if (resolved is not null)
            return resolved;

        if (DefaultCulture is not null)
            return FindSupportedCulture(DefaultCulture) ?? DefaultCulture;

        return SupportedCultures.Count > 0 ? SupportedCultures[0] : current;
    }

    private CultureInfo? FindSupportedCultureByExactName(string name)
    {
        foreach (CultureInfo supported in SupportedCultures)
        {
            if (supported.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return supported;
        }

        return null;
    }

    private CultureInfo? FindSupportedCultureByLanguage(string language)
    {
        foreach (CultureInfo supported in SupportedCultures)
        {
            if (supported.TwoLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase))
                return supported;
        }

        return null;
    }
}
