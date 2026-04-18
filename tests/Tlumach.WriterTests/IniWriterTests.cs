// <copyright file="IniWriterTests.cs" company="Allied Bits Ltd.">
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
using System.Linq;
using System.Reflection;
using System.Text;

using Tlumach.Base;
using Tlumach.Writers;

#pragma warning disable MA0011 // Use an overload of ... that has a ... parameter
namespace Tlumach.WriterTests
{
    [Trait("Category", "Writer")]
    [Trait("Category", "Ini")]
    public class IniWriterTests
    {
        private static readonly string TestDataBaseDirectory = GetTestDataDirectory();
        private static readonly string IniTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Ini");
        private static readonly string JsonTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Json");
        private static readonly string TomlTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Toml");
        private static readonly string OutputPath = Path.Combine(Path.GetTempPath(), "IniWriterTests");

        static IniWriterTests()
        {
            BaseParser.KeepEntryOrder = true;

            IniParser.Use();
            JsonParser.Use();
            TomlParser.Use();
        }

        private static string GetTestDataDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            var testDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
            return Path.Combine(testDirectory, "Tlumach.Tests");
        }

        public IniWriterTests()
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
        }

        private static void CleanupTestFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static List<TranslationEntry> LoadTranslationsFromFile(string filePath)
        {
            var parser = FileFormats.GetParser(Path.GetExtension(filePath));
            Assert.NotNull(parser);

            var fileContent = File.ReadAllText(filePath);
            var translation = parser.LoadTranslation(fileContent, CultureInfo.InvariantCulture, TextFormat.None);
            if (translation is null)
            {
                return [];
            }

            return translation.Values.ToList();
        }

        [Fact]
        public void ShouldWriteAndLoadBackSimpleTranslations()
        {
            // Arrange
            var configPath = Path.Combine(IniTestDataPath, "ValidConfigWithTranslations.cfg");
            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = IniTestDataPath;

            var defaultTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            Assert.NotNull(defaultTranslation);

            var originalTranslations = defaultTranslation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "SimpleTranslations_Output.ini");

            try
            {
                // Act - Write to INI format using a stream
                var writer = new IniWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Load back and compare
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteAndLoadBackGroupedTranslations()
        {
            // Arrange
            var configPath = Path.Combine(IniTestDataPath, "ValidConfigWithGroups.cfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = IniTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            Assert.NotNull(translation);

            var originalTranslations = translation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "GroupedTranslations_Output.ini");

            try
            {
                // Act - Write to INI format
                var writer = new IniWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Load back and compare
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);

                // Verify group structure is preserved
                var groupedKeys = reloadedTranslations.Where(t => t.Key.Contains('.')).ToList();
                Assert.NotEmpty(groupedKeys);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteJsonToIniFormat()
        {
            // Arrange
            var jsonConfigPath = Path.Combine(JsonTestDataPath, "SimpleValidConfig.jsoncfg");

            // Skip test if JSON config doesn't exist
            if (!File.Exists(jsonConfigPath))
            {
                return;
            }

            using var manager = new TranslationManager(jsonConfigPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = JsonTestDataPath;

            var jsonTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (jsonTranslation == null || jsonTranslation.Count == 0)
            {
                return;
            }

            var originalTranslations = jsonTranslation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "JsonToIni_Output.ini");

            try
            {
                // Act - Write JSON translations to INI format
                var writer = new IniWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Load back and compare
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);

                // Verify file is valid INI format
                Assert.True(File.Exists(outputFile));
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteTomlToIniFormat()
        {
            // Arrange
            var tomlConfigPath = Path.Combine(TomlTestDataPath, "ValidConfig.tomlcfg");

            // Skip test if TOML config doesn't exist
            if (!File.Exists(tomlConfigPath))
            {
                return;
            }

            using var manager = new TranslationManager(tomlConfigPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TomlTestDataPath;

            var tomlTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (tomlTranslation == null || tomlTranslation.Count == 0)
            {
                return;
            }

            var originalTranslations = tomlTranslation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "TomlToIni_Output.ini");

            try
            {
                // Act - Write TOML translations to INI format
                var writer = new IniWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Load back and compare
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldCreateValidIniFileFormat()
        {
            // Arrange
            var configPath = Path.Combine(IniTestDataPath, "ValidConfigWithTranslations.cfg");
            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = IniTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            Assert.NotNull(translation);

            var originalTranslations = translation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "ValidFormat_Output.ini");

            try
            {
                // Act - Write to INI
                var writer = new IniWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - File should be readable and valid
                Assert.True(File.Exists(outputFile));

                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);

                // Should be able to parse the generated file
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                Assert.Equal(originalTranslations.Count, reloadedTranslations.Count);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }
    }
}
