// <copyright file="TlumachBlazorServiceProviderExtensions.cs" company="Allied Bits Ltd.">
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Startup helpers for Blazor WebAssembly applications.
/// </summary>
public static class TlumachBlazorServiceProviderExtensions
{
    private static readonly Action<ILogger, Exception?> LogInvariantGlobalization = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2, "InvariantGlobalization"),
        "Globalization invariant mode is enabled, so cultures cannot be switched. Remove InvariantGlobalization and set BlazorWebAssemblyLoadAllGlobalizationData to true.");

    private static readonly Action<ILogger, Exception?> LogLoadFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(3, "CultureLoadFailed"),
        "Tlumach could not load the stored culture; the default culture is used.");

    private static readonly Action<ILogger, string, Exception?> LogNotAppliedGlobally = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(4, "CultureNotAppliedGlobally"),
        "LoadTlumachCultureAsync did not apply the culture {Culture}, because ApplyCultureGlobally is off. The method is meant for Blazor WebAssembly and Blazor Hybrid, where one user owns the process; in Blazor Hybrid, set ApplyCultureGlobally to true.");

    /// <summary>
    /// Loads the stored culture and makes it the culture of the application. Call it in a Blazor WebAssembly client before <c>RunAsync</c>:
    /// <c>await host.Services.LoadTlumachCultureAsync();</c>.
    /// <para>The culture is made the culture of the whole process (<see cref="CultureInfo.DefaultThreadCurrentCulture"/>, <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>,
    /// and <see cref="TranslationManager.CurrentCulture"/> of every translation manager) only when <see cref="TlumachBlazorOptions.ApplyCultureGlobally"/> allows it, which it does by default
    /// in the browser. Otherwise, e.g. when the method is called on a server by mistake, nothing is changed, a warning is logged, and the culture is only returned.</para>
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The culture that was loaded, and applied if <see cref="TlumachBlazorOptions.ApplyCultureGlobally"/> allows it.</returns>
    public static async Task<CultureInfo> LoadTlumachCultureAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TlumachBlazorOptions options = services.GetRequiredService<TlumachBlazorOptions>();
        ILogger? logger = services.GetService<ILoggerFactory>()?.CreateLogger("Tlumach.Blazor");

        if (logger is not null && AppContext.TryGetSwitch("System.Globalization.Invariant", out bool invariant) && invariant)
            LogInvariantGlobalization(logger, null);

        string? name = null;
        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            try
            {
                name = await scope.ServiceProvider.GetRequiredService<ITlumachCultureStore>().LoadAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException)
            {
                if (logger is not null)
                    LogLoadFailed(logger, ex);
            }
        }

        CultureInfo? culture = null;
        if (!string.IsNullOrEmpty(name))
        {
            try
            {
                culture = options.FindSupportedCulture(CultureInfo.GetCultureInfo(name));
            }
            catch (CultureNotFoundException)
            {
                culture = null;
            }
        }

        culture ??= options.ResolveInitialCulture(CultureInfo.CurrentUICulture);

        // The process-wide culture is shared by all users of a server, so it is changed only where TlumachCultureState would change it as well.
        if (options.EffectiveApplyCultureGlobally)
            CultureApplier.ApplyGlobally(culture);
        else if (logger is not null)
            LogNotAppliedGlobally(logger, culture.Name, null);

        return culture;
    }
}
