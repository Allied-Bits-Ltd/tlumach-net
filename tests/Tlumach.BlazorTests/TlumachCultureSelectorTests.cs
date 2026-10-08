// <copyright file="TlumachCultureSelectorTests.cs" company="Allied Bits Ltd.">
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

using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureSelectorTests : IDisposable
{
    private static readonly string?[] ExpectedValues = ["en-US", "de-DE"];

    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task Interactive_RendersSupportedCultures_WithCurrentSelected()
    {
        await using BunitContext ctx = Create(interactive: true);

        var cut = ctx.Render<TlumachCultureSelector>(p => p.AddUnmatched("class", "lang"));

        var options = cut.FindAll("option");
        Assert.Equal(ExpectedValues, options.Select(o => o.GetAttribute("value")), StringComparer.Ordinal);
        Assert.True(options[0].HasAttribute("selected"));
        Assert.Equal("lang", cut.Find("select").GetAttribute("class"));
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public async Task Interactive_Change_SwitchesCulture()
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>();

        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "de-DE" });

        Assert.Same(TestTranslations.De, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
        await cut.WaitForAssertionAsync(() => Assert.True(cut.FindAll("option")[1].HasAttribute("selected")));
    }

    [Theory]
    [InlineData("not-a-culture")]
    [InlineData("fr-FR")]
    public async Task Interactive_Change_IgnoresForgedOrUnsupportedValue(string forged)
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>();

        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = forged });

        Assert.Same(TestTranslations.En, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
    }

    [Fact]
    public async Task Interactive_ForceReload_ReloadsPage()
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>(p => p.Add(x => x.ForceReload, true));

        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "de-DE" });

        Assert.True(ctx.Services.GetRequiredService<BunitNavigationManager>().History.First().Options.ForceLoad);
    }

    [Fact]
    public async Task DisplayName_IsUsedForOptions()
    {
        await using BunitContext ctx = Create(interactive: true);

        var cut = ctx.Render<TlumachCultureSelector>(p => p.Add(x => x.DisplayName, (CultureInfo c) => c.Name.ToUpperInvariant()));

        Assert.Equal("DE-DE", cut.FindAll("option")[1].TextContent);
    }

    [Fact]
    public async Task Static_RendersGetFormToEndpoint()
    {
        await using BunitContext ctx = Create(interactive: false);
        ctx.Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/page?x=1");

        var cut = ctx.Render<TlumachCultureSelector>(p => p.Add(x => x.SubmitText, "Go"));

        var form = cut.Find("form");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal("http://localhost/tlumach/culture", form.GetAttribute("action"));
        Assert.Equal("/page?x=1", cut.Find("input[name=redirectUri]").GetAttribute("value"));
        Assert.Equal("culture", cut.Find("select").GetAttribute("name"));
        Assert.Equal("Go", cut.Find("button").TextContent);
    }

    [Fact]
    public async Task Static_EscapesARawNonAsciiQuery()
    {
        await using BunitContext ctx = Create(interactive: false);
        ctx.Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/page?q=привіт&r=a%20b");

        var cut = ctx.Render<TlumachCultureSelector>();

        // The culture endpoint returns only to a URL that can be written to the Location header, i.e. an ASCII one.
        Assert.Equal("/page?q=%D0%BF%D1%80%D0%B8%D0%B2%D1%96%D1%82&r=a%20b", cut.Find("input[name=redirectUri]").GetAttribute("value"));
    }

    [Fact]
    public async Task ReRenders_WhenCultureIsSwitchedElsewhere()
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>();

        await cut.InvokeAsync(() => ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));

        await cut.WaitForAssertionAsync(() => Assert.True(cut.FindAll("option")[1].HasAttribute("selected")));
    }

    private BunitContext Create(bool interactive)
    {
        BunitContext ctx = TestContexts.Create(_translations);
        ctx.SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        return ctx;
    }
}
