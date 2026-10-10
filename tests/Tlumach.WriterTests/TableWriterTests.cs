// <copyright file="TableWriterTests.cs" company="Allied Bits Ltd.">
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
using System.IO;
using System.Reflection;

using Tlumach.Base;
using Tlumach.Writers;

#pragma warning disable MA0011 // Use an overload of ... that has a ... parameter
namespace Tlumach.WriterTests
{
    /// <summary>
    /// Tests of the CSV and TSV writers: a table written by a writer is read back by the parser of the format.
    /// </summary>
    [Trait("Category", "Writer")]
    [Trait("Category", "Table")]
    public sealed class TableWriterTests : IDisposable
    {
        private static readonly string TestDataBaseDirectory = GetTestDataDirectory();

        private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");

        private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");

        private static readonly CultureInfo DeAt = CultureInfo.GetCultureInfo("de-AT");

        // Values that a table format has to protect: separators of both formats, line breaks, and double quotes at the start and in the middle of a value
        private static readonly Dictionary<string, (string En, string De)> SpecialValues = new(StringComparer.Ordinal)
        {
            ["Comma"] = ("one, two", "eins, zwei"),
            ["Tab"] = ("one\ttwo", "eins\tzwei"),
            ["LineBreak"] = ("first line\nsecond line", "erste Zeile\nzweite Zeile"),
            ["StartsWithQuote"] = ("\"Quoted\" at the start", "\"Zitiert\" am Anfang"),
            ["InnerQuote"] = ("He said \"hi\"", "Er sagte \"hallo\""),
            ["Plain"] = ("plain", "einfach"),
        };

        private readonly string _directory;

        static TableWriterTests()
        {
            IniParser.Use();
            JsonParser.Use();
            CsvParser.Use();
            TsvParser.Use();
        }

        public TableWriterTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "TlumachTableWriterTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary directory does not affect the results.
            }
        }

        [Theory]
        [InlineData("CSV")]
        [InlineData("TSV")]
        public void WritesTableThatParserReadsBack(string format)
        {
            string sourceDirectory = Path.Combine(TestDataBaseDirectory, "TestData", format);
            using TranslationManager source = new(Path.Combine(sourceDirectory, "Basic.cfg")) { LoadFromDisk = true, TranslationsDirectory = sourceDirectory };

            using TranslationManager result = WriteAndLoad(source, format, [En, De, DeAt]);

            foreach (CultureInfo culture in new[] { En, De, DeAt })
            {
                foreach (string key in new[] { "Hello", "Welcome" })
                    Assert.Equal(source.GetValue(key, culture).Text, result.GetValue(key, culture).Text);
            }
        }

        [Theory]
        [InlineData("CSV")]
        [InlineData("TSV")]
        public void ProtectsSeparatorsLineBreaksAndQuotes(string format)
        {
            using TranslationManager source = CreateSpecialSource();

            using TranslationManager result = WriteAndLoad(source, format, [En, De]);

            foreach (KeyValuePair<string, (string En, string De)> pair in SpecialValues)
            {
                Assert.Equal(pair.Value.En, result.GetValue(pair.Key, En).Text);
                Assert.Equal(pair.Value.De, result.GetValue(pair.Key, De).Text);
            }
        }

        [Fact]
        public void TsvWriterQuotesOnlyValuesThatNeedIt()
        {
            using TranslationManager source = CreateSpecialSource();

            string text = Write(source, new TsvWriter(), [En, De]);

            Assert.Contains("\"one\ttwo\"", text, StringComparison.Ordinal);
            Assert.Contains("\"first line\nsecond line\"", text.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
            Assert.Contains("\"\"\"Quoted\"\" at the start\"", text, StringComparison.Ordinal);

            // A double quote in the middle of a value and a comma need no quotes in TSV
            Assert.Contains("\tHe said \"hi\"\t", text, StringComparison.Ordinal);
            Assert.Contains("\tone, two\t", text, StringComparison.Ordinal);
        }

        [Fact]
        public void TsvParserReadsQuotedCellsByDefaultAndInnerQuotesAsText()
        {
            // The default of ExpectQuotes is true, and a double quote that does not start a cell is a part of the value
            Assert.True(TsvParser.ExpectQuotes);

            const string Content = "key\ten\n" +
                "Multi\t\"a\tb\nc\"\n" +
                "Inner\tsay \"hi\" now\n" +
                "Doubled\t\"\"\"x\"\" y\"\n";

            Translation? translation = new TsvParser().LoadTranslation(Content, En, TextFormat.None);

            Assert.NotNull(translation);
            Assert.Equal("a\tb\nc", translation!["MULTI"].Text);
            Assert.Equal("say \"hi\" now", translation["INNER"].Text);
            Assert.Equal("\"x\" y", translation["DOUBLED"].Text);
        }

        private static string GetTestDataDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            var testDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
            return Path.Combine(testDirectory, "Tlumach.Tests");
        }

        private static string Write(TranslationManager source, BaseWriter writer, IReadOnlyCollection<CultureInfo> cultures)
        {
            // The writer takes the translations that the manager has loaded
            foreach (CultureInfo culture in cultures)
                Assert.NotNull(source.GetTranslation(culture, true));

            using MemoryStream stream = new();
            writer.WriteTranslations(source, cultures, stream);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        // A CSV table with the special values, quoted as CsvParser reads them: a value with a separator, a double quote, or a line break is quoted, with doubled double quotes inside.
        // (The writers take the translations of a table by their column names; a per-file source whose default file is named by defaultLocale is a separate case.)
        private TranslationManager CreateSpecialSource()
        {
            string directory = Path.Combine(_directory, "source");
            Directory.CreateDirectory(directory);

            System.Text.StringBuilder table = new();
            table.Append("key,en,de\n");
            foreach (KeyValuePair<string, (string En, string De)> pair in SpecialValues)
                table.Append(pair.Key).Append(',').Append(QuoteCsv(pair.Value.En)).Append(',').Append(QuoteCsv(pair.Value.De)).Append('\n');

            File.WriteAllText(Path.Combine(directory, "Strings.csv"), table.ToString());
            File.WriteAllText(Path.Combine(directory, "Strings.cfg"), "defaultFile=Strings.csv\n");

            return new TranslationManager(Path.Combine(directory, "Strings.cfg")) { LoadFromDisk = true, TranslationsDirectory = directory };
        }

        private static string QuoteCsv(string value)
        {
            if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
                return value;

            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        private TranslationManager WriteAndLoad(TranslationManager source, string format, IReadOnlyCollection<CultureInfo> cultures)
        {
            string extension = format == "CSV" ? ".csv" : ".tsv";
            BaseWriter writer = format == "CSV" ? new CsvWriter() : new TsvWriter();

            string directory = Path.Combine(_directory, "result" + extension.Substring(1));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "Result" + extension), Write(source, writer, cultures));
            File.WriteAllText(Path.Combine(directory, "Result.cfg"), "defaultFile=Result" + extension + "\n");

            return new TranslationManager(Path.Combine(directory, "Result.cfg")) { LoadFromDisk = true, TranslationsDirectory = directory };
        }
    }
}
