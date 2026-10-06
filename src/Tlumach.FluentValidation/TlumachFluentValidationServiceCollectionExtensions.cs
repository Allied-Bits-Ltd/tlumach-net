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

        var options = new TlumachFluentValidationOptions();
        configure(options);

        if (options.TranslationManager is null)
            throw new ArgumentException("The TranslationManager property of the options must be set.", nameof(configure));

        var languageManager = new TlumachLanguageManager(options.TranslationManager, options);
        ValidatorOptions.Global.LanguageManager = languageManager;
        services.AddSingleton(languageManager);
        services.AddSingleton<ILanguageManager>(languageManager);

        if (options.UseDisplayNameResolver)
        {
            var resolver = new TlumachDisplayNameResolver(options.TranslationManager, options.DisplayNamesGroup);
            ValidatorOptions.Global.DisplayNameResolver = resolver.Resolve;
            services.AddSingleton(resolver);
        }

        return services;
    }
}
