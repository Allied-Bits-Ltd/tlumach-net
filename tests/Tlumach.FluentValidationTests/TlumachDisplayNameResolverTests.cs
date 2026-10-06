// <copyright file="TlumachDisplayNameResolverTests.cs" company="Allied Bits Ltd.">
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

public class TlumachDisplayNameResolverTests
{
    [Fact]
    public void Resolve_PrefersTypeQualifiedKeyThenMemberKey()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();
        validator.RuleFor(c => c.Email).NotEmpty();

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
        string[] messages = validator.Validate(new Customer()).Errors.Select(e => e.ErrorMessage).ToArray();

        Assert.Equal(["'Kundenname' wird benötigt.", "'E-Mail-Adresse' wird benötigt."], messages);
    }

    [Fact]
    public void Resolve_PrefersMemberKeyOfTheCultureOverTypeQualifiedKeyOfTheDefaultFile()
    {
        // DisplayNames.Customer.Discount is only in the default (English) file, DisplayNames.Discount is in the German file.
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Discount).NotEmpty();

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
        Assert.Equal("'Rabatt' wird benötigt.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        Assert.Equal("'Customer discount' is required.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Resolve_PrefersTypeQualifiedKeyWhenBothKeysAreInTheCulture()
    {
        // The German file has both DisplayNames.Customer.Name and DisplayNames.Name.
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        var resolver = new TlumachDisplayNameResolver(manager);

        System.Reflection.PropertyInfo? member = typeof(Customer).GetProperty(nameof(Customer.Name));
        Assert.NotNull(member);

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
        Assert.Equal("Name (Mitglied)", manager.GetValue("DisplayNames.Name", CultureInfo.GetCultureInfo("de")).Text);
        Assert.Equal("Kundenname", resolver.Resolve(typeof(Customer), member, null!));
    }

    [Fact]
    public void Resolve_ReturnsNullForUnknownMember_SoFluentValidationDefaultApplies()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Nickname).NotEmpty();

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        Assert.Equal("'Nickname' is required.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void WithName_OverridesTheResolver()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithName("Override");

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        Assert.Equal("'Override' is required.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Resolve_WithEmptyGroup_LooksUpRootKeys()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var resolver = new TlumachDisplayNameResolver(manager, displayNamesGroup: null);

        System.Reflection.PropertyInfo? member = typeof(Customer).GetProperty(nameof(Customer.Name));
        Assert.NotNull(member);

        Assert.Null(resolver.Resolve(typeof(Customer), member, null!));
    }

    [Fact]
    public void Resolve_NullMember_ReturnsNull()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Null(new TlumachDisplayNameResolver(manager).Resolve(typeof(Customer), null!, null!));
    }
}
