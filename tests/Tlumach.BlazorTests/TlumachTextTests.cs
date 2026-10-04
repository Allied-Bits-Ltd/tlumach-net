// <copyright file="TlumachTextTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachTextTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task Renders_UnitText_InStateCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));

        Assert.Equal("Hallo", cut.Markup);
    }

    [Fact]
    public async Task ReRenders_WhenCultureChanges()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        Assert.Equal("Hello", cut.Markup);

        await cut.InvokeAsync(() => state.SetCultureAsync(TestTranslations.De));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Hallo", cut.Markup));
    }

    [Fact]
    public async Task Args_ReplaceNamedPlaceholders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p
            .Add(x => x.Unit, _translations.Greeting)
            .Add(x => x.Args, new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Anna" }));

        Assert.Equal("Hello, Anna!", cut.Markup);
    }

    [Fact]
    public async Task Values_ReplaceIndexedPlaceholders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        var cut = ctx.Render<TlumachText>(p => p
            .Add(x => x.Unit, _translations.Position)
            .Add(x => x.Values, new object[] { 2, 5 }));

        Assert.Equal("Element 2 von 5", cut.Markup);
    }

    [Fact]
    public async Task Text_IsHtmlEncoded()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Script));

        Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt;", cut.Markup);
        Assert.Empty(cut.FindAll("script"));
    }

    [Fact]
    public async Task WebEncodeValues_IsNotEncodedTwice()
    {
        _translations.Manager.WebEncodeValues = true;
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Rich));

        Assert.Equal("Click &lt;b&gt;here&lt;/b&gt;", cut.Markup);
        Assert.DoesNotContain("&amp;", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AsMarkup_RendersTrustedHtml()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Rich).Add(x => x.AsMarkup, true));

        Assert.Equal("here", cut.Find("b").TextContent);
    }

    [Fact]
    public async Task Key_UsesDefaultManager()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        var cut = ctx.Render<TlumachText>(p => p
            .Add(x => x.Key, "greeting")
            .Add(x => x.Args, new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "Anna" }));

        Assert.Equal("Hallo, Anna!", cut.Markup);
    }

    [Fact]
    public async Task Key_Missing_RendersNothing()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Key, "missing"));

        Assert.Equal(string.Empty, cut.Markup);
    }

    [Fact]
    public async Task NoUnitAndNoKey_Throws()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        Assert.Throws<InvalidOperationException>(() => ctx.Render<TlumachText>());
    }

    [Fact]
    public async Task KeyWithoutManager_Throws()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.DefaultManager = null);

        Assert.Throws<InvalidOperationException>(() => ctx.Render<TlumachText>(p => p.Add(x => x.Key, "hello")));
    }

    [Fact]
    public async Task TwoContexts_RenderTheirOwnCulture()
    {
        await using BunitContext first = TestContexts.Create(_translations);
        await using BunitContext second = TestContexts.Create(_translations);
        var firstCut = first.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        var secondCut = second.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));

        await Task.WhenAll(
            firstCut.InvokeAsync(() => first.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De)),
            secondCut.InvokeAsync(() => second.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.En)));

        await firstCut.WaitForAssertionAsync(() => Assert.Equal("Hallo", firstCut.Markup));
        Assert.Equal("Hello", secondCut.Markup);
    }

    [Fact]
    public async Task DisposedComponent_IsNotRenderedAgain()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        int renders = cut.RenderCount;

        await ctx.Renderer.DisposeComponents();
        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(renders, cut.RenderCount);
    }
}
