using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "KeyValue")]
    [Trait("Category", "Ini")]
    public class IniParserTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\Ini";

        static IniParserTests()
        {
            IniParser.Use();
        }

        [Theory]
        [InlineData("ConfigInvalidJunk.cfg")]
        [InlineData("ConfigInvalidKey.cfg")]
        [InlineData("ConfigInvalidSection.cfg")]
        [InlineData("ConfigInvalidSectionName.cfg")]
        [InlineData("ConfigInvalidSeparator.cfg")]
        public void ShouldFailOnInvalidConfig(string configFile)
        {
            Assert.Throws<ParserFileException>(() => new TranslationManager(Path.Combine(TestFilesPath, configFile)));
        }

        [Theory]
        [InlineData("ConfigInvalidJunk.cfg", 3, 6)]
        [InlineData("ConfigInvalidKey.cfg", 2, 8)]
        [InlineData("ConfigInvalidSection.cfg", 4, 9)]
        [InlineData("ConfigInvalidSectionName.cfg", 4, 2)]
        [InlineData("ConfigInvalidSeparator.cfg", 2, 13)]
        public void ShouldFailOnInvalidConfigWithPositionCheck(string configFile, int lineNumber, int columnNumber)
        {
            try
            {
                _ = new TranslationManager(Path.Combine(TestFilesPath, configFile));
                Assert.Fail("An exception has not been thrown");
            }
            catch(Exception ex)
            {
                Assert.True(ex is ParserFileException);
                Assert.NotNull(ex.InnerException);
                Assert.True(ex.InnerException is TextParseException);
                TextParseException? tex = ex.InnerException as TextParseException;
                Assert.NotNull(tex);
                Assert.Equal(lineNumber, tex.LineNumber);
                Assert.Equal(columnNumber, tex.ColumnNumber);
            }
        }

        [Fact]
        public void ShouldLoadValidConfigWithTranslations()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.cfg"));
            Assert.Equal("Strings.ini", manager.DefaultConfiguration?.DefaultFile);
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE-AT"), "de-AT translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE"), "de translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("other"), "translation for 'other' not found");
        }

        [Fact]
        public void ShouldLoadValidConfigWithGroups()
        {
            IniParser? parser = FileFormats.GetConfigParser(".cfg") as IniParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigWithGroups.cfg"), string.Empty, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("StringsWithGroups.ini", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("hello"));
            Assert.NotNull(tree.FindNode("ui"));
            Assert.True(tree.RootNode.ChildNodes.ContainsKey("logs"));
            TranslationTreeNode? node = tree.FindNode("logs.server");
            Assert.NotNull(node);
            Assert.True(node.Keys.ContainsKey("started"));
        }

        [Fact]
        public void ShouldGetKeyFromGroup()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithGroups.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithGroups.ini", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("sk"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Started", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRef()
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithRef.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("logs.server.started");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyDefaultLanguage()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.ini", manager.DefaultConfiguration?.DefaultFile);
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFile()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.ini", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }
    }
}
