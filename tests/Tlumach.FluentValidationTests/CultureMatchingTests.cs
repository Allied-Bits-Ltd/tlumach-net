// <copyright file="CultureMatchingTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class CultureMatchingTests
{
    [Theory]
    [InlineData("en-GB", "en")]
    [InlineData("en", "en")]
    [InlineData("de-AT", "de")]
    [InlineData("zh-Hant-TW", "zh-Hant")]
    [InlineData("", "")]
    public void NeutralName_ReturnsTopmostNeutralCulture(string cultureName, string expected)
        => Assert.Equal(expected, CultureMatching.NeutralName(CultureInfo.GetCultureInfo(cultureName)));

    [Theory]
    [InlineData("en", true)]
    [InlineData("en-GB", true)]
    [InlineData("de", false)]
    [InlineData("", false)]
    public void IsEnglish_RecognizesEnglishCultures(string cultureName, bool expected)
        => Assert.Equal(expected, CultureMatching.IsEnglish(CultureInfo.GetCultureInfo(cultureName)));

    [Theory]
    [InlineData("en-GB", true)]
    [InlineData("en", true)]
    [InlineData("de", false)]
    public void DefaultFileSharesLanguage_ComparesWithDefaultFileLocale(string cultureName, bool expected)
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Equal(expected, CultureMatching.DefaultFileSharesLanguage(manager, CultureInfo.GetCultureInfo(cultureName)));
    }
}
