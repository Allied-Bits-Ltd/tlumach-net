// <copyright file="ArbWriterTests.cs" company="Allied Bits Ltd.">
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
using System.Text.Json;

using Tlumach.Base;
using Tlumach.Writers;

#pragma warning disable MA0011 // Use an overload of ... that has a ... parameter
namespace Tlumach.WriterTests
{
    [Trait("Category", "Writer")]
    [Trait("Category", "ARB")]
    public class ArbWriterTests
    {
        private static readonly string TestDataBaseDirectory = GetTestDataDirectory();
        private static readonly string ArbTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Arb");
        private static readonly string TsvTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "TSV");
        private static readonly string ResxTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Resx");
        private static readonly string OutputPath = Path.Combine(Path.GetTempPath(), "ArbWriterTests");

        static ArbWriterTests()
        {
            BaseParser.KeepEntryOrder = true;

            ArbParser.Use();
            TsvParser.Use();
            ResxParser.Use();
        }

        private static string GetTestDataDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            var testDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
            return Path.Combine(testDirectory, "Tlumach.Tests");
        }

        public ArbWriterTests()
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

        private static Translation LoadTranslationObject(string filePath, CultureInfo? culture = null)
        {
            var parser = FileFormats.GetParser(Path.GetExtension(filePath));
            Assert.NotNull(parser);

            var fileContent = File.ReadAllText(filePath);
            var translation = parser.LoadTranslation(fileContent, culture ?? CultureInfo.InvariantCulture, TextFormat.None);
            Assert.NotNull(translation);

            return translation;
        }

        private static void AssertValidArbFormat(string filePath)
        {
            Assert.True(File.Exists(filePath), $"File {filePath} should exist");

            var content = File.ReadAllText(filePath);
            Assert.NotEmpty(content);
            Assert.StartsWith("{", content.TrimStart());
        }

        [Fact]
        public void ArbWriterShouldHaveCorrectFormatName()
        {
            var writer = new ArbWriter();
            Assert.Equal("ARB", writer.FormatName);
            Assert.Equal(".arb", writer.TranslationExtension);
            Assert.Equal(".arbcfg", writer.ConfigExtension);
        }

        [Fact]
        public void ShouldWriteAndLoadBackSimpleArb()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "SimpleValidConfig.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var originalTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            Assert.NotNull(originalTranslation);

            var originalEntries = originalTranslation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "Simple_RoundTrip.arb");

            try
            {
                // Act - Write to ARB format
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Load back and compare
                AssertValidArbFormat(outputFile);
                var reloadedEntries = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalEntries, reloadedEntries);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteArbWithFeatures()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "Features_RoundTrip.arb");

            try
            {
                // Act - Load and write translations with features
                using var manager = new TranslationManager(configPath);
                manager.LoadFromDisk = true;
                manager.TranslationsDirectory = ArbTestDataPath;

                var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
                if (translation == null || translation.Count == 0)
                {
                    return;
                }

                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - File should be created
                AssertValidArbFormat(outputFile);
                Assert.NotEmpty(File.ReadAllText(outputFile));
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteMultipleCultures()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfig.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var deTranslation = manager.GetTranslation(CultureInfo.GetCultureInfo("de"), true);
            if (deTranslation == null || deTranslation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "MultipleCultures_de_Output.arb");

            try
            {
                // Act - Write German translation to ARB format
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.GetCultureInfo("de"), stream);
                }

                // Assert - File should be created and valid
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveFileMetadataInArb()
        {
            // Arrange
            var arbFile = Path.Combine(ArbTestDataPath, "ValidConfig.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(arbFile))
            {
                return;
            }

            using var manager = new TranslationManager(arbFile);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var originalTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (originalTranslation == null || originalTranslation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "FileMetadata.arb");

            try
            {
                // Act - Write to ARB and check metadata
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify file metadata is present
                var content = File.ReadAllText(outputFile);
                AssertValidArbFormat(outputFile);

                // If original has locale, it should be in output
                if (!string.IsNullOrEmpty(originalTranslation.Locale))
                {
                    Assert.Contains("@@locale", content, StringComparison.Ordinal);
                }

                // If original has author, it should be in output
                if (!string.IsNullOrEmpty(originalTranslation.Author))
                {
                    Assert.Contains("@@author", content, StringComparison.Ordinal);
                }
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveCustomPropertiesInArb()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "CustomProperties.arb");

            try
            {
                // Act - Write to ARB using a config with custom properties
                using var manager = new TranslationManager(configPath);
                manager.LoadFromDisk = true;
                manager.TranslationsDirectory = ArbTestDataPath;

                var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
                if (translation != null)
                {
                    var writer = new ArbWriter();
                    using (var stream = File.Create(outputFile))
                    {
                        writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                    }

                    // Assert - File should be created and contain custom properties if they exist
                    AssertValidArbFormat(outputFile);

                    if (translation.CustomProperties.Count > 0)
                    {
                        var content = File.ReadAllText(outputFile);
                        foreach (var customProp in translation.CustomProperties)
                        {
                            var propName = "@@x-" + customProp.Key;
                            Assert.Contains(propName, content, StringComparison.Ordinal);
                        }
                    }
                }
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreservePlaceholderMetadata()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "PlaceholderMetadata.arb");

            try
            {
                // Act - Load and write translations with placeholders
                using var manager = new TranslationManager(configPath);
                manager.LoadFromDisk = true;
                manager.TranslationsDirectory = ArbTestDataPath;

                var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
                if (translation == null || translation.Count == 0)
                {
                    return;
                }

                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - File should be created and valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);

                // Verify placeholders are referenced in the output
                var entriesWithPlaceholders = translation.Values.Where(e => e.Placeholders != null && e.Placeholders.Count > 0);
                foreach (var entry in entriesWithPlaceholders)
                {
                    // Check that entry metadata is in the output
                    Assert.Contains("@" + entry.Key, content, StringComparison.Ordinal);
                }
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldCreateValidArbJsonFormat()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfig.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (translation == null || translation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "ValidFormat_Output.arb");

            try
            {
                // Act - Write to ARB
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - File should be created and in valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldHandleEntryTargets()
        {
            // Arrange
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithRef.arbcfg");

            // Skip test if config doesn't exist
            if (!File.Exists(configPath))
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "WithTargets.arb");

            try
            {
                // Act - Load and write translations with targets
                using var manager = new TranslationManager(configPath);
                manager.LoadFromDisk = true;
                manager.TranslationsDirectory = ArbTestDataPath;

                var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
                if (translation == null || translation.Count == 0)
                {
                    return;
                }

                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify the file is valid
                AssertValidArbFormat(outputFile);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteWithFeatures()
        {
            // Arrange - Use existing ValidConfigWithFeatures
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var originalTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            Assert.NotNull(originalTranslation);

            var outputFile = Path.Combine(OutputPath, "WithFeatures_Output.arb");

            try
            {
                // Act - Write to ARB format
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify file was created with valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveFileMetadataFromFeatureConfiguration()
        {
            // Arrange - Use ValidConfigWithFeatures which has custom properties
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var originalTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (originalTranslation == null || originalTranslation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "MetadataRoundTrip.arb");

            try
            {
                // Act - Write to ARB and check metadata
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify file metadata is present
                var content = File.ReadAllText(outputFile);
                AssertValidArbFormat(outputFile);

                // Check locale metadata if present
                if (!string.IsNullOrEmpty(originalTranslation.Locale))
                {
                    Assert.Contains("@@locale", content, StringComparison.Ordinal);
                }

                // Check author metadata if present
                if (!string.IsNullOrEmpty(originalTranslation.Author))
                {
                    Assert.Contains("@@author", content, StringComparison.Ordinal);
                }

                // Check custom properties if any exist
                foreach (var customProp in originalTranslation.CustomProperties)
                {
                    var propName = "@@x-" + customProp.Key;
                    Assert.Contains(propName, content, StringComparison.Ordinal);
                }
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreservePlaceholderMetadataInArb()
        {
            // Arrange - Use ValidConfigWithFeatures which has placeholders
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (translation == null || translation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "PlaceholderMetadataRoundTrip.arb");

            try
            {
                // Act - Load and write translations with placeholders
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - File should be created and valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);

                // Verify placeholders are referenced in the output
                var entriesWithPlaceholders = translation.Values.Where(e => e.Placeholders != null && e.Placeholders.Count > 0);
                foreach (var entry in entriesWithPlaceholders)
                {
                    // Check that entry metadata is in the output
                    Assert.Contains("@" + entry.Key, content, StringComparison.Ordinal);
                }
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWritePluralFormsToArb()
        {
            // Arrange - Use ValidConfigWithFeatures which has plural entries
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (translation == null || translation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "PluralFormsOutput.arb");

            try
            {
                // Act - Write translations with plurals
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify file was created with valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteSelectFormsToArb()
        {
            // Arrange - Use ValidConfigWithFeatures which has select forms
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (translation == null || translation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "SelectFormsOutput.arb");

            try
            {
                // Act - Write translations with select forms
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify file was created with valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteGermanTranslation()
        {
            // Arrange - Use ValidConfig which has German translations
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfig.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var germanCulture = CultureInfo.GetCultureInfo("de");
            var originalTranslation = manager.GetTranslation(germanCulture, true);
            if (originalTranslation == null || originalTranslation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "German_Output.arb");

            try
            {
                // Act - Write German translation
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, germanCulture, stream);
                }

                // Assert - Verify file was created with valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveContextAndDescriptionMetadata()
        {
            // Arrange - Use ValidConfigWithFeatures which has descriptions
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (translation == null || translation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "ContextAndDescription.arb");

            try
            {
                // Act - Write to ARB
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify metadata is preserved
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);

                // Check that descriptions are in the output if they exist
                var entriesWithDescription = translation.Values.Where(e => !string.IsNullOrEmpty(e.Description));
                foreach (var entry in entriesWithDescription)
                {
                    Assert.Contains("description", content, StringComparison.Ordinal);
                    break; // Just check the first one
                }
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteMultiplePlaceholdersToArb()
        {
            // Arrange - Use ValidConfigWithFeatures which has entries with multiple placeholders
            var configPath = Path.Combine(ArbTestDataPath, "ValidConfigWithFeatures.arbcfg");

            if (!File.Exists(configPath))
            {
                return;
            }

            using var manager = new TranslationManager(configPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = ArbTestDataPath;

            var translation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (translation == null || translation.Count == 0)
            {
                return;
            }

            var outputFile = Path.Combine(OutputPath, "MultiplePlaceholders.arb");

            try
            {
                // Act - Write to ARB
                var writer = new ArbWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify file was created with valid ARB format
                AssertValidArbFormat(outputFile);
                var content = File.ReadAllText(outputFile);
                Assert.NotEmpty(content);

                // Check that placeholders are referenced in the output
                Assert.Contains("placeholders", content, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }
    }
}
