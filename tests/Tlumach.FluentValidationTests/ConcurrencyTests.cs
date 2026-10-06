// <copyright file="ConcurrencyTests.cs" company="Allied Bits Ltd.">
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

public class ConcurrencyTests
{
    [Fact]
    public async Task ParallelValidationInDifferentCultures_GetsTheMessagesOfTheirOwnCulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();
        validator.RuleFor(c => c.Email).EmailAddress();
        using TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");
        validator.RuleFor(c => c.Nickname).MaximumLength(3).WithMessage(unit);

        var customer = new Customer { Email = "not-an-email", Nickname = "Alice" };
        var stock = new global::FluentValidation.Resources.LanguageManager();

        string[] expectedEn =
        [
            "'Name' is required.",
            stock.GetString("EmailValidator", CultureInfo.GetCultureInfo("en")).Replace("{PropertyName}", "Email", StringComparison.Ordinal),
            "'Nickname' must be at most 3 characters long. You entered 5.",
        ];
        string[] expectedDe =
        [
            "'Name' wird benötigt.",
            stock.GetString("EmailValidator", CultureInfo.GetCultureInfo("de")).Replace("{PropertyName}", "Email", StringComparison.Ordinal),
            "'Nickname' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.",
        ];

        Task<bool>[] tasks = Enumerable.Range(0, 200).Select(i => Task.Run(() =>
        {
            bool german = i % 2 == 0;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(german ? "de" : "en");
            string[] actual = validator.Validate(customer).Errors.Select(e => e.ErrorMessage).ToArray();
            return actual.SequenceEqual(german ? expectedDe : expectedEn);
        })).ToArray();

        bool[] results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.True);
    }
}
