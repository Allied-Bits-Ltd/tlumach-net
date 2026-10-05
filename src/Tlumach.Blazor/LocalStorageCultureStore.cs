// <copyright file="LocalStorageCultureStore.cs" company="Allied Bits Ltd.">
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

using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Stores the culture in the local storage of the browser. Used by standalone Blazor WebAssembly and Blazor Hybrid applications.
/// </summary>
public sealed class LocalStorageCultureStore : ITlumachCultureStore
{
    private readonly IJSRuntime _jsRuntime;
    private readonly TlumachBlazorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageCultureStore"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    /// <param name="options">The options, which supply the storage key.</param>
    public LocalStorageCultureStore(IJSRuntime jsRuntime, TlumachBlazorOptions options)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default)
        => _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, _options.LocalStorageKey);

    /// <inheritdoc/>
    public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, _options.LocalStorageKey, culture.Name);
    }
}
