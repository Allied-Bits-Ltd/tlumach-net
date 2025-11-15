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
    [Trait("Category", "TSV")]
    public class TsvParserTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\TSV";

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
    }
}
