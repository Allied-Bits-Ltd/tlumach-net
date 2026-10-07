// <copyright file="TlumachTextTagHelper.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Renders a translation without a wrapping element: <c>&lt;tlumach-text key="Welcome" /&gt;</c>. Attributes: <c>key</c>, <c>unit</c>, <c>args</c>, <c>arg-*</c>, <c>culture</c>;
/// the rules are those of <see cref="TlumachKeyTagHelper"/>: a key is looked up with the prefix of the file that is being executed, then with the prefix of the main view (a <c>@section</c> runs in the context of the layout),
/// then without a prefix. An invalid <c>culture</c> name throws <see cref="System.Globalization.CultureNotFoundException"/>.
/// </summary>
[HtmlTargetElement("tlumach-text")]
public sealed class TlumachTextTagHelper : TagHelper
{
    private readonly TlumachHtmlLocalizerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachTextTagHelper"/> class.
    /// </summary>
    /// <param name="factory">The factory registered by <see cref="TlumachMvcBuilderExtensions.AddTlumachViewLocalization"/>.</param>
    public TlumachTextTagHelper(IHtmlLocalizerFactory factory)
    {
        _factory = TlumachTagRenderer.GetFactory(factory);
    }

    /// <summary>Gets or sets the key of the translation.</summary>
    [HtmlAttributeName("key")]
    public string? Key { get; set; }

    /// <summary>Gets or sets the translation unit, e.g. a unit of a class created by Tlumach Generator.</summary>
    [HtmlAttributeName("unit")]
    public BaseTranslationUnit? Unit { get; set; }

    /// <summary>Gets or sets the positional placeholder values.</summary>
    [HtmlAttributeName("args")]
    public IReadOnlyList<object?>? Args { get; set; }

    /// <summary>Gets or sets the named placeholder values, set with <c>arg-{name}</c> attributes.</summary>
    [HtmlAttributeName("all-args", DictionaryAttributePrefix = "arg-")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Razor assigns the dictionary when all-args is used, as for asp-all-route-data.")]
    public IDictionary<string, object?> NamedArgs { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the name of the culture to use instead of the culture of the request.</summary>
    [HtmlAttributeName("culture")]
    public string? Culture { get; set; }

    /// <summary>Gets or sets the context of the view; set by Razor.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(output);

        string html = TlumachTagRenderer.Render(_factory, ViewContext, Key, Unit, NamedArgs, Args, Culture);
        output.TagName = null;
        output.Content.SetHtmlContent(html);
    }
}
