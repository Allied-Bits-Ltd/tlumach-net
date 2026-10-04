// <copyright file="CultureEndpointTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;

using Tlumach.AspNetCore;
using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public class CultureEndpointTests
{
    [Fact]
    public async Task Post_SetsCookie_AndReturnsNoContent()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsync(new Uri("/tlumach/culture?culture=de-DE", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith(".AspNetCore.Culture=" + Uri.EscapeDataString("c=de-DE|uic=de-DE"), cookie, StringComparison.Ordinal);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("")]
    [InlineData("not a culture")]
    public async Task Post_UnsupportedOrInvalidCulture_Returns400WithoutCookie(string culture)
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsync(new Uri("/tlumach/culture?culture=" + Uri.EscapeDataString(culture), UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Get_SetsCookie_AndRedirectsToLocalUri()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/tlumach/culture?culture=de-DE&redirectUri=" + Uri.EscapeDataString("/counter?x=1"), UriKind.Relative));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/counter?x=1", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
#pragma warning disable CA1054 // The value is a raw query-string value that must be sent as is, so it is a string.
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example/")]
    public async Task Get_NonLocalRedirect_FallsBackToRoot(string redirectUri)
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/tlumach/culture?culture=de-DE&redirectUri=" + Uri.EscapeDataString(redirectUri), UriKind.Relative));

        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }
#pragma warning restore CA1054

    [Fact]
    public async Task RequestLocalization_ReadsCookie()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/culture");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=" + Uri.EscapeDataString("c=de-DE|uic=de-DE"));
        request.Headers.Add("Accept-Language", "en-US");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal("de-DE", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RequestLocalization_WithoutCookie_UsesDefaultCulture()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        Assert.Equal("en-US", await client.GetStringAsync(new Uri("/culture", UriKind.Relative)));
    }

    [Theory]
#pragma warning disable CA1054 // The tested method takes a string, so the test data are strings too.
    [InlineData("/", true)]
    [InlineData("/a/b?c=d", true)]
    [InlineData("//evil", false)]
    [InlineData("/\\evil", false)]
    [InlineData("https://evil", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalUrl_AcceptsOnlyRootRelativePaths(string? url, bool expected)
    {
        Assert.Equal(expected, TlumachAspNetCoreExtensions.IsLocalUrl(url));
    }
#pragma warning restore CA1054

    private static async Task<WebApplication> StartAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddTlumachBlazor(o =>
        {
            o.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            o.DefaultCulture = TestTranslations.En;
        });

        WebApplication app = builder.Build();
        app.UseTlumachRequestLocalization();
        app.MapTlumachCultureEndpoint();
        app.MapGet("/culture", (HttpContext context) => context.Features.GetRequiredFeature<IRequestCultureFeature>().RequestCulture.UICulture.Name);
        await app.StartAsync();
        return app;
    }
}
