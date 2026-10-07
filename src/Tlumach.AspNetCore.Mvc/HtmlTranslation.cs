// <copyright file="HtmlTranslation.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;

using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Converts the arguments of the MVC localization APIs to placeholder values and the rendered HTML to MVC types.
/// </summary>
internal static class HtmlTranslation
{
    /// <summary>
    /// Converts the arguments of an indexer: a single <see cref="IReadOnlyDictionary{TKey, TValue}"/> supplies named values, anything else positional values.
    /// <see cref="IHtmlContent"/> values become <see cref="TemplateMarkup"/>, so that they are inserted without encoding, as <c>HtmlFormattableString</c> of MVC does.
    /// </summary>
    /// <param name="arguments">The arguments of the indexer.</param>
    /// <param name="encoder">The encoder that renders <see cref="IHtmlContent"/> values.</param>
    /// <returns>The placeholder values.</returns>
    internal static TemplateArguments ToArguments(object?[]? arguments, HtmlEncoder encoder)
    {
        TemplateArguments result = new();
        if (arguments is null || arguments.Length == 0)
            return result;

        if (arguments.Length == 1 && arguments[0] is IReadOnlyDictionary<string, object?> named)
        {
            foreach (KeyValuePair<string, object?> pair in named)
                result.AddNamed(pair.Key, Prepare(pair.Value, encoder));

            return result;
        }

        foreach (object? argument in arguments)
            result.AddPositional(Prepare(argument, encoder));

        return result;
    }

    /// <summary>
    /// Converts the named and positional values of a tag helper or an HTML helper.
    /// </summary>
    /// <param name="named">The named values.</param>
    /// <param name="positional">The positional values.</param>
    /// <param name="encoder">The encoder that renders <see cref="IHtmlContent"/> values.</param>
    /// <returns>The placeholder values.</returns>
    internal static TemplateArguments ToArguments(IEnumerable<KeyValuePair<string, object?>>? named, IEnumerable<object?>? positional, HtmlEncoder encoder)
    {
        TemplateArguments result = new();
        if (named is not null)
        {
            foreach (KeyValuePair<string, object?> pair in named)
                result.AddNamed(pair.Key, Prepare(pair.Value, encoder));
        }

        if (positional is not null)
        {
            foreach (object? argument in positional)
                result.AddPositional(Prepare(argument, encoder));
        }

        return result;
    }

    internal static string Render(IHtmlContent content, HtmlEncoder encoder)
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        content.WriteTo(writer, encoder);
        return writer.ToString();
    }

    /// <summary>
    /// Wraps rendered HTML. <see cref="LocalizedHtmlString"/> always passes its value through <c>string.Format</c>, even without arguments,
    /// so the braces of the HTML are doubled; <see cref="LocalizedHtmlString.WriteTo"/> then writes the HTML exactly.
    /// </summary>
    /// <param name="name">The key of the translation.</param>
    /// <param name="html">The rendered HTML.</param>
    /// <param name="notFound">Whether the translation was not found.</param>
    /// <returns>The localized HTML string.</returns>
    internal static LocalizedHtmlString ToLocalizedHtmlString(string name, string html, bool notFound)
        => new(name, EscapeBraces(html), notFound);

    internal static string EscapeBraces(string html)
        => html.AsSpan().IndexOfAny('{', '}') < 0
            ? html
            : html.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);

    private static object? Prepare(object? value, HtmlEncoder encoder)
        => value is IHtmlContent content ? new TemplateMarkup(Render(content, encoder)) : value;
}
