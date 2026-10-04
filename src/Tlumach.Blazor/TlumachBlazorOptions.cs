// <copyright file="TlumachBlazorOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Blazor;

/// <summary>
/// The options of the Blazor integration of Tlumach, configured in <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/>.
/// </summary>
public sealed class TlumachBlazorOptions
{
    /// <summary>
    /// The default value of <see cref="CultureEndpoint"/>.
    /// </summary>
    public const string DefaultCultureEndpoint = "/tlumach/culture";

    /// <summary>
    /// The default value of <see cref="LocalStorageKey"/>.
    /// </summary>
    public const string DefaultLocalStorageKey = "tlumach.culture";

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
    /// Gets or sets the translation manager, from which <see cref="TlumachText"/> takes texts by key when its <c>Manager</c> parameter is not set.
    /// </summary>
    public TranslationManager? DefaultManager { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a culture switch also changes the process-wide culture: <see cref="CultureInfo.DefaultThreadCurrentCulture"/>,
    /// <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>, and <see cref="TranslationManager.CurrentCulture"/> of every translation manager.
    /// <para>This is right only when one user owns the process: Blazor WebAssembly and Blazor Hybrid. When <see langword="null"/>, the value is
    /// <see langword="true"/> in the browser and <see langword="false"/> elsewhere, so Blazor Hybrid applications set it to <see langword="true"/> explicitly.</para>
    /// </summary>
    public bool? ApplyCultureGlobally { get; set; }

    /// <summary>
    /// Gets or sets where the chosen culture is stored. When <see langword="null"/>, it is <see cref="TlumachCulturePersistence.LocalStorage"/> in the browser and
    /// <see cref="TlumachCulturePersistence.Cookie"/> elsewhere. A WebAssembly client of a Blazor Web App and a Blazor Hybrid application set it explicitly.
    /// </summary>
    public TlumachCulturePersistence? Persistence { get; set; }

    /// <summary>
    /// Gets or sets the path of the endpoint that stores the culture in a cookie, relative to the base path of the application.
    /// </summary>
    public string CultureEndpoint { get; set; } = DefaultCultureEndpoint;

    /// <summary>
    /// Gets or sets the key, under which the culture is kept in the local storage of the browser.
    /// </summary>
    public string LocalStorageKey { get; set; } = DefaultLocalStorageKey;

    internal bool EffectiveApplyCultureGlobally => ApplyCultureGlobally ?? OperatingSystem.IsBrowser();

    internal TlumachCulturePersistence EffectivePersistence
        => Persistence ?? (OperatingSystem.IsBrowser() ? TlumachCulturePersistence.LocalStorage : TlumachCulturePersistence.Cookie);

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
    /// Finds the supported culture that serves the culture with the given name: the culture itself, then the closest culture whose name is a prefix of the name
    /// ("de-AT" is served by "de", "zh-Hant-TW" by "zh-Hant", then by "zh"), then a culture of the same language.
    /// <para>The name is matched with string operations only, and no culture is created for it while <see cref="SupportedCultures"/> is not empty,
    /// so arbitrary names (for example, from a request) do not grow the process-wide culture cache.</para>
    /// <para>When <see cref="SupportedCultures"/> is empty, only the name of a predefined culture (one listed by <see cref="CultureInfo.GetCultures(CultureTypes)"/>
    /// with <see cref="CultureTypes.AllCultures"/>, compared ignoring case) is accepted. Other well-formed names, which ICU would accept as well, are rejected,
    /// because the runtime caches the data of every created culture by name for the lifetime of the process.</para>
    /// </summary>
    /// <param name="cultureName">The name of the requested culture, for example "de-AT".</param>
    /// <returns>The matching supported culture, the predefined culture with the given name when <see cref="SupportedCultures"/> is empty, or <see langword="null"/>
    /// if no supported culture matches (or, when <see cref="SupportedCultures"/> is empty, if no predefined culture has the given name).</returns>
    public CultureInfo? FindSupportedCulture(string cultureName)
    {
        ArgumentNullException.ThrowIfNull(cultureName);

        if (SupportedCultures.Count == 0)
        {
            if (!PredefinedCultureNames.Value.Contains(cultureName))
                return null;

            try
            {
                // The set of predefined names is finite, so the cache of CultureInfo.GetCultureInfo stays bounded.
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

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
    internal CultureInfo ResolveInitialCulture(CultureInfo current)
    {
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
