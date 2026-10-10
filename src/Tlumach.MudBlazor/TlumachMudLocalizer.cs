// <copyright file="TlumachMudLocalizer.cs" company="Allied Bits Ltd.">
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

using Microsoft.Extensions.Localization;

using MudBlazor;

using Tlumach.Base;
using Tlumach.Blazor;

namespace Tlumach.MudBlazor;

/// <summary>
/// A <see cref="MudLocalizer"/> that takes the built-in texts of MudBlazor from Tlumach, in the culture of the user (<see cref="TlumachCultureState.LocalizerCulture"/>).
/// <para>When a key is not found, the result has <see cref="LocalizedString.ResourceNotFound"/> set, and MudBlazor shows its built-in English text.</para>
/// <para>The texts of MudBlazor are .NET composite format strings, such as <c>"{0}-{1} of {2}"</c>, so they are formatted with <see cref="string.Format(IFormatProvider, string, object[])"/>
/// whatever text processing mode the translation set uses.</para>
/// </summary>
public class TlumachMudLocalizer : MudLocalizer
{
    private readonly TlumachCultureState _cultureState;
    private readonly string? _prefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachMudLocalizer"/> class.
    /// </summary>
    /// <param name="manager">The translation manager that holds the texts.</param>
    /// <param name="group">The group of the translation, in which the keys are stored (see <see cref="TlumachMudBlazorOptions.Group"/>), or <see langword="null"/>.</param>
    /// <param name="cultureState">The culture state of the user.</param>
    public TlumachMudLocalizer(TranslationManager manager, string? group, TlumachCultureState cultureState)
    {
        TranslationManager = manager ?? throw new ArgumentNullException(nameof(manager));
        _cultureState = cultureState ?? throw new ArgumentNullException(nameof(cultureState));
        Group = string.IsNullOrEmpty(group) ? null : group;
        _prefix = Group is null ? null : Group + ".";
    }

    /// <summary>
    /// Gets the translation manager that holds the texts.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the group of the translation, in which the keys are stored, or <see langword="null"/> when the keys are stored at the root.
    /// </summary>
    public string? Group { get; }

    /// <summary>
    /// Gets the text with the given key in the culture of the user.
    /// </summary>
    /// <param name="key">The key used by MudBlazor, such as <c>MudDataGrid_Contains</c>.</param>
    /// <returns>The text, or the key with <see cref="LocalizedString.ResourceNotFound"/> set when the key is not found.</returns>
    public override LocalizedString this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            string? text = Find(key, _cultureState.LocalizerCulture);
            return text is null ? NotFound(key) : new LocalizedString(key, text, resourceNotFound: false);
        }
    }

    /// <summary>
    /// Gets the text with the given key in the culture of the user, with the format items replaced by <paramref name="arguments"/>.
    /// </summary>
    /// <param name="key">The key used by MudBlazor, such as <c>MudDataGridPager_InfoFormat</c>.</param>
    /// <param name="arguments">The values of the format items.</param>
    /// <returns>The text, or the key with <see cref="LocalizedString.ResourceNotFound"/> set when the key is not found or the translation is not a valid format string for the arguments.</returns>
    public override LocalizedString this[string key, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            if (arguments is null || arguments.Length == 0)
                return this[key];

            CultureInfo culture = _cultureState.LocalizerCulture;
            string? text = Find(key, culture);
            if (text is null)
                return NotFound(key);

            try
            {
                return new LocalizedString(key, string.Format(culture, text, arguments), resourceNotFound: false);
            }
            catch (FormatException)
            {
                // A broken translation must not break the component; MudBlazor shows its English text instead.
                return NotFound(key);
            }
        }
    }

    private static LocalizedString NotFound(string key) => new(key, key, resourceNotFound: true);

    // An empty text counts as missing, as in Tlumach.Syncfusion.Blazor, so that MudBlazor shows its English text instead of an empty label. An empty text of the user's culture
    // does not hide the text of the default file either: the normal lookup returns the entry of the default file itself only when the culture has no entry for the key,
    // so in that case the default file is asked for separately, as the default locale.
    private string? Find(string key, CultureInfo culture)
    {
        string fullKey = _prefix is null ? key : _prefix + key;

        TranslationEntry entry = TranslationManager.GetValueWithSource(fullKey, culture, out TranslationEntrySource source);
        if (HasText(entry))
            return entry.Text;

        if (source is not (TranslationEntrySource.Culture or TranslationEntrySource.BasicCulture))
            return null;

        // The default locale is unknown when the configuration does not specify it and no default text has been read yet; the default file cannot be addressed then.
        string? defaultLocale = TranslationManager.DefaultConfiguration?.DefaultFileLocale;
        if (string.IsNullOrEmpty(defaultLocale))
            return null;

        CultureInfo defaultCulture;
        try
        {
            defaultCulture = CultureInfo.GetCultureInfo(defaultLocale);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }

        TranslationEntry defaultEntry = TranslationManager.GetValueWithSource(fullKey, defaultCulture, out TranslationEntrySource defaultSource);
        return defaultSource != TranslationEntrySource.NotFound && HasText(defaultEntry) ? defaultEntry.Text : null;
    }

    private static bool HasText(TranslationEntry entry) => !string.IsNullOrEmpty(entry.Text);
}
