// <copyright file="TlumachCultureState.cs" company="Allied Bits Ltd.">
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
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Holds the culture of one user and switches it.
/// <para>The service is scoped: Blazor Server creates one per circuit (and one per prerendering request), so the users of one server do not share the culture;
/// Blazor WebAssembly and Blazor Hybrid effectively have one per application.</para>
/// <para>Components re-render after a switch through the cascading <see cref="TlumachCulture"/> value; other code can subscribe to <see cref="CultureChanged"/>.</para>
/// </summary>
public sealed class TlumachCultureState
{
    private static readonly Action<ILogger, string, Exception?> LogSaveFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, "CultureSaveFailed"),
        "Tlumach could not store the culture '{Culture}'. The language was switched, but the next visit may start with the previous language.");

    private readonly TlumachBlazorOptions _options;
    private readonly ITlumachCultureStore _store;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<TlumachCultureState> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachCultureState"/> class with the culture that the request localization middleware (Blazor Server, static SSR)
    /// or <see cref="TlumachBlazorServiceProviderExtensions.LoadTlumachCultureAsync"/> (Blazor WebAssembly) has set.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="store">The store for the chosen culture.</param>
    /// <param name="navigationManager">The navigation manager, used to reload the page.</param>
    /// <param name="logger">The logger.</param>
    public TlumachCultureState(TlumachBlazorOptions options, ITlumachCultureStore store, NavigationManager navigationManager, ILogger<TlumachCultureState> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Current = new TlumachCulture(options.ResolveInitialCulture(CultureInfo.CurrentUICulture));
        CascadingSource = new CascadingValueSource<TlumachCulture>(Current, isFixed: false);
    }

    /// <summary>
    /// Occurs after the culture has been switched, before the components re-render.
    /// </summary>
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <summary>
    /// Gets the culture of the user.
    /// </summary>
    public CultureInfo Culture => Current.Culture;

    /// <summary>
    /// Gets the snapshot of the culture that is cascaded to the components.
    /// </summary>
    public TlumachCulture Current { get; private set; }

    /// <summary>
    /// Gets the cultures that the user may choose.
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures => _options.SupportedCultures;

    internal CascadingValueSource<TlumachCulture> CascadingSource { get; }

    /// <summary>
    /// Switches the culture of the user.
    /// </summary>
    /// <param name="culture">The new culture. It must match one of <see cref="SupportedCultures"/> (see <see cref="TlumachBlazorOptions.FindSupportedCulture(CultureInfo)"/>).</param>
    /// <param name="forceReload"><see langword="true"/> to store the culture and reload the page, so that everything, including the formatting that does not go through Tlumach,
    /// uses the new culture; <see langword="false"/> to switch the language live.</param>
    /// <returns>A task that completes when the components have been notified and the culture has been stored.</returns>
    /// <exception cref="ArgumentException">The culture is not supported.</exception>
    public async Task SetCultureAsync(CultureInfo culture, bool forceReload = false)
    {
        ArgumentNullException.ThrowIfNull(culture);

        CultureInfo target = _options.FindSupportedCulture(culture)
            ?? throw new ArgumentException($"The culture '{culture.Name}' is not one of TlumachBlazorOptions.SupportedCultures.", nameof(culture));

        if (target.Name.Equals(Culture.Name, StringComparison.OrdinalIgnoreCase))
            return;

        if (forceReload)
        {
            await SaveAsync(target).ConfigureAwait(true);
            _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
            return;
        }

        Current = new TlumachCulture(target);

        // Affects the rest of this call, including components that render synchronously during the notification.
        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;

        if (_options.EffectiveApplyCultureGlobally)
            CultureApplier.ApplyGlobally(target);

        CultureChanged?.Invoke(this, new CultureChangedEventArgs(target));
        await CascadingSource.NotifyChangedAsync(Current).ConfigureAwait(true);

        // Stored last, so that the page does not wait for a network round trip before it shows the new language.
        await SaveAsync(target).ConfigureAwait(true);
    }

    private async Task SaveAsync(CultureInfo culture)
    {
        try
        {
            await _store.SaveAsync(culture).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or OperationCanceledException)
        {
            // JS interop is unavailable while prerendering and fails when the circuit is gone; neither should break the switch.
            LogSaveFailed(_logger, culture.Name, ex);
        }
    }
}
