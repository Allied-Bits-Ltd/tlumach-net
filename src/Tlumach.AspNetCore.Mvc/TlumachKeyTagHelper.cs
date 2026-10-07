// <copyright file="TlumachKeyTagHelper.cs" company="Allied Bits Ltd.">
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
/// Replaces the content of an element with a translation: <c>&lt;h1 tlumach-key="Title"&gt;&lt;/h1&gt;</c> or <c>&lt;p tlumach-unit="Strings.Home.Intro" tlumach-arg-name="@Model.Name"&gt;&lt;/p&gt;</c>.
/// <para>A key is looked up in three steps: with the prefix of the file that is being executed (e.g. "Views.Home.Index."), then with the prefix of the main view, then without a prefix (the shared key).
/// The second step exists because a <c>@section</c> of a view runs while the layout executes, so the executing file is the layout there; thanks to it, a tag helper in a section finds the keys of its own view.
/// The side effect is that a tag helper in a layout finds the keys of the current view, if the layout has no such key. <see cref="TlumachViewLocalizer"/> does not do that: it uses the file that is being executed and the shared key.</para>
/// <para>The translation is trusted HTML; values are HTML-encoded, except <see cref="Microsoft.AspNetCore.Html.IHtmlContent"/> values. Values of <c>tlumach-arg-*</c> and <c>tlumach-args</c> are C# expressions (<c>@Model.Name</c>, <c>@("text")</c>).
/// <c>tlumach-culture</c> is the name of a culture; an invalid name throws <see cref="System.Globalization.CultureNotFoundException"/>.</para>
/// </summary>
[HtmlTargetElement("*", Attributes = KeyAttributeName)]
[HtmlTargetElement("*", Attributes = UnitAttributeName)]
public sealed class TlumachKeyTagHelper : TagHelper
{
    private const string KeyAttributeName = "tlumach-key";
    private const string UnitAttributeName = "tlumach-unit";
    private readonly TlumachHtmlLocalizerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachKeyTagHelper"/> class.
    /// </summary>
    /// <param name="factory">The factory registered by <see cref="TlumachMvcBuilderExtensions.AddTlumachViewLocalization"/>.</param>
    public TlumachKeyTagHelper(IHtmlLocalizerFactory factory)
    {
        _factory = TlumachTagRenderer.GetFactory(factory);
    }

    /// <summary>Gets or sets the key of the translation.</summary>
    [HtmlAttributeName(KeyAttributeName)]
    public string? Key { get; set; }

    /// <summary>Gets or sets the translation unit, e.g. a unit of a class created by Tlumach Generator.</summary>
    [HtmlAttributeName(UnitAttributeName)]
    public BaseTranslationUnit? Unit { get; set; }

    /// <summary>Gets or sets the positional placeholder values.</summary>
    [HtmlAttributeName("tlumach-args")]
    public IReadOnlyList<object?>? Args { get; set; }

    /// <summary>Gets or sets the named placeholder values, set with <c>tlumach-arg-{name}</c> attributes.</summary>
    [HtmlAttributeName("tlumach-all-args", DictionaryAttributePrefix = "tlumach-arg-")]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Razor assigns the dictionary when tlumach-all-args is used, as for asp-all-route-data.")]
    public IDictionary<string, object?> NamedArgs { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the name of the culture to use instead of the culture of the request.</summary>
    [HtmlAttributeName("tlumach-culture")]
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
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(html);
    }
}
