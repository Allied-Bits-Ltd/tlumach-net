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
    [Trait("Category", "Table")]
    [Trait("Category", "Resx")]
    public class ResxParserTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\Resx";

        static ResxParserTests()
        {
            ResxParser.Use();
        }

        [Fact]
        public void ShouldLoadValidConfig()
        {
            ResxParser? parser = FileFormats.GetParser(".resx") as ResxParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "Config.resxcfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("Strings.resx", config.DefaultFile);
            Assert.Equal("Tlumach.Tests", config.Namespace);
            Assert.Equal("Strings", config.ClassName);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("Hello"));
        }

        [Fact]
        public void ShouldLoadValidConfigWithTranslations()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ConfigWithTranslations.resxcfg"));
            Assert.NotNull(manager.DefaultConfiguration);
            var config = manager.DefaultConfiguration;
            Assert.Equal("Strings.resx", config.DefaultFile);
            Assert.True(config.Translations.ContainsKey("DE-AT"), "de-AT translation not found");
            Assert.True(config.Translations.ContainsKey("DE"), "de translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("other"), "translation for 'other' not found");
        }

        [Fact]
        public void ShouldGetKey()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Config.resxcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.resx", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeySpecificCulture()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Config.resxcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.resx", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Servus", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyBasicCulture()
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            CsvParser.OnCultureNameMatchCheck += (sender, args) => { if (args.Candidate.Equals("de", StringComparison.OrdinalIgnoreCase) && args.Culture.Name.Equals("de-DE", StringComparison.OrdinalIgnoreCase)) args.Match = true; };
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Config.resxcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.resx", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyDefaultCulture()
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Config.resxcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.resx", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Bye", entry.Text);
        }

    }
}
