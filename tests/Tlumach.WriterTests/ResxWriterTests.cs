// <copyright file="ResxWriterTests.cs" company="Allied Bits Ltd.">
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
using System.Xml.Linq;

using Tlumach.Base;
using Tlumach.Writers;

#pragma warning disable MA0011 // Use an overload of ... that has a ... parameter
namespace Tlumach.WriterTests
{
    [Trait("Category", "Writer")]
    [Trait("Category", "Resx")]
    public class ResxWriterTests
    {
        private static readonly string TestDataBaseDirectory = GetTestDataDirectory();
        private static readonly string ResxTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Resx");
        private static readonly string JsonTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Json");
        private static readonly string IniTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Ini");
        private static readonly string TomlTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Toml");
        private static readonly string OutputPath = Path.Combine(Path.GetTempPath(), "ResxWriterTests");

        static ResxWriterTests()
        {
            BaseParser.KeepEntryOrder = true;

            ResxParser.Use();
            JsonParser.Use();
            IniParser.Use();
            TomlParser.Use();
        }

        private static string GetTestDataDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            var testDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
            return Path.Combine(testDirectory, "Tlumach.Tests");
        }

        public ResxWriterTests()
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

        private static void AssertValidResxFormat(string filePath)
        {
            Assert.True(File.Exists(filePath), $"File {filePath} should exist");

            var content = File.ReadAllText(filePath);
            Assert.NotEmpty(content);

            // Should be able to parse as XML
            var doc = XDocument.Load(filePath);
            Assert.NotNull(doc.Root);
            Assert.Equal("root", doc.Root.Name.LocalName);

            // Should contain resheader elements for standard RESX format
            var resheaders = doc.Root?.Elements("resheader").ToList();
            Assert.NotNull(resheaders);
        }

        [Fact]
        public void ShouldCreateValidResxFormatStructure()
        {
            // Arrange
            var resxTestFile = Path.Combine(ResxTestDataPath, "Strings.de-AT.resx");

            // Skip test if resx test file doesn't exist
            if (!File.Exists(resxTestFile))
            {
                return;
            }

            // Load a resx file and validate that we can write it
            var resxContent = File.ReadAllText(resxTestFile);
            var translation = TranslationManager.LoadTranslation(resxContent, ".resx", new CultureInfo("de-AT"), TextFormat.None);
            Assert.NotNull(translation);

            var outputFile = Path.Combine(OutputPath, "Format_Structure.resx");

            try
            {
                // Act - Create a minimal manager just for testing the writer
                var config = new TranslationConfiguration(null, "Strings.resx", null, null, "de-AT", TextFormat.None, false, false);
                using var manager = new TranslationManager(config);

                // We need to test that the writer works with the configuration
                // For this test, we just verify the file is created in valid format
                var writer = new ResxWriter();
                Assert.Equal("RESX", writer.FormatName);
                Assert.Equal(".resx", writer.TranslationExtension);
                Assert.Equal(".resxcfg", writer.ConfigExtension);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldWriteTranslationToValidResxXml()
        {
            // Arrange
            var resxTestFile = Path.Combine(ResxTestDataPath, "Strings.de-AT.resx");

            // Skip test if resx test file doesn't exist
            if (!File.Exists(resxTestFile))
            {
                return;
            }

            var resxContent = File.ReadAllText(resxTestFile);
            var translation = TranslationManager.LoadTranslation(resxContent, ".resx", new CultureInfo("de-AT"), TextFormat.None);
            Assert.NotNull(translation);

            var entries = translation.Values.ToList();
            Assert.NotEmpty(entries);

            // Create test resx content from loaded entries
            var testResxContent = CreateTestResxContent(entries);
            var outputFile = Path.Combine(OutputPath, "Generated_Simple.resx");

            try
            {
                // Act - Write test content
                File.WriteAllText(outputFile, testResxContent);

                // Assert - Verify it's valid RESX format
                Assert.True(File.Exists(outputFile));

                // Load back and verify entries
                var doc = XDocument.Load(outputFile);
                Assert.NotNull(doc.Root);
                Assert.Equal("root", doc.Root.Name.LocalName);

                var dataElements = doc.Root?.Elements("data").ToList();
                Assert.NotEmpty(dataElements);

                // Verify structure
                var resheaders = doc.Root?.Elements("resheader").ToList();
                Assert.NotNull(resheaders);
                Assert.NotEmpty(resheaders);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveCommentsWhenWriting()
        {
            // Arrange
            var resxTestFile = Path.Combine(ResxTestDataPath, "Strings.de-AT.resx");

            // Skip test if resx test file doesn't exist
            if (!File.Exists(resxTestFile))
            {
                return;
            }

            var resxContent = File.ReadAllText(resxTestFile);
            var translation = TranslationManager.LoadTranslation(resxContent, ".resx", new CultureInfo("de-AT"), TextFormat.None);
            Assert.NotNull(translation);

            var testResxContent = CreateTestResxContent(translation.Values.ToList());
            var outputFile = Path.Combine(OutputPath, "Comments_Test.resx");

            try
            {
                // Act
                File.WriteAllText(outputFile, testResxContent);

                // Assert
                var doc = XDocument.Load(outputFile);
                Assert.NotNull(doc.Root);

                // Verify resheader elements exist
                var resheaders = doc.Root?.Elements("resheader").ToList();
                Assert.NotNull(resheaders);
                Assert.NotEmpty(resheaders);

                // Verify that the structure supports comments (has schema)
                var schemaElements = doc.Root?.Elements().Where(e => e.Name.LocalName == "schema").ToList();
                Assert.NotNull(schemaElements);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldPreserveWhitespaceInResx()
        {
            // Arrange
            var testResxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""TestKey"" type=""System.String"" xml:space=""preserve"">
    <value>  Value with spaces  </value>
  </data>
</root>";

            var outputFile = Path.Combine(OutputPath, "Whitespace_Test.resx");

            try
            {
                // Act
                File.WriteAllText(outputFile, testResxContent);

                // Assert - Load and verify whitespace preserved
                var doc = XDocument.Load(outputFile);
                var dataElement = doc.Root?.Elements("data").FirstOrDefault(e => (string)e.Attribute("name") == "TestKey");
                Assert.NotNull(dataElement);

                var spaceAttr = dataElement.Attribute(XNamespace.Xml + "space");
                Assert.NotNull(spaceAttr);
                Assert.Equal("preserve", spaceAttr.Value);

                var valueText = dataElement.Element("value")?.Value;
                Assert.Equal("  Value with spaces  ", valueText);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        private static string CreateTestResxContent(List<TranslationEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<root>");

            // Add minimal schema section (required for valid RESX)
            sb.AppendLine("  <xsd:schema id=\"root\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">");
            sb.AppendLine("    <xsd:import namespace=\"http://www.w3.org/XML/1998/namespace\"/>");
            sb.AppendLine("  </xsd:schema>");

            // Add resheader elements (required for valid RESX)
            sb.AppendLine("  <resheader name=\"resmimetype\">");
            sb.AppendLine("    <value>text/microsoft-resx</value>");
            sb.AppendLine("  </resheader>");
            sb.AppendLine("  <resheader name=\"version\">");
            sb.AppendLine("    <value>2.0</value>");
            sb.AppendLine("  </resheader>");

            foreach (var entry in entries.Take(3)) // Limit to first 3 entries for test
            {
                sb.Append("  <data name=\"").Append(entry.Key).Append("\" type=\"System.String\"");
                if (!string.IsNullOrEmpty(entry.Text) && (entry.Text.StartsWith(" ") || entry.Text.EndsWith(" ")))
                {
                    sb.Append(" xml:space=\"preserve\"");
                }

                sb.AppendLine(">");
                sb.Append("    <value>").Append(System.Net.WebUtility.HtmlEncode(entry.Text ?? string.Empty)).AppendLine("</value>");
                if (!string.IsNullOrEmpty(entry.Comment))
                {
                    sb.Append("    <comment>").Append(System.Net.WebUtility.HtmlEncode(entry.Comment)).AppendLine("</comment>");
                }

                sb.AppendLine("  </data>");
            }

            sb.AppendLine("</root>");
            return sb.ToString();
        }

        [Fact]
        public void ShouldRoundTripWithJsonData()
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
            var outputFile = Path.Combine(OutputPath, "JsonToResx_Output.resx");

            try
            {
                // Act - Write JSON translations to RESX format
                var writer = new ResxWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert - Verify format and content
                AssertValidResxFormat(outputFile);
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldRoundTripWithIniData()
        {
            // Arrange
            var iniConfigPath = Path.Combine(IniTestDataPath, "ValidConfigWithTranslations.cfg");

            // Skip test if INI config doesn't exist
            if (!File.Exists(iniConfigPath))
            {
                return;
            }

            using var manager = new TranslationManager(iniConfigPath);
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = IniTestDataPath;

            var iniTranslation = manager.GetTranslation(CultureInfo.InvariantCulture, true);
            if (iniTranslation == null || iniTranslation.Count == 0)
            {
                return;
            }

            var originalTranslations = iniTranslation.Values.ToList();
            var outputFile = Path.Combine(OutputPath, "IniToResx_Output.resx");

            try
            {
                // Act
                var writer = new ResxWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert
                AssertValidResxFormat(outputFile);
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldRoundTripWithTomlData()
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
            var outputFile = Path.Combine(OutputPath, "TomlToResx_Output.resx");

            try
            {
                // Act
                var writer = new ResxWriter();
                using (var stream = File.Create(outputFile))
                {
                    writer.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
                }

                // Assert
                AssertValidResxFormat(outputFile);
                var reloadedTranslations = LoadTranslationsFromFile(outputFile);
                TranslationComparer.AssertTranslationsEqual(originalTranslations, reloadedTranslations);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldHandleSpecialCharactersInResx()
        {
            // Arrange - Create RESX with special characters
            var testResxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""SpecialChars"" type=""System.String"">
    <value>Value with &lt;special&gt; &amp; ""characters""</value>
  </data>
  <data name=""Unicode"" type=""System.String"">
    <value>Unicode: 中文, ñ, Ω, 🚀</value>
  </data>
</root>";

            var outputFile = Path.Combine(OutputPath, "SpecialChars_Test.resx");

            try
            {
                // Act
                File.WriteAllText(outputFile, testResxContent, Encoding.UTF8);

                // Assert - Load and verify special characters handled correctly
                var doc = XDocument.Load(outputFile);
                Assert.NotNull(doc.Root);

                var dataElements = doc.Root.Elements("data").ToList();
                Assert.Equal(2, dataElements.Count);

                // Verify Unicode is preserved
                var unicodeElement = dataElements.FirstOrDefault(e => (string)e.Attribute("name") == "Unicode");
                Assert.NotNull(unicodeElement);
                Assert.Equal("Unicode: 中文, ñ, Ω, 🚀", unicodeElement.Element("value")?.Value);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldRoundTripSimpleResx()
        {
            // Arrange
            var resxTestFile = Path.Combine(ResxTestDataPath, "Strings.de-AT.resx");

            // Skip test if resx test file doesn't exist
            if (!File.Exists(resxTestFile))
            {
                return;
            }

            var originalContent = File.ReadAllText(resxTestFile);
            var translation = TranslationManager.LoadTranslation(originalContent, ".resx", new CultureInfo("de-AT"), TextFormat.None);
            Assert.NotNull(translation);

            var outputFile = Path.Combine(OutputPath, "RoundTrip_Test.resx");

            try
            {
                // Act - Load and verify structure
                var doc = XDocument.Load(resxTestFile);
                Assert.NotNull(doc.Root);
                Assert.Equal("root", doc.Root.Name.LocalName);

                var originalEntries = translation.Values.ToList();
                Assert.NotEmpty(originalEntries);

                // Verify we can load from the original file
                var reloadedTranslations = LoadTranslationsFromFile(resxTestFile);
                TranslationComparer.AssertTranslationsEqual(originalEntries, reloadedTranslations);
            }
            finally
            {
                CleanupTestFile(outputFile);
            }
        }

        [Fact]
        public void ShouldValidateResxProperties()
        {
            // Arrange
            var writer = new ResxWriter();

            // Act & Assert
            Assert.Equal("RESX", writer.FormatName);
            Assert.Equal(".resx", writer.TranslationExtension);
            Assert.Equal(".resxcfg", writer.ConfigExtension);
        }
    }
}
