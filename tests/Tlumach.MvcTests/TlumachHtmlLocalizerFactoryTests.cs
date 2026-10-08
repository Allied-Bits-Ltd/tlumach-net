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
using System.Reflection;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

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

    // The string localizers and the HTML localizers (and through them the views, model binding messages, and display names) must load a default file
    // without an assembly from the same place, so that GetString and the HTML localizer return the same text for a key.
    [Fact]
    public void DefaultFileWithoutAssembly_StringAndHtmlLocalizersLoadItFromTheEntryAssembly()
    {
        string file = "NoAssembly" + Guid.NewGuid().ToString("N") + ".arb";
        static TlumachLocalizationOptions Options(string file) => new() { DefaultFile = file };
        TlumachHtmlLocalizerFactory htmlFactory = CreateFactory(() => Options(file));
        TlumachStringLocalizerFactory stringFactory = new(new NewOptionsProvider(() => Options(file)));

        LocalizedHtmlString html = htmlFactory.Create(typeof(GeneratedLikeClass))["hello"];
        LocalizedString text = stringFactory.Create(typeof(GeneratedLikeClass))["hello"];

        TranslationManager[] managers = [.. TranslationManager.TranslationManagers.Where(manager => string.Equals(manager.DefaultConfiguration?.DefaultFile, file, StringComparison.Ordinal))];
        Assembly?[] assemblies = [.. managers.Select(manager => manager.DefaultConfiguration?.Assembly)];
        foreach (TranslationManager manager in managers)
            manager.Dispose();

        Assert.Equal(2, assemblies.Length);
        Assert.All(assemblies, assembly => Assert.Same(Assembly.GetEntryAssembly(), assembly));
        Assert.Equal(text.Value, TlumachHtmlLocalizerTests.Render(html));
        Assert.Equal(text.ResourceNotFound, html.IsResourceNotFound);
    }

    // As in ResourceManagerStringLocalizerFactory and MVC's ViewLocalizer, the location is the name of the assembly that holds the resources.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_BaseName_LocationNamesAnAssembly_LoadsTheFileFromThatAssembly(bool fullName)
    {
        string file = UniqueFile();
        Assembly expected = typeof(TranslationManager).Assembly;
        TlumachHtmlLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions());

        factory.Create(file, fullName ? expected.FullName! : expected.GetName().Name!);

        Assert.Same(expected, SingleManagerAssemblyOf(file));
    }

    // A location that is not the name of a loadable assembly (e.g. only a context of the options) falls back to the calling assembly, as in TlumachStringLocalizerFactory.
    [Theory]
    [InlineData("")]
    [InlineData("Some.Context.That.Is.Not.An.Assembly")]
    [InlineData("Not, An = Assembly, Name")]
    public void Create_BaseName_LocationIsNotAnAssembly_LoadsTheFileFromTheCallingAssembly(string location)
    {
        string file = UniqueFile();
        TlumachHtmlLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions());

        factory.Create(file, location);

        Assert.Same(typeof(TlumachHtmlLocalizerFactoryTests).Assembly, SingleManagerAssemblyOf(file));
    }

    [Fact]
    public void Create_BaseName_TheSameAssemblyByLocationOrByCaller_SharesOneManager()
    {
        string file = UniqueFile();
        Assembly self = typeof(TlumachHtmlLocalizerFactoryTests).Assembly;
        TlumachHtmlLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions());

        factory.Create(file, string.Empty);
        factory.Create(file, self.GetName().Name!);
        factory.Create(file, self.FullName!);
        factory.Create(file, "Some.Context.That.Is.Not.An.Assembly");
        factory.Create(file, typeof(TranslationManager).Assembly.GetName().Name!);

        TranslationManager[] managers = ManagersOf(file);
        foreach (TranslationManager manager in managers)
            manager.Dispose();

        Assert.Equal(2, managers.Length);
    }

    // Framework code calls the string factory: MVC's HtmlLocalizerFactory passes the location on, so the calling assembly is Microsoft.AspNetCore.Mvc.Localization,
    // which does not hold the file. The location names the application.
    [Fact]
    public void StringFactory_CalledByMvcHtmlLocalizerFactory_LoadsTheFileFromTheLocationAssembly()
    {
        string file = UniqueFile();
        Assembly self = typeof(TlumachHtmlLocalizerFactoryTests).Assembly;
        HtmlLocalizerFactory mvcFactory = new(new TlumachStringLocalizerFactory(new NewOptionsProvider(() => new TlumachLocalizationOptions())));

        mvcFactory.Create(file, self.GetName().Name!);

        Assert.Same(self, SingleManagerAssemblyOf(file));
    }

    private static string UniqueFile() => "Location" + Guid.NewGuid().ToString("N") + ".arb";

    private static TranslationManager[] ManagersOf(string file)
        => [.. TranslationManager.TranslationManagers.Where(manager => string.Equals(manager.DefaultConfiguration?.DefaultFile, file, StringComparison.Ordinal))];

    private static Assembly? SingleManagerAssemblyOf(string file)
    {
        TranslationManager manager = Assert.Single(ManagersOf(file));
        Assembly? assembly = manager.DefaultConfiguration?.Assembly;
        manager.Dispose();
        return assembly;
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
