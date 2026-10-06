// <copyright file="TestDataTests.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.FluentValidationTests;

/// <summary>
/// Verifies that the embedded test data loads, so that a failure of a later test is not caused by the data.
/// </summary>
public class TestDataTests
{
    [Fact]
    public void JsonData_LoadsGroupedKeysForDefaultAndGerman()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Equal("'{PropertyName}' is required.", manager.GetValue("FluentValidation.NotEmptyValidator", CultureInfo.GetCultureInfo("en")).Text);
        Assert.Equal("'{PropertyName}' wird benötigt.", manager.GetValue("FluentValidation.NotEmptyValidator", CultureInfo.GetCultureInfo("de")).Text);
        Assert.Equal("Kundenname", manager.GetValue("DisplayNames.Customer.Name", CultureInfo.GetCultureInfo("de")).Text);
        Assert.Equal("en", manager.DefaultConfiguration!.DefaultFileLocale);
    }

    [Fact]
    public void ArbData_KeepsApostrophesInText()
    {
        using TranslationManager manager = TestTranslations.CreateArbManager();

        Assert.Equal("'{PropertyName}' must not be blank (ICU).", manager.GetValue("NotEmptyValidator", CultureInfo.GetCultureInfo("en")).Text);
    }
}
