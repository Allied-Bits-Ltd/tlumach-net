// <copyright file="TranslationLookup.cs" company="Allied Bits Ltd.">
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
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Looks up keys of one translation manager, first with each of the prefixes in order (the key of a view: "Views.Home.Index.") and then without a prefix (the shared key).
/// Immutable; shared by all localizers and tag helpers of a view.
/// </summary>
internal sealed class TranslationLookup
{
    private static readonly string[] NoPrefixes = [];

    private readonly ManagerEntry _entry;
    private readonly string[] _prefixes;
    private readonly HtmlEncoder _encoder;
    private readonly Func<string, string> _encode;

    internal TranslationLookup(ManagerEntry entry, string? prefix, HtmlEncoder encoder)
        : this(entry, string.IsNullOrEmpty(prefix) ? NoPrefixes : [prefix], encoder)
    {
    }

    internal TranslationLookup(ManagerEntry entry, string[] prefixes, HtmlEncoder encoder)
    {
        _entry = entry;
        _prefixes = prefixes;
        _encoder = encoder;
        _encode = encoder.Encode;
    }

    internal LocalizedHtmlString GetHtml(string key, object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(key);

        bool found = TryRender(key, HtmlTranslation.ToArguments(arguments, _encoder), CultureInfo.CurrentCulture, out string html);
        return HtmlTranslation.ToLocalizedHtmlString(key, found ? html : _encode(key), notFound: !found);
    }

    internal bool TryRender(string key, TemplateArguments arguments, CultureInfo culture, out string html)
    {
        foreach (string prefix in _prefixes)
        {
            if (_entry.Translator.TryTranslateMarkup(prefix + key, arguments, culture, _encode, out html))
                return true;
        }

        return _entry.Translator.TryTranslateMarkup(key, arguments, culture, _encode, out html);
    }

    internal string RenderOrKey(string key, TemplateArguments arguments, CultureInfo culture)
        => TryRender(key, arguments, culture, out string html) ? html : _encode(key);

    internal LocalizedString GetString(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (string prefix in _prefixes)
        {
            LocalizedString prefixed = _entry.Strings[prefix + key];
            if (!prefixed.ResourceNotFound)
                return new LocalizedString(key, prefixed.Value, resourceNotFound: false, prefixed.SearchedLocation);
        }

        return _entry.Strings[key];
    }

    internal LocalizedString GetString(string key, object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(key);

        // MVC can pass a null array for a call such as L.GetString("Key", null).
        arguments ??= Array.Empty<object>();

        foreach (string prefix in _prefixes)
        {
            LocalizedString prefixed = _entry.Strings[prefix + key, arguments];
            if (!prefixed.ResourceNotFound)
                return new LocalizedString(key, prefixed.Value, resourceNotFound: false, prefixed.SearchedLocation);
        }

        return _entry.Strings[key, arguments];
    }

    internal IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _entry.Strings.GetAllStrings(includeParentCultures);
}
