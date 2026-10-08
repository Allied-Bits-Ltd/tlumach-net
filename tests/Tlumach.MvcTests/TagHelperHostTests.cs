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

using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
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

    [Fact]
    public async Task Selector_SubmitsToTheEndpoint_WhichSwitchesTheCulture()
    {
        BaseTranslationUnit unit = _translations.Unit("richGreeting", containsPlaceholders: true);
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.Services.AddSingleton(unit));

        string page = await TestHost.GetStringAsync(app, "/Home/TagHelpers", TestTranslations.En);
        Assert.Contains("action=\"/tlumach/culture\"", HtmlAssert.InnerHtml(page, "sel"), StringComparison.Ordinal);

        using HttpClient client = app.GetTestClient();
        using HttpResponseMessage switched = await client.GetAsync(new Uri("/tlumach/culture?culture=uk-UA&redirectUri=%2FHome%2FTagHelpers", UriKind.Relative));
        Assert.Equal(System.Net.HttpStatusCode.Redirect, switched.StatusCode);
        Assert.Equal("/Home/TagHelpers", switched.Headers.Location?.OriginalString);
        string cookie = Assert.Single(switched.Headers.GetValues("Set-Cookie")).Split(';')[0];

        using HttpRequestMessage next = new(HttpMethod.Get, new Uri("/Home/TagHelpers", UriKind.Relative));
        next.Headers.Add("Cookie", cookie);
        using HttpResponseMessage response = await client.SendAsync(next);
        Assert.Equal("Заголовок (спільний)", HtmlAssert.InnerHtml(await response.Content.ReadAsStringAsync(), "t1"));
    }

    [Fact]
    public async Task Selector_OnAPageWithARawNonAsciiQuery_ReturnsToThatPage()
    {
        BaseTranslationUnit unit = _translations.Unit("richGreeting", containsPlaceholders: true);
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.Services.AddSingleton(unit));

        // HttpClient escapes the query, so the request is built directly, as a lenient server or a proxy can pass it.
        HttpContext page = await app.GetTestServer().SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Get;
            c.Request.Path = "/Home/TagHelpers";
            c.Request.QueryString = new QueryString("?q=привіт");
        });
        Assert.Equal(StatusCodes.Status200OK, page.Response.StatusCode);
        string html = await new StreamReader(page.Response.Body).ReadToEndAsync();
        Match value = Regex.Match(HtmlAssert.InnerHtml(html, "sel"), "name=\"redirectUri\" type=\"hidden\" value=\"([^\"]*)\"", RegexOptions.None, TimeSpan.FromSeconds(1));
        Assert.True(value.Success, html);
        string redirectUri = WebUtility.HtmlDecode(value.Groups[1].Value);
        Assert.Equal("/Home/TagHelpers?q=привіт", Uri.UnescapeDataString(redirectUri));

        // The browser submits the form with the value escaped once more.
        using HttpClient client = app.GetTestClient();
        using HttpResponseMessage switched = await client.GetAsync(new Uri("/tlumach/culture?culture=uk-UA&redirectUri=" + Uri.EscapeDataString(redirectUri), UriKind.Relative));
        Assert.Equal(System.Net.HttpStatusCode.Redirect, switched.StatusCode);
        Assert.Equal(redirectUri, switched.Headers.Location?.OriginalString);
    }
}
