// <copyright file="TlumachSyncfusionBlazorServiceCollectionExtensionsTests.cs" company="Allied Bits Ltd.">
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

public sealed class TlumachSyncfusionBlazorServiceCollectionExtensionsTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReplacesLocalizerInAnyOrderWithAddSyncfusionBlazor(bool syncfusionFirst)
    {
        ServiceCollection services = TestServices.Create(_translations.Manager);
        if (syncfusionFirst)
            services.AddSyncfusionBlazor();

        services.AddTlumachSyncfusionBlazor();

        if (!syncfusionFirst)
            services.AddSyncfusionBlazor();

        CultureInfo.CurrentUICulture = TestTranslations.De;
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        ISyncfusionStringLocalizer localizer = scope.ServiceProvider.GetRequiredService<ISyncfusionStringLocalizer>();
        Assert.IsType<TlumachSyncfusionLocalizer>(localizer);
        Assert.Equal("Keine Datensätze vorhanden", localizer.GetText("Grid_EmptyRecord"));
        Assert.Single(services, d => d.ServiceType == typeof(ISyncfusionStringLocalizer));
    }

    [Fact]
    public void LocalizerIsScoped()
    {
        ServiceCollection services = TestServices.Create(_translations.Manager);
        services.AddSyncfusionBlazor();
        services.AddTlumachSyncfusionBlazor();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        ISyncfusionStringLocalizer localizer = first.ServiceProvider.GetRequiredService<ISyncfusionStringLocalizer>();
        Assert.Same(localizer, first.ServiceProvider.GetRequiredService<TlumachSyncfusionLocalizer>());
        Assert.NotSame(localizer, second.ServiceProvider.GetRequiredService<ISyncfusionStringLocalizer>());
    }

    [Fact]
    public void UsesDefaultManagerOfTlumachBlazorAndDefaultGroups()
    {
        ServiceCollection services = TestServices.Create(_translations.Manager);
        services.AddTlumachSyncfusionBlazor();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        TlumachSyncfusionLocalizer localizer = scope.ServiceProvider.GetRequiredService<TlumachSyncfusionLocalizer>();

        Assert.Same(_translations.Manager, localizer.TranslationManager);
        Assert.Equal(TlumachSyncfusionBlazorOptions.DefaultGroup, localizer.Group);
        Assert.Null(localizer.FallbackTranslationManager);
        Assert.Null(localizer.FallbackGroup);
    }

    [Fact]
    public void ExplicitOptionsWin()
    {
        ServiceCollection services = TestServices.Create(_translations.Manager);
        services.AddTlumachSyncfusionBlazor(options =>
        {
            options.TranslationManager = _translations.PlainManager;
            options.Group = "Components";
            options.FallbackTranslationManager = _translations.OfficialManager;
            options.FallbackGroup = "Official";
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        TlumachSyncfusionLocalizer localizer = scope.ServiceProvider.GetRequiredService<TlumachSyncfusionLocalizer>();

        Assert.Same(_translations.PlainManager, localizer.TranslationManager);
        Assert.Equal("Components", localizer.Group);
        Assert.Same(_translations.OfficialManager, localizer.FallbackTranslationManager);
        Assert.Equal("Official", localizer.FallbackGroup);
    }

    [Fact]
    public void MissingManagerThrowsWhenTheLocalizerIsResolved()
    {
        ServiceCollection services = TestServices.Create(defaultManager: null);
        services.AddTlumachSyncfusionBlazor();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ISyncfusionStringLocalizer>());
        Assert.Contains("TranslationManager", exception.Message, StringComparison.Ordinal);
        Assert.Contains("DefaultManager", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistersTlumachBlazor()
    {
        ServiceCollection services = new();
        services.AddTlumachSyncfusionBlazor();

        Assert.Contains(services, d => d.ServiceType == typeof(TlumachCultureState));
        Assert.Contains(services, d => d.ServiceType == typeof(TlumachBlazorOptions));
    }

    [Fact]
    public void RepeatedCallConfiguresTheFirstOptions()
    {
        ServiceCollection services = new();
        services.AddTlumachSyncfusionBlazor();
        services.AddTlumachSyncfusionBlazor(options => options.Group = "Again");

        ServiceDescriptor descriptor = Assert.Single(services, d => d.ServiceType == typeof(TlumachSyncfusionBlazorOptions));
        Assert.Equal("Again", Assert.IsType<TlumachSyncfusionBlazorOptions>(descriptor.ImplementationInstance).Group);
        Assert.Single(services, d => d.ServiceType == typeof(TlumachSyncfusionLocalizer));
        Assert.Single(services, d => d.ServiceType == typeof(ISyncfusionStringLocalizer));
    }

    [Fact]
    public void RejectsNullServices()
    {
        Assert.Throws<ArgumentNullException>(() => TlumachSyncfusionBlazorServiceCollectionExtensions.AddTlumachSyncfusionBlazor(null!));
    }
}
