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
/// Looks up keys of one translation manager, first with a prefix (the key of a view: "Views.Home.Index.") and then without it (the shared key).
/// Immutable; shared by all localizers and tag helpers of a view.
/// </summary>
internal sealed class TranslationLookup
{
    private readonly ManagerEntry _entry;
    private readonly HtmlEncoder _encoder;
    private readonly Func<string, string> _encode;

    internal TranslationLookup(ManagerEntry entry, string? prefix, HtmlEncoder encoder)
    {
        _entry = entry;
        _encoder = encoder;
        _encode = encoder.Encode;
        Prefix = prefix ?? string.Empty;
    }

    internal string Prefix { get; }

    internal LocalizedHtmlString GetHtml(string key, object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(key);

        bool found = TryRender(key, HtmlTranslation.ToArguments(arguments, _encoder), CultureInfo.CurrentCulture, out string html);
        return HtmlTranslation.ToLocalizedHtmlString(key, found ? html : _encode(key), notFound: !found);
    }

    internal bool TryRender(string key, TemplateArguments arguments, CultureInfo culture, out string html)
    {
        if (Prefix.Length != 0 && _entry.Translator.TryTranslateMarkup(Prefix + key, arguments, culture, _encode, out html))
            return true;

        return _entry.Translator.TryTranslateMarkup(key, arguments, culture, _encode, out html);
    }

    internal string RenderOrKey(string key, TemplateArguments arguments, CultureInfo culture)
        => TryRender(key, arguments, culture, out string html) ? html : _encode(key);

    internal LocalizedString GetString(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (Prefix.Length != 0)
        {
            LocalizedString prefixed = _entry.Strings[Prefix + key];
            if (!prefixed.ResourceNotFound)
                return new LocalizedString(key, prefixed.Value, resourceNotFound: false, prefixed.SearchedLocation);
        }

        return _entry.Strings[key];
    }

    internal LocalizedString GetString(string key, object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (Prefix.Length != 0)
        {
            LocalizedString prefixed = _entry.Strings[Prefix + key, arguments];
            if (!prefixed.ResourceNotFound)
                return new LocalizedString(key, prefixed.Value, resourceNotFound: false, prefixed.SearchedLocation);
        }

        return _entry.Strings[key, arguments];
    }

    internal IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _entry.Strings.GetAllStrings(includeParentCultures);
}
