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
    public void AddTlumachFluentValidation_RequiresTranslationManager()
    {
        using var scope = new ValidatorOptionsScope();

        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTlumachFluentValidation(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTlumachFluentValidation(null!));
    }
}
