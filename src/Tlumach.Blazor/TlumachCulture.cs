// <copyright file="TlumachCulture.cs" company="Allied Bits Ltd.">
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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Components;

using Tlumach.Base;

namespace Tlumach.Blazor;

/// <summary>
/// An immutable snapshot of the culture of one user, cascaded to components by <see cref="TlumachCultureState"/>.
/// <para>A component that declares a <see cref="CascadingParameterAttribute">cascading parameter</see> of this type is re-rendered when the user switches the language.
/// The methods of this class retrieve texts for <see cref="Culture"/> explicitly, which is what keeps the users of a Blazor Server application apart.</para>
/// </summary>
public sealed class TlumachCulture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachCulture"/> class.
    /// </summary>
    /// <param name="culture">The culture of the user.</param>
    public TlumachCulture(CultureInfo culture)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    /// <summary>
    /// Gets the culture of the user.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> as plain (not HTML-encoded) text, suitable for attributes and code.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The text.</returns>
    public string Get(BaseTranslationUnit unit) => ToPlainText(unit, GetRaw(unit, args: null, values: null));

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with indexed placeholders replaced by <paramref name="values"/>.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="values">The values of the placeholders, in the order of their indexes.</param>
    /// <returns>The text.</returns>
    public string Get(BaseTranslationUnit unit, params object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return ToPlainText(unit, GetRaw(unit, args: null, values));
    }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with named placeholders replaced by the values in <paramref name="args"/>.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the placeholders, keyed by placeholder names.</param>
    /// <returns>The text.</returns>
    public string Get(BaseTranslationUnit unit, IDictionary<string, object?> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return ToPlainText(unit, GetRaw(unit, args, values: null));
    }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with named placeholders replaced by the values of the public properties of <paramref name="args"/>,
    /// e.g., an anonymous object. The trimmer keeps those properties, so this method is safe in trimmed applications.
    /// </summary>
    /// <typeparam name="TArgs">The type that supplies the values through its public properties.</typeparam>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The object that supplies the values.</param>
    /// <returns>The text.</returns>
    public string GetFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(BaseTranslationUnit unit, TArgs args)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return ToPlainText(unit, unit.GetValueFrom(Culture, args));
    }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> as markup. Use it only for translations that contain trusted HTML.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The markup.</returns>
    public MarkupString Markup(BaseTranslationUnit unit) => new(GetRaw(unit, args: null, values: null));

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with named placeholders replaced, as markup. Use it only for translations that contain trusted HTML.
    /// <para>The translation text is trusted, while the values are data: <see cref="string"/> values are HTML-encoded before they are substituted.
    /// To insert trusted HTML through a value, pass a <see cref="MarkupString"/>. Other values (numbers, dates, ...) are formatted as usual.
    /// When <see cref="TranslationManager.WebEncodeValues"/> is set, the unit encodes the whole text, values included, and the values are not encoded again.</para>
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the placeholders, keyed by placeholder names.</param>
    /// <returns>The markup.</returns>
    public MarkupString Markup(BaseTranslationUnit unit, IDictionary<string, object?> args)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(args);
        return new(GetRaw(unit, unit.TranslationManager.WebEncodeValues ? args : MarkupPlaceholderValues.Encode(args), values: null));
    }

    /// <summary>
    /// Returns the text as the unit produces it, i.e. HTML-encoded when <see cref="TranslationManager.WebEncodeValues"/> is set.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the named placeholders, or <see langword="null"/>.</param>
    /// <param name="values">The values of the indexed placeholders, or <see langword="null"/>.</param>
    /// <returns>The text.</returns>
#pragma warning disable SA1011 // Closing square bracket must be followed by space - false positive for the nullable array type "object[]?"
    internal string GetRaw(BaseTranslationUnit unit, IDictionary<string, object?>? args, object[]? values)
#pragma warning restore SA1011
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (args is not null)
            return unit.GetValue(Culture, args);

        if (values is not null)
            return unit.GetValue(Culture, values);

        return unit.GetValue(Culture);
    }

    /// <summary>
    /// Returns the text of the entry with the given key, processing placeholders when values are supplied. A missing key yields an empty string.
    /// </summary>
    /// <param name="manager">The translation manager that holds the entry.</param>
    /// <param name="key">The key of the entry.</param>
    /// <param name="args">The values of the named placeholders, or <see langword="null"/>.</param>
    /// <param name="values">The values of the indexed placeholders, or <see langword="null"/>.</param>
    /// <returns>The text.</returns>
#pragma warning disable SA1011 // Closing square bracket must be followed by space - false positive for the nullable array type "object[]?"
    internal string GetByKey(TranslationManager manager, string key, IDictionary<string, object?>? args, object[]? values)
#pragma warning restore SA1011
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);

        TranslationEntry entry = manager.GetValue(key, Culture);
        string? text = entry.Text;
        if (text is null)
            return string.Empty;

        if (!entry.ContainsPlaceholders || (args is null && values is null))
            return text;

        TextFormat mode = manager.DefaultConfiguration?.TextProcessingMode ?? TextFormat.None;
        return args is not null
            ? entry.ProcessTemplatedValue(Culture, mode, args)
            : entry.ProcessTemplatedValue(Culture, mode, values!);
    }

    private static string ToPlainText(BaseTranslationUnit unit, string value)
        => unit.TranslationManager.WebEncodeValues ? WebUtility.HtmlDecode(value) : value;
}
