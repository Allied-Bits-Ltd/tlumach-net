// <copyright file="ResxParserTests.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

#pragma warning disable MA0011 // Use an overload of ... that has a ... parameter
namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "Table")]
    [Trait("Category", "Resx")]
    public class ResxParserTests
    {
        private const string TestFilesPath = "../../../TestData/Resx";

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

        private const string IdenticalDuplicatesResx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Grid_EmptyRecord" xml:space="preserve">
                <value>No records to display</value>
              </data>
              <data name="Grid_ClearButton" xml:space="preserve">
                <value>Clear</value>
              </data>
              <data name="Grid_ClearButton" xml:space="preserve">
                <value>Clear</value>
              </data>
            </root>
            """;

        private const string ConflictingDuplicatesResx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Grid_ClearButton" xml:space="preserve">
                <value>Clear</value>
              </data>
              <data name="grid_clearbutton" xml:space="preserve">
                <value>Reset</value>
              </data>
            </root>
            """;

        private const string CaseDifferingDuplicatesResx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Grid_EmptyRecord" xml:space="preserve">
                <value>No records to display</value>
              </data>
              <data name="Grid_ClearButton" xml:space="preserve">
                <value>Clear</value>
              </data>
              <data name="grid_clearbutton" xml:space="preserve">
                <value>Clear</value>
              </data>
            </root>
            """;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldSkipDuplicateKeyWithSameValue(bool populateKeyLocations)
        {
            bool oldFlag = BaseParser.PopulateKeyLocations;
            BaseParser.PopulateKeyLocations = populateKeyLocations;
            try
            {
                ResxParser parser = Assert.IsType<ResxParser>(FileFormats.GetParser(".resx"));

                Translation? translation = parser.LoadTranslation(IdenticalDuplicatesResx, culture: null, TextFormat.DotNet);

                Assert.NotNull(translation);
                Assert.Equal(2, translation.Count);
                Assert.Equal("Clear", translation["Grid_ClearButton"].Text);
            }
            finally
            {
                BaseParser.PopulateKeyLocations = oldFlag;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldRejectDuplicateKeyWithDifferentValue(bool populateKeyLocations)
        {
            bool oldFlag = BaseParser.PopulateKeyLocations;
            BaseParser.PopulateKeyLocations = populateKeyLocations;
            try
            {
                ResxParser parser = Assert.IsType<ResxParser>(FileFormats.GetParser(".resx"));

                GenericParserException exception = Assert.ThrowsAny<GenericParserException>(() => parser.LoadTranslation(ConflictingDuplicatesResx, culture: null, TextFormat.DotNet));
                Assert.Contains("Duplicate key", exception.ToString(), StringComparison.Ordinal);
            }
            finally
            {
                BaseParser.PopulateKeyLocations = oldFlag;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldSkipDuplicateKeyWithSameValueThatDiffersInCase(bool populateKeyLocations)
        {
            bool oldFlag = BaseParser.PopulateKeyLocations;
            BaseParser.PopulateKeyLocations = populateKeyLocations;
            try
            {
                ResxParser parser = Assert.IsType<ResxParser>(FileFormats.GetParser(".resx"));

                Translation? translation = parser.LoadTranslation(CaseDifferingDuplicatesResx, culture: null, TextFormat.DotNet);

                Assert.NotNull(translation);
                Assert.Equal(2, translation.Count);
                Assert.Equal("Clear", translation["Grid_ClearButton"].Text);
            }
            finally
            {
                BaseParser.PopulateKeyLocations = oldFlag;
            }
        }

        [Fact]
        public void ShouldLoadStructureWithDuplicateKeyWithSameValueThatDiffersInCase()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TlumachResxDuplicates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "Config.resxcfg"), "<root><defaultFile>Strings.resx</defaultFile></root>");
                File.WriteAllText(Path.Combine(directory, "Strings.resx"), CaseDifferingDuplicatesResx);
                ResxParser parser = Assert.IsType<ResxParser>(FileFormats.GetParser(".resx"));

                TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(directory, "Config.resxcfg"), directory, out _);

                Assert.NotNull(tree);
                Assert.Equal(2, tree.RootNode.Keys.Count);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void ShouldLoadStructureWithDuplicateKeyWithSameValue()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TlumachResxDuplicates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "Config.resxcfg"), "<root><defaultFile>Strings.resx</defaultFile></root>");
                File.WriteAllText(Path.Combine(directory, "Strings.resx"), IdenticalDuplicatesResx);
                ResxParser parser = Assert.IsType<ResxParser>(FileFormats.GetParser(".resx"));

                TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(directory, "Config.resxcfg"), directory, out _);

                Assert.NotNull(tree);
                Assert.Equal(2, tree.RootNode.Keys.Count);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void ShouldRejectStructureWithDuplicateKeyWithDifferentValue()
        {
            string directory = Path.Combine(Path.GetTempPath(), "TlumachResxDuplicates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "Config.resxcfg"), "<root><defaultFile>Strings.resx</defaultFile></root>");
                File.WriteAllText(Path.Combine(directory, "Strings.resx"), ConflictingDuplicatesResx);
                ResxParser parser = Assert.IsType<ResxParser>(FileFormats.GetParser(".resx"));

                Exception exception = Assert.ThrowsAny<Exception>(() => parser.LoadTranslationStructure(Path.Combine(directory, "Config.resxcfg"), directory, out _));
                Assert.Contains("Duplicate key", exception.ToString(), StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

    }
}
