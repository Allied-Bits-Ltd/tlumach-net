// <copyright file="TlumachCultureStringLocalizerTests.cs" company="Allied Bits Ltd.">
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
using Microsoft.Extensions.Localization;

using Tlumach.Blazor;
using Tlumach.Extensions.Localization;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureStringLocalizerTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task GenericLocalizer_FollowsStateCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, configureServices: s => s.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager));
        IStringLocalizer<TlumachCultureStringLocalizerTests> localizer = ctx.Services.GetRequiredService<IStringLocalizer<TlumachCultureStringLocalizerTests>>();
        Assert.Equal("Hello", localizer["hello"].Value);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        Assert.Equal("Hallo", localizer["hello"].Value);
        Assert.Equal("Element 2 von 5", localizer["position", 2, 5].Value);
    }

    [Fact]
    public async Task NonGenericLocalizer_FollowsStateCulture()
    {
        await using BunitContext ctx = TestContexts.Create(
            _translations,
            configureServices: s => s.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager),
            initialCulture: TestTranslations.De);

        Assert.Equal("Hallo", ctx.Services.GetRequiredService<IStringLocalizer>()["hello"].Value);
    }

    [Fact]
    public async Task AddTlumachLocalizationAfterAddTlumachBlazor_StillFollowsStateCulture()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddLogging();
        ctx.Services.AddTlumachBlazor(o => o.SupportedCultures = [TestTranslations.En, TestTranslations.De]);
        ctx.Services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);
        System.Globalization.CultureInfo.CurrentUICulture = TestTranslations.En;
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        IStringLocalizer<TlumachCultureStringLocalizerTests> localizer = ctx.Services.GetRequiredService<IStringLocalizer<TlumachCultureStringLocalizerTests>>();

        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal("Hallo", localizer["hello"].Value);
    }

    [Fact]
    public async Task OtherLocalizerFactory_IsPassedThrough()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, configureServices: s => s.AddSingleton<IStringLocalizerFactory, FixedFactory>());

        Assert.Equal("fixed", ctx.Services.GetRequiredService<IStringLocalizer<TlumachCultureStringLocalizerTests>>()["anything"].Value);
    }

    private sealed class FixedFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new FixedLocalizer();

        public IStringLocalizer Create(string baseName, string location) => new FixedLocalizer();
    }

    private sealed class FixedLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, "fixed");

        public LocalizedString this[string name, params object[] arguments] => new(name, "fixed");

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
