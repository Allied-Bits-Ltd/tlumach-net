// <copyright file="TlumachBlazorServiceCollectionExtensions.cs" company="Allied Bits Ltd.">
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
using Microsoft.Extensions.Localization;

using Tlumach.Web;

namespace Tlumach.Blazor;

/// <summary>
/// Registers the Blazor integration of Tlumach.
/// </summary>
public static class TlumachBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the per-user culture state, the cascading <see cref="TlumachCulture"/> value, and the culture store.
    /// The options are also registered as <see cref="TlumachCultureOptions"/>, which <c>UseTlumachRequestLocalization</c> and <c>MapTlumachCultureEndpoint</c> of Tlumach.AspNetCore use.
    /// It also makes the injected <see cref="IStringLocalizer"/> and <see cref="IStringLocalizer{T}"/> follow the culture of the user.
    /// <para>In a Blazor Web App, call this method both in the server and in the client project.
    /// A repeated call in the same project only applies <paramref name="configure"/> to the options of the first call.</para>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddTlumachBlazor(this IServiceCollection services, Action<TlumachBlazorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A repeated call configures the options of the first call and registers nothing again.
        foreach (ServiceDescriptor descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that AddTlumachBlazor owns.
            if (descriptor.IsKeyedService)
                continue;

            if (descriptor.ServiceType == typeof(TlumachBlazorOptions) && descriptor.ImplementationInstance is TlumachBlazorOptions existing)
            {
                configure?.Invoke(existing);
                return services;
            }
        }

        TlumachBlazorOptions options = new();

        // The culture settings of an earlier AddTlumachCultures call are taken over, so that the order of the two calls does not matter.
        ServiceDescriptor? cultureDescriptor = services.FirstOrDefault(d => !d.IsKeyedService && d.ServiceType == typeof(TlumachCultureOptions));
        if (TlumachWebServiceCollectionExtensions.FindRegisteredOptions(services) is { } earlier)
        {
            options.SupportedCultures = earlier.SupportedCultures;
            options.DefaultCulture = earlier.DefaultCulture;
            options.CultureEndpoint = earlier.CultureEndpoint;
        }

        if (cultureDescriptor is not null)
            services.Remove(cultureDescriptor);

        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<TlumachCultureOptions>(options);
        services.TryAddScoped(sp => CultureStoreFactory.Create(sp, sp.GetRequiredService<TlumachBlazorOptions>()));
        services.TryAddScoped<TlumachCultureState>();
        services.AddCascadingValue(sp => sp.GetRequiredService<TlumachCultureState>().CascadingSource);

        // Scoped, so that each user's localizer follows that user's culture; Replace, so that the registrations of AddTlumachLocalization or AddLocalization do not win.
        services.Replace(ServiceDescriptor.Scoped(typeof(IStringLocalizer<>), typeof(TlumachCultureStringLocalizer<>)));
        services.Replace(ServiceDescriptor.Scoped<IStringLocalizer>(sp => new TlumachCultureStringLocalizer(
            sp.GetRequiredService<IStringLocalizerFactory>().Create(string.Empty, string.Empty),
            sp.GetRequiredService<TlumachCultureState>())));

        return services;
    }
}
