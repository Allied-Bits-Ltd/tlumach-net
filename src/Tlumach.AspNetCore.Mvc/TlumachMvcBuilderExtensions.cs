// <copyright file="TlumachMvcBuilderExtensions.cs" company="Allied Bits Ltd.">
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

using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Tlumach.Extensions.Localization;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Registers Tlumach for MVC and Razor Pages: <c>services.AddControllersWithViews()</c> or <c>services.AddRazorPages()</c>, followed by these methods.
/// </summary>
public static class TlumachMvcBuilderExtensions
{
    /// <summary>
    /// Registers the Tlumach <see cref="IHtmlLocalizerFactory"/>, <see cref="IHtmlLocalizer{TResource}"/>, and <see cref="IViewLocalizer"/>, replacing those of
    /// <c>AddViewLocalization</c> regardless of the order of the calls, and adds <see cref="LanguageViewLocationExpander"/> (see <see cref="TlumachViewLocalizationOptions.ViewLocationExpanderFormat"/>).
    /// <para>Call <c>AddTlumachLocalization</c> as well; it provides the translation managers. A repeated call (e.g. after both <c>AddControllersWithViews()</c> and
    /// <c>AddRazorPages()</c>) configures the same options.</para>
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IMvcBuilder AddTlumachViewLocalization(this IMvcBuilder builder, Action<TlumachViewLocalizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IServiceCollection services = builder.Services;
        TlumachViewLocalizationOptions? options = FindInstance<TlumachViewLocalizationOptions>(services);
        if (options is null)
        {
            options = new TlumachViewLocalizationOptions();
            services.AddSingleton(options);
            services.Replace(ServiceDescriptor.Singleton<IHtmlLocalizerFactory>(CreateFactory));
            services.Replace(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(TlumachHtmlLocalizer<>)));
            services.Replace(ServiceDescriptor.Transient<IViewLocalizer, TlumachViewLocalizer>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RazorViewEngineOptions>, TlumachRazorViewEngineOptionsSetup>());
        }

        configure?.Invoke(options);
        return builder;
    }

    /// <summary>
    /// Takes the model binding messages of MVC (<see cref="Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelBindingMessageProvider"/>) from Tlumach, in the culture of the request.
    /// <para>Keys are <see cref="TlumachModelBindingOptions.KeyPrefix"/> followed by the name of the accessor without "Accessor", e.g. "ModelBinding.AttemptedValueIsInvalid".
    /// The attempted value and the name of the field are available as <c>{value}</c> and <c>{field}</c>, which is the only way in the ARB formats (<c>Arb</c>, <c>ArbNoEscaping</c>).
    /// With the .NET text format they are available also by position as in MVC's texts (<c>{0}</c>, <c>{1}</c>); the ARB formats reject indexed placeholders.
    /// A message without a translation keeps MVC's English text.</para>
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IMvcBuilder AddTlumachModelBindingMessages(this IMvcBuilder builder, Action<TlumachModelBindingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        TlumachModelBindingOptions? options = FindInstance<TlumachModelBindingOptions>(builder.Services);
        if (options is null)
        {
            options = new TlumachModelBindingOptions();
            builder.Services.AddSingleton(options);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, TlumachModelBindingMessagesSetup>());
        }

        configure?.Invoke(options);
        return builder;
    }

    internal static T? FindInstance<T>(IServiceCollection services)
        where T : class
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            // ImplementationInstance throws for keyed descriptors on .NET 8+, and a keyed registration is not the one that Tlumach owns.
            if (descriptor.IsKeyedService)
                continue;

            if (descriptor.ServiceType == typeof(T) && descriptor.ImplementationInstance is T instance)
                return instance;
        }

        return null;
    }

    private static TlumachHtmlLocalizerFactory CreateFactory(IServiceProvider services)
        => new(
            services.GetService<ITlumachSettingsProvider>() ?? throw new InvalidOperationException(TlumachHtmlLocalizerFactory.MissingLocalizationMessage),
            services.GetService<HtmlEncoder>() ?? HtmlEncoder.Default,
            services.GetRequiredService<TlumachViewLocalizationOptions>());
}
