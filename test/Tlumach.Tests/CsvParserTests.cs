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
    [Trait("Category", "CSV")]
    public class CsvParserTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\CSV";

        static CsvParserTests()
        {
            IniParser.Use();
            CsvParser.Use();
        }

        public CsvParserTests()
        {
            CsvParser.TreatEmptyValuesAsAbsent = false;
            CsvParser.SeparatorChar = ',';
        }

        [Fact]
        public void ShouldGetKey()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Basic.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Basic.csv", manager.DefaultConfiguration?.DefaultFile);

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
            Assert.Equal("Basic.csv", manager.DefaultConfiguration?.DefaultFile);
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
            Assert.Equal("Basic.csv", manager.DefaultConfiguration?.DefaultFile);
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
            Assert.Equal("Basic.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Bye", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRef()
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "BasicWithRef.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue("logs.server.started");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRefSpecificLanguage()
        {
            BaseParser.RecognizeFileRefs = true;
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "BasicWithRef.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithRef.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("logs.server.started", manager.CurrentCulture);
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Die Protokollierung wurde gestartet.", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRefDefaultLanguage()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "BasicWithRef.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithRef.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("logs.server.started", manager.CurrentCulture);
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFile()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "BasicWithTranslations.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Strings.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultiline()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Complex.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Complex.csv", manager.DefaultConfiguration?.DefaultFile);

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
            Assert.Equal("Complex.csv", manager.DefaultConfiguration?.DefaultFile);
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
            Assert.Equal("Complex.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("MultilineWithComma");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Mehrzeiliger\nWert, mit Kommas", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineDefaultCulture()
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "Complex.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("Complex.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("MultilineWithComma");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Multiline\nvalue, with commas", entry.Text);
        }

        [Theory]
        [InlineData("InvalidMissingCells.cfg")]
        [InlineData("InvalidUnclosedQuote.cfg")]
        [InlineData("InvalidUnclosedQuote2.cfg ")]
        public void ShouldFailOnInvalidFile(string configFile)
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, configFile));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;

            manager.CurrentCulture = new CultureInfo("de-AT");
            Assert.Throws<TextFileParseException>(() => manager.GetValue("Hello"));
        }

        [Theory]
        [InlineData("InvalidMissingCells.cfg", 4, 1)]
        [InlineData("InvalidUnclosedQuote.cfg", 4, 16)]
        [InlineData("InvalidUnclosedQuote2.cfg ", 4, 16)]
        public void ShouldFailOnInvalidFileWithPositionCheck(string configFile, int lineNumber, int columnNumber)
        {
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, configFile));
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
                Assert.Equal(lineNumber, tex.LineNumber);
                Assert.Equal(columnNumber, tex.ColumnNumber);

            }
        }

        [Fact]
        public void ShouldGetKeyMultilineWithSemicolon()
        {
            CsvParser.SeparatorChar = ';';
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComplexWithSemicolon.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("ComplexWithSemicolon.csv", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("Multiline");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Multiline\nvalue", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineSpecificCultureWithSemicolon()
        {
            CsvParser.SeparatorChar = ';';
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComplexWithSemicolon.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("ComplexWithSemicolon.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Multiline");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("AT Mehrzeiliger\nWert", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineBasicCultureWithSemicolon()
        {
            CsvParser.SeparatorChar = ';';
            CsvParser.TreatEmptyValuesAsAbsent = true;
            CsvParser.OnCultureNameMatchCheck += (sender, args) => { if (args.Candidate.Equals("de", StringComparison.OrdinalIgnoreCase) && args.Culture.Name.Equals("de-DE", StringComparison.OrdinalIgnoreCase)) args.Match = true; };
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComplexWithSemicolon.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("ComplexWithSemicolon.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("MultilineWithComma");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Mehrzeiliger\nWert, mit Kommas", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyMultilineDefaultCultureWithSemicolon()
        {
            CsvParser.SeparatorChar = ';';
            CsvParser.TreatEmptyValuesAsAbsent = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ComplexWithSemicolon.cfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("ComplexWithSemicolon.csv", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("MultilineWithComma");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Multiline\nvalue, with commas", entry.Text);
        }
    }
}
