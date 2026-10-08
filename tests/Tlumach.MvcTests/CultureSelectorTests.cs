// <copyright file="CultureSelectorTests.cs" company="Allied Bits Ltd.">
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
using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.AspNetCore;
using Tlumach.AspNetCore.Mvc;
using Tlumach.Web;

namespace Tlumach.MvcTests;

public sealed class CultureSelectorTests : IDisposable
{
    private readonly CultureInfo _savedUiCulture = CultureInfo.CurrentUICulture;

    public void Dispose() => CultureInfo.CurrentUICulture = _savedUiCulture;

    private static string Run(Action<TlumachCultureOptions>? configure, Action<TlumachCultureSelectorTagHelper>? setup = null, TagHelperAttributeList? attributes = null, string query = "?page=2")
    {
        ServiceCollection services = new();
        services.AddTlumachCultures(configure);
        DefaultHttpContext http = new() { RequestServices = services.BuildServiceProvider() };
        http.Request.PathBase = "/app";
        http.Request.Path = "/Home/Index";
        http.Request.QueryString = new QueryString(query);

        TlumachCultureSelectorTagHelper helper = new() { ViewContext = new ViewContext { HttpContext = http } };
        setup?.Invoke(helper);

        attributes ??= [];
        TagHelperContext context = new("tlumach-culture-selector", attributes, new Dictionary<object, object>(), "unique");
        TagHelperOutput output = new("tlumach-culture-selector", attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent())) { TagMode = TagMode.SelfClosing };
        helper.Process(context, output);

        using StringWriter writer = new(CultureInfo.InvariantCulture);
        output.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    [Fact]
    public void RendersAGetFormForTheCultureEndpoint_WithTheCurrentPage()
    {
        CultureInfo.CurrentUICulture = TestTranslations.De;

        string html = Run(o => o.SupportedCultures = [TestTranslations.En, TestTranslations.De]);

        Assert.StartsWith("<form method=\"get\" action=\"/app/tlumach/culture\">", html, StringComparison.Ordinal);
        Assert.Contains("<input name=\"redirectUri\" type=\"hidden\" value=\"/app/Home/Index?page=2\" />", html, StringComparison.Ordinal);
        Assert.Contains("<select name=\"culture\">", html, StringComparison.Ordinal);
        Assert.Contains($"<option value=\"en-US\">{HtmlEncoder.Default.Encode(TestTranslations.En.NativeName)}</option>", html, StringComparison.Ordinal);
        Assert.Contains($"<option selected=\"selected\" value=\"de-DE\">{HtmlEncoder.Default.Encode(TestTranslations.De.NativeName)}</option>", html, StringComparison.Ordinal);
        Assert.Contains("<button type=\"submit\">OK</button>", html, StringComparison.Ordinal);
        Assert.EndsWith("</form>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("?a=1&b=2", "?a=1&b=2")]
    [InlineData("?q=привіт", "?q=%D0%BF%D1%80%D0%B8%D0%B2%D1%96%D1%82")]
    [InlineData("?q=%D0%BF%D1%80&r=a%20b&s=%2F", "?q=%D0%BF%D1%80&r=a%20b&s=%2F")]
    [InlineData("?q=a b&c=<\"{|}^`\\>", "?q=a%20b&c=%3C%22%7B%7C%7D%5E%60%5C%3E")]
    [InlineData("?d=100%&e=%zz&f=%4", "?d=100%25&e=%25zz&f=%254")]
    [InlineData("?g=\t\u007F&h=😀", "?g=%09%7F&h=%F0%9F%98%80")]
    [InlineData("?i=x/y:z@w?v&j=!$'()*+,;=-._~[]", "?i=x/y:z@w?v&j=!$'()*+,;=-._~[]")]
    public void RedirectUri_EscapesTheCharactersThatAreNotAllowedInAQuery(string query, string expected)
    {
        CultureInfo.CurrentUICulture = TestTranslations.En;

        string redirectUri = RedirectUri(Run(o => o.SupportedCultures = [TestTranslations.En], query: query));

        // A raw query would fail the local URL check of the culture endpoint, which then returns to the root instead of the current page.
        Assert.Equal("/app/Home/Index" + expected, redirectUri);
        Assert.True(TlumachAspNetCoreExtensions.IsLocalUrl(redirectUri));
        Assert.Equal(Uri.UnescapeDataString(query), Uri.UnescapeDataString(redirectUri["/app/Home/Index".Length..]));
    }

    [Fact]
    public void Options_ChangeTextsClassesAndEndpoint()
    {
        CultureInfo.CurrentUICulture = TestTranslations.En;

        string html = Run(
            o =>
            {
                o.SupportedCultures = [TestTranslations.En];
                o.CultureEndpoint = "/lang";
            },
            h =>
            {
                h.ButtonText = "Go";
                h.SelectClass = "s";
                h.ButtonClass = "b";
                h.DisplayName = c => c.TwoLetterISOLanguageName;
            },
            [new TagHelperAttribute("class", "lang")]);

        Assert.StartsWith("<form class=\"lang\" method=\"get\" action=\"/app/lang\">", html, StringComparison.Ordinal);
        Assert.Contains("<select class=\"s\" name=\"culture\">", html, StringComparison.Ordinal);
        Assert.Contains(">en</option>", html, StringComparison.Ordinal);
        Assert.Contains("<button class=\"b\" type=\"submit\">Go</button>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void NoSupportedCultures_RendersNothing()
        => Assert.Equal(string.Empty, Run(o => o.SupportedCultures = []));

    [Fact]
    public void WithoutCultureOptions_Throws()
    {
        DefaultHttpContext http = new() { RequestServices = new ServiceCollection().BuildServiceProvider() };
        TlumachCultureSelectorTagHelper helper = new() { ViewContext = new ViewContext { HttpContext = http } };
        TagHelperOutput output = new("tlumach-culture-selector", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => helper.Process(new TagHelperContext([], new Dictionary<object, object>(), "u"), output));
        Assert.Contains("AddTlumachCultures", error.Message, StringComparison.Ordinal);
    }

    private static string RedirectUri(string html)
        => WebUtility.HtmlDecode(Regex.Match(html, "name=\"redirectUri\" type=\"hidden\" value=\"([^\"]*)\"", RegexOptions.None, TimeSpan.FromSeconds(1)).Groups[1].Value);
}
