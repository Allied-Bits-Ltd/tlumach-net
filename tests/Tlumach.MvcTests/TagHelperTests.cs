// <copyright file="TagHelperTests.cs" company="Allied Bits Ltd.">
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
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.AspNetCore.Mvc;
using Tlumach.Extensions.Localization;

namespace Tlumach.MvcTests;

public sealed class TagHelperTests : IDisposable
{
    private readonly CultureInfo _savedCulture = CultureInfo.CurrentCulture;
    private readonly TestTranslations _translations = new();
    private readonly ServiceProvider _services;
    private readonly TlumachHtmlLocalizerFactory _factory;
    private readonly ViewContext _viewContext = new() { ExecutingFilePath = "/Views/Home/Index.cshtml" };

    public TagHelperTests()
    {
        CultureInfo.CurrentCulture = TestTranslations.En;
        ServiceCollection services = new();
        services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);
        _services = services.BuildServiceProvider();
        _factory = new TlumachHtmlLocalizerFactory(_services.GetRequiredService<ITlumachSettingsProvider>(), HtmlEncoder.Default, new TlumachViewLocalizationOptions());
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _savedCulture;
        _services.Dispose();
        _translations.Dispose();
    }

    private static string Run(TagHelper helper, string tagName, TagMode mode = TagMode.StartTagAndEndTag)
    {
        TagHelperContext context = new(tagName, new TagHelperAttributeList(), new Dictionary<object, object>(), "unique");
        TagHelperOutput output = new(tagName, new TagHelperAttributeList(), (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())) { TagMode = mode };
        helper.Process(context, output);
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        output.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private TlumachKeyTagHelper KeyHelper() => new(_factory) { ViewContext = _viewContext };

    [Fact]
    public void Key_UsesTheViewKey_AndEncodesNamedValues()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "Intro";
        helper.NamedArgs["name"] = "<Bob>";

        Assert.Equal("<h1>Hello, <b>&lt;Bob&gt;</b>!</h1>", Run(helper, "h1"));
    }

    [Fact]
    public void Key_FallsBackToTheSharedKey()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "Welcome";

        Assert.Equal("<p>Welcome (shared)</p>", Run(helper, "p"));
    }

    [Fact]
    public void Unit_WithPositionalValues()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Unit = _translations.Unit("richGreeting", containsPlaceholders: true);
        helper.Args = ["<i>"];

        Assert.Equal("<span>Welcome, <b>&lt;i&gt;</b></span>", Run(helper, "span"));
    }

    [Fact]
    public void HtmlContentValue_IsInsertedAsIs()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "richGreeting";
        helper.NamedArgs["name"] = new HtmlString("<i>Bob</i>");

        Assert.Equal("<p>Welcome, <b><i>Bob</i></b></p>", Run(helper, "p"));
    }

    [Fact]
    public void Culture_OverridesTheCurrentCulture()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "hello";
        helper.Culture = "de-DE";

        Assert.Equal("<p>Hallo</p>", Run(helper, "p"));
    }

    [Fact]
    public void SelfClosingElement_GetsContentAndEndTag()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "hello";

        Assert.Equal("<span>Hello</span>", Run(helper, "span", TagMode.SelfClosing));
    }

    [Fact]
    public void MissingKey_RendersTheEncodedKey()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "no<such>";

        Assert.Equal("<p>no&lt;such&gt;</p>", Run(helper, "p"));
    }

    [Fact]
    public void KeyAndUnit_Throws()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "hello";
        helper.Unit = _translations.Unit("hello", containsPlaceholders: false);

        Assert.Throws<InvalidOperationException>(() => Run(helper, "p"));
    }

    [Fact]
    public void AllArgs_AssignedAsAWholeDictionary()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Key = "Intro";
        helper.NamedArgs = new Dictionary<string, object?> { ["name"] = "<Bob>" };

        Assert.Equal("<h1>Hello, <b>&lt;Bob&gt;</b>!</h1>", Run(helper, "h1"));
    }

    [Fact]
    public void Unit_WithAMissingKey_RendersTheEncodedUnitKey()
    {
        TlumachKeyTagHelper helper = KeyHelper();
        helper.Unit = _translations.Unit("no<such>", containsPlaceholders: false);

        Assert.Equal("<p>no&lt;such&gt;</p>", Run(helper, "p"));
    }

    [Fact]
    public void TextElement_RendersWithoutWrapper()
    {
        TlumachTextTagHelper helper = new(_factory) { ViewContext = _viewContext, Key = "greeting" };
        helper.NamedArgs["name"] = "A&B";

        Assert.Equal("Hello, A&amp;B!", Run(helper, "tlumach-text"));
    }

    [Fact]
    public void TextElement_WithoutKeyOrUnit_Throws()
    {
        TlumachTextTagHelper helper = new(_factory) { ViewContext = _viewContext };

        Assert.Throws<InvalidOperationException>(() => Run(helper, "tlumach-text"));
    }
}
