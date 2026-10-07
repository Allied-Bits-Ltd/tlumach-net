// <copyright file="TlumachHtmlLocalizer.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// An <see cref="IHtmlLocalizer"/> backed by a Tlumach translation manager.
/// <para>The translation is trusted HTML and is not encoded; the values of its placeholders are HTML-encoded, except <see cref="Microsoft.AspNetCore.Html.IHtmlContent"/> values,
/// which are inserted as they are. All Tlumach placeholders are supported: <c>{0}</c>, <c>{name}</c>, ICU <c>plural</c> and <c>select</c>, dates, and numbers.
/// A single <see cref="IReadOnlyDictionary{TKey, TValue}"/> argument supplies named values. <see cref="Tlumach.TranslationManager.WebEncodeValues"/> does not affect the HTML.</para>
/// <para>The braces of the rendered HTML are doubled in <see cref="LocalizedHtmlString.Value"/>, which MVC treats as a format string; <see cref="LocalizedHtmlString.WriteTo"/> writes the HTML exactly.</para>
/// </summary>
public sealed class TlumachHtmlLocalizer : IHtmlLocalizer
{
    private readonly TranslationLookup _lookup;

    internal TlumachHtmlLocalizer(TranslationLookup lookup)
    {
        _lookup = lookup;
    }

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name] => _lookup.GetHtml(name, Array.Empty<object?>());

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name, params object[] arguments] => _lookup.GetHtml(name, arguments);

    /// <inheritdoc/>
    public LocalizedString GetString(string name) => _lookup.GetString(name);

    /// <inheritdoc/>
    public LocalizedString GetString(string name, params object[] arguments) => _lookup.GetString(name, arguments);

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _lookup.GetAllStrings(includeParentCultures);
}

/// <summary>
/// An <see cref="IHtmlLocalizer{TResource}"/> backed by Tlumach; see <see cref="TlumachHtmlLocalizer"/>.
/// </summary>
/// <typeparam name="TResource">The type, whose full name is the context of the options, or a class created by Tlumach Generator.</typeparam>
public sealed class TlumachHtmlLocalizer<TResource> : IHtmlLocalizer<TResource>
{
    private readonly IHtmlLocalizer _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachHtmlLocalizer{TResource}"/> class.
    /// </summary>
    /// <param name="factory">The factory that creates the localizer for <typeparamref name="TResource"/>.</param>
    public TlumachHtmlLocalizer(IHtmlLocalizerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _inner = factory.Create(typeof(TResource));
    }

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name] => _inner[name];

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name, params object[] arguments] => _inner[name, arguments];

    /// <inheritdoc/>
    public LocalizedString GetString(string name) => _inner.GetString(name);

    /// <inheritdoc/>
    public LocalizedString GetString(string name, params object[] arguments) => _inner.GetString(name, arguments);

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _inner.GetAllStrings(includeParentCultures);
}
