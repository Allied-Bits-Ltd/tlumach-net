// <copyright file="TemplateTranslator.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach.Templating;

/// <summary>
/// Retrieves translations for template engines. It resolves the culture of a call, looks up a text by key or by translation unit, fills the placeholders of the text with
/// the values of the call, and handles missing keys.
/// <para>Texts are read from translation entries without the encoding of <see cref="TranslationManager.WebEncodeValues"/>, so that a template engine encodes them exactly once.
/// An instance does not change after it is created and may be used by many renders concurrently.</para>
/// </summary>
public sealed class TemplateTranslator
{
    private readonly TranslationConfiguration? _configuration;
    private readonly string _keyPrefix;
    private readonly MissingKeyBehavior _missingKey;
    private readonly Func<string, CultureInfo, string?>? _onMissingKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateTranslator"/> class.
    /// </summary>
    /// <param name="manager">The translation manager to take the texts from.</param>
    /// <param name="options">The options. Their values are copied.</param>
    public TemplateTranslator(TranslationManager manager, TemplateTranslationOptions? options = null)
    {
        if (manager is null)
            throw new ArgumentNullException(nameof(manager));

        options ??= new TemplateTranslationOptions();
        Manager = manager;
        _configuration = options.Configuration ?? manager.DefaultConfiguration;
        _keyPrefix = options.KeyPrefix ?? string.Empty;
        _missingKey = options.MissingKey;
        _onMissingKey = options.OnMissingKey;
        CultureArgumentName = string.IsNullOrEmpty(options.CultureArgumentName) ? TemplateTranslationOptions.DefaultCultureArgumentName : options.CultureArgumentName;
    }

    /// <summary>
    /// Gets the translation manager, from which the texts are taken.
    /// </summary>
    public TranslationManager Manager { get; }

    /// <summary>
    /// Gets the name of the named argument that sets the culture of a call.
    /// </summary>
    public string CultureArgumentName { get; }

    /// <summary>
    /// Converts a culture given by a template to a <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="value">A <see cref="CultureInfo"/>, a culture name, or <see langword="null"/>.</param>
    /// <returns>The culture, or <see langword="null"/> for <see langword="null"/> and an empty name.</returns>
    /// <exception cref="CultureNotFoundException">The name is not a valid culture name.</exception>
    /// <exception cref="ArgumentException">The value is neither a <see cref="CultureInfo"/> nor a string.</exception>
    public static CultureInfo? ToCulture(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case CultureInfo culture:
                return culture;
            case string name:
                return name.Length == 0 ? null : CultureInfo.GetCultureInfo(name);
            default:
                throw new ArgumentException($"A culture must be given as a CultureInfo or a culture name, not as a value of the type '{value.GetType().FullName}'.", nameof(value));
        }
    }

    /// <summary>
    /// Resolves the culture of a call: the explicit culture of the call if given, otherwise the culture of the render unless it is <see langword="null"/> or invariant,
    /// otherwise <see cref="TranslationManager.CurrentCulture"/>.
    /// </summary>
    /// <param name="explicitCulture">The culture argument of the call: a <see cref="CultureInfo"/>, a culture name, or <see langword="null"/>.</param>
    /// <param name="renderCulture">The culture of the render as the template engine knows it, or <see langword="null"/>.</param>
    /// <returns>The culture of the call.</returns>
    public CultureInfo ResolveCulture(object? explicitCulture, CultureInfo? renderCulture)
    {
        CultureInfo? culture = ToCulture(explicitCulture);
        if (culture is not null)
            return culture;

        if (renderCulture is not null && renderCulture.Name.Length != 0)
            return renderCulture;

        return Manager.CurrentCulture;
    }

    /// <summary>
    /// Returns the text for the key or the translation unit, with the placeholders filled, not encoded.
    /// </summary>
    /// <param name="keyOrUnit">A key (<see cref="string"/>) or a <see cref="BaseTranslationUnit"/>.</param>
    /// <param name="arguments">The values of the placeholders.</param>
    /// <param name="culture">The culture of the call, usually obtained from <see cref="ResolveCulture"/>.</param>
    /// <returns>The text.</returns>
    public string Translate(object? keyOrUnit, TemplateArguments arguments, CultureInfo culture)
        => Resolve(keyOrUnit, arguments, culture, encode: null);

    /// <summary>
    /// Returns the text for the key or the translation unit as HTML: the translation is trusted HTML and is not encoded, while the values of the placeholders are encoded with
    /// <paramref name="encode"/>. Strings and other objects (converted to strings) are encoded, <see cref="TemplateMarkup"/> values are inserted as they are,
    /// and numbers, dates and other <see cref="IFormattable"/> values are formatted for the culture and not encoded.
    /// </summary>
    /// <param name="keyOrUnit">A key (<see cref="string"/>) or a <see cref="BaseTranslationUnit"/>.</param>
    /// <param name="arguments">The values of the placeholders.</param>
    /// <param name="culture">The culture of the call, usually obtained from <see cref="ResolveCulture"/>.</param>
    /// <param name="encode">The function that HTML-encodes a value.</param>
    /// <returns>The HTML.</returns>
    public string TranslateMarkup(object? keyOrUnit, TemplateArguments arguments, CultureInfo culture, Func<string, string> encode)
    {
        if (encode is null)
            throw new ArgumentNullException(nameof(encode));

        return Resolve(keyOrUnit, arguments, culture, encode);
    }

    private static bool IsMissing(TranslationEntry? entry)
        => entry is null || ReferenceEquals(entry, TranslationEntry.Empty) || (entry.Text is null && entry.EscapedText is null);

    private static object? ResolveValue(string name, int index, TemplateArguments arguments, BaseTranslationUnit? unit, Func<string, string>? encode)
    {
        if (name.Length != 0 && arguments.Named.TryGetValue(name, out object? value))
            return Prepare(value, encode);

        if (index >= 0 && index < arguments.Positional.Count)
            return Prepare(arguments.Positional[index], encode);

        if (unit is not null)
        {
            // The values cached in the unit or supplied by its OnPlaceholderValueNeeded event, as GetValue() of the unit would use them.
            value = unit.ResolvePlaceholderValue(name, index);
            if (value is not null)
                return Prepare(value, encode);
        }

        // No value: Tlumach leaves the placeholder unresolved.
        return null;
    }

    private static object Prepare(object? value, Func<string, string>? encode)
    {
        switch (value)
        {
            case null:
                // Template engines render nil as nothing; the "null" text of the other overloads would end up in emails.
                return string.Empty;
            case TemplateMarkup markup:
                return markup.Value;
            case string text:
                return encode is null ? text : encode(text);
            case IFormattable:
                return value;
            default:
                return encode is null ? value : encode(value.ToString() ?? string.Empty);
        }
    }

    private string Resolve(object? keyOrUnit, TemplateArguments arguments, CultureInfo culture, Func<string, string>? encode)
    {
        if (arguments is null)
            throw new ArgumentNullException(nameof(arguments));
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));

        TranslationEntry? entry;
        TextFormat mode;
        string key;
        BaseTranslationUnit? unit = null;
        switch (keyOrUnit)
        {
            case string text:
                key = _keyPrefix + text;
                mode = _configuration?.TextProcessingMode ?? TextFormat.None;
                entry = _configuration is null ? null : Manager.GetValue(_configuration, key, culture);
                break;
            case BaseTranslationUnit translationUnit:
                unit = translationUnit;
                key = translationUnit.Key;
                entry = translationUnit.GetEntryForTemplate(culture, out mode);
                break;
            default:
                throw new ArgumentException(
                    keyOrUnit is null
                        ? "A key or a translation unit is required, but null was given."
                        : $"A key must be a string or a translation unit, not a value of the type '{keyOrUnit.GetType().FullName}'.",
                    nameof(keyOrUnit));
        }

        if (IsMissing(entry))
            return HandleMissing(key, culture, encode);

        // A unit decides by its own flag, as its GetValue() does (an UntranslatedUnit creates its entry without parsing it).
        bool containsPlaceholders = unit?.ContainsPlaceholders ?? entry!.ContainsPlaceholders;
        if (!containsPlaceholders)
            return entry!.Text ?? string.Empty;

        return entry!.ProcessTemplatedValue(culture, mode, (name, index) => ResolveValue(name, index, arguments, unit, encode));
    }

    private string HandleMissing(string key, CultureInfo culture, Func<string, string>? encode)
    {
        string? text = _onMissingKey?.Invoke(key, culture);
        if (text is null)
        {
            switch (_missingKey)
            {
                case MissingKeyBehavior.Empty:
                    return string.Empty;
                case MissingKeyBehavior.Throw:
                    throw new TemplateKeyNotFoundException(key, culture);
                default:
                    text = key;
                    break;
            }
        }

        return encode is null ? text : encode(text);
    }
}
