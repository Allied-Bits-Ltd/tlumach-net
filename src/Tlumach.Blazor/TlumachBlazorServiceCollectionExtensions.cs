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

namespace Tlumach.Blazor;

/// <summary>
/// Registers the Blazor integration of Tlumach.
/// </summary>
public static class TlumachBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the per-user culture state, the cascading <see cref="TlumachCulture"/> value, and the culture store.
    /// <para>In a Blazor Web App, call this method both in the server and in the client project.</para>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddTlumachBlazor(this IServiceCollection services, Action<TlumachBlazorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TlumachBlazorOptions options = new();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddScoped(sp => CultureStoreFactory.Create(sp, options));
        services.TryAddScoped<TlumachCultureState>();
        services.AddCascadingValue(sp => sp.GetRequiredService<TlumachCultureState>().CascadingSource);

        return services;
    }
}
