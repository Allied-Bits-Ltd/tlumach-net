// <copyright file="TlumachAspNetCoreExtensions.cs" company="Allied Bits Ltd.">
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
using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.AspNetCore;

/// <summary>
/// Connects the culture chosen in Blazor (or any ASP.NET Core application) with the request localization of ASP.NET Core.
/// </summary>
public static class TlumachAspNetCoreExtensions
{
    // The longest culture name that Windows accepts (LOCALE_NAME_MAX_LENGTH); longer values are rejected without parsing.
    private const int MaxCultureNameLength = 85;

    /// <summary>
    /// Adds the request localization middleware configured from <see cref="TlumachBlazorOptions"/>: the supported cultures, the default culture,
    /// and the culture cookie as the first source of the culture. Call <c>AddTlumachBlazor</c> first.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The value of <paramref name="app"/>.</returns>
    public static IApplicationBuilder UseTlumachRequestLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        TlumachBlazorOptions options = app.ApplicationServices.GetRequiredService<TlumachBlazorOptions>();
        RequestLocalizationOptions localization = new();

        if (options.SupportedCultures.Count > 0)
        {
            List<CultureInfo> cultures = [.. options.SupportedCultures];
            localization.SupportedCultures = cultures;
            localization.SupportedUICultures = cultures;
            localization.DefaultRequestCulture = new RequestCulture(options.DefaultCulture ?? cultures[0]);
        }
        else if (options.DefaultCulture is not null)
        {
            localization.DefaultRequestCulture = new RequestCulture(options.DefaultCulture);
        }

        IRequestCultureProvider? cookieProvider = localization.RequestCultureProviders.OfType<CookieRequestCultureProvider>().FirstOrDefault();
        if (cookieProvider is not null)
        {
            localization.RequestCultureProviders.Remove(cookieProvider);
            localization.RequestCultureProviders.Insert(0, cookieProvider);
        }

        return app.UseRequestLocalization(localization);
    }

    /// <summary>
    /// Maps the endpoint that stores the chosen culture in the ASP.NET Core culture cookie.
    /// <para><c>POST {pattern}?culture=de-DE</c> sets the cookie and returns 204; it is called by <see cref="CookieCultureStore"/>.
    /// <c>GET {pattern}?culture=de-DE&amp;redirectUri=/page</c> sets the cookie and redirects to the local URI; it is used by the form of <see cref="TlumachCultureSelector"/>
    /// in static server-side rendering. An unsupported culture yields 400.</para>
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern. When <see langword="null"/>, <see cref="TlumachBlazorOptions.CultureEndpoint"/> is used.</param>
    /// <returns>A builder for further configuration of both endpoints.</returns>
    public static IEndpointConventionBuilder MapTlumachCultureEndpoint(this IEndpointRouteBuilder endpoints, string? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        pattern ??= endpoints.ServiceProvider.GetRequiredService<TlumachBlazorOptions>().CultureEndpoint;
        RouteGroupBuilder group = endpoints.MapGroup(pattern);

        // RequestDelegate handlers need no request delegate generator, which keeps the library trimming- and AOT-safe.
        group.MapPost(string.Empty, context => SetCultureAsync(context, redirect: false));
        group.MapGet(string.Empty, context => SetCultureAsync(context, redirect: true));
        return group;
    }

    /// <summary>
    /// Checks that the URL is a path within this application ("/page"), not a URL of another site ("//site", "/\site", "https://site").
    /// <para>Like <c>UrlHelper.IsLocalUrl</c> of ASP.NET Core, it also rejects control characters: browsers drop tabs and line breaks from
    /// a <c>Location</c> header, so "/&lt;tab&gt;/site" would be followed as "//site", and a line break cannot be written to a header at all.</para>
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns><see langword="true"/> when the URL is a path within this application.</returns>
    internal static bool IsLocalUrl([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
            return false;

        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
            return false;

        return !HasControlCharacter(url.AsSpan(1));
    }

    private static bool HasControlCharacter(ReadOnlySpan<char> value)
    {
        foreach (char c in value)
        {
            if (char.IsControl(c))
                return true;
        }

        return false;
    }

    private static Task SetCultureAsync(HttpContext context, bool redirect)
    {
        TlumachBlazorOptions options = context.RequestServices.GetRequiredService<TlumachBlazorOptions>();
        CultureInfo? culture = ParseCulture(context.Request.Query["culture"], options);
        if (culture is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Task.CompletedTask;
        }

#pragma warning disable S2092 // Secure follows the request scheme on purpose: forcing it would break the cookie in plain-HTTP development and behind TLS-terminating proxies.
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = true,
                IsEssential = true,
                Secure = context.Request.IsHttps,
            });
#pragma warning restore S2092

        if (!redirect)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }

        string? redirectUri = context.Request.Query["redirectUri"];
        context.Response.Redirect(IsLocalUrl(redirectUri) ? redirectUri : $"{context.Request.PathBase}/");
        return Task.CompletedTask;
    }

    private static CultureInfo? ParseCulture(string? name, TlumachBlazorOptions options)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxCultureNameLength)
            return null;

        // The name is matched against the supported cultures as a string: CultureInfo.GetCultureInfo would cache a culture for every well-formed name
        // that a client sends, growing the memory of the server without bound.
        return options.FindSupportedCulture(name);
    }
}
