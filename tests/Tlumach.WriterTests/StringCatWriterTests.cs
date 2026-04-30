// <copyright file="StringCatWriterTests.cs" company="Allied Bits Ltd.">
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
using System.Reflection;

using Tlumach.Base;
using Tlumach.Writers;

namespace Tlumach.WriterTests
{
    [Trait("Category", "Writer")]
    [Trait("Category", "StringCat")]
    public class StringCatWriterTests
    {
        private static readonly string TestDataBaseDirectory = GetTestDataDirectory();
        private static readonly string StringCatTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "StringCat");
        private static readonly string OutputPath = Path.Combine(Path.GetTempPath(), "StringCatWriterTests");

        static StringCatWriterTests()
        {
            BaseParser.KeepEntryOrder = true;
            StringCatParser.Use();
        }

        public StringCatWriterTests()
        {
            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);
        }

        private static string GetTestDataDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            var testDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
            return Path.Combine(testDirectory, "Tlumach.Tests");
        }

        private static void CleanupTestFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        private static List<TranslationEntry> LoadXcstringsForCulture(string filePath, CultureInfo culture)
        {
            var parser = new StringCatParser();
            var content = File.ReadAllText(filePath);
            var translation = parser.LoadTranslation(content, culture, TextFormat.Apple);
            return translation?.Values.ToList() ?? [];
        }

        [Fact]
        public void StringCatWriterShouldHaveCorrectFormatName()
        {
            var writer = new StringCatWriter();
            Assert.Equal("StringCatalog", writer.FormatName);
            Assert.Equal(".xcstrings", writer.TranslationExtension);
            Assert.Equal(".jsoncfg", writer.ConfigExtension);
        }

        [Fact]
        public void ShouldContainSourceLanguageField()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;
            var en = CultureInfo.GetCultureInfo("en");
            manager.GetTranslation(en, true);

            var outputFile = Path.Combine(OutputPath, "SourceLanguage.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [en], stream);

                var content = File.ReadAllText(outputFile);
                Assert.Contains("sourceLanguage", content, StringComparison.Ordinal);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldContainStringUnitStructure()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;
            var en = CultureInfo.GetCultureInfo("en");
            manager.GetTranslation(en, true);

            var outputFile = Path.Combine(OutputPath, "StringUnitStructure.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [en], stream);

                var content = File.ReadAllText(outputFile);
                Assert.Contains("stringUnit", content, StringComparison.Ordinal);
                Assert.Contains("state", content, StringComparison.Ordinal);
                Assert.Contains("value", content, StringComparison.Ordinal);
                Assert.Contains("translated", content, StringComparison.Ordinal);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveCommentInOutput()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;
            var en = CultureInfo.GetCultureInfo("en");
            manager.GetTranslation(en, true);

            var outputFile = Path.Combine(OutputPath, "WithComment.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [en], stream);

                var content = File.ReadAllText(outputFile);
                Assert.Contains("A greeting", content, StringComparison.Ordinal);
                Assert.Contains("comment", content, StringComparison.Ordinal);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteMultipleCulturesInSingleFile()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;
            var en = CultureInfo.GetCultureInfo("en");
            var de = CultureInfo.GetCultureInfo("de");
            manager.GetTranslation(en, true);
            manager.GetTranslation(de, true);

            var outputFile = Path.Combine(OutputPath, "MultipleCultures.xcstrings");
            try
            {
                var cultures = new[] { en, de };
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, cultures, stream);

                var content = File.ReadAllText(outputFile);
                Assert.Contains("\"en\"", content, StringComparison.Ordinal);
                Assert.Contains("\"de\"", content, StringComparison.Ordinal);
                Assert.Contains("Hello", content, StringComparison.Ordinal);
                Assert.Contains("Hallo", content, StringComparison.Ordinal);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldRoundTripEnglishEntries()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;

            var en = CultureInfo.GetCultureInfo("en");
            var originalTranslation = manager.GetTranslation(en, true);
            Assert.NotNull(originalTranslation);

            var outputFile = Path.Combine(OutputPath, "RoundTripEn.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [en], stream);

                var reloaded = LoadXcstringsForCulture(outputFile, en);
                TranslationComparer.AssertTranslationsEqual(originalTranslation.Values, reloaded);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldRoundTripGermanEntries()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;

            var de = CultureInfo.GetCultureInfo("de");
            var originalTranslation = manager.GetTranslation(de, true);
            Assert.NotNull(originalTranslation);
            if (originalTranslation.Count == 0)
                return;

            var outputFile = Path.Combine(OutputPath, "RoundTripDe.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [de], stream);

                var reloaded = LoadXcstringsForCulture(outputFile, de);
                TranslationComparer.AssertTranslationsEqual(originalTranslation.Values, reloaded);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteSingleCultureAndLoadBack()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;
            var sk = CultureInfo.GetCultureInfo("sk");
            manager.GetTranslation(sk, true);
            var originalTranslation = manager.GetTranslation(sk, true);
            Assert.NotNull(originalTranslation);

            var outputFile = Path.Combine(OutputPath, "SingleCultureSk.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [sk], stream);

                Assert.True(File.Exists(outputFile));
                var content = File.ReadAllText(outputFile);
                Assert.Contains("Ahoj", content, StringComparison.Ordinal);
                Assert.Contains("Vitajte", content, StringComparison.Ordinal);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldThrowWhenNoCulturesProvided()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;

            var writer = new StringCatWriter();
            using var stream = new MemoryStream();
            Assert.Throws<ArgumentException>(() => writer.WriteTranslations(manager, [], stream));
        }

        [Fact]
        public void ShouldProduceValidJsonOutput()
        {
            var configPath = Path.Combine(StringCatTestDataPath, "ValidConfig.jsoncfg");
            if (!File.Exists(configPath))
                return;

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = StringCatTestDataPath;
            var en = CultureInfo.GetCultureInfo("en");
            manager.GetTranslation(en, true);

            var outputFile = Path.Combine(OutputPath, "ValidJson.xcstrings");
            try
            {
                var writer = new StringCatWriter();
                using (var stream = File.Create(outputFile))
                    writer.WriteTranslations(manager, [en], stream);

                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
                Assert.StartsWith("{", content.TrimStart(), StringComparison.Ordinal);

                var doc = System.Text.Json.JsonDocument.Parse(content);
                Assert.True(doc.RootElement.TryGetProperty("strings", out _));
                Assert.True(doc.RootElement.TryGetProperty("version", out _));
                Assert.True(doc.RootElement.TryGetProperty("sourceLanguage", out _));
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }
    }
}
