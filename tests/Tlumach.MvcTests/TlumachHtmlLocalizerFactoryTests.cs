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

    private static class GeneratedLikeClass
    {
        public static TranslationManager? Manager { get; set; }

        public static TranslationManager? TranslationManager => Manager;
    }
}
