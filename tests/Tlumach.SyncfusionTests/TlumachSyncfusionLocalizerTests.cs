// <copyright file="TlumachSyncfusionLocalizerTests.cs" company="Allied Bits Ltd.">
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
using Tlumach.Syncfusion.Blazor;

namespace Tlumach.SyncfusionTests;

public sealed class TlumachSyncfusionLocalizerTests : IDisposable
{
    private readonly TestTranslations _translations = new();
    private readonly ServiceProvider _provider;

    public TlumachSyncfusionLocalizerTests() => _provider = TestServices.Create(_translations.Manager).BuildServiceProvider();

    public void Dispose()
    {
        _provider.Dispose();
        _translations.Dispose();
    }

    [Fact]
    public void ReturnsTranslationInCultureOfUser()
    {
        Assert.Equal("Keine Datensätze vorhanden", Create(TestTranslations.De).GetText("Grid_EmptyRecord"));
    }

    [Fact]
    public void ReturnsPlaceholdersUnchanged()
    {
        Assert.Equal("{0} von {1} Seiten", Create(TestTranslations.De).GetText("Pager_CurrentPageInfo"));
    }

    [Fact]
    public void EnglishUserGetsTextOfDefaultFile()
    {
        Assert.Equal("Find", Create(TestTranslations.En).GetText("Grid_Search"));
    }

    [Fact]
    public void MissingKeyFallsBackToBuiltInEnglish()
    {
        // Grid_ClearButton is in no translation; Syncfusion's built-in English text is "Clear".
        Assert.Equal("Clear", Create(TestTranslations.De).GetText("Grid_ClearButton"));
    }

    [Fact]
    public void EmptyTextCountsAsMissing()
    {
        Assert.Equal("Contains", Create(TestTranslations.De).GetText("Grid_Contains"));
    }

    [Fact]
    public void EmptyCultureTextDoesNotHideTheDefaultFile()
    {
        // German has an empty Grid_FilterButton, and the English default file of the application has "Apply filter", which must win over the built-in English "Filter".
        Assert.Equal("Apply filter", Create(TestTranslations.De).GetText("Grid_FilterButton"));
    }

    [Fact]
    public void KeyUnknownToSyncfusionReturnsNull()
    {
        // Some components test for null to use a hard-coded English text.
        Assert.Null(Create(TestTranslations.De).GetText("NoSuchComponent_NoSuchText"));
    }

    [Fact]
    public void ResourceManagerIsNull()
    {
        Assert.Null(Create(TestTranslations.De).ResourceManager);
    }

    [Fact]
    public void RejectsNullKey()
    {
        Assert.Throws<ArgumentNullException>(() => Create(TestTranslations.De).GetText(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void KeysAtRootAreFoundWithoutGroup(string? group)
    {
        TlumachSyncfusionLocalizer localizer = Create(TestTranslations.De, _translations.PlainManager, group);

        Assert.Equal("Keine Zeilen (plain)", localizer.GetText("Grid_EmptyRecord"));
        Assert.Null(localizer.Group);
    }

    [Fact]
    public void GroupedKeyIsNotFoundAtRoot()
    {
        // Built-in English, because the application set has Grid_EmptyRecord only in its Syncfusion group.
        Assert.Equal("No records to display", Create(TestTranslations.De, _translations.Manager, group: null).GetText("Grid_EmptyRecord"));
    }

    [Fact]
    public void PrimaryCultureTextWinsOverFallbackCultureText()
    {
        Assert.Equal("Keine Datensätze vorhanden", CreateWithFallback(TestTranslations.De).GetText("Grid_EmptyRecord"));
    }

    [Fact]
    public void FallbackCultureTextWinsOverPrimaryDefaultText()
    {
        Assert.Equal("Suchen (offiziell)", CreateWithFallback(TestTranslations.De).GetText("Grid_Search"));
    }

    [Fact]
    public void PrimaryDefaultTextWinsOverFallbackDefaultText()
    {
        Assert.Equal("Apply filter", CreateWithFallback(TestTranslations.De).GetText("Grid_FilterButton"));
    }

    [Fact]
    public void FallbackDefaultTextIsUsedWhenPrimaryLacksTheKey()
    {
        // Grid_EndsWith is repeated with the same value in the official-format file, which the ResX parser accepts.
        Assert.Equal("Ends With (official)", CreateWithFallback(TestTranslations.De).GetText("Grid_EndsWith"));
    }

    [Fact]
    public void EmptyPrimaryTextFallsBackToFallbackSet()
    {
        Assert.Equal("Enthält (offiziell)", CreateWithFallback(TestTranslations.De).GetText("Grid_Contains"));
    }

    [Fact]
    public void FallbackSetLackingTheKeyFallsBackToBuiltInEnglish()
    {
        Assert.Equal("Clear", CreateWithFallback(TestTranslations.De).GetText("Grid_ClearButton"));
    }

    [Fact]
    public void FallbackGroupIsNullWhenEmpty()
    {
        TlumachCultureState state = TestServices.CreateCultureState(_provider, TestTranslations.De);
        TlumachSyncfusionLocalizer localizer = new(_translations.Manager, TlumachSyncfusionBlazorOptions.DefaultGroup, _translations.OfficialManager, string.Empty, state);

        Assert.Null(localizer.FallbackGroup);
        Assert.Same(_translations.OfficialManager, localizer.FallbackTranslationManager);
    }

    [Fact]
    public void ConstructorRejectsNullArguments()
    {
        TlumachCultureState state = TestServices.CreateCultureState(_provider, TestTranslations.De);

        Assert.Throws<ArgumentNullException>(() => new TlumachSyncfusionLocalizer(null!, null, null, null, state));
        Assert.Throws<ArgumentNullException>(() => new TlumachSyncfusionLocalizer(_translations.Manager, null, null, null, null!));
    }

    private TlumachSyncfusionLocalizer Create(CultureInfo culture) => Create(culture, _translations.Manager, TlumachSyncfusionBlazorOptions.DefaultGroup);

    private TlumachSyncfusionLocalizer Create(CultureInfo culture, TranslationManager manager, string? group)
        => new(manager, group, fallbackManager: null, fallbackGroup: null, TestServices.CreateCultureState(_provider, culture));

    private TlumachSyncfusionLocalizer CreateWithFallback(CultureInfo culture)
        => new(_translations.Manager, TlumachSyncfusionBlazorOptions.DefaultGroup, _translations.OfficialManager, fallbackGroup: null, TestServices.CreateCultureState(_provider, culture));
}
