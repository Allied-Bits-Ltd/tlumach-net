// <copyright file="ServiceCollectionExtensionsTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTlumachFluentValidation_InstallsAndRegistersTheLanguageManager()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        using ServiceProvider provider = new ServiceCollection()
            .AddTlumachFluentValidation(options =>
            {
                options.TranslationManager = manager;
                options.FluentValidationGroup = null;
                options.CultureSource = MessageCultureSource.TranslationManager;
            })
            .BuildServiceProvider();

        TlumachLanguageManager languageManager = Assert.IsType<TlumachLanguageManager>(ValidatorOptions.Global.LanguageManager);
        Assert.Same(languageManager, provider.GetRequiredService<TlumachLanguageManager>());
        Assert.Same(languageManager, provider.GetRequiredService<ILanguageManager>());
        Assert.Same(manager, languageManager.TranslationManager);
        Assert.Null(languageManager.FluentValidationGroup);
        Assert.Equal(MessageCultureSource.TranslationManager, languageManager.CultureSource);
    }

    [Fact]
    public void AddTlumachFluentValidation_InstallsTheDisplayNameResolverOnlyWhenAskedTo()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var defaultResolver = ValidatorOptions.Global.DisplayNameResolver;

        new ServiceCollection().AddTlumachFluentValidation(options => options.TranslationManager = manager);
        Assert.Same(defaultResolver, ValidatorOptions.Global.DisplayNameResolver);

        using ServiceProvider provider = new ServiceCollection()
            .AddTlumachFluentValidation(options =>
            {
                options.TranslationManager = manager;
                options.UseDisplayNameResolver = true;
                options.DisplayNamesGroup = "Names";
            })
            .BuildServiceProvider();

        Assert.NotSame(defaultResolver, ValidatorOptions.Global.DisplayNameResolver);
        Assert.Equal("Names", provider.GetRequiredService<TlumachDisplayNameResolver>().DisplayNamesGroup);
    }

    [Fact]
    public void AddTlumachFluentValidation_CalledTwice_ConfiguresTheOptionsOfTheFirstCall()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        TlumachFluentValidationOptions? first = null;

        ServiceCollection services = new();
        services
            .AddTlumachFluentValidation(options =>
            {
                first = options;
                options.TranslationManager = manager;
                options.UseDisplayNameResolver = true;
                options.FluentValidationGroup = "First";
                options.DisplayNamesGroup = "FirstNames";
                options.CultureSource = MessageCultureSource.TranslationManager;
            })
            .AddTlumachFluentValidation(options =>
            {
                // The second callback receives the options of the first call, so it needs to change only what differs.
                Assert.Same(first, options);
                Assert.Same(manager, options.TranslationManager);
                options.FluentValidationGroup = "Second";
                options.DisplayNamesGroup = "SecondNames";
            });

        // One registration of each service, so that neither the services nor the global options keep an instance of the first call.
        Assert.Single(services, d => d.ServiceType == typeof(TlumachLanguageManager));
        Assert.Single(services, d => d.ServiceType == typeof(ILanguageManager));
        Assert.Single(services, d => d.ServiceType == typeof(TlumachDisplayNameResolver));

        using ServiceProvider provider = services.BuildServiceProvider();
        TlumachLanguageManager languageManager = provider.GetRequiredService<TlumachLanguageManager>();
        TlumachDisplayNameResolver resolver = provider.GetRequiredService<TlumachDisplayNameResolver>();

        Assert.Equal("Second", languageManager.FluentValidationGroup);
        Assert.Equal(MessageCultureSource.TranslationManager, languageManager.CultureSource);
        Assert.Same(languageManager, provider.GetRequiredService<ILanguageManager>());
        Assert.Same(languageManager, ValidatorOptions.Global.LanguageManager);
        Assert.Equal("SecondNames", resolver.DisplayNamesGroup);
        Assert.Same(resolver, ValidatorOptions.Global.DisplayNameResolver.Target);
    }

    [Fact]
    public void AddTlumachFluentValidation_RepeatedCallTurnsTheResolverOff_RestoresTheEarlierResolver()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var defaultResolver = ValidatorOptions.Global.DisplayNameResolver;

        ServiceCollection services = new();
        services.AddTlumachFluentValidation(options =>
        {
            options.TranslationManager = manager;
            options.UseDisplayNameResolver = true;
        });
        Assert.NotSame(defaultResolver, ValidatorOptions.Global.DisplayNameResolver);

        services.AddTlumachFluentValidation(options => options.UseDisplayNameResolver = false);

        Assert.Same(defaultResolver, ValidatorOptions.Global.DisplayNameResolver);
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(TlumachDisplayNameResolver));
        Assert.Single(services, d => d.ServiceType == typeof(ILanguageManager));
    }

    [Fact]
    public void AddTlumachFluentValidation_RepeatedCallTurnsTheResolverOn_InstallsIt()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        ServiceCollection services = new();
        services.AddTlumachFluentValidation(options => options.TranslationManager = manager);
        services.AddTlumachFluentValidation(options => options.UseDisplayNameResolver = true);

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<TlumachDisplayNameResolver>(), ValidatorOptions.Global.DisplayNameResolver.Target);
    }

    [Fact]
    public void AddTlumachFluentValidation_OnSeparateServiceCollections_RegistersEachIndependently()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        ServiceCollection first = new();
        first.AddTlumachFluentValidation(options =>
        {
            options.TranslationManager = manager;
            options.FluentValidationGroup = "First";
        });

        // A repeated call is recognized per service collection; another collection starts with new options.
        ServiceCollection second = new();
        second.AddTlumachFluentValidation(options =>
        {
            Assert.Null(options.TranslationManager);
            options.TranslationManager = manager;
        });

        using ServiceProvider provider = second.BuildServiceProvider();
        Assert.Equal("FluentValidation", provider.GetRequiredService<TlumachLanguageManager>().FluentValidationGroup);
    }

    [Fact]
    public void AddTlumachFluentValidation_RequiresTranslationManager()
    {
        using var scope = new ValidatorOptionsScope();

        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTlumachFluentValidation(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTlumachFluentValidation(null!));
    }
}
