// <copyright file="TagHelperHostTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.AspNetCore.Testing;

namespace Tlumach.MvcTests;

public sealed class TagHelperHostTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task TagHelpersAndHtmlHelper_RenderInTheRequestCulture()
    {
        BaseTranslationUnit unit = _translations.Unit("richGreeting", containsPlaceholders: true);
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.Services.AddSingleton(unit));

        string html = await TestHost.GetStringAsync(app, "/Home/TagHelpers", TestTranslations.De);

        Assert.Equal("Titel (gemeinsam)", HtmlAssert.InnerHtml(html, "t1"));
        Assert.Equal("Hallo, <b>&lt;Bob&gt;</b>!", HtmlAssert.InnerHtml(html, "t2"));
        Assert.Equal("Willkommen (gemeinsam)", HtmlAssert.InnerHtml(html, "t3"));
        Assert.Equal("Willkommen, <b>&lt;x&gt;</b>", HtmlAssert.InnerHtml(html, "t4"));
        Assert.Equal("Привіт", HtmlAssert.InnerHtml(html, "t5"));
        Assert.Equal("Willkommen, <b><i>raw</i></b>", HtmlAssert.InnerHtml(html, "t6"));
        Assert.Equal("Willkommen, <b>A&amp;B</b>", HtmlAssert.InnerHtml(html, "t7"));

        // The attributes of the tag helpers and the <tlumach-text> element do not reach the browser.
        Assert.DoesNotContain("tlumach-key", html, StringComparison.Ordinal);
        Assert.DoesNotContain("tlumach-arg", html, StringComparison.Ordinal);
        Assert.DoesNotContain("tlumach-unit", html, StringComparison.Ordinal);
        Assert.DoesNotContain("tlumach-culture", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<tlumach-text", html, StringComparison.Ordinal);
        Assert.DoesNotContain("</tlumach-text", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HtmlHelper_WithoutArguments_RendersTheUnit()
    {
        BaseTranslationUnit unit = _translations.Unit("hello", containsPlaceholders: false);
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.Services.AddSingleton(unit));

        string html = await TestHost.GetStringAsync(app, "/Home/UnitOnly", TestTranslations.De);

        Assert.Equal("Hallo", HtmlAssert.InnerHtml(html, "u1"));
    }

    [Fact]
    public async Task SectionOfAContentView_UsesTheKeysOfTheContentView()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations);

        string html = await TestHost.GetStringAsync(app, "/Home/Sections", TestTranslations.De);

        // A section runs while the layout executes, but the key of the content view still wins over the shared key.
        Assert.Equal("Abschnittstitel", HtmlAssert.InnerHtml(html, "main"));
        Assert.Equal("Abschnittstitel", HtmlAssert.InnerHtml(html, "s1"));
        Assert.Equal("Abschnittstitel", HtmlAssert.InnerHtml(html, "s2"));
    }

    [Fact]
    public async Task TagHelperInTheLayout_UsesTheKeyOfTheCurrentView_ThenTheSharedKey()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations);

        Assert.Equal("Startseite", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Home/Index", TestTranslations.De), "layout-title"));
        Assert.Equal("Titel (gemeinsam)", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Home/Culture", TestTranslations.De), "layout-title"));
    }
}
