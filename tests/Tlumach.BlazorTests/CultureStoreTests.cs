// <copyright file="CultureStoreTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public class CultureStoreTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public async Task LocalStorage_Save_SetsItem()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.SetupVoid("localStorage.setItem", "tlumach.culture", "de-DE").SetVoidResult();
        LocalStorageCultureStore store = new(ctx.JSInterop.JSRuntime, new TlumachBlazorOptions());

        await store.SaveAsync(De);

        ctx.JSInterop.VerifyInvoke("localStorage.setItem");
    }

    [Fact]
    public async Task LocalStorage_Load_GetsItemWithConfiguredKey()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "my.key").SetResult("de-DE");
        LocalStorageCultureStore store = new(ctx.JSInterop.JSRuntime, new TlumachBlazorOptions { LocalStorageKey = "my.key" });

        Assert.Equal("de-DE", await store.LoadAsync());
    }

    [Fact]
    public async Task Cookie_Save_PostsToEndpoint()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.SetupVoid("fetch", _ => true).SetVoidResult();
        CookieCultureStore store = new(ctx.JSInterop.JSRuntime, ctx.Services.GetRequiredService<NavigationManager>(), new TlumachBlazorOptions());

        await store.SaveAsync(De);

        var invocation = ctx.JSInterop.VerifyInvoke("fetch");
        Assert.Equal("http://localhost/tlumach/culture?culture=de-DE", invocation.Arguments[0]);
        var init = Assert.IsType<Dictionary<string, string>>(invocation.Arguments[1]);
        Assert.Equal("POST", init["method"]);
        Assert.Equal("same-origin", init["credentials"]);
    }

    [Fact]
    public async Task Cookie_Load_ReadsHtmlLang()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Setup<string?>("document.documentElement.getAttribute", "lang").SetResult("de-DE");
        CookieCultureStore store = new(ctx.JSInterop.JSRuntime, ctx.Services.GetRequiredService<NavigationManager>(), new TlumachBlazorOptions());

        Assert.Equal("de-DE", await store.LoadAsync());
    }

    [Fact]
    public async Task Composite_Save_CallsEveryStore_Load_ReturnsFirstValue()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("document.documentElement.getAttribute", "lang").SetResult(null);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult("de-DE");
        TlumachBlazorOptions options = new();
        CompositeCultureStore store = new(
            new CookieCultureStore(ctx.JSInterop.JSRuntime, ctx.Services.GetRequiredService<NavigationManager>(), options),
            new LocalStorageCultureStore(ctx.JSInterop.JSRuntime, options));

        await store.SaveAsync(De);

        ctx.JSInterop.VerifyInvoke("fetch");
        ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        Assert.Equal("de-DE", await store.LoadAsync());
    }

    [Fact]
    public async Task Composite_Save_FailingStore_StillSavesToOthers_AndRethrowsFirstError()
    {
        RecordingStore first = new(new JSException("first failed"));
        RecordingStore second = new(null);
        RecordingStore third = new(new JSException("third failed"));
        CompositeCultureStore store = new(first, second, third);

        JSException ex = await Assert.ThrowsAsync<JSException>(async () => await store.SaveAsync(De));

        Assert.Equal("first failed", ex.Message);
        Assert.Equal(1, first.Saves);
        Assert.Equal(1, second.Saves);
        Assert.Equal(1, third.Saves);
    }

    [Theory]
    [InlineData(TlumachCulturePersistence.Cookie, typeof(CookieCultureStore))]
    [InlineData(TlumachCulturePersistence.LocalStorage, typeof(LocalStorageCultureStore))]
    [InlineData(TlumachCulturePersistence.Cookie | TlumachCulturePersistence.LocalStorage, typeof(CompositeCultureStore))]
    [InlineData(TlumachCulturePersistence.None, typeof(CompositeCultureStore))]
    public async Task Factory_CreatesStoreForPersistence(TlumachCulturePersistence persistence, Type expected)
    {
        await using BunitContext ctx = new();

        ITlumachCultureStore store = CultureStoreFactory.Create(ctx.Services, new TlumachBlazorOptions { Persistence = persistence });

        Assert.IsType(expected, store);
    }

    [Fact]
    public async Task Factory_None_LoadsNothingAndSavesNothing()
    {
        await using BunitContext ctx = new();
        ITlumachCultureStore store = CultureStoreFactory.Create(ctx.Services, new TlumachBlazorOptions { Persistence = TlumachCulturePersistence.None });

        await store.SaveAsync(De);

        Assert.Null(await store.LoadAsync());
        Assert.Empty(ctx.JSInterop.Invocations);
    }

    private sealed class RecordingStore : ITlumachCultureStore
    {
        private readonly Exception? _error;

        public RecordingStore(Exception? error)
        {
            _error = error;
        }

        public int Saves { get; private set; }

        public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<string?>(null);

        public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
        {
            Saves++;
            return _error is null ? ValueTask.CompletedTask : ValueTask.FromException(_error);
        }
    }
}
