// <copyright file="TlumachRuleBuilderExtensionsTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class TlumachRuleBuilderExtensionsTests
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");

    // Mimics the unit classes of Avalonia, WinUI and UWP: they derive from BaseTranslationUnit, not from Tlumach.TranslationUnit, and declare their own conversion to string.
    private sealed class PlatformLikeUnit : BaseTranslationUnit
    {
        public PlatformLikeUnit(TranslationManager manager, string key)
            : base(manager, manager.DefaultConfiguration!, key, containsPlaceholders: true)
        {
        }

        public static implicit operator string(PlatformLikeUnit unit) => unit?.CurrentTemplate ?? string.Empty;
    }

    private static string SingleMessage(InlineValidator<Customer> validator, Customer customer)
        => Assert.Single(validator.Validate(customer).Errors).ErrorMessage;

    [Fact]
    public void WithMessageUnit_IsReadAtValidationTimeAndFormattedByFluentValidation()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");

        CultureInfo.CurrentUICulture = En; // the rule is built in English ...
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(unit);

        CultureInfo.CurrentUICulture = De; // ... and validated in German
        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));

        CultureInfo.CurrentUICulture = En;
        Assert.Equal("'Name' must be at most 3 characters long. You entered 5.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void WithMessageManagerAndKey_IsReadAtValidationTime()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(manager, "Messages.NameTooLong");

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void WithMessagePlaceholders_FillsCustomValuesWithFormatAndLeavesTheRestToFluentValidation()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.DiscountLimit");

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Discount).LessThanOrEqualTo(1000m).WithMessage(unit, (customer, value, formatter) => formatter.AppendArgument("Limit", 1000m));

        CultureInfo.CurrentUICulture = En;
        CultureInfo.CurrentCulture = En;
        Assert.Equal("'Discount' must not exceed 1,000. You entered 1500.", SingleMessage(validator, new Customer { Discount = 1500m }));
    }

    [Fact]
    public void WithMessagePlaceholders_ManagerAndKeyOverload()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Discount).LessThanOrEqualTo(1000m).WithMessage(manager, "Messages.DiscountLimit", (customer, value, formatter) => formatter.AppendArgument("Limit", 1000m));

        CultureInfo.CurrentUICulture = De;
        CultureInfo.CurrentCulture = De;
        Assert.Equal("'Discount' darf 1.000 nicht überschreiten. Sie haben 1500 eingegeben.", SingleMessage(validator, new Customer { Discount = 1500m }));
    }

    [Fact]
    public void WithNameUnit_LocalizesTheDisplayNameAtValidationTime()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        using TranslationUnit name = TestTranslations.Unit(manager, "Messages.Nickname");

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Nickname).NotEmpty().WithName(name);

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Spitzname' wird benötigt.", SingleMessage(validator, new Customer()));

        CultureInfo.CurrentUICulture = En;
        Assert.Equal("'Nickname' is required.", SingleMessage(validator, new Customer()));
    }

    [Fact]
    public void WithNameManagerAndKey_LocalizesTheDisplayName()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithName(manager, "DisplayNames.Customer.Name");

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Kundenname' wird benötigt.", SingleMessage(validator, new Customer()));
    }

    [Fact]
    public void Helpers_FollowTheCulturePropertyOfTheLanguageManager()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager) { Culture = De };
        CultureInfo.CurrentUICulture = En;

        var validator = new InlineValidator<Customer>();
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(unit);

        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void Helpers_WithStockLanguageManager_FollowCurrentUICulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        // ValidatorOptions.Global.LanguageManager stays the stock one.
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(manager, "Messages.NameTooLong");

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void MissingText_ThrowsInvalidOperationExceptionNamingKeyAndCulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.Missing");
        validator.RuleFor(c => c.Name).NotEmpty().WithMessage(unit);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(new Customer()));
        Assert.Contains("Messages.Missing", ex.Message, StringComparison.Ordinal);
        Assert.Contains("'de'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TemplateRead_DoesNotFirePlaceholderValueNeeded()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");
        bool fired = false;
        unit.OnPlaceholderValueNeeded += (_, _) => fired = true;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(unit);
        validator.Validate(new Customer { Name = "Alice" });

        Assert.False(fired);
    }

    [Fact]
    public void Helpers_RejectNullArguments()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var validator = new InlineValidator<Customer>();

        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage((BaseTranslationUnit)null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage(manager, (string)null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithName((BaseTranslationUnit)null!));
    }

    [Fact]
    public void UnitClassWithItsOwnStringConversion_BindsToTheHelpersWithoutAmbiguity()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        var message = new PlatformLikeUnit(manager, "Messages.NameTooLong");
        var name = new PlatformLikeUnit(manager, "Messages.Nickname");

        CultureInfo.CurrentUICulture = En; // the rules are built in English ...
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(message);
        validator.RuleFor(c => c.Nickname).NotEmpty().WithName(name);

        CultureInfo.CurrentUICulture = De; // ... and validated in German
        string[] german = validator.Validate(new Customer { Name = "Alice" }).Errors.Select(e => e.ErrorMessage).ToArray();
        Assert.Equal(["'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", "'Spitzname' wird benötigt."], german);

        CultureInfo.CurrentUICulture = En;
        string[] english = validator.Validate(new Customer { Name = "Alice" }).Errors.Select(e => e.ErrorMessage).ToArray();
        Assert.Equal(["'Name' must be at most 3 characters long. You entered 5.", "'Nickname' is required."], english);
    }

    [Fact]
    public void Helpers_RejectNullPlaceholdersManagerAndKey()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");
        var validator = new InlineValidator<Customer>();

        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage(unit, null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage(manager, "Messages.NameTooLong", null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage((TranslationManager)null!, "Messages.NameTooLong"));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithName(manager, null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithName((TranslationManager)null!, "DisplayNames.Customer.Name"));
    }
}
