// <copyright file="TestHost.cs" company="Allied Bits Ltd.">
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
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;

namespace Tlumach.AspNetCore.Testing;

/// <summary>
/// Starts an application on the test server and sends requests to it in a given culture.
/// </summary>
internal static class TestHost
{
    public static async Task<WebApplication> StartAsync(Action<WebApplicationBuilder> configureServices, Action<WebApplication> configureApp)
    {
        // The application name selects the application parts, i.e. the assembly with the compiled views or pages of the test project.
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
            EnvironmentName = "Development",
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.WebHost.UseTestServer();
        configureServices(builder);

        WebApplication app = builder.Build();
        configureApp(app);
        await app.StartAsync();
        return app;
    }

    public static string CultureCookie(CultureInfo culture)
        => CookieRequestCultureProvider.DefaultCookieName + "=" + Uri.EscapeDataString(CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)));

    public static async Task<string> GetStringAsync(WebApplication app, string url, CultureInfo? culture = null)
    {
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(url, UriKind.Relative));
        if (culture is not null)
            request.Headers.Add("Cookie", CultureCookie(culture));

        using HttpResponseMessage response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"GET {url} returned {(int)response.StatusCode}: {body}");
        return body;
    }

    public static async Task<HttpResponseMessage> PostFormAsync(WebApplication app, string url, IReadOnlyDictionary<string, string> form, CultureInfo? culture = null)
    {
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(url, UriKind.Relative)) { Content = new FormUrlEncodedContent(form) };
        if (culture is not null)
            request.Headers.Add("Cookie", CultureCookie(culture));

        return await client.SendAsync(request);
    }
}
