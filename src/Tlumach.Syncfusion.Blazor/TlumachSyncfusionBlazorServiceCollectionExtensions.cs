// <copyright file="TlumachSyncfusionBlazorServiceCollectionExtensions.cs" company="Allied Bits Ltd.">
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

using Syncfusion.Blazor;

using Tlumach.Blazor;

namespace Tlumach.Syncfusion.Blazor;

/// <summary>
/// Registers the Syncfusion Blazor integration of Tlumach.
/// </summary>
public static class TlumachSyncfusionBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Makes the built-in texts of Syncfusion Blazor components come from Tlumach in the culture of the user.
    /// <para>The method registers a scoped <see cref="TlumachSyncfusionLocalizer"/> as the <see cref="ISyncfusionStringLocalizer"/> of Syncfusion, replacing the default localizer,
    /// so that Syncfusion asks Tlumach for every text and shows its built-in English text for the keys that Tlumach does not have.</para>
    /// <para>It also calls <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/>, whose options can still be set with another call of that method.
    /// The method can be called before or after <c>AddSyncfusionBlazor</c>. A later registration of another <see cref="ISyncfusionStringLocalizer"/> turns the integration off.
    /// A repeated call only applies <paramref name="configure"/> to the options of the first call.</para>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddTlumachSyncfusionBlazor(this IServiceCollection services, Action<TlumachSyncfusionBlazorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A repeated call configures the options of the first call and registers nothing again.
        foreach (ServiceDescriptor descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that AddTlumachSyncfusionBlazor owns.
            if (descriptor.IsKeyedService)
                continue;

            if (descriptor.ServiceType == typeof(TlumachSyncfusionBlazorOptions) && descriptor.ImplementationInstance is TlumachSyncfusionBlazorOptions existing)
            {
                configure?.Invoke(existing);
                return services;
            }
        }

        TlumachSyncfusionBlazorOptions options = new();
        configure?.Invoke(options);

        services.AddTlumachBlazor();
        services.AddSingleton(options);

        // Scoped, because the localizer follows the culture of one user (one circuit in Blazor Server). Syncfusion components inject the localizer, so they get the one of their scope.
        services.TryAddScoped(sp => CreateLocalizer(sp, sp.GetRequiredService<TlumachSyncfusionBlazorOptions>()));

        // Replace works in any order with AddSyncfusionBlazor, which registers its default localizer with TryAddSingleton.
        services.Replace(ServiceDescriptor.Scoped<ISyncfusionStringLocalizer>(sp => sp.GetRequiredService<TlumachSyncfusionLocalizer>()));

        return services;
    }

    private static TlumachSyncfusionLocalizer CreateLocalizer(IServiceProvider services, TlumachSyncfusionBlazorOptions options)
    {
        TranslationManager manager = options.TranslationManager
            ?? services.GetRequiredService<TlumachBlazorOptions>().DefaultManager
            ?? throw new InvalidOperationException(
                "Tlumach.Syncfusion.Blazor has no translation manager. Set TlumachSyncfusionBlazorOptions.TranslationManager in AddTlumachSyncfusionBlazor, or TlumachBlazorOptions.DefaultManager in AddTlumachBlazor.");

        return new TlumachSyncfusionLocalizer(manager, options.Group, options.FallbackTranslationManager, options.FallbackGroup, services.GetRequiredService<TlumachCultureState>());
    }
}
