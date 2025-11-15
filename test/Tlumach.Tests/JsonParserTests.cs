using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach;
using Tlumach.Base;

namespace Tlumach.Tests
{

#pragma warning disable MA0011 // Use the overload that includes CultureInfo
#pragma warning disable CA1304 // Also Use the overload that includes CultureInfo

    [Trait("Category", "Parser")]
    [Trait("Category", "BaseJson")]
    [Trait("Category", "Json")]

    public class JsonParserTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\Json";

        static JsonParserTests()
        {
            JsonParser.Use();
        }

        [Fact]
        public void ShouldLoadSimpleValidConfig()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SimpleValidConfig.jsoncfg"));
            Assert.Equal("EmptyDefault.json", manager.DefaultConfiguration?.DefaultFile);
        }

        [Fact]
        public void ShouldLoadSimpleValidConfigWithSpaces()
        {
            var manager = new TranslationManager(" " + Path.Combine(TestFilesPath, "SimpleValidConfigWithSpaces.jsoncfg "));
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
        }

        [Fact]
        public void ShouldFailOnInvalidConfig()
        {
            TranslationManager manager;
            Assert.Throws<ParserFileException>(() => manager = new TranslationManager(Path.Combine(TestFilesPath, "SimpleInvalidConfig.jsoncfg")));
        }

        [Fact]
        public void ShouldLoadValidConfigWithTranslations()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.jsoncfg"));
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE-AT"), "de-AT translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE"), "de translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("other"), "default translation not found");
        }

        [Fact]
        public void ShouldLoadValidConfig()
        {
            JsonParser? parser = FileFormats.GetParser(".json") as JsonParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("Strings.json", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("Hello"));
        }

        [Fact]
        public void ShouldLoadValidConfigWithGroups()
        {
            JsonParser? parser = FileFormats.GetParser(".json") as JsonParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigWithGroups.jsoncfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("StringsWithGroups.json", config.DefaultFile);
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
            JsonParser? parser = FileFormats.GetParser(".json") as JsonParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            Assert.Throws<ParserLoadException>(() => parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigUnknownExt.jsoncfg"), TestFilesPath, out config));
        }

        [Fact]
        public void ShouldGetKeyDefaultLanguage()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFile()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-CH");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToDefaultLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("cz");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyFromGroup()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithGroups.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithGroups.json", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("sk"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Started", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFileWithSearch()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocaleWithSearch()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.json", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-CH");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRef()
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithRef.jsoncfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("logs.server.started");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }
    }
}
