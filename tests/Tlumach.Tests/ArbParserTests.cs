// <copyright file="ArbParserTests.cs" company="Allied Bits Ltd.">
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "BaseJson")]
    [Trait("Category", "Arb")]
    public class ArbParserTests
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\Arb";

        static ArbParserTests()
        {
            ArbParser.Use();
        }

        public ArbParserTests()
        {
            ArbParser.TextProcessingMode = TextFormat.Arb;
        }

        [Fact]
        public void ShouldLoadSimpleValidConfig()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SimpleValidConfig.arbcfg"));
            Assert.Equal("EmptyDefault.arb", manager.DefaultConfiguration?.DefaultFile);
        }

        [Fact]
        public void ShouldFailOnInvalidConfig()
        {
            TranslationManager manager;
            Assert.Throws<ParserFileException>(() => manager = new TranslationManager(Path.Combine(TestFilesPath, "SimpleInvalidConfig.arbcfg")));
        }

        [Fact]
        public void ShouldLoadValidConfigWithTranslations()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE-AT"), "de-AT translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE"), "de translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("other"), "translation for 'other' not found");
        }

        [Fact]
        public void ShouldLoadValidConfig()
        {
            ArbParser? parser = FileFormats.GetConfigParser(".arbcfg") as ArbParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("Strings.arb", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("Hello"));
        }

        [Fact]
        public void ShouldLoadValidConfigWithGroups()
        {
            ArbParser? parser = FileFormats.GetConfigParser(".arbcfg") as ArbParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("StringsWithGroups.arb", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("hello"));
            Assert.NotNull(tree.FindNode("ui"));
            Assert.True(tree.RootNode.ChildNodes.ContainsKey("logs"));
            TranslationTreeNode? node = tree.FindNode("logs.server");
            Assert.NotNull(node);
            Assert.True(node.Keys.ContainsKey("started"));
        }

        [Fact]
        public void ShouldFailOnValidConfigWithUnknownExt()
        {
            ArbParser? parser = FileFormats.GetConfigParser(".arbcfg") as ArbParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            Assert.Throws<ParserLoadException>(() => parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigUnknownExt.arbcfg"), TestFilesPath, out config));
        }

        [Fact]
        public void ShouldGetKeyDefaultLanguage()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFile()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFileWithoutPath()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            //manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-CH");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToDefaultLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("cz");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyFromGroup()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithGroups.arb", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("sk"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Started", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFileWithSearch()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocaleWithSearch()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-CH");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocaleForUnknownKey()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
        }

        [Fact]
        public void ShouldFallbackWithExplicitLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigExplicitLocale.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("DEDEStrings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
            entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.True(entry.Text?.StartsWith("Tsch", StringComparison.Ordinal));
        }

        [Fact]
        public void ShouldFallbackWithExplicitLocaleNoBasic()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigExplicitLocaleNoBasic.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("FRFRStrings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Servus", entry.Text);
            entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
            entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Au revoir", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRef()
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithRef.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("logs.server.started");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFileInResourceWithoutPath()
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            var manager = new TranslationManager(Assembly.GetExecutingAssembly(), "TestData\\Arb/ValidConfigWithTranslations.arbcfg");
            manager.LoadFromDisk = false;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFileInResourceWithPath()
        {
            var manager = new TranslationManager(Assembly.GetExecutingAssembly(), "TestData\\Arb/ValidConfigWithTranslations.arbcfg");
            manager.LoadFromDisk = false;
            manager.TranslationsDirectory = "TestData\\Arb";
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldLoadComplexARB()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithFeatures.arbcfg "));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithFeatures.arb", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("text", entry.Type);
            Assert.Equal("A message with a single parameter", entry.Description);
            Assert.NotNull(entry.Placeholders);
            Assert.NotEmpty(entry.Placeholders);
            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);

            Assert.Equal("userName", placeholder.Name);
            Assert.Equal("String", placeholder.Type);
            Assert.Equal("Bob", placeholder.Example);
        }

        [Fact]
        public void ShouldLoadComplexARB2()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithFeatures.arbcfg "));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithFeatures.arb", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("animals.nWombats");

            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("A plural message", entry.Description);
            Assert.NotNull(entry.Placeholders);
            Assert.NotEmpty(entry.Placeholders);
            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);

            Assert.Equal("count", placeholder.Name);
            Assert.Equal("num", placeholder.Type);
            Assert.Equal("compact", placeholder.Format);
        }

        [Fact]
        public void ShouldLoadComplexARB3()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithFeatures.arb", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("Hello");

            Assert.NotEqual(0, entry.Key.Length);

            Translation? translation = manager.GetTranslation(manager.CurrentCulture);

            Assert.NotNull(translation);

            Assert.True(translation.CustomProperties.ContainsKey("Custom"));
            Assert.Equal("Value", translation.CustomProperties["Custom"]);

            entry = manager.GetValue("Ref");
            Assert.NotEqual(0, entry.Key.Length);

            Assert.Equal("alt", entry.Target);
            Assert.Equal("ALT text", entry.Text);

            entry = manager.GetValue("Hello");
            Assert.NotEqual(0, entry.Key.Length);
            Assert.NotNull(entry.Placeholders);
            Placeholder? placeholder = entry.Placeholders.FirstOrDefault(p => p.Name.Equals("userName", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(placeholder);
            Assert.True(placeholder.Properties.ContainsKey("custom"));
            Assert.Equal("value", placeholder.Properties["custom"]);
            placeholder = entry.Placeholders.FirstOrDefault(p => p.Name.Equals("currentDate", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(placeholder);
            Assert.True(placeholder.OptionalParameters.ContainsKey("locale"));
            Assert.Equal("en_US", placeholder.OptionalParameters["locale"]);
        }

        [Fact]
        public void ShouldLoadComprehensiveFeatures()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("ComprehensiveFeatures.arb", manager.DefaultConfiguration?.DefaultFile);

            Translation? translation = manager.GetTranslation(manager.CurrentCulture, true);
            Assert.NotNull(translation);

            // Check file metadata
            Assert.Equal("en-US", translation.Locale);
            Assert.Equal("Test Suite", translation.Author);
            Assert.True(translation.CustomProperties.ContainsKey("version"));
            Assert.Equal("1.0.0", translation.CustomProperties["version"]);
            Assert.True(translation.CustomProperties.ContainsKey("timestamp"));
        }

        [Fact]
        public void ShouldLoadSimpleTextEntry()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("simpleText");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello, World!", entry.Text);
            Assert.Equal("text", entry.Type);
            Assert.Equal("A simple greeting message", entry.Description);
            Assert.Equal("greeting", entry.Context);
        }

        [Fact]
        public void ShouldLoadEntryWithSinglePlaceholder()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("greetingWithPlaceholder");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello, {name}!", entry.Text);
            Assert.NotNull(entry.Placeholders);
            Assert.Equal(1, entry.Placeholders.Count);

            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);
            Assert.Equal("name", placeholder.Name);
            Assert.Equal("String", placeholder.Type);
            Assert.Equal("Alice", placeholder.Example);
            Assert.True(placeholder.Properties.ContainsKey("custom"));
            Assert.Equal("user-input", placeholder.Properties["custom"]);
        }

        [Fact]
        public void ShouldLoadDateTimeFormattingPlaceholder()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("dateTimeExample");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Today is {currentDate}", entry.Text);
            Assert.NotNull(entry.Placeholders);
            Assert.Single(entry.Placeholders);

            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);
            Assert.Equal("currentDate", placeholder.Name);
            Assert.Equal("DateTime", placeholder.Type);
            Assert.Equal("yMMMMEEEEd", placeholder.Format);
            Assert.True(placeholder.OptionalParameters.ContainsKey("locale"));
            Assert.Equal("en_US", placeholder.OptionalParameters["locale"]);
            Assert.True(placeholder.OptionalParameters.ContainsKey("timezone"));
            Assert.Equal("UTC", placeholder.OptionalParameters["timezone"]);
        }

        [Fact]
        public void ShouldLoadNumberFormattingPlaceholder()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("numberFormat");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("You have {count, number} items", entry.Text);
            Assert.NotNull(entry.Placeholders);
            Assert.Single(entry.Placeholders);

            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);
            Assert.Equal("count", placeholder.Name);
            Assert.Equal("num", placeholder.Type);
            Assert.Equal("decimal", placeholder.Format);
        }

        [Fact]
        public void ShouldLoadPluralExample()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("pluralExample");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.True(entry.Text.Contains("plural"));
            Assert.NotNull(entry.Placeholders);
            Assert.Single(entry.Placeholders);

            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);
            Assert.Equal("count", placeholder.Name);
            Assert.Equal("num", placeholder.Type);
            Assert.Equal("compact", placeholder.Format);
        }

        [Fact]
        public void ShouldLoadSelectExample()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("selectGender");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.True(entry.Text.Contains("select"));
            Assert.NotNull(entry.Placeholders);
            Assert.Single(entry.Placeholders);

            Placeholder? placeholder = entry.Placeholders[0];
            Assert.NotNull(placeholder);
            Assert.Equal("gender", placeholder.Name);
            Assert.Equal("String", placeholder.Type);
        }

        [Fact]
        public void ShouldLoadComplexPluralWithMultiplePlaceholders()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("complexPlural");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.True(entry.Text.Contains("plural"));
            Assert.NotNull(entry.Placeholders);
            Assert.Equal(2, entry.Placeholders.Count);

            var countPlaceholder = entry.Placeholders.FirstOrDefault(p => p.Name == "count");
            Assert.NotNull(countPlaceholder);
            Assert.Equal("num", countPlaceholder.Type);

            var datePlaceholder = entry.Placeholders.FirstOrDefault(p => p.Name == "date");
            Assert.NotNull(datePlaceholder);
            Assert.Equal("DateTime", datePlaceholder.Type);
        }

        [Fact]
        public void ShouldLoadEntryWithMetadataPath()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("metadata.user.welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Welcome, {userName}!", entry.Text);
            Assert.Equal("user-management", entry.Context);
            Assert.Equal("Welcome message for users", entry.Description);
            Assert.NotNull(entry.Placeholders);
            Assert.Single(entry.Placeholders);
        }

        [Fact]
        public void ShouldLoadErrorMessageWithMultiplePlaceholders()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("metadata.system.error");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("System error: {errorCode} - {errorMessage}", entry.Text);
            Assert.Equal("error-handling", entry.Context);
            Assert.NotNull(entry.Placeholders);
            Assert.Equal(2, entry.Placeholders.Count);

            var codeParam = entry.Placeholders.FirstOrDefault(p => p.Name == "errorCode");
            Assert.NotNull(codeParam);
            Assert.Equal("num", codeParam.Type);

            var msgParam = entry.Placeholders.FirstOrDefault(p => p.Name == "errorMessage");
            Assert.NotNull(msgParam);
            Assert.Equal("String", msgParam.Type);
        }

        [Fact]
        public void ShouldLoadGermanTranslationWithComprehensiveFeatures()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de");

            TranslationEntry entry = manager.GetValue("simpleText");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo Welt!", entry.Text);

            entry = manager.GetValue("greetingWithPlaceholder");
            Assert.Equal("Hallo, {name}!", entry.Text);

            entry = manager.GetValue("pluralExample");
            Assert.True(entry.Text.Contains("Elemente"));

            entry = manager.GetValue("selectGender");
            Assert.True(entry.Text.Contains("Er"));

            entry = manager.GetValue("metadata.user.welcome");
            Assert.Equal("Willkommen, {userName}!", entry.Text);
        }

        [Fact]
        public void ShouldPreserveUntranslatableMarker()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComprehensiveFeatures.arbcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            TranslationEntry entry = manager.GetValue("x-untranslatable");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Not to be translated", entry.Text);
        }
    }
}
