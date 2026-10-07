// <copyright file="DisplayNamesTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tlumach.AspNetCore.Mvc;
using Tlumach.AspNetCore.Testing;
using Tlumach.Extensions.Localization;
using Tlumach.MvcTests.Models;

namespace Tlumach.MvcTests;

public sealed class DisplayNamesTests : IDisposable
{
    private readonly CultureInfo _savedCulture = CultureInfo.CurrentCulture;
    private readonly TestTranslations _translations = new();
    private readonly CollectingLoggerProvider _logs = new();

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _savedCulture;
        _translations.Dispose();
        _logs.Dispose();
    }

    private static string DisplayName(IModelMetadataProvider metadata, Type type, string property)
        => metadata.GetMetadataForProperty(type, property).GetDisplayName();

    private IModelMetadataProvider Metadata(Action<TlumachDisplayNameOptions>? configure = null, bool registerLocalization = true)
    {
        ServiceCollection services = new();
        services.AddLogging(b => b.AddProvider(_logs).SetMinimumLevel(LogLevel.Debug));
        if (registerLocalization)
            services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);

        services.AddControllersWithViews().AddTlumachDisplayNames(configure);
        return services.BuildServiceProvider().GetRequiredService<IModelMetadataProvider>();
    }

    [Fact]
    public void TypeKey_IsUsed_InTheCurrentCulture()
    {
        IModelMetadataProvider metadata = Metadata();

        CultureInfo.CurrentCulture = TestTranslations.De;
        Assert.Equal("Ihr Alter", DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Age)));
        CultureInfo.CurrentCulture = TestTranslations.En;
        Assert.Equal("Your age", DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Age)));
    }

    [Fact]
    public void SharedKey_IsTheFallback()
    {
        IModelMetadataProvider metadata = Metadata();
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Equal("E-Mail-Adresse", DisplayName(metadata, typeof(ContactViewModel), nameof(ContactViewModel.Email)));
    }

    [Fact]
    public void NoKey_KeepsThePropertyName_AndLogsTheTriedKeys()
    {
        IModelMetadataProvider metadata = Metadata();
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Equal("Phone", DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Phone)));
        Assert.Contains(_logs.Messages, m => m.Contains("DisplayNames.Models.RegisterViewModel.Phone", StringComparison.Ordinal) && m.Contains("DisplayNames.Phone", StringComparison.Ordinal));
    }

    [Fact]
    public void DisplayAttribute_WinsOverTheConvention()
    {
        IModelMetadataProvider metadata = Metadata();

        Assert.Equal("Mail", DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Backup)));
    }

    [Fact]
    public void SameNamedNestedTypes_GetDistinctKeys()
    {
        TestTranslations translations = new(defaultArb: """
            {
                "@@locale": "en",
                "DisplayNames": {
                    "Title": "Title",
                    "Pages": {
                        "Movies": { "CreateModel": { "InputModel": { "Title": "Movie title" } } },
                        "Actors": { "CreateModel": { "InputModel": { "Title": "Stage name" } } }
                    }
                }
            }
            """);
        using (translations)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddTlumachLocalization(o => o.TranslationManager = translations.Manager);
            services.AddControllersWithViews().AddTlumachDisplayNames();
            IModelMetadataProvider metadata = services.BuildServiceProvider().GetRequiredService<IModelMetadataProvider>();
            CultureInfo.CurrentCulture = TestTranslations.En;

            Assert.Equal("Movie title", DisplayName(metadata, typeof(Pages.Movies.CreateModel.InputModel), "Title"));
            Assert.Equal("Stage name", DisplayName(metadata, typeof(Pages.Actors.CreateModel.InputModel), "Title"));
        }
    }

    [Fact]
    public void ContainerKey_OverridesTheKeyStyle()
    {
        IModelMetadataProvider metadata = Metadata(o => o.ContainerKey = _ => "Models.RegisterViewModel");
        CultureInfo.CurrentCulture = TestTranslations.En;

        Assert.Equal("Your e-mail", DisplayName(metadata, typeof(ContactViewModel), nameof(ContactViewModel.Email)));
    }

    [Fact]
    public void FailedManagerResolution_IsNotCached_AndIsRetried()
    {
        TlumachDisplayNameOptions? captured = null;
        IModelMetadataProvider metadata = Metadata(o => captured = o, registerLocalization: false);
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Throws<InvalidOperationException>(() => DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Age)));
        Assert.Throws<InvalidOperationException>(() => DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Age)));

        // The setup reads the options when it resolves the manager, so assigning the manager now makes the next lookup succeed.
        Assert.NotNull(captured);
        captured.TranslationManager = _translations.Manager;

        Assert.Equal("Ihr Alter", DisplayName(metadata, typeof(RegisterViewModel), nameof(RegisterViewModel.Age)));
    }
}
