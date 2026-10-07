// <copyright file="TlumachWebServiceCollectionExtensions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Web;

/// <summary>
/// Registers the culture options of a web application.
/// </summary>
public static class TlumachWebServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="TlumachCultureOptions"/>, which the request localization, the culture endpoint, and the culture selectors of Tlumach use.
    /// <para>Nothing else is registered. A repeated call, or a call after <c>AddTlumachBlazor</c>, configures the instance that is already registered.</para>
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddTlumachCultures(this IServiceCollection services, Action<TlumachCultureOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TlumachCultureOptions? existing = FindRegisteredOptions(services);
        if (existing is not null)
        {
            configure?.Invoke(existing);
            return services;
        }

        TlumachCultureOptions options = new();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Returns the <see cref="TlumachCultureOptions"/> instance registered in <paramref name="services"/>, if any.
    /// <para>Only registrations that carry an instance are found, which is how <c>AddTlumachCultures</c> and <c>AddTlumachBlazor</c> register the options. Keyed registrations are ignored.</para>
    /// </summary>
    /// <param name="services">The services to search.</param>
    /// <returns>The registered <see cref="TlumachCultureOptions"/> instance (including a <c>TlumachBlazorOptions</c> instance registered as <see cref="TlumachCultureOptions"/>), or <see langword="null"/> if there is none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static TlumachCultureOptions? FindRegisteredOptions(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (ServiceDescriptor descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that Tlumach owns.
            if (descriptor.IsKeyedService)
                continue;

            if (descriptor.ServiceType == typeof(TlumachCultureOptions) && descriptor.ImplementationInstance is TlumachCultureOptions options)
                return options;
        }

        return null;
    }
}
