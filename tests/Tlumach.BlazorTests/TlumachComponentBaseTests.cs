// <copyright file="TlumachComponentBaseTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachComponentBaseTests : IDisposable
{
    private static readonly string[] ExpectedGerman = ["de-DE"];

    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task T_UsesStateCulture_AndReRenders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        var cut = Render(ctx);
        Assert.Equal("Hello", cut.Find("span").TextContent);

        await cut.InvokeAsync(() => ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));

        await cut.WaitForAssertionAsync(() => Assert.Equal("Hallo", cut.Find("span").TextContent));
    }

    [Fact]
    public async Task TFrom_AnonymousObject_FillsPlaceholders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = Render(ctx);

        Assert.Equal("Hello, Anna!", cut.Find("em").TextContent);
    }

    [Fact]
    public async Task T_WithWebEncodeValues_AttributeIsNotEncodedTwice()
    {
        _translations.Manager.WebEncodeValues = true;
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = Render(ctx);

        Assert.Equal("Click <b>here</b>", cut.Find("span").GetAttribute("title"));
    }

    [Fact]
    public async Task OnCultureChangedAsync_IsCalledOncePerSwitch()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = Render(ctx);

        await cut.InvokeAsync(() => state.SetCultureAsync(TestTranslations.De));
        await cut.WaitForAssertionAsync(() => Assert.Equal(ExpectedGerman, cut.Instance.CultureChanges, StringComparer.Ordinal));
        cut.Render();

        Assert.Equal(ExpectedGerman, cut.Instance.CultureChanges, StringComparer.Ordinal);
    }

    [Fact]
    public async Task DisposedComponent_LeavesNoSubscription()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = Render(ctx);
        ProbeComponent probe = cut.Instance;
        int renders = cut.RenderCount;

        await ctx.Renderer.DisposeComponents();
        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(renders, cut.RenderCount);
        Assert.Empty(probe.CultureChanges);
    }

    private IRenderedComponent<ProbeComponent> Render(BunitContext ctx)
        => ctx.Render<ProbeComponent>(p => p
            .Add(x => x.Hello, _translations.Hello)
            .Add(x => x.Greeting, _translations.Greeting)
            .Add(x => x.Rich, _translations.Rich));

    internal sealed class ProbeComponent : TlumachComponentBase
    {
        [Parameter]
        public BaseTranslationUnit Hello { get; set; } = default!;

        [Parameter]
        public BaseTranslationUnit Greeting { get; set; } = default!;

        [Parameter]
        public BaseTranslationUnit Rich { get; set; } = default!;

        public List<string> CultureChanges { get; } = [];

        protected override Task OnCultureChangedAsync(TlumachCulture culture)
        {
            CultureChanges.Add(culture.Culture.Name);
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "title", T(Rich));
            builder.AddContent(2, T(Hello));
            builder.CloseElement();
            builder.OpenElement(3, "em");
            builder.AddContent(4, TFrom(Greeting, new { name = "Anna" }));
            builder.CloseElement();
        }
    }
}
