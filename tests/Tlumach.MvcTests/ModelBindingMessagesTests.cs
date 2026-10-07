// <copyright file="ModelBindingMessagesTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Tlumach.AspNetCore.Mvc;
using Tlumach.Base;
using Tlumach.Extensions.Localization;

namespace Tlumach.MvcTests;

public sealed class ModelBindingMessagesTests : IDisposable
{
    private const string GermanMessages = """
        {
            "@@locale": "de",
            "ModelBinding": {
                "MissingBindRequiredValue": "DE1 {field}",
                "MissingKeyOrValue": "DE2",
                "MissingRequestBodyRequiredValue": "DE3",
                "ValueMustNotBeNull": "DE4 {value}",
                "AttemptedValueIsInvalid": "DE5 {value} {field}",
                "NonPropertyAttemptedValueIsInvalid": "DE6 {value}",
                "UnknownValueIsInvalid": "DE7 {field}",
                "NonPropertyUnknownValueIsInvalid": "DE8",
                "ValueIsInvalid": "DE9 {0}",
                "ValueMustBeANumber": "DE10 {0}",
                "NonPropertyValueMustBeANumber": "DE11"
            },
            "Errors": { "ValueIsInvalid": "Custom {value}" }
        }
        """;

    private readonly CultureInfo _savedCulture = CultureInfo.CurrentCulture;
    private readonly TestTranslations _translations = new(defaultArb: """{ "@@locale": "en", "x": "x" }""", germanArb: GermanMessages, textFormat: TextFormat.DotNet);

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _savedCulture;
        _translations.Dispose();
    }

    private DefaultModelBindingMessageProvider Messages(Action<TlumachModelBindingOptions>? configure = null, bool registerLocalization = true)
    {
        ServiceCollection services = new();
        services.AddLogging();
        if (registerLocalization)
            services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);

        services.AddControllersWithViews().AddTlumachModelBindingMessages(configure);
        return services.BuildServiceProvider().GetRequiredService<IOptions<MvcOptions>>().Value.ModelBindingMessageProvider;
    }

    [Fact]
    public void EveryAccessor_UsesItsKey_WithNamedAndPositionalValues()
    {
        DefaultModelBindingMessageProvider m = Messages();
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Equal("DE1 f", m.MissingBindRequiredValueAccessor("f"));
        Assert.Equal("DE2", m.MissingKeyOrValueAccessor());
        Assert.Equal("DE3", m.MissingRequestBodyRequiredValueAccessor());
        Assert.Equal("DE4 v", m.ValueMustNotBeNullAccessor("v"));
        Assert.Equal("DE5 v f", m.AttemptedValueIsInvalidAccessor("v", "f"));
        Assert.Equal("DE6 v", m.NonPropertyAttemptedValueIsInvalidAccessor("v"));
        Assert.Equal("DE7 f", m.UnknownValueIsInvalidAccessor("f"));
        Assert.Equal("DE8", m.NonPropertyUnknownValueIsInvalidAccessor());
        Assert.Equal("DE9 v", m.ValueIsInvalidAccessor("v"));
        Assert.Equal("DE10 f", m.ValueMustBeANumberAccessor("f"));
        Assert.Equal("DE11", m.NonPropertyValueMustBeANumberAccessor());
    }

    [Fact]
    public void MissingKey_FallsBackToTheMvcMessage()
    {
        DefaultModelBindingMessageProvider m = Messages();
        CultureInfo.CurrentCulture = TestTranslations.En;

        Assert.Equal("The value 'v' is not valid for f.", m.AttemptedValueIsInvalidAccessor("v", "f"));
        Assert.Equal("The field must be a number.", m.NonPropertyValueMustBeANumberAccessor());
    }

    [Fact]
    public void KeyPrefix_IsConfigurable()
    {
        DefaultModelBindingMessageProvider m = Messages(o => o.KeyPrefix = "Errors.");
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Equal("Custom v", m.ValueIsInvalidAccessor("v"));
    }

    [Fact]
    public void ExplicitManager_IsUsedWithoutAddTlumachLocalization()
    {
        DefaultModelBindingMessageProvider m = Messages(o => o.TranslationManager = _translations.Manager, registerLocalization: false);
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Equal("DE2", m.MissingKeyOrValueAccessor());
    }

    [Fact]
    public void WithoutAnyManager_TheFirstMessageNamesTheMissingCall()
    {
        DefaultModelBindingMessageProvider m = Messages(registerLocalization: false);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => m.MissingKeyOrValueAccessor());
        Assert.Contains("AddTlumachLocalization", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailedManagerResolution_IsNotCached_AndIsRetried()
    {
        TlumachModelBindingOptions? captured = null;
        DefaultModelBindingMessageProvider m = Messages(o => captured = o, registerLocalization: false);
        CultureInfo.CurrentCulture = TestTranslations.De;

        Assert.Throws<InvalidOperationException>(() => m.MissingKeyOrValueAccessor());
        Assert.Throws<InvalidOperationException>(() => m.MissingKeyOrValueAccessor());

        // The setup reads the options when it resolves the manager, so assigning the manager now makes the next call succeed.
        Assert.NotNull(captured);
        captured.TranslationManager = _translations.Manager;

        Assert.Equal("DE2", m.MissingKeyOrValueAccessor());
    }

    [Fact]
    public void RepeatedRegistration_AddsOneSetup_AndConfiguresTheSameOptions()
    {
        ServiceCollection services = new();
        services.AddLogging();
        IMvcBuilder mvc = services.AddControllersWithViews();
        TlumachModelBindingOptions? first = null;
        TlumachModelBindingOptions? second = null;

        mvc.AddTlumachModelBindingMessages(o => first = o);
        mvc.AddTlumachModelBindingMessages(o => second = o);

        Assert.Single(services, d => d.ServiceType == typeof(TlumachModelBindingOptions));
        Assert.Single(services, d => d.ServiceType == typeof(IConfigureOptions<MvcOptions>) && d.ImplementationType == typeof(TlumachModelBindingMessagesSetup));
        Assert.NotNull(first);
        Assert.Same(first, second);
    }
}
