// <copyright file="ViewLocalizerHostTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.AspNetCore.Mvc;
using Tlumach.AspNetCore.Testing;
using Tlumach.Extensions.Localization;

namespace Tlumach.MvcTests;

public sealed class ViewLocalizerHostTests : IDisposable
{
    private readonly TestTranslations _translations = new();
    private readonly TestTranslations _other = new(defaultArb: """{ "@@locale": "en", "Views": { "Home": { "Index": { "Title": "Title from the view file" } } } }""");

    public void Dispose()
    {
        _other.Dispose();
        _translations.Dispose();
    }

    [Fact]
    public async Task Index_English_UsesViewKeys_SharedFallback_LayoutAndPartial()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations);

        string html = await TestHost.GetStringAsync(app, "/Home/Index", TestTranslations.En);

        Assert.Equal("Home page", HtmlAssert.InnerHtml(html, "title"));
        Assert.Equal("Hello, <b>&lt;Bob&gt;</b>!", HtmlAssert.InnerHtml(html, "intro"));
        Assert.Equal("Welcome (shared)", HtmlAssert.InnerHtml(html, "welcome"));
        Assert.Equal("Missing&lt;x&gt;", HtmlAssert.InnerHtml(html, "missing"));
        Assert.Equal("Home page", HtmlAssert.InnerHtml(html, "string"));
        Assert.Equal("Layout footer", HtmlAssert.InnerHtml(html, "footer"));
        Assert.Equal("Partial text", HtmlAssert.InnerHtml(html, "status"));
    }

    [Fact]
    public async Task Index_German_UsesGermanTexts()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations);

        string html = await TestHost.GetStringAsync(app, "/Home/Index", TestTranslations.De);

        Assert.Equal("Startseite", HtmlAssert.InnerHtml(html, "title"));
        Assert.Equal("Willkommen (gemeinsam)", HtmlAssert.InnerHtml(html, "welcome"));
        Assert.Equal("Fu\u00dfzeile", HtmlAssert.InnerHtml(html, "footer"));
        Assert.Equal("Teilansicht", HtmlAssert.InnerHtml(html, "status"));
    }

    [Fact]
    public async Task AreaView_UsesTheAreaPrefix()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations);

        Assert.Equal("Benutzer", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Admin/Users/List", TestTranslations.De), "title"));
    }

    [Fact]
    public async Task ViewContext_CanUseItsOwnManager()
    {
        await using WebApplication app = await MvcHost.StartAsync(
            _translations,
            perContext: p => p.AddContext("Views.Home.Index", new TlumachLocalizationOptions { TranslationManager = _other.Manager }));

        string html = await TestHost.GetStringAsync(app, "/Home/Index", TestTranslations.En);

        Assert.Equal("Title from the view file", HtmlAssert.InnerHtml(html, "title"));
        Assert.Equal("Layout footer", HtmlAssert.InnerHtml(html, "footer"));
    }

    [Fact]
    public async Task NullPrefix_UsesSharedKeysOnly()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.AddTlumachViewLocalization(o => o.ViewKeyPrefix = _ => null));

        Assert.Equal("Title (shared)", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Home/Index", TestTranslations.En), "title"));
    }

    [Fact]
    public async Task CultureSpecificViewFile_IsPickedByTheExpander()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations);

        Assert.Equal("de", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Home/Culture", TestTranslations.De), "variant"));
        Assert.Equal("default", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Home/Culture", TestTranslations.En), "variant"));
    }
}
