// <copyright file="TlumachMudBlazorServiceCollectionExtensions.cs" company="Allied Bits Ltd.">
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using MudBlazor;

using Tlumach.Blazor;

namespace Tlumach.MudBlazor;

/// <summary>
/// Registers the MudBlazor integration of Tlumach.
/// </summary>
public static class TlumachMudBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Makes the built-in texts of MudBlazor components come from Tlumach in the culture of the user.
    /// <para>The method registers a scoped <see cref="TlumachMudLocalizer"/> as the <see cref="MudLocalizer"/> and replaces the <see cref="ILocalizationInterceptor"/> of MudBlazor
    /// with a scoped <see cref="DefaultLocalizationInterceptor"/> that has <see cref="DefaultLocalizationInterceptor.IgnoreDefaultEnglish"/> set, so that MudBlazor asks Tlumach for every text,
    /// whatever the culture of the thread is, and shows its built-in English text for the keys that Tlumach does not have.</para>
    /// <para>It also calls <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/>, whose options can still be set with another call of that method.
    /// The method can be called before or after <c>AddMudServices</c>. A later call of <c>AddMudTranslations</c> or <c>AddLocalizationInterceptor</c> replaces the interceptor and thus turns
    /// the integration off. A repeated call only applies <paramref name="configure"/> to the options of the first call.</para>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddTlumachMudBlazor(this IServiceCollection services, Action<TlumachMudBlazorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A repeated call configures the options of the first call and registers nothing again.
        foreach (ServiceDescriptor descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that AddTlumachMudBlazor owns.
            if (descriptor.IsKeyedService)
                continue;

            if (descriptor.ServiceType == typeof(TlumachMudBlazorOptions) && descriptor.ImplementationInstance is TlumachMudBlazorOptions existing)
            {
                configure?.Invoke(existing);
                return services;
            }
        }

        TlumachMudBlazorOptions options = new();
        configure?.Invoke(options);

        services.AddTlumachBlazor();
        services.AddSingleton(options);

        // Scoped, because the localizer follows the culture of one user (one circuit in Blazor Server).
        services.TryAddScoped(sp => CreateLocalizer(sp, sp.GetRequiredService<TlumachMudBlazorOptions>()));
        services.Replace(ServiceDescriptor.Scoped<MudLocalizer>(sp => sp.GetRequiredService<TlumachMudLocalizer>()));

        // Replace works in any order with AddMudServices, which registers its interceptor with TryAdd. Scoped, because the interceptor builds a cache of the built-in English texts.
        services.Replace(ServiceDescriptor.Scoped<ILocalizationInterceptor>(sp => new DefaultLocalizationInterceptor(sp.GetRequiredService<ILoggerFactory>(), sp.GetRequiredService<MudLocalizer>())
        {
            IgnoreDefaultEnglish = true,
        }));

        return services;
    }

    private static TlumachMudLocalizer CreateLocalizer(IServiceProvider services, TlumachMudBlazorOptions options)
    {
        TranslationManager manager = options.TranslationManager
            ?? services.GetRequiredService<TlumachBlazorOptions>().DefaultManager
            ?? throw new InvalidOperationException(
                "Tlumach.MudBlazor has no translation manager. Set TlumachMudBlazorOptions.TranslationManager in AddTlumachMudBlazor, or TlumachBlazorOptions.DefaultManager in AddTlumachBlazor.");

        return new TlumachMudLocalizer(manager, options.Group, services.GetRequiredService<TlumachCultureState>());
    }
}
