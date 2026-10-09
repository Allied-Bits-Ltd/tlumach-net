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

using Syncfusion.Blazor;

using Tlumach.Blazor;
using Tlumach.Syncfusion.Blazor;

namespace Tlumach.SyncfusionTests;

internal static class TestContexts
{
    /// <summary>
    /// Creates a bUnit context with Syncfusion and Tlumach.Syncfusion.Blazor registered (en-US and de-DE supported, local-storage persistence, loose JS interop
    /// as Syncfusion recommends for bUnit) and creates the culture state with <paramref name="initialCulture"/> as the culture of the "request".
    /// </summary>
    /// <param name="translations">The translation sets of the test.</param>
    /// <param name="initialCulture">The culture of the "request", with which the culture state is created.</param>
    /// <param name="configure">An optional callback that configures <see cref="TlumachSyncfusionBlazorOptions"/>.</param>
    /// <returns>The context, in which the components are rendered.</returns>
    public static BunitContext Create(TestTranslations translations, CultureInfo initialCulture, Action<TlumachSyncfusionBlazorOptions>? configure = null)
    {
        BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddLogging();
        ctx.Services.AddOptions();
        ctx.Services.AddSyncfusionBlazor();
        ctx.Services.AddTlumachSyncfusionBlazor(configure);
        ctx.Services.AddTlumachBlazor(options =>
        {
            options.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            options.DefaultManager = translations.Manager;
            options.Persistence = TlumachCulturePersistence.LocalStorage;
        });

        // The state takes CultureInfo.CurrentUICulture when it is created, and until a live switch the localizers follow the culture of the thread,
        // as they follow the culture of the request or circuit in an application. Set it here so that the result does not depend on the machine.
        CultureInfo.CurrentUICulture = initialCulture;
        _ = ctx.Services.GetRequiredService<TlumachCultureState>();
        return ctx;
    }
}
