// <copyright file="TlumachMudBlazorServiceCollectionExtensionsTests.cs" company="Allied Bits Ltd.">
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

using MudBlazor;
using MudBlazor.Services;

using Tlumach.Blazor;
using Tlumach.MudBlazor;

namespace Tlumach.MudBlazorTests;

public sealed class TlumachMudBlazorServiceCollectionExtensionsTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReplacesInterceptorInAnyOrderWithAddMudServices(bool mudServicesFirst)
    {
        ServiceCollection services = CreateServices();
        if (mudServicesFirst)
            services.AddMudServices();

        services.AddTlumachMudBlazor();

        if (!mudServicesFirst)
            services.AddMudServices();

        CultureInfo.CurrentUICulture = TestTranslations.De;
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        DefaultLocalizationInterceptor interceptor = Assert.IsType<DefaultLocalizationInterceptor>(scope.ServiceProvider.GetRequiredService<ILocalizationInterceptor>());
        Assert.True(interceptor.IgnoreDefaultEnglish);
        Assert.Equal("enthält", interceptor.Handle("MudDataGrid_Contains").Value);
        Assert.Single(services, d => d.ServiceType == typeof(ILocalizationInterceptor));
    }

    [Fact]
    public void LocalizerIsScoped()
    {
        ServiceCollection services = CreateServices();
        services.AddMudServices();
        services.AddTlumachMudBlazor();
        using ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        MudLocalizer localizer = first.ServiceProvider.GetRequiredService<MudLocalizer>();
        Assert.Same(localizer, first.ServiceProvider.GetRequiredService<TlumachMudLocalizer>());
        Assert.NotSame(localizer, second.ServiceProvider.GetRequiredService<MudLocalizer>());
    }

    [Fact]
    public void UsesDefaultManagerOfTlumachBlazorAndDefaultGroup()
    {
        ServiceCollection services = CreateServices();
        services.AddTlumachMudBlazor();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        TlumachMudLocalizer localizer = scope.ServiceProvider.GetRequiredService<TlumachMudLocalizer>();

        Assert.Same(_translations.Manager, localizer.TranslationManager);
        Assert.Equal(TlumachMudBlazorOptions.DefaultGroup, localizer.Group);
    }

    [Fact]
    public void ExplicitManagerAndGroupWin()
    {
        ServiceCollection services = CreateServices();
        services.AddTlumachMudBlazor(options =>
        {
            options.TranslationManager = _translations.PlainManager;
            options.Group = "Components";
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        TlumachMudLocalizer localizer = scope.ServiceProvider.GetRequiredService<TlumachMudLocalizer>();

        Assert.Same(_translations.PlainManager, localizer.TranslationManager);
        Assert.Equal("Components", localizer.Group);
    }

    [Fact]
    public void MissingManagerThrowsWhenTheLocalizerIsResolved()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IJSRuntime, NoJSRuntime>();
        services.AddTlumachMudBlazor();
        services.AddTlumachBlazor(options => options.Persistence = TlumachCulturePersistence.LocalStorage);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<MudLocalizer>());
        Assert.Contains("TranslationManager", exception.Message, StringComparison.Ordinal);
        Assert.Contains("DefaultManager", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistersTlumachBlazor()
    {
        ServiceCollection services = new();
        services.AddTlumachMudBlazor();

        Assert.Contains(services, d => d.ServiceType == typeof(TlumachCultureState));
        Assert.Contains(services, d => d.ServiceType == typeof(TlumachBlazorOptions));
    }

    [Fact]
    public void RepeatedCallConfiguresTheFirstOptions()
    {
        ServiceCollection services = new();
        services.AddTlumachMudBlazor();
        services.AddTlumachMudBlazor(options => options.Group = "Again");

        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(TlumachMudBlazorOptions));
        Assert.Equal("Again", Assert.IsType<TlumachMudBlazorOptions>(descriptor.ImplementationInstance).Group);
        Assert.Single(services, d => d.ServiceType == typeof(TlumachMudLocalizer));
    }

    [Fact]
    public void RejectsNullServices()
    {
        Assert.Throws<ArgumentNullException>(() => TlumachMudBlazorServiceCollectionExtensions.AddTlumachMudBlazor(null!));
    }

    private ServiceCollection CreateServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IJSRuntime, NoJSRuntime>();
        services.AddTlumachBlazor(options =>
        {
            options.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            options.DefaultManager = _translations.Manager;
            options.Persistence = TlumachCulturePersistence.LocalStorage;
        });
        return services;
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
