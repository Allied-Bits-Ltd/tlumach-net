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

        // SampleComplete tests — cover all String Catalog format features

        [Fact]
        public void ShouldLoadSampleCompleteConfig()
        {
            var parser = new StringCatParser();
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("SampleComplete.xcstrings", config.DefaultFile);
            Assert.NotNull(config.Translations);
            Assert.Equal(4, config.Translations.Count);
            Assert.True(config.Translations.ContainsKey("EN"));
            Assert.True(config.Translations.ContainsKey("DE"));
            Assert.True(config.Translations.ContainsKey("FR"));
            Assert.True(config.Translations.ContainsKey("SK"));
        }

        [Fact]
        public void ShouldSampleCompleteTreeHaveAllKeys()
        {
            var parser = new StringCatParser();
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.Equal(15, tree.RootNode.Keys.Count);
        }

        [Fact]
        public void ShouldLoadSampleCompleteAppTitleEnglish()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("en");
            TranslationEntry entry = manager.GetValue("app.title");
            Assert.Equal("String Catalog Test App", entry.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteAppTitleGerman()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de");
            TranslationEntry entry = manager.GetValue("app.title");
            Assert.Equal("String-Catalog-Test-App", entry.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteAppTitleFrench()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("fr");
            TranslationEntry entry = manager.GetValue("app.title");
            Assert.Equal("Application de test du catalogue de chaînes", entry.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteAppTitleSlovak()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("app.title");
            Assert.Equal("Testovacia aplikácia String Catalog", entry.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteCommentForAppTitle()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("app.title");
            Assert.Equal("Application title shown in the window title and About panel.", entry.Comment);
        }

        [Fact]
        public void ShouldFallbackSampleCompleteDeAtToDe()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("app.title");
            Assert.Equal("String-Catalog-Test-App", entry.Text);
        }

        [Fact]
        public void ShouldDetectSampleCompletePlaceholderPercentAt()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("en");
            TranslationEntry entry = manager.GetValue("format.welcome.user");
            Assert.True(entry.ContainsPlaceholders, "format.welcome.user with %@ should be detected as containing placeholders");
        }

        [Fact]
        public void ShouldDetectSampleCompletePlaceholderPositional()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SampleComplete.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("en");
            TranslationEntry entry = manager.GetValue("format.download.progress");
            Assert.True(entry.ContainsPlaceholders, "format.download.progress with %1$lld should be detected as containing placeholders");
        }

        [Fact]
        public void ShouldLoadSampleCompletePluralOtherFormEnglish()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("items.count", out TranslationEntry? entry));
            Assert.Equal("%lld items", entry?.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompletePluralFrench()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("fr"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("items.count", out TranslationEntry? entry));
            Assert.Equal("%lld éléments", entry?.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteNonTranslatableBrandName()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));

            Translation? en = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Translation? de = parser.LoadTranslation(content, new CultureInfo("de"), TextFormat.Apple);
            Translation? fr = parser.LoadTranslation(content, new CultureInfo("fr"), TextFormat.Apple);

            Assert.True(en!.TryGetValue("brand.name", out TranslationEntry? enEntry));
            Assert.True(de!.TryGetValue("brand.name", out TranslationEntry? deEntry));
            Assert.True(fr!.TryGetValue("brand.name", out TranslationEntry? frEntry));
            Assert.Equal("Tlumach", enEntry?.Text);
            Assert.Equal("Tlumach", deEntry?.Text);
            Assert.Equal("Tlumach", frEntry?.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteStaleEntry()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("menu.legacy.import", out TranslationEntry? entry));
            Assert.Equal("Import Legacy File", entry?.Text);
        }

        [Fact]
        public void ShouldLoadSampleCompleteSubstitutionSingleToken()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("substitution.files.deleted", out TranslationEntry? entry));
            Assert.Equal("Deleted %#@FILES@.", entry?.Text);
            Assert.True(entry?.ContainsPlaceholders, "Substitution token %#@FILES@ should be detected as containing a placeholder");
        }

        [Fact]
        public void ShouldLoadSampleCompleteSubstitutionMultipleTokens()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("substitution.users.devices", out TranslationEntry? entry));
            Assert.Equal("Found %#@USERS@ using %#@DEVICES@.", entry?.Text);
            Assert.True(entry?.ContainsPlaceholders, "Substitution tokens %#@USERS@ and %#@DEVICES@ should be detected as containing placeholders");
        }

        [Fact]
        public void ShouldLoadSampleCompleteEscapeSequences()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.True(translation.TryGetValue("quote.escape.test", out TranslationEntry? entry));
            Assert.NotNull(entry?.Text);
            Assert.True(entry!.Text.Contains('"'), "Text should contain unescaped double quote");
            Assert.True(entry.Text.Contains('\\'), "Text should contain unescaped backslash");
            Assert.True(entry.Text.Contains('\n'), "Text should contain unescaped newline");
            Assert.True(entry.Text.Contains('\t'), "Text should contain unescaped tab");
            Assert.True(entry.Text.Contains("😀"), "Text should contain emoji");
        }

        [Fact]
        public void ShouldNotLoadSampleCompleteDeviceOnlyVariationKey()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.False(translation.TryGetValue("device.action.open", out _), "Device-only variation keys are not loaded by the parser");
        }

        [Fact]
        public void ShouldNotLoadSampleCompleteKeyMissingFromLocale()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("sk"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.False(translation.TryGetValue("button.save", out _), "button.save has no sk localization and should not be present");
        }

        [Fact]
        public void ShouldLoadSampleCompleteCorrectEnglishKeyCount()
        {
            var parser = new StringCatParser();
            string content = File.ReadAllText(Path.Combine(TestFilesPath, "SampleComplete.xcstrings"));
            Translation? translation = parser.LoadTranslation(content, new CultureInfo("en"), TextFormat.Apple);
            Assert.NotNull(translation);
            Assert.Equal(11, translation.Count);
        }

        [Fact]
        public void ShouldReplaceSubstitutionToken()
        {
            var entry = new TranslationEntry("key", "Deleted %#@FILES@.", escapedText: null, reference: null);
            string result = entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Apple, (key, _) => key == "FILES" ? (object)"3 files" : null);
            Assert.Equal("Deleted 3 files.", result);
        }

        [Fact]
        public void ShouldReplaceMultipleSubstitutionTokens()
        {
            var entry = new TranslationEntry("key", "Found %#@USERS@ using %#@DEVICES@.", escapedText: null, reference: null);
            string result = entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Apple,
                (key, _) => key == "USERS" ? (object)"2 users" : key == "DEVICES" ? "3 devices" : null);
            Assert.Equal("Found 2 users using 3 devices.", result);
        }

        [Fact]
        public void ShouldEmitSubstitutionTokenLiterallyWhenNoValueProvided()
        {
            var entry = new TranslationEntry("key", "Deleted %#@FILES@.", escapedText: null, reference: null);
            string result = entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.Apple, (_, _) => null);
            Assert.Equal("Deleted %#@FILES@.", result);
        }
    }
}
