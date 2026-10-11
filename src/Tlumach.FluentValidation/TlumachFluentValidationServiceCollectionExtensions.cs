// <copyright file="TlumachFluentValidationServiceCollectionExtensions.cs" company="Allied Bits Ltd.">
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

using System.Linq.Expressions;
using System.Reflection;

using FluentValidation;
using FluentValidation.Resources;

using Microsoft.Extensions.DependencyInjection;

namespace Tlumach.FluentValidation;

/// <summary>
/// Registers the Tlumach integration of FluentValidation.
/// </summary>
public static class TlumachFluentValidationServiceCollectionExtensions
{
    /// <summary>
    /// Creates a <see cref="TlumachLanguageManager"/>, installs it as <c>ValidatorOptions.Global.LanguageManager</c>, and registers it as a singleton for
    /// <see cref="TlumachLanguageManager"/> and <see cref="ILanguageManager"/>. With <see cref="TlumachFluentValidationOptions.UseDisplayNameResolver"/>, it does the same for a
    /// <see cref="TlumachDisplayNameResolver"/> and <c>ValidatorOptions.Global.DisplayNameResolver</c>.
    /// <para>FluentValidation reads its language manager only from the process-wide <c>ValidatorOptions.Global</c>, so this method changes a global setting at the moment it is
    /// called. It can be combined with <c>AddTlumachLocalization</c> and <c>AddValidatorsFromAssemblyContaining</c> in any order.</para>
    /// <para>A repeated call applies <paramref name="configure"/> to the options of the first call. Because the language manager and the resolver copy the options when they are created,
    /// they are then created anew from these options and replace those of the earlier call, both in the services (which keep one registration of each) and in <c>ValidatorOptions.Global</c>.
    /// If the options no longer set <see cref="TlumachFluentValidationOptions.UseDisplayNameResolver"/>, the display name resolver that was installed before the first call is restored.</para>
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configure">Sets the options. <see cref="TlumachFluentValidationOptions.TranslationManager"/> is required.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the callback leaves <see cref="TlumachFluentValidationOptions.TranslationManager"/> unset.</exception>
    public static IServiceCollection AddTlumachFluentValidation(this IServiceCollection services, Action<TlumachFluentValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // A repeated call configures the options of the first call.
        Registration? registration = FindRegistration(services);
        TlumachFluentValidationOptions options = registration?.Options ?? new TlumachFluentValidationOptions();
        configure(options);

        if (options.TranslationManager is null)
            throw new ArgumentException("The TranslationManager property of the options must be set.", nameof(configure));

        if (registration is null)
        {
            registration = new Registration(options, ValidatorOptions.Global.DisplayNameResolver);
            services.AddSingleton(registration);
        }
        else
        {
            // The instances of the earlier call copied the options before this callback changed them, so they give way to new ones.
            RemoveInstance(services, registration.LanguageManager);
            RemoveInstance(services, registration.DisplayNameResolver);
        }

        var languageManager = new TlumachLanguageManager(options.TranslationManager, options);
        ValidatorOptions.Global.LanguageManager = languageManager;
        services.AddSingleton(languageManager);
        services.AddSingleton<ILanguageManager>(languageManager);
        registration.LanguageManager = languageManager;

        if (options.UseDisplayNameResolver)
        {
            var resolver = new TlumachDisplayNameResolver(options.TranslationManager, options.DisplayNamesGroup);
            ValidatorOptions.Global.DisplayNameResolver = resolver.Resolve;
            services.AddSingleton(resolver);
            registration.DisplayNameResolver = resolver;
        }
        else if (registration.DisplayNameResolver is not null)
        {
            // A repeated call turned the resolver off. The resolver that was there before the first call comes back, unless someone else has replaced Tlumach's meanwhile.
            if (ReferenceEquals(ValidatorOptions.Global.DisplayNameResolver?.Target, registration.DisplayNameResolver))
                ValidatorOptions.Global.DisplayNameResolver = registration.OriginalDisplayNameResolver;

            registration.DisplayNameResolver = null;
        }

        return services;
    }

    private static Registration? FindRegistration(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that Tlumach owns.
            if (descriptor.IsKeyedService)
                continue;

            if (descriptor.ServiceType == typeof(Registration) && descriptor.ImplementationInstance is Registration registration)
                return registration;
        }

        return null;
    }

    private static void RemoveInstance(IServiceCollection services, object? instance)
    {
        if (instance is null)
            return;

        for (int i = services.Count - 1; i >= 0; i--)
        {
            ServiceDescriptor descriptor = services[i];
            if (!descriptor.IsKeyedService && ReferenceEquals(descriptor.ImplementationInstance, instance))
                services.RemoveAt(i);
        }
    }

    /// <summary>
    /// The state of the first call, registered as a service so that a repeated call on the same services finds it.
    /// </summary>
    private sealed class Registration
    {
        internal Registration(TlumachFluentValidationOptions options, Func<Type, MemberInfo, LambdaExpression, string> originalDisplayNameResolver)
        {
            Options = options;
            OriginalDisplayNameResolver = originalDisplayNameResolver;
        }

        internal TlumachFluentValidationOptions Options { get; }

        internal Func<Type, MemberInfo, LambdaExpression, string> OriginalDisplayNameResolver { get; }

        internal TlumachLanguageManager? LanguageManager { get; set; }

        internal TlumachDisplayNameResolver? DisplayNameResolver { get; set; }
    }
}
