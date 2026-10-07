// <copyright file="TlumachBlazorServiceCollectionExtensionsTests.cs" company="Allied Bits Ltd.">
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
using Tlumach.Web;

namespace Tlumach.BlazorTests;

public sealed class TlumachBlazorServiceCollectionExtensionsTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task AddTlumachBlazor_Twice_ConfiguresExistingOptions_AndRegistersNothingAgain()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddLogging();
        ctx.Services.AddTlumachBlazor(options =>
        {
            options.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            options.DefaultManager = _translations.Manager;
            options.Persistence = TlumachCulturePersistence.LocalStorage;
        });
        int registrations = ctx.Services.Count;

        ctx.Services.AddTlumachBlazor(options => options.LocalStorageKey = "second.key");

        Assert.Equal(registrations, ctx.Services.Count);
        Assert.Single(ctx.Services, d => d.ServiceType == typeof(TlumachBlazorOptions));
        Assert.Equal("second.key", ctx.Services.GetRequiredService<TlumachBlazorOptions>().LocalStorageKey);

        CultureInfo.CurrentUICulture = TestTranslations.En;
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        await cut.InvokeAsync(() => state.SetCultureAsync(TestTranslations.De));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Hallo", cut.Markup));
        var invocation = ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        Assert.Equal(new object?[] { "second.key", "de-DE" }, invocation.Arguments);
    }

    [Fact]
    public void AddTlumachBlazor_WithKeyedOptionsRegisteredFirst_DoesNotThrow()
    {
        ServiceCollection services = new();
        services.AddKeyedSingleton("x", new TlumachBlazorOptions());

        services.AddTlumachBlazor();

        Assert.Single(services, d => !d.IsKeyedService && d.ServiceType == typeof(TlumachBlazorOptions));
    }

    [Fact]
    public void AddTlumachBlazor_RegistersOptionsAsCultureOptions()
    {
        ServiceCollection services = new();
        services.AddTlumachBlazor(o => o.SupportedCultures = [TestTranslations.En, TestTranslations.De]);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<TlumachBlazorOptions>(), provider.GetRequiredService<TlumachCultureOptions>());
    }

    [Fact]
    public void AddTlumachCultures_ThenAddTlumachBlazor_CopiesCultureSettings_AndKeepsOneRegistration()
    {
        ServiceCollection services = new();
        services.AddTlumachCultures(o =>
        {
            o.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            o.DefaultCulture = TestTranslations.De;
            o.CultureEndpoint = "/lang";
        });

        services.AddTlumachBlazor(o => o.LocalStorageKey = "k");

        Assert.Single(services, d => d.ServiceType == typeof(TlumachCultureOptions));
        using ServiceProvider provider = services.BuildServiceProvider();
        TlumachBlazorOptions options = provider.GetRequiredService<TlumachBlazorOptions>();
        Assert.Same(options, provider.GetRequiredService<TlumachCultureOptions>());
        Assert.Equal([TestTranslations.En, TestTranslations.De], options.SupportedCultures);
        Assert.Same(TestTranslations.De, options.DefaultCulture);
        Assert.Equal("/lang", options.CultureEndpoint);
        Assert.Equal("k", options.LocalStorageKey);
    }

    [Fact]
    public void AddTlumachBlazor_ThenAddTlumachCultures_ConfiguresTheBlazorOptions()
    {
        ServiceCollection services = new();
        services.AddTlumachBlazor();

        services.AddTlumachCultures(o => o.CultureEndpoint = "/lang");

        Assert.Single(services, d => d.ServiceType == typeof(TlumachCultureOptions));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Equal("/lang", provider.GetRequiredService<TlumachBlazorOptions>().CultureEndpoint);
    }
}
