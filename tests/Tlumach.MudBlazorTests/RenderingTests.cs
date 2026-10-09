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

namespace Tlumach.MudBlazorTests;

/// <summary>
/// Renders real MudBlazor components (MudDataGrid with MudDataGridPager, MudTable with MudTablePager) and checks the texts they show.
/// </summary>
public sealed class RenderingTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task GermanUserSeesTranslatedTexts()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        IRenderedComponent<PagerHost> cut = ctx.Render<PagerHost>();

        Assert.Equal("Nächste Seite", NextPageLabel(cut, "grid"));
        Assert.Equal("Nächste Seite", NextPageLabel(cut, "table"));
        Assert.Contains("1-10 von 25", cut.Find("#grid").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingTranslationFallsBackToEnglishOfMudBlazor()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        IRenderedComponent<PagerHost> cut = ctx.Render<PagerHost>();

        // MudDataGridPager_PreviousPage and MudTablePager_PreviousPage are in neither translation.
        Assert.Equal("Previous page", PreviousPageLabel(cut, "grid"));
        Assert.Equal("Previous page", PreviousPageLabel(cut, "table"));
    }

    [Fact]
    public async Task EnglishUserSeesEnglishTexts()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.En);

        IRenderedComponent<PagerHost> cut = ctx.Render<PagerHost>();

        Assert.Equal("Next page", NextPageLabel(cut, "grid"));
        Assert.Contains("1-10 of 25", cut.Find("#grid").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UsersOfOneServerSeeTheirOwnLanguage()
    {
        // Two circuits of one server: both start with the culture of the server (en-US), and one user switches to German.
        await using BunitContext english = TestContexts.Create(_translations, TestTranslations.En);
        await using BunitContext german = TestContexts.Create(_translations, TestTranslations.En);
        IRenderedComponent<PagerHost> englishCut = english.Render<PagerHost>();
        IRenderedComponent<PagerHost> germanCut = german.Render<PagerHost>();

        await germanCut.InvokeAsync(() => german.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));
        englishCut.Render();

        await germanCut.WaitForAssertionAsync(() => Assert.Equal("Nächste Seite", NextPageLabel(germanCut, "grid")));
        Assert.Equal("Next page", NextPageLabel(englishCut, "grid"));

        // A later event of the German circuit runs with the culture that the circuit started with (en-US), as in Blazor Server; the texts must stay German.
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        await ClickNextPageAsync(germanCut, "grid");
        Assert.Contains("11-20 von 25", germanCut.Find("#grid").TextContent, StringComparison.Ordinal);
        Assert.Equal("Nächste Seite", NextPageLabel(germanCut, "grid"));
        Assert.Equal("Next page", NextPageLabel(englishCut, "grid"));
    }

    [Fact]
    public async Task CultureSwitchReRendersTheComponentsWithoutRecreatingThem()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.En);
        IRenderedComponent<PagerHost> cut = ctx.Render<PagerHost>();
        var gridPager = cut.FindComponent<global::MudBlazor.MudDataGridPager<int>>().Instance;
        var tablePager = cut.FindComponent<global::MudBlazor.MudTablePager>().Instance;
        Assert.Equal("Next page", NextPageLabel(cut, "grid"));

        await cut.InvokeAsync(() => ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));

        await cut.WaitForAssertionAsync(() =>
        {
            Assert.Equal("Nächste Seite", NextPageLabel(cut, "grid"));
            Assert.Equal("Nächste Seite", NextPageLabel(cut, "table"));
            Assert.Contains("1-10 von 25", cut.Find("#grid").TextContent, StringComparison.Ordinal);
        });
        Assert.Same(gridPager, cut.FindComponent<global::MudBlazor.MudDataGridPager<int>>().Instance);
        Assert.Same(tablePager, cut.FindComponent<global::MudBlazor.MudTablePager>().Instance);

        // Renders caused by later events run with the culture of the thread (still en-US) and must keep the language of the user.
        await ClickNextPageAsync(cut, "table");
        Assert.Contains("11-20", cut.Find("#table").TextContent, StringComparison.Ordinal);
        Assert.Equal("Nächste Seite", NextPageLabel(cut, "table"));
        Assert.Equal("Nächste Seite", NextPageLabel(cut, "grid"));
    }

    private static Task ClickNextPageAsync(IRenderedComponent<PagerHost> cut, string container)
        => cut.FindAll($"#{container} .mud-table-pagination-actions button")[2].ClickAsync(new());

    private static string? NextPageLabel(IRenderedComponent<PagerHost> cut, string container) => PagerButtonLabel(cut, container, index: 2);

    private static string? PreviousPageLabel(IRenderedComponent<PagerHost> cut, string container) => PagerButtonLabel(cut, container, index: 1);

    // The pagers render four buttons in this order: first, previous, next, last page.
    private static string? PagerButtonLabel(IRenderedComponent<PagerHost> cut, string container, int index)
        => cut.FindAll($"#{container} .mud-table-pagination-actions button")[index].GetAttribute("aria-label");
}
