// <copyright file="TlumachSyncfusionLocalizer.cs" company="Allied Bits Ltd.">
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
using System.Resources;

using Syncfusion.Blazor;

using Tlumach.Base;
using Tlumach.Blazor;

namespace Tlumach.Syncfusion.Blazor;

/// <summary>
/// An <see cref="ISyncfusionStringLocalizer"/> that takes the built-in texts of Syncfusion Blazor components from Tlumach, in the culture of the user (<see cref="TlumachCultureState.LocalizerCulture"/>).
/// <para>A key is looked up in this order: the translation for the culture of the user (or its parent culture) in <see cref="TranslationManager"/>, the same in <see cref="FallbackTranslationManager"/>,
/// the default file of <see cref="TranslationManager"/>, the default file of <see cref="FallbackTranslationManager"/>, and the English text built into Syncfusion. An empty text counts as missing.</para>
/// <para>The texts are returned as they are; Syncfusion replaces their placeholders, such as <c>{0}</c> or <c>${count}</c>, itself.</para>
/// </summary>
public class TlumachSyncfusionLocalizer : ISyncfusionStringLocalizer
{
    // The default localizer of Syncfusion, which returns the English texts built into Syncfusion.Blazor.Core.
    private readonly SyncfusionStringLocalizer _builtIn = new();
    private readonly TlumachCultureState _cultureState;
    private readonly string? _prefix;
    private readonly string? _fallbackPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachSyncfusionLocalizer"/> class.
    /// </summary>
    /// <param name="manager">The translation manager that holds the texts.</param>
    /// <param name="group">The group, in which the keys are stored in <paramref name="manager"/> (see <see cref="TlumachSyncfusionBlazorOptions.Group"/>), or <see langword="null"/>.</param>
    /// <param name="fallbackManager">An optional second translation manager (see <see cref="TlumachSyncfusionBlazorOptions.FallbackTranslationManager"/>), or <see langword="null"/>.</param>
    /// <param name="fallbackGroup">The group, in which the keys are stored in <paramref name="fallbackManager"/>, or <see langword="null"/>.</param>
    /// <param name="cultureState">The culture state of the user.</param>
    public TlumachSyncfusionLocalizer(TranslationManager manager, string? group, TranslationManager? fallbackManager, string? fallbackGroup, TlumachCultureState cultureState)
    {
        TranslationManager = manager ?? throw new ArgumentNullException(nameof(manager));
        _cultureState = cultureState ?? throw new ArgumentNullException(nameof(cultureState));
        FallbackTranslationManager = fallbackManager;
        Group = string.IsNullOrEmpty(group) ? null : group;
        FallbackGroup = string.IsNullOrEmpty(fallbackGroup) ? null : fallbackGroup;
        _prefix = Group is null ? null : Group + ".";
        _fallbackPrefix = FallbackGroup is null ? null : FallbackGroup + ".";
    }

    /// <summary>
    /// Gets the translation manager that holds the texts.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the group, in which the keys are stored in <see cref="TranslationManager"/>, or <see langword="null"/> when they are stored at the root.
    /// </summary>
    public string? Group { get; }

    /// <summary>
    /// Gets the second translation manager, which is asked for the keys that <see cref="TranslationManager"/> does not have, or <see langword="null"/>.
    /// </summary>
    public TranslationManager? FallbackTranslationManager { get; }

    /// <summary>
    /// Gets the group, in which the keys are stored in <see cref="FallbackTranslationManager"/>, or <see langword="null"/> when they are stored at the root.
    /// </summary>
    public string? FallbackGroup { get; }

    /// <summary>
    /// Gets <see langword="null"/>: the texts come from Tlumach, and Syncfusion components do not read this property.
    /// </summary>
    public ResourceManager? ResourceManager => null;

    /// <summary>
    /// Gets the text with the given key in the culture of the user.
    /// </summary>
    /// <param name="key">The key used by Syncfusion, such as <c>Grid_EmptyRecord</c>.</param>
    /// <returns>The text, or the English text built into Syncfusion when Tlumach does not have it, or <see langword="null"/> when Syncfusion does not know the key either.</returns>
    public string? GetText(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        CultureInfo culture = _cultureState.LocalizerCulture;

        TranslationEntry primary = TranslationManager.GetValueWithSource(_prefix is null ? key : _prefix + key, culture, out TranslationEntrySource primarySource);
        if (IsForCulture(primarySource) && HasText(primary))
            return primary.Text;

        TranslationEntry? fallback = null;
        TranslationEntrySource fallbackSource = TranslationEntrySource.NotFound;
        if (FallbackTranslationManager is not null)
        {
            fallback = FallbackTranslationManager.GetValueWithSource(_fallbackPrefix is null ? key : _fallbackPrefix + key, culture, out fallbackSource);
            if (IsForCulture(fallbackSource) && HasText(fallback))
                return fallback.Text;
        }

        // The default files, typically English, come before the English built into Syncfusion, so that an application can change the English texts.
        if (primarySource != TranslationEntrySource.NotFound && HasText(primary))
            return primary.Text;

        if (fallback is not null && fallbackSource != TranslationEntrySource.NotFound && HasText(fallback))
            return fallback.Text;

        return _builtIn.GetText(key);
    }

    private static bool IsForCulture(TranslationEntrySource source) => source is TranslationEntrySource.Culture or TranslationEntrySource.BasicCulture;

    private static bool HasText(TranslationEntry entry) => !string.IsNullOrEmpty(entry.Text);
}
