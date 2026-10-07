// <copyright file="TlumachHtmlHelperExtensions.cs" company="Allied Bits Ltd.">
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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Renders translation units in views without <c>@Html.Raw</c>: <c>@Html.Tlumach(Strings.Home.Intro, Model.Name)</c>.
/// <para>The translation is trusted HTML; values are HTML-encoded, except <see cref="IHtmlContent"/> values. <see cref="TranslationManager.WebEncodeValues"/> does not affect the output.
/// Values cached on the unit or supplied by <see cref="BaseTranslationUnit.OnPlaceholderValueNeeded"/> are used for placeholders without a value; they are shared by all requests.</para>
/// </summary>
public static class TlumachHtmlHelperExtensions
{
    /// <summary>Renders the unit in the current culture.</summary>
    /// <param name="htmlHelper">The HTML helper of the view.</param>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The HTML.</returns>
    public static IHtmlContent Tlumach(this IHtmlHelper htmlHelper, BaseTranslationUnit unit)
        => Render(htmlHelper, unit, named: null, positional: null);

    /// <summary>Renders the unit in the current culture with positional values.</summary>
    /// <param name="htmlHelper">The HTML helper of the view.</param>
    /// <param name="unit">The translation unit.</param>
    /// <param name="arguments">The values of the placeholders, by position.</param>
    /// <returns>The HTML.</returns>
    public static IHtmlContent Tlumach(this IHtmlHelper htmlHelper, BaseTranslationUnit unit, params object?[] arguments)
        => Render(htmlHelper, unit, named: null, positional: arguments);

    /// <summary>Renders the unit in the current culture with named values.</summary>
    /// <param name="htmlHelper">The HTML helper of the view.</param>
    /// <param name="unit">The translation unit.</param>
    /// <param name="values">The values of the placeholders, by name.</param>
    /// <returns>The HTML.</returns>
    public static IHtmlContent Tlumach(this IHtmlHelper htmlHelper, BaseTranslationUnit unit, IReadOnlyDictionary<string, object?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Render(htmlHelper, unit, values, positional: null);
    }

    private static HtmlString Render(IHtmlHelper htmlHelper, BaseTranslationUnit unit, IEnumerable<KeyValuePair<string, object?>>? named, IEnumerable<object?>? positional)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(unit);

        HtmlEncoder encoder = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<HtmlEncoder>() ?? HtmlEncoder.Default;
        return new HtmlString(TlumachTagRenderer.RenderUnit(unit, HtmlTranslation.ToArguments(named, positional, encoder), CultureInfo.CurrentCulture, encoder));
    }
}
