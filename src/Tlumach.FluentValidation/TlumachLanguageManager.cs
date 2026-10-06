// <copyright file="TlumachLanguageManager.cs" company="Allied Bits Ltd.">
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

using FluentValidation.Resources;

using Tlumach.Base;

namespace Tlumach.FluentValidation;

/// <summary>
/// The language manager of FluentValidation that takes the messages from Tlumach translations and falls back to the messages built into FluentValidation.
/// <para>Assign an instance to <c>ValidatorOptions.Global.LanguageManager</c>, or call <c>AddTlumachFluentValidation</c>. The setting is process-wide, as FluentValidation
/// offers no other way to supply a language manager.</para>
/// </summary>
/// <remarks>
/// <para>A message is chosen in this order:</para>
/// <list type="number">
/// <item>The Tlumach text for the culture: text found for the culture or its basic culture, or the text of the default file when that file is written in the language of the culture.</item>
/// <item>The message built into FluentValidation for the culture.</item>
/// <item>The text of the default file of Tlumach.</item>
/// <item>The English message of FluentValidation.</item>
/// </list>
/// <para>The text is returned as it appears in the translation. The placeholder engine of Tlumach is not involved, so the placeholders of FluentValidation, such as
/// <c>{PropertyName}</c>, reach the message formatter of FluentValidation intact.</para>
/// <para>The class holds no mutable state of its own apart from the inherited <c>Culture</c> and <c>Enabled</c> properties, and it can be used from many threads at once.</para>
/// </remarks>
public class TlumachLanguageManager : LanguageManager
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachLanguageManager"/> class.
    /// </summary>
    /// <param name="translationManager">The translation manager that provides the messages, typically <c>Strings.TranslationManager</c> of a class created by Generator.</param>
    /// <param name="options">The options. The values are copied, so a later change of the options object has no effect. When <see langword="null"/>, the defaults apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="translationManager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="TlumachLanguageManagerOptions.CultureSource"/> is not a defined value.</exception>
    public TlumachLanguageManager(TranslationManager translationManager, TlumachLanguageManagerOptions? options = null)
    {
        TranslationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));

        options ??= new TlumachLanguageManagerOptions();

        if (options.CultureSource != MessageCultureSource.CurrentUICulture && options.CultureSource != MessageCultureSource.TranslationManager)
            throw new ArgumentOutOfRangeException(nameof(options), options.CultureSource, "The culture source must be either CurrentUICulture or TranslationManager.");

        FluentValidationGroup = options.FluentValidationGroup;
        CultureSource = options.CultureSource;
    }

    /// <summary>
    /// Gets the translation manager that provides the messages.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the group, in which the keys of FluentValidation are looked up. See <see cref="TlumachLanguageManagerOptions.FluentValidationGroup"/>.
    /// </summary>
    public string? FluentValidationGroup { get; }

    /// <summary>
    /// Gets the culture source used when neither the caller nor <c>Culture</c> specifies a culture.
    /// </summary>
    public MessageCultureSource CultureSource { get; }

    /// <summary>
    /// Returns the culture, for which a message is read: <paramref name="culture"/> when it is given, otherwise <c>Culture</c> when it is set, otherwise the culture that
    /// <see cref="CultureSource"/> selects.
    /// </summary>
    /// <param name="culture">An optional explicit culture.</param>
    /// <returns>The effective culture.</returns>
    public CultureInfo ResolveCulture(CultureInfo? culture = null)
    {
        // Culture is read once, as another thread may change it between a check and a second read.
        return culture ?? Culture ?? (CultureSource == MessageCultureSource.TranslationManager ? TranslationManager.CurrentCulture : CultureInfo.CurrentUICulture);
    }

    /// <summary>
    /// Returns the message template for the key, chosen as described in the remarks of the class.
    /// </summary>
    /// <param name="key">The key: the name of a validator, such as <c>NotEmptyValidator</c>, or an error code set with <c>WithErrorCode</c>.</param>
    /// <param name="culture">An optional culture. See <see cref="ResolveCulture(CultureInfo?)"/>.</param>
    /// <returns>The template, or an empty string when neither Tlumach nor FluentValidation knows the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <see langword="null"/>.</exception>
    public override string GetString(string key, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        string lookupKey = string.IsNullOrEmpty(FluentValidationGroup) ? key : FluentValidationGroup + "." + key;

        if (!Enabled)
            return GetStringWhenDisabled(key, lookupKey);

        CultureInfo effectiveCulture = ResolveCulture(culture);

        TranslationEntry entry = TranslationManager.GetValueWithSource(lookupKey, effectiveCulture, out TranslationEntrySource source);
        string? tlumachText = entry.Text;
        bool hasTlumachText = !string.IsNullOrEmpty(tlumachText);

        // 1. The Tlumach text for the culture.
        if (hasTlumachText
            && (source is TranslationEntrySource.Culture or TranslationEntrySource.BasicCulture
                || (source == TranslationEntrySource.DefaultTranslation && CultureMatching.DefaultFileSharesLanguage(TranslationManager, effectiveCulture))))
        {
            return tlumachText!;
        }

        // 2. The built-in message for the culture. FluentValidation does not tell whether it has a translation for a culture, and it falls back to English silently, so a
        //    result that equals the English message is treated as missing unless the culture itself is English.
        string builtIn = base.GetString(key, effectiveCulture);
        if (builtIn.Length != 0
            && (CultureMatching.IsEnglish(effectiveCulture) || !string.Equals(builtIn, base.GetString(key, English), StringComparison.Ordinal)))
        {
            return builtIn;
        }

        // 3. The text of the default file of Tlumach, or a text supplied by an OnTranslationValueNotFound handler.
        if (hasTlumachText)
            return tlumachText!;

        // 4. The English message of FluentValidation.
        return base.GetString(key, English);
    }

    private string GetStringWhenDisabled(string key, string lookupKey)
    {
        CultureInfo defaultCulture = CultureMatching.DefaultFileCulture(TranslationManager) ?? CultureInfo.InvariantCulture;
        string? text = TranslationManager.GetValueWithSource(lookupKey, defaultCulture, out _).Text;

        // With Enabled set to false, the base class returns the English message regardless of the culture.
        return string.IsNullOrEmpty(text) ? base.GetString(key) : text;
    }
}
