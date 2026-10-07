// <copyright file="ViewLocalizationRegistrationTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Tlumach.AspNetCore.Mvc;
using Tlumach.Extensions.Localization;

namespace Tlumach.MvcTests;

public sealed class ViewLocalizationRegistrationTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    private ServiceCollection CreateServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);
        return services;
    }

    [Fact]
    public void WithoutAddTlumachLocalization_FactoryResolutionNamesTheMissingCall()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddControllersWithViews().AddTlumachViewLocalization();
        using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IHtmlLocalizerFactory>());
        Assert.Contains("AddTlumachLocalization", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TlumachRegistrationsWin_RegardlessOfTheOrderWithAddViewLocalization(bool tlumachFirst)
    {
        ServiceCollection services = CreateServices();
        IMvcBuilder mvc = services.AddControllersWithViews();
        if (tlumachFirst)
            mvc.AddTlumachViewLocalization().AddViewLocalization();
        else
            mvc.AddViewLocalization().AddTlumachViewLocalization();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<TlumachHtmlLocalizerFactory>(provider.GetRequiredService<IHtmlLocalizerFactory>());
        Assert.IsType<TlumachHtmlLocalizer<ViewLocalizationRegistrationTests>>(provider.GetRequiredService<IHtmlLocalizer<ViewLocalizationRegistrationTests>>());
        Assert.IsType<TlumachViewLocalizer>(provider.GetRequiredService<IViewLocalizer>());
        Assert.Single(provider.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value.ViewLocationExpanders.OfType<LanguageViewLocationExpander>());
    }

    [Fact]
    public void RepeatedCalls_ConfigureOneOptionsInstance_AndAddOneExpander()
    {
        ServiceCollection services = CreateServices();
        services.AddControllersWithViews().AddTlumachViewLocalization(o => o.ViewKeyPrefix = _ => "A.");
        services.AddRazorPages().AddTlumachViewLocalization(o => o.ViewLocationExpanderFormat = LanguageViewLocationExpanderFormat.SubFolder);

        Assert.Single(services, d => d.ServiceType == typeof(TlumachViewLocalizationOptions));
        using ServiceProvider provider = services.BuildServiceProvider();
        TlumachViewLocalizationOptions options = provider.GetRequiredService<TlumachViewLocalizationOptions>();
        Assert.Equal("A.", options.ViewKeyPrefix("x"));
        RazorViewEngineOptions razor = provider.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
        Assert.Single(razor.ViewLocationExpanders.OfType<LanguageViewLocationExpander>());
    }

    [Fact]
    public void NullExpanderFormat_AddsNoExpander()
    {
        ServiceCollection services = CreateServices();
        services.AddControllersWithViews().AddTlumachViewLocalization(o => o.ViewLocationExpanderFormat = null);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value.ViewLocationExpanders.OfType<LanguageViewLocationExpander>());
    }

    [Fact]
    public void ViewLocalizer_BeforeContextualize_Throws()
    {
        ServiceCollection services = CreateServices();
        services.AddControllersWithViews().AddTlumachViewLocalization();
        using ServiceProvider provider = services.BuildServiceProvider();
        IViewLocalizer localizer = provider.GetRequiredService<IViewLocalizer>();

        Assert.Throws<InvalidOperationException>(() => localizer["Title"]);
    }
}
