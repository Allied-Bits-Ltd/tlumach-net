// <copyright file="StringCatParserTests.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "StringCat")]
    public class StringCatParserTests
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\StringCat";

        static StringCatParserTests()
        {
            StringCatParser.Use();
        }

        public StringCatParserTests()
        {
            StringCatParser.TextProcessingMode = TextFormat.Apple;
        }

        [Fact]
        public void ShouldLoadValidConfig()
        {
            var parser = new StringCatParser();
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("Strings.xcstrings", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("Hello"));
            Assert.True(tree.RootNode.Keys.ContainsKey("Welcome"));
        }

        [Fact]
        public void ShouldHaveCorrectKeyCount()
        {
            var parser = new StringCatParser();
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.Equal(2, tree.RootNode.Keys.Count);
        }

        [Fact]
        public void ShouldBeRegisteredForXcstringsExtension()
        {
            var parser = FileFormats.GetParser(".xcstrings");
            Assert.NotNull(parser);
            Assert.IsType<StringCatParser>(parser);
        }

        [Fact]
        public void ShouldGetKeyDefaultLanguage()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("en");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyGermanTranslation()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldGetKeySlovakTranslation()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToLanguagePrefix()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToLanguagePrefixViaParserDirectly()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "Strings.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("de-CH"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("Hello", out TranslationEntry? entry));
            Assert.Equal("Hallo", entry?.Text);
        }

        [Fact]
        public void ShouldLoadComment()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.Equal("A greeting", entry.Comment);
        }

        [Fact]
        public void ShouldDetectApplePlaceholder()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithPlaceholders.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("en");
            TranslationEntry entry = manager.GetValue("Greeting");
            Assert.True(entry.ContainsPlaceholders, "Greeting entry with %@ should be detected as containing placeholders");
        }

        [Fact]
        public void ShouldNotDetectPlaceholderInPlainEntry()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithPlaceholders.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("en");
            TranslationEntry entry = manager.GetValue("ItemLabel");
            Assert.False(entry.ContainsPlaceholders, "Plain entry should not be detected as containing placeholders");
        }

        [Fact]
        public void ShouldLoadPluralVariation()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithPlurals.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("ItemCount");
            Assert.False(string.IsNullOrEmpty(entry.Text));
        }

        [Fact]
        public void ShouldLoadAllKeysFromXcstrings()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "Strings.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.Equal(2, translation.Count);
            Assert.True(translation.TryGetValue("Hello", out _));
            Assert.True(translation.TryGetValue("Welcome", out _));
        }

        [Fact]
        public void ShouldHandleParserExtensionCheck()
        {
            var parser = new StringCatParser();
            Assert.True(parser.CanHandleExtension(".xcstrings"));
            Assert.True(parser.CanHandleExtension(".XCSTRINGS"));
            Assert.False(parser.CanHandleExtension(".json"));
            Assert.False(parser.CanHandleExtension(".arb"));
        }
    }
}
