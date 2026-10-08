// <copyright file="TlumachHtmlLocalizerFactoryTests.cs" company="Allied Bits Ltd.">
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
using System.Text.Encodings.Web;

using Microsoft.Extensions.DependencyInjection;

using Tlumach.AspNetCore.Mvc;
using Tlumach.Base;
using Tlumach.Extensions.Localization;

namespace Tlumach.MvcTests;

public sealed class TlumachHtmlLocalizerFactoryTests : IDisposable
{
    private readonly CultureInfo _savedCulture = CultureInfo.CurrentCulture;
    private readonly TestTranslations _translations = new();
    private readonly TestTranslations _other = new(defaultArb: """{ "@@locale": "en", "hello": "Hi from the other file" }""");

    public TlumachHtmlLocalizerFactoryTests() => CultureInfo.CurrentCulture = TestTranslations.En;

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _savedCulture;
        GeneratedLikeClass.Manager = null;
        _other.Dispose();
        _translations.Dispose();
    }

    private static TlumachHtmlLocalizerFactory CreateFactory(Action<TlumachLocalizationOptions> configureDefault, Action<TlumachSettingsProvider>? perContext = null)
    {
        ServiceCollection services = new();
        services.AddTlumachLocalization(configureDefault, perContext);
        ServiceProvider provider = services.BuildServiceProvider();
        return new TlumachHtmlLocalizerFactory(provider.GetRequiredService<ITlumachSettingsProvider>(), HtmlEncoder.Default, new TlumachViewLocalizationOptions());
    }

    private static TlumachHtmlLocalizerFactory CreateFactory(Func<TlumachLocalizationOptions> getOptions)
        => new(new NewOptionsProvider(getOptions), HtmlEncoder.Default, new TlumachViewLocalizationOptions());

    [Fact]
    public void Create_GeneratedClass_UsesItsManager_WhenOptionsHaveNoSource()
    {
        GeneratedLikeClass.Manager = _other.Manager;
        TlumachHtmlLocalizerFactory factory = CreateFactory(_ => { });

        Assert.Equal("Hi from the other file", TlumachHtmlLocalizerTests.Render(factory.Create(typeof(GeneratedLikeClass))["hello"]));
    }

    [Fact]
    public void Create_ContextOptions_WinOverDefault()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(
            o => o.TranslationManager = _translations.Manager,
            p => p.AddContext(typeof(GeneratedLikeClass).FullName!, new TlumachLocalizationOptions { TranslationManager = _other.Manager }));

        Assert.Equal("Hi from the other file", TlumachHtmlLocalizerTests.Render(factory.Create(typeof(GeneratedLikeClass))["hello"]));
        Assert.Equal("Hello", TlumachHtmlLocalizerTests.Render(factory.Create(typeof(TlumachHtmlLocalizerFactoryTests))["hello"]));
    }

    [Fact]
    public void Create_GeneratedClass_FailedCreationIsNotCached_AndIsRetried()
    {
        GeneratedLikeClass.Manager = null;
        TlumachHtmlLocalizerFactory factory = CreateFactory(_ => { });

        Assert.ThrowsAny<Exception>(() => factory.Create(typeof(GeneratedLikeClass)));

        GeneratedLikeClass.Manager = _other.Manager;
        Assert.Equal("Hi from the other file", TlumachHtmlLocalizerTests.Render(factory.Create(typeof(GeneratedLikeClass))["hello"]));
    }

    [Fact]
    public void Create_BaseNameAndLocation_UsesContextOptions()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(
            o => o.TranslationManager = _translations.Manager,
            p => p.AddContext("App.Strings", new TlumachLocalizationOptions { TranslationManager = _other.Manager }));

        Assert.Equal("Hi from the other file", TlumachHtmlLocalizerTests.Render(factory.Create("Strings", "App")["hello"]));
    }

    [Fact]
    public void GetEntry_CachesTheManagerPerOptionsInstance()
    {
        TlumachLocalizationOptions options = new() { Configuration = _translations.Configuration };
        TlumachHtmlLocalizerFactory factory = CreateFactory(o => o.TranslationManager = _translations.Manager);

        ManagerEntry first = factory.GetEntry(options);
        ManagerEntry second = factory.GetEntry(options);

        Assert.Same(first, second);
        Assert.NotSame(_translations.Manager, first.Manager);
    }

    [Fact]
    public void Create_ProviderReturnsNewOptionsWithTheSameConfiguration_CreatesOneManager()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { Configuration = _translations.Configuration });

        TranslationManager[] before = [.. TranslationManager.TranslationManagers];
        for (int i = 0; i < 5; i++)
            factory.Create(typeof(GeneratedLikeClass));

        TranslationManager[] created = [.. TranslationManager.TranslationManagers.Except(before)];
        foreach (TranslationManager manager in created)
            manager.Dispose();

        Assert.Single(created);
    }

    [Fact]
    public void GetEntry_ParallelCallsWithNewOptionsOfTheSameConfiguration_CreateOneManager()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(o => o.TranslationManager = _translations.Manager);
        ManagerEntry[] entries = new ManagerEntry[64];

        TranslationManager[] before = [.. TranslationManager.TranslationManagers];
        Parallel.For(0, entries.Length, i => entries[i] = factory.GetEntry(new TlumachLocalizationOptions { Configuration = _translations.Configuration }));

        TranslationManager[] created = [.. TranslationManager.TranslationManagers.Except(before)];
        foreach (TranslationManager manager in created)
            manager.Dispose();

        Assert.Single(created);
        Assert.All(entries, entry => Assert.Same(entries[0], entry));
    }

    [Fact]
    public void GetEntry_OptionsWithTheSameManager_ShareTheEntry()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(o => o.TranslationManager = _translations.Manager);

        ManagerEntry first = factory.GetEntry(new TlumachLocalizationOptions { TranslationManager = _translations.Manager });
        ManagerEntry second = factory.GetEntry(new TlumachLocalizationOptions { TranslationManager = _translations.Manager });

        Assert.Same(first, second);
        Assert.Same(_translations.Manager, first.Manager);
    }

    [Fact]
    public void GetEntry_OptionsWithTheSameDefaultFile_ShareTheEntry()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(o => o.TranslationManager = _translations.Manager);

        static TlumachLocalizationOptions Options() => new() { Assembly = typeof(TlumachHtmlLocalizerFactoryTests).Assembly, DefaultFile = "Strings.arb", DefaultFileLocale = "en" };

        ManagerEntry first = factory.GetEntry(Options());
        ManagerEntry second = factory.GetEntry(Options());
        ManagerEntry other = factory.GetEntry(new TlumachLocalizationOptions { Assembly = typeof(TlumachHtmlLocalizerFactoryTests).Assembly, DefaultFile = "Other.arb" });
        first.Manager.Dispose();
        other.Manager.Dispose();

        Assert.Same(first, second);
        Assert.NotSame(first, other);
    }

    [Fact]
    public void GetEntry_DifferentSourcesOrModes_GetDifferentEntries()
    {
        TlumachHtmlLocalizerFactory factory = CreateFactory(o => o.TranslationManager = _translations.Manager);

        ManagerEntry translations = factory.GetEntry(new TlumachLocalizationOptions { TranslationManager = _translations.Manager });
        ManagerEntry other = factory.GetEntry(new TlumachLocalizationOptions { TranslationManager = _other.Manager });
        ManagerEntry dotNet = factory.GetEntry(new TlumachLocalizationOptions { TranslationManager = _translations.Manager, TextProcessingMode = TextFormat.DotNet });

        Assert.NotSame(translations, other);
        Assert.NotSame(translations, dotNet);
        Assert.Same(_translations.Manager, dotNet.Manager);
    }

    [Fact]
    public void Create_ProviderChangesTheOptionsAtRuntime_UsesTheNewManager()
    {
        TranslationManager current = _translations.Manager;
        TlumachHtmlLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { TranslationManager = current });

        Assert.Equal("Hello", TlumachHtmlLocalizerTests.Render(factory.Create(typeof(GeneratedLikeClass))["hello"]));

        current = _other.Manager;
        Assert.Equal("Hi from the other file", TlumachHtmlLocalizerTests.Render(factory.Create(typeof(GeneratedLikeClass))["hello"]));
    }

    private static class GeneratedLikeClass
    {
        public static TranslationManager? Manager { get; set; }

        public static TranslationManager? TranslationManager => Manager;
    }

    // A custom provider that builds a new options object on every call.
    private sealed class NewOptionsProvider : ITlumachSettingsProvider
    {
        private readonly Func<TlumachLocalizationOptions> _getOptions;

        public NewOptionsProvider(Func<TlumachLocalizationOptions> getOptions) => _getOptions = getOptions;

        public TlumachLocalizationOptions GetOptionsFor(string context) => _getOptions();
    }
}
