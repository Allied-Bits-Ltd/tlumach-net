// <copyright file="LoadTlumachCultureTests.cs" company="Allied Bits Ltd.">
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
using Microsoft.JSInterop;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class LoadTlumachCultureTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task StoredCulture_IsAppliedGlobally()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult("de-DE");

        CultureInfo culture = await ctx.Services.LoadTlumachCultureAsync();

        Assert.Same(TestTranslations.De, culture);
        Assert.Same(TestTranslations.De, CultureInfo.DefaultThreadCurrentUICulture);
        Assert.Equal("de-DE", _translations.Manager.CurrentCulture.Name);
    }

    [Fact]
    public async Task CookiePersistence_ReadsHtmlLang()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.Cookie);
        ctx.JSInterop.Setup<string?>("document.documentElement.getAttribute", "lang").SetResult("de-DE");

        Assert.Same(TestTranslations.De, await ctx.Services.LoadTlumachCultureAsync());
    }

    [Fact]
    public async Task NothingStored_FallsBackToDefaultCulture()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult(null);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Same(TestTranslations.De, await ctx.Services.LoadTlumachCultureAsync());
    }

    [Fact]
    public async Task UnsupportedStoredCulture_IsIgnored()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult("fr-FR");
        CultureInfo.CurrentUICulture = TestTranslations.En;

        Assert.Same(TestTranslations.En, await ctx.Services.LoadTlumachCultureAsync());
    }

    [Fact]
    public async Task StoreFailure_FallsBack()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetException(new JSException("unavailable"));
        CultureInfo.CurrentUICulture = TestTranslations.En;

        Assert.Same(TestTranslations.En, await ctx.Services.LoadTlumachCultureAsync());
    }

    private static BunitContext CreateContext(TlumachCulturePersistence persistence)
    {
        BunitContext ctx = new();
        ctx.Services.AddLogging();
        ctx.Services.AddTlumachBlazor(o =>
        {
            o.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            o.DefaultCulture = TestTranslations.De;
            o.Persistence = persistence;
        });
        return ctx;
    }
}
