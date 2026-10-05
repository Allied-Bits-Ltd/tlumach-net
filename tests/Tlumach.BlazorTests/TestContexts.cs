// <copyright file="TestContexts.cs" company="Allied Bits Ltd.">
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

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

internal static class TestContexts
{
    /// <summary>
    /// Creates a bUnit context with Tlumach.Blazor registered (en-US and de-DE supported, local-storage persistence, loose JS interop)
    /// and creates the culture state with <paramref name="initialCulture"/> (en-US by default) as the culture of the "request".
    /// </summary>
    /// <param name="translations">The translations whose manager is the default one.</param>
    /// <param name="configure">An optional callback that adjusts the options.</param>
    /// <param name="configureServices">An optional callback that registers extra services before Tlumach.Blazor is registered.</param>
    /// <param name="initialCulture">The culture of the "request"; en-US when not specified.</param>
    /// <returns>The context, with the culture state already created.</returns>
    public static BunitContext Create(
        TestTranslations translations,
        Action<TlumachBlazorOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null,
        CultureInfo? initialCulture = null)
    {
        BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddLogging();
        configureServices?.Invoke(ctx.Services);
        ctx.Services.AddTlumachBlazor(options =>
        {
            options.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            options.DefaultManager = translations.Manager;
            options.Persistence = TlumachCulturePersistence.LocalStorage;
            configure?.Invoke(options);
        });

        // The state reads CultureInfo.CurrentUICulture once, when it is created; create it now so that the result does not depend on the machine.
        CultureInfo.CurrentUICulture = initialCulture ?? TestTranslations.En;
        _ = ctx.Services.GetRequiredService<TlumachCultureState>();
        return ctx;
    }
}
