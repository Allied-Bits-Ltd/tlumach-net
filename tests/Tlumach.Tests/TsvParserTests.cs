// <copyright file="TsvParserTests.cs" company="Allied Bits Ltd.">
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
    [Trait("Category", "TSV")]
    public class TsvParserTests
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\TSV";

        static TsvParserTests()
        {
            IniParser.Use();
            TsvParser.Use();
            TsvParser.ExpectQuotes = true;
            TsvParser.TextProcessingMode = TextFormat.BackslashEscaping;
        }

        [Fact]
        public void ShouldGetKey()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Basic.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Basic.tsv", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeySpecificCulture()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Basic.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Basic.tsv", manager.DefaultConfiguration?.DefaultFile);
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
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Basic.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Basic.tsv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyDefaultCulture()
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Basic.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Basic.tsv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Bye", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultiline()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Complex.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Complex.tsv", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("Multiline");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Multiline\nvalue", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineSpecificCulture()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Complex.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Complex.tsv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Multiline");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("AT Mehrzeiliger\nWert", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineBasicCulture()
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            CsvParser.OnCultureNameMatchCheck += (sender, args) => { if (args.Candidate.Equals("de", StringComparison.OrdinalIgnoreCase) && args.Culture.Name.Equals("de-DE", StringComparison.OrdinalIgnoreCase)) args.Match = true; };
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Complex.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Complex.tsv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("MultilineWithComma");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Mehrzeiliger\nWert,\nmit Kommas", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineDefaultCulture()
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Complex.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Complex.tsv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("MultilineWithComma");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Multiline\nvalue,\nwith commas", entry.Text);
        }

        [Fact]
        public void ShouldFailOnInvalidFile()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "InvalidMissingCells.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de-AT");
            Assert.Throws<TextFileParseException>(() => manager.GetValue("Hello"));
        }

        [Fact]
        public void ShouldFailOnInvalidFileWithPositionCheck()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "InvalidMissingCells.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new CultureInfo("de-AT");
            try
            {
                manager.GetValue("Hello");
                Assert.Fail("An exception has not been thrown");
            }
            catch (Exception ex)
            {
                Assert.True(ex is TextFileParseException);
                Assert.NotNull(ex.InnerException);
                Assert.True(ex.InnerException is TextParseException);
                TextParseException? tex = ex.InnerException as TextParseException;
                Assert.NotNull(tex);
                Assert.Equal(4, tex.LineNumber);
                Assert.Equal(1, tex.ColumnNumber);
            }
        }
    }
}
