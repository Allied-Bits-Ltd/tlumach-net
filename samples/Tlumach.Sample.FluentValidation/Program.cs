// <copyright file="Program.cs" company="Allied Bits Ltd.">
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
using System.Text;

using FluentValidation;

using Tlumach.FluentValidation;
using Tlumach.Sample.Validation;

Console.OutputEncoding = Encoding.UTF8;

ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(Strings.TranslationManager);
ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(Strings.TranslationManager).Resolve;

var customer = new Customer { Name = string.Empty, Email = "not-an-email", Nickname = "AVeryLongNickname", Discount = 1500m };
var validator = new CustomerValidator();

foreach (string cultureName in new[] { "en", "de", "uk" })
{
    // FluentValidation and the language manager follow CurrentUICulture, which is what ASP.NET Core request localization sets per request.
    CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
    CultureInfo.CurrentUICulture = culture;
    CultureInfo.CurrentCulture = culture;

    Console.WriteLine($"--- {culture.DisplayName} ---");
    foreach (var error in validator.Validate(customer).Errors)
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");

    Console.WriteLine();
}
