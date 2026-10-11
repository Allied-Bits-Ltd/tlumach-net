// <copyright file="TlumachLocalizationExtensions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// Adds the classes necessary for dependency injection.
    /// </summary>
    public static class TlumachLocalizationExtensions
    {
        /// <summary>
        /// Registers the classes so that Tlumach can be used via dependency injection.
        /// <para>The <see cref="IStringLocalizer"/> and <see cref="IStringLocalizer{T}"/> registrations are added only if no other registration exists.</para>
        /// <para>A repeated call registers nothing again: it applies <paramref name="configureDefault"/> to the default options of the first call and <paramref name="configurePerContext"/>
        /// to the <see cref="TlumachSettingsProvider"/> of the first call.</para>
        /// </summary>
        /// <param name="services">The services to which Tlumach is added.</param>
        /// <param name="configureDefault">A configuration callback for default/global options.</param>
        /// <param name="configurePerContext">A configuration callback for per-context options.</param>
        /// <returns>The value of <paramref name="services"/>.</returns>
        public static IServiceCollection AddTlumachLocalization(
            this IServiceCollection services,
            Action<TlumachLocalizationOptions> configureDefault,
            Action<TlumachSettingsProvider>? configurePerContext = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureDefault);

            // A repeated call configures the options of the first call and registers nothing again.
            TlumachSettingsProvider? existing = FindRegisteredProvider(services);
            if (existing is not null)
            {
                configureDefault(existing.DefaultOptions);
                configurePerContext?.Invoke(existing);
                return services;
            }

            var defaultOptions = new TlumachLocalizationOptions();
            configureDefault(defaultOptions);

            var provider = new TlumachSettingsProvider(defaultOptions);
            configurePerContext?.Invoke(provider);

            services.AddSingleton<ITlumachSettingsProvider>(provider);
            services.AddSingleton<IStringLocalizerFactory, TlumachStringLocalizerFactory>();

            // TryAdd keeps localizers registered earlier, e.g. the per-user ones of AddTlumachBlazor, so the order of the calls does not matter.
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(TlumachStringLocalizer<>));
            services.TryAddTransient<IStringLocalizer>(sp =>
            {
                var factory = sp.GetRequiredService<IStringLocalizerFactory>();
                return factory.Create(string.Empty, string.Empty);
            });

            return services;
        }

        private static TlumachSettingsProvider? FindRegisteredProvider(IServiceCollection services)
        {
            foreach (ServiceDescriptor descriptor in services)
            {
                // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that AddTlumachLocalization owns.
                if (descriptor.IsKeyedService)
                    continue;

                if (descriptor.ServiceType == typeof(ITlumachSettingsProvider) && descriptor.ImplementationInstance is TlumachSettingsProvider provider)
                    return provider;
            }

            return null;
        }
    }
}
