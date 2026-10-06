// <copyright file="TestTranslations.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach.TemplateEngineTests;

/// <summary>
/// Creates translation managers over the embedded test data: an English default file and German and Ukrainian files, in the Arb (ICU) text processing mode.
/// </summary>
internal static class TestTranslations
{
    /// <summary>
    /// Creates a translation manager over the given configuration: by default the Arb (ICU) data, or <c>"TestData/Indexed.jsoncfg"</c> for the DotNet-mode data with indexed placeholders,
    /// which Arb mode does not accept.
    /// </summary>
    /// <param name="config">The embedded configuration file.</param>
    /// <returns>The translation manager, with English as its current culture.</returns>
    public static TranslationManager CreateManager(string config = "TestData/Strings.jsoncfg")
    {
        JsonParser.Use();
        return new TranslationManager(typeof(TestTranslations).Assembly, config)
        {
            LoadFromDisk = false,
            CurrentCulture = CultureInfo.GetCultureInfo("en"),
        };
    }

    /// <summary>
    /// Creates the unit that Generator would create for the key.
    /// </summary>
    /// <param name="manager">The translation manager that owns the unit.</param>
    /// <param name="key">The key of the translation.</param>
    /// <returns>The translation unit.</returns>
    public static TranslationUnit Unit(TranslationManager manager, string key)
        => new(manager, manager.DefaultConfiguration!, key, containsPlaceholders: true);
}
