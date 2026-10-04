// <copyright file="TlumachCultureStateTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureStateTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task InitialCulture_FollowsCurrentUICulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        Assert.Same(TestTranslations.De, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
    }

    [Fact]
    public async Task InitialCulture_Unsupported_FallsBackToDefaultCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.DefaultCulture = TestTranslations.De, initialCulture: CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Same(TestTranslations.De, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
    }

    [Fact]
    public async Task SetCultureAsync_ChangesCultureAndSnapshot()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        await state.SetCultureAsync(CultureInfo.GetCultureInfo("de-DE"));

        Assert.Same(TestTranslations.De, state.Culture);
        Assert.Same(TestTranslations.De, state.Current.Culture);
        Assert.Equal("Hallo", state.Current.Get(_translations.Hello));
    }

    [Fact]
    public async Task SetCultureAsync_RaisesCultureChangedOnce_AndNotForSameCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        List<string> raised = [];
        state.CultureChanged += (_, e) => raised.Add(e.Culture.Name);

        await state.SetCultureAsync(TestTranslations.De);
        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(new[] { "de-DE" }, raised);
    }

    [Fact]
    public async Task SetCultureAsync_UnsubscribedHandler_IsNotCalled()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        int calls = 0;
        void Handler(object? sender, CultureChangedEventArgs e) => calls++;
        state.CultureChanged += Handler;
        state.CultureChanged -= Handler;

        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SetCultureAsync_Unsupported_Throws()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        await Assert.ThrowsAsync<ArgumentException>(() => state.SetCultureAsync(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public async Task SetCultureAsync_SavesToStore()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        var invocation = ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        Assert.Equal(new object?[] { "tlumach.culture", "de-DE" }, invocation.Arguments);
    }

    [Fact]
    public async Task SetCultureAsync_StoreFailure_DoesNotThrow()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, configureServices: s => s.AddScoped<ITlumachCultureStore, ThrowingStore>());
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        await state.SetCultureAsync(TestTranslations.De);

        Assert.Same(TestTranslations.De, state.Culture);
    }

    [Fact]
    public async Task SetCultureAsync_ForceReload_SavesAndReloadsCurrentPage()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        BunitNavigationManager navigation = ctx.Services.GetRequiredService<BunitNavigationManager>();
        navigation.NavigateTo("/page?x=1");

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De, forceReload: true);

        ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        NavigationHistory last = navigation.History.First();
        Assert.Equal("http://localhost/page?x=1", last.Uri);
        Assert.True(last.Options.ForceLoad);
    }

    [Fact]
    public async Task ApplyCultureGlobally_True_ChangesDefaultsAndManagers()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.ApplyCultureGlobally = true);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        Assert.Same(TestTranslations.De, CultureInfo.DefaultThreadCurrentUICulture);
        Assert.Same(TestTranslations.De, CultureInfo.DefaultThreadCurrentCulture);
        Assert.Equal("de-DE", _translations.Manager.CurrentCulture.Name);
    }

    [Fact]
    public async Task ApplyCultureGlobally_False_LeavesProcessWideCultureAlone()
    {
        using GlobalCultureScope scope = new();
        CultureInfo? before = CultureInfo.DefaultThreadCurrentUICulture;
        string managerCulture = _translations.Manager.CurrentCulture.Name;
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.ApplyCultureGlobally = false);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        Assert.Same(before, CultureInfo.DefaultThreadCurrentUICulture);
        Assert.Equal(managerCulture, _translations.Manager.CurrentCulture.Name);
    }

    [Fact]
    public async Task TwoScopes_SwitchConcurrently_StayIsolated()
    {
        await using BunitContext first = TestContexts.Create(_translations);
        await using BunitContext second = TestContexts.Create(_translations, initialCulture: TestTranslations.De);
        TlumachCultureState firstState = first.Services.GetRequiredService<TlumachCultureState>();
        TlumachCultureState secondState = second.Services.GetRequiredService<TlumachCultureState>();

        await Task.WhenAll(
            Task.Run(() => firstState.SetCultureAsync(TestTranslations.De)),
            Task.Run(() => secondState.SetCultureAsync(TestTranslations.En)));

        Assert.Equal("Hallo", firstState.Current.Get(_translations.Hello));
        Assert.Equal("Hello", secondState.Current.Get(_translations.Hello));
    }

    private sealed class ThrowingStore : ITlumachCultureStore
    {
        public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default) => throw new JSException("load failed");

        public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default) => throw new JSException("save failed");
    }
}
