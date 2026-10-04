// <copyright file="CookieCultureStore.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Stores the culture in the ASP.NET Core culture cookie by calling the endpoint mapped by <c>MapTlumachCultureEndpoint</c> through <c>fetch</c>,
/// and loads it from the <c>lang</c> attribute of the <c>html</c> element, which the server renders from the same cookie.
/// <para>The store uses only built-in browser functions, so no JavaScript file is needed.</para>
/// </summary>
public sealed class CookieCultureStore : ITlumachCultureStore
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly TlumachBlazorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieCultureStore"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    /// <param name="navigationManager">The navigation manager, which supplies the base address of the application.</param>
    /// <param name="options">The options, which supply the path of the endpoint.</param>
    public CookieCultureStore(IJSRuntime jsRuntime, NavigationManager navigationManager, TlumachBlazorOptions options)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default)
        => _jsRuntime.InvokeAsync<string?>("document.documentElement.getAttribute", cancellationToken, "lang");

    /// <inheritdoc/>
    public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);

        string url = _navigationManager.BaseUri + _options.CultureEndpoint.TrimStart('/') + "?culture=" + Uri.EscapeDataString(culture.Name);
        Dictionary<string, string> init = new(StringComparer.Ordinal)
        {
            ["method"] = "POST",
            ["credentials"] = "same-origin",
        };

        // JS interop awaits the promise that fetch returns, so the cookie is set when this task completes.
        return _jsRuntime.InvokeVoidAsync("fetch", cancellationToken, url, init);
    }
}
