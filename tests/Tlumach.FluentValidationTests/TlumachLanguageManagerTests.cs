// <copyright file="TlumachLanguageManagerTests.cs" company="Allied Bits Ltd.">
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

using FluentValidation;
using FluentValidation.Resources;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class TlumachLanguageManagerTests
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");
    private static readonly CultureInfo DeAt = CultureInfo.GetCultureInfo("de-AT");
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk");
    private static readonly CultureInfo GaIe = CultureInfo.GetCultureInfo("ga-IE"); // a culture that FluentValidation 12 has no translation for

    private static readonly LanguageManager Stock = new();

    [Fact]
    public void Step1_TlumachTextForCulture_Wins()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator", De));
        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator", DeAt));
    }

    [Fact]
    public void Step1_DefaultFileInSameLanguage_CountsAsCultureText()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", En));
        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", EnGb));
    }

    [Fact]
    public void Step2_BuiltInTextForCulture_WinsOverTlumachDefault()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        // German has no EmailValidator in Tlumach, FluentValidation has a German text.
        Assert.Equal(Stock.GetString("EmailValidator", De), languageManager.GetString("EmailValidator", De));

        // Ukrainian has no Tlumach file at all; FluentValidation's Ukrainian beats Tlumach's English default text.
        Assert.Equal(Stock.GetString("NotEmptyValidator", Uk), languageManager.GetString("NotEmptyValidator", Uk));
    }

    [Fact]
    public void Step2_EnglishCulture_UsesBuiltInEnglishForKeysMissingInTlumach()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal(Stock.GetString("LengthValidator", EnGb), languageManager.GetString("LengthValidator", EnGb));
    }

    [Fact]
    public void Step3_TlumachDefault_WhenNeitherHasTheCulture()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", GaIe));

        // A key that FluentValidation does not know: German falls back to the default text of Tlumach.
        Assert.Equal("Only in the default file.", languageManager.GetString("OnlyInDefault", De));
    }

    [Fact]
    public void Step4_BuiltInEnglish_WhenTlumachHasNothing()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal(Stock.GetString("LengthValidator", En), languageManager.GetString("LengthValidator", GaIe));
    }

    [Fact]
    public void UnknownKey_ReturnsEmptyString()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal(string.Empty, languageManager.GetString("NoSuchValidator", De));
    }

    [Fact]
    public void NullGroup_LooksUpRootKeys()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { FluentValidationGroup = null });

        Assert.Equal("'{PropertyName}' wird benötigt (Wurzel).", languageManager.GetString("NotEmptyValidator", De));
    }

    [Fact]
    public void EmptyGroup_LooksUpRootKeys()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { FluentValidationGroup = string.Empty });

        Assert.Equal("'{PropertyName}' is required (root).", languageManager.GetString("NotEmptyValidator", En));
    }

    [Fact]
    public void CultureProperty_OverridesCultureSource()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager) { Culture = De };
        CultureInfo.CurrentUICulture = En;

        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator"));
    }

    [Fact]
    public void ExplicitCulture_OverridesCultureProperty()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager) { Culture = De };

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", En));
    }

    [Fact]
    public void CurrentUICultureSource_FollowsCurrentUICulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator"));

        CultureInfo.CurrentUICulture = En;
        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator"));
    }

    [Fact]
    public void TranslationManagerSource_FollowsManagerCulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { CultureSource = MessageCultureSource.TranslationManager });
        CultureInfo.CurrentUICulture = En;

        manager.CurrentCulture = De;

        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator"));
    }

    [Fact]
    public void Disabled_UsesTlumachDefaultThenBuiltInEnglish()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager) { Enabled = false };

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", De));
        Assert.Equal(Stock.GetString("EmailValidator", En), languageManager.GetString("EmailValidator", De));
    }

    [Fact]
    public void Constructor_RejectsNullManagerAndUndefinedCultureSource()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Throws<ArgumentNullException>(() => new TlumachLanguageManager(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { CultureSource = (MessageCultureSource)42 }));
    }

    [Fact]
    public void Validation_FormatsTlumachTemplateWithFluentValidationPlaceholders()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();

        Assert.Equal("'Name' wird benötigt.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Validation_IcuFileKeepsApostrophesAndFluentValidationFillsPlaceholders()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateArbManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { FluentValidationGroup = null });
        CultureInfo.CurrentUICulture = En;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();

        Assert.Equal("'Name' must not be blank (ICU).", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Validation_ErrorCodeIsLookedUpInTheGroup()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEqual("Bob").WithErrorCode("CustomerNameCode");

        Assert.Equal("Der Kundenname 'Bob' ist nicht erlaubt.", Assert.Single(validator.Validate(new Customer { Name = "Bob" }).Errors).ErrorMessage);
    }

    [Fact]
    public void Validation_UnknownErrorCodeFallsBackToValidatorName()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithErrorCode("NoSuchCode");

        Assert.Equal("'Name' wird benötigt.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }
}
