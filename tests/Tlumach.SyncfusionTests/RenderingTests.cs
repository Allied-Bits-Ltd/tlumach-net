// <copyright file="RenderingTests.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.SyncfusionTests;

/// <summary>
/// Renders a real SfGrid without rows and checks the text of its empty row (Grid_EmptyRecord). No Syncfusion license key is needed.
/// </summary>
public sealed class RenderingTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task GermanUserSeesTranslatedText()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        IRenderedComponent<GridHost> cut = ctx.Render<GridHost>();

        Assert.Equal("Keine Datensätze vorhanden", EmptyRowText(cut));
    }

    [Fact]
    public async Task CultureWithoutTranslationSeesEnglishOfSyncfusion()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, CultureInfo.GetCultureInfo("fr-FR"));

        IRenderedComponent<GridHost> cut = ctx.Render<GridHost>();

        Assert.Equal("No records to display", EmptyRowText(cut));
    }

    [Fact]
    public async Task FallbackSetProvidesTextsMissingInTheApplication()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De, options =>
        {
            options.TranslationManager = _translations.PlainManager;
            options.Group = "NoSuchGroup";
            options.FallbackTranslationManager = _translations.OfficialManager;
        });

        IRenderedComponent<GridHost> cut = ctx.Render<GridHost>();

        Assert.Equal("Keine Datensätze (offiziell)", EmptyRowText(cut));
    }

    [Fact]
    public async Task UsersOfOneServerSeeTheirOwnLanguage()
    {
        // Two circuits of one server: both start with the culture of the server (en-US), and one user switches to German before the grid is rendered.
        await using BunitContext english = TestContexts.Create(_translations, TestTranslations.En);
        await using BunitContext german = TestContexts.Create(_translations, TestTranslations.En);
        await german.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        IRenderedComponent<GridHost> englishCut = english.Render<GridHost>();
        IRenderedComponent<GridHost> germanCut = german.Render<GridHost>();

        // The thread still has the culture that both circuits started with; each localizer follows the culture of its own user.
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("Keine Datensätze vorhanden", EmptyRowText(germanCut));
        Assert.Equal("No records to display", EmptyRowText(englishCut));
    }

    [Fact]
    public async Task CultureSwitchReRendersTheGridWithoutRecreatingIt()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.En);
        IRenderedComponent<GridHost> cut = ctx.Render<GridHost>();
        var grid = cut.FindComponent<global::Syncfusion.Blazor.Grids.SfGrid<GridHost.Row>>().Instance;
        Assert.Equal("No records to display", EmptyRowText(cut));

        await cut.InvokeAsync(() => ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Keine Datensätze vorhanden", EmptyRowText(cut)));
        Assert.Same(grid, cut.FindComponent<global::Syncfusion.Blazor.Grids.SfGrid<GridHost.Row>>().Instance);
    }

    private static string EmptyRowText(IRenderedComponent<GridHost> cut) => cut.Find("#grid tr.e-emptyrow td").TextContent.Trim();
}
