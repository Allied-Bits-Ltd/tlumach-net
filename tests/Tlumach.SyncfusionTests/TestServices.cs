// <copyright file="TestServices.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Tlumach.Blazor;

namespace Tlumach.SyncfusionTests;

internal static class TestServices
{
    /// <summary>
    /// Registers what Tlumach.Blazor needs outside a Blazor host (logging, a navigation manager, a JS runtime that is never called) and its options
    /// (en-US and de-DE supported, local-storage persistence, <paramref name="defaultManager"/> as the default manager).
    /// </summary>
    /// <param name="defaultManager">The default translation manager of Tlumach.Blazor.</param>
    /// <returns>The service collection.</returns>
    public static ServiceCollection Create(TranslationManager? defaultManager)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IJSRuntime, NoJSRuntime>();
        services.AddTlumachBlazor(options =>
        {
            options.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            options.DefaultManager = defaultManager;
            options.Persistence = TlumachCulturePersistence.LocalStorage;
        });
        return services;
    }

    /// <summary>
    /// Creates the culture state of one user whose request had <paramref name="initialCulture"/>.
    /// The state follows the culture of the thread until a live switch, so the thread culture is set here and stays set for the test.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="initialCulture">The culture of the request of the user.</param>
    /// <returns>The culture state.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The scope must outlive the call, because the state belongs to it; it lives as long as the provider, which each test disposes.")]
    public static TlumachCultureState CreateCultureState(ServiceProvider provider, CultureInfo initialCulture)
    {
        CultureInfo.CurrentUICulture = initialCulture;
        return provider.CreateScope().ServiceProvider.GetRequiredService<TlumachCultureState>();
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }
}
