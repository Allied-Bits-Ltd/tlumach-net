// <copyright file="TlumachMudLocalizerTests.cs" company="Allied Bits Ltd.">
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
using Microsoft.Extensions.Localization;

using MudBlazor;

using Tlumach.Blazor;
using Tlumach.MudBlazor;

namespace Tlumach.MudBlazorTests;

public sealed class TlumachMudLocalizerTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public void ReturnsTranslationInCultureOfUser()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        LocalizedString result = GetLocalizer(ctx)["MudDataGrid_Contains"];

        Assert.Equal("enthält", result.Value);
        Assert.False(result.ResourceNotFound);
        Assert.Equal("MudDataGrid_Contains", result.Name);
    }

    [Fact]
    public void EnglishCultureStillUsesTranslation()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.En);

        Assert.Equal("includes", GetLocalizer(ctx)["MudDataGrid_Contains"].Value);
    }

    [Fact]
    public void MissingKeyReportsResourceNotFound()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        LocalizedString result = GetLocalizer(ctx)["MudTablePager_PreviousPage"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("MudTablePager_PreviousPage", result.Value);
    }

    [Fact]
    public void MissingKeyWithArgumentsReportsResourceNotFound()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        LocalizedString result = GetLocalizer(ctx)["MudTablePager_InfoFormat", "1", "10", "25"];

        Assert.True(result.ResourceNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void KeysAtRootAreFoundWithoutGroup(string? group)
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De, options =>
        {
            options.TranslationManager = _translations.PlainManager;
            options.Group = group;
        });

        TlumachMudLocalizer localizer = GetLocalizer(ctx);

        Assert.Equal("enthält (plain)", localizer["MudDataGrid_Contains"].Value);
        Assert.Null(localizer.Group);
    }

    [Fact]
    public void GroupedKeyIsNotFoundAtRoot()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De, options => options.Group = null);

        Assert.True(GetLocalizer(ctx)["MudDataGrid_Contains"].ResourceNotFound);
    }

    [Fact]
    public void FormatsArgumentsInCultureOfUser()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        LocalizedString result = GetLocalizer(ctx)["MudDataGridPager_InfoFormat", "1", "10", 1234.5];

        Assert.Equal("1-10 von 1234,5", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void MalformedTranslationReportsResourceNotFound()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        LocalizedString result = GetLocalizer(ctx)["Broken", "a", "b"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("Broken", result.Value);
    }

    [Fact]
    public void EmptyArgumentsReturnTextUnformatted()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.De);

        Assert.Equal("{0}-{1} von {2}", GetLocalizer(ctx)["MudDataGridPager_InfoFormat", []].Value);
    }

    [Fact]
    public async Task FollowsLiveSwitchWhateverTheCultureOfTheThread()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.En);
        TlumachMudLocalizer localizer = GetLocalizer(ctx);
        Assert.Equal("includes", localizer["MudDataGrid_Contains"].Value);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        // The switch changes the culture only inside SetCultureAsync; the thread keeps the culture that the circuit started with.
        Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("enthält", localizer["MudDataGrid_Contains"].Value);
    }

    [Fact]
    public void ConstructorRejectsNullArguments()
    {
        using BunitContext ctx = TestContexts.Create(_translations, TestTranslations.En);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        Assert.Throws<ArgumentNullException>(() => new TlumachMudLocalizer(null!, "MudBlazor", state));
        Assert.Throws<ArgumentNullException>(() => new TlumachMudLocalizer(_translations.Manager, "MudBlazor", null!));
    }

    private static TlumachMudLocalizer GetLocalizer(BunitContext ctx) => Assert.IsType<TlumachMudLocalizer>(ctx.Services.GetRequiredService<MudLocalizer>());
}
