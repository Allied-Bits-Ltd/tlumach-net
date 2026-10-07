// <copyright file="TlumachCultureSelectorTagHelper.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Web;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Lets the user choose one of <see cref="TlumachCultureOptions.SupportedCultures"/>: a form that sends the choice to the culture endpoint
/// (<c>MapTlumachCultureEndpoint</c>), which stores it in the culture cookie and returns to the current page. No script is used; add one that submits the form
/// on change if the button is not wanted. Nothing is rendered when no cultures are supported.
/// </summary>
[HtmlTargetElement("tlumach-culture-selector", TagStructure = TagStructure.WithoutEndTag)]
public sealed class TlumachCultureSelectorTagHelper : TagHelper
{
    /// <summary>Gets or sets the text of the submit button. The default is "OK".</summary>
    [HtmlAttributeName("button-text")]
    public string ButtonText { get; set; } = "OK";

    /// <summary>Gets or sets the function that returns the text of a culture. The default is <see cref="CultureInfo.NativeName"/>.</summary>
    [HtmlAttributeName("display-name")]
    public Func<CultureInfo, string>? DisplayName { get; set; }

    /// <summary>Gets or sets the class of the <c>select</c> element.</summary>
    [HtmlAttributeName("select-class")]
    public string? SelectClass { get; set; }

    /// <summary>Gets or sets the class of the submit button.</summary>
    [HtmlAttributeName("button-class")]
    public string? ButtonClass { get; set; }

    /// <summary>Gets or sets the context of the view; set by Razor.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        HttpContext http = ViewContext.HttpContext;
        TlumachCultureOptions options = http.RequestServices.GetService<TlumachCultureOptions>()
            ?? throw new InvalidOperationException("The culture selector needs the supported cultures. Call services.AddTlumachCultures(...) (or AddTlumachBlazor(...)).");

        if (options.SupportedCultures.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        HttpRequest request = http.Request;
        CultureInfo current = options.ResolveInitialCulture(CultureInfo.CurrentUICulture);
        string endpoint = options.CultureEndpoint.StartsWith('/') ? options.CultureEndpoint : "/" + options.CultureEndpoint;

        output.TagName = "form";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("method", "get");
        output.Attributes.SetAttribute("action", request.PathBase.Add(new PathString(endpoint)).ToUriComponent());

        TagBuilder redirect = new("input") { TagRenderMode = TagRenderMode.SelfClosing };
        redirect.MergeAttribute("type", "hidden");
        redirect.MergeAttribute("name", "redirectUri");
        redirect.MergeAttribute("value", string.Concat(request.PathBase.ToUriComponent(), request.Path.ToUriComponent(), request.QueryString.ToUriComponent()));

        TagBuilder select = new("select");
        select.MergeAttribute("name", "culture");
        if (!string.IsNullOrEmpty(SelectClass))
            select.MergeAttribute("class", SelectClass);

        foreach (CultureInfo culture in options.SupportedCultures)
        {
            TagBuilder option = new("option");
            option.MergeAttribute("value", culture.Name);
            if (culture.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase))
                option.MergeAttribute("selected", "selected");

            option.InnerHtml.Append(DisplayName?.Invoke(culture) ?? culture.NativeName);
            select.InnerHtml.AppendHtml(option);
        }

        TagBuilder button = new("button");
        button.MergeAttribute("type", "submit");
        if (!string.IsNullOrEmpty(ButtonClass))
            button.MergeAttribute("class", ButtonClass);

        button.InnerHtml.Append(ButtonText);

        output.Content.AppendHtml(redirect);
        output.Content.AppendHtml(select);
        output.Content.AppendHtml(button);
    }
}
