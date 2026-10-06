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

using Tlumach.Base;

namespace Tlumach.FluentValidationTests;

/// <summary>
/// Creates translation managers over the embedded test data.
/// <list type="bullet">
/// <item>The JSON data (DotNet text processing mode) has an English default file and a German file.</item>
/// <item>The ARB data (ICU text processing mode) has an English default file only, with keys at the root.</item>
/// </list>
/// </summary>
internal static class TestTranslations
{
    public static TranslationManager CreateJsonManager()
    {
        JsonParser.Use();
        return new TranslationManager(typeof(TestTranslations).Assembly, "TestData/Json/Strings.jsoncfg") { LoadFromDisk = false };
    }

    public static TranslationManager CreateArbManager()
    {
        ArbParser.Use();
        return new TranslationManager(typeof(TestTranslations).Assembly, "TestData/Arb/Strings.arbcfg") { LoadFromDisk = false };
    }

    /// <summary>
    /// Creates the unit that Generator would create for the key, for example <c>Strings.Messages.NameTooLong</c> for <c>Messages.NameTooLong</c>.
    /// </summary>
    /// <param name="manager">The translation manager that owns the unit.</param>
    /// <param name="key">The key of the translation, with the group names separated by dots.</param>
    /// <returns>The translation unit.</returns>
    public static TranslationUnit Unit(TranslationManager manager, string key)
        => new(manager, manager.DefaultConfiguration!, key, containsPlaceholders: true);
}
