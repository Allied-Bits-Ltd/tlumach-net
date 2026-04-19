// <copyright file="XliffWriterTests.cs" company="Allied Bits Ltd.">
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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Tlumach;
using Tlumach.Base;
using Tlumach.Writers;

#pragma warning disable MA0011 // Use an overload of ... that has a ... parameter
namespace Tlumach.WriterTests
{
    [Trait("Category", "Writer")]
    [Trait("Category", "Xliff")]
    public class XliffWriterTests
    {
        private static readonly string TestDataBaseDirectory = GetTestDataDirectory();
        private static readonly string XliffTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Xliff");
        private static readonly string JsonTestDataPath = Path.Combine(TestDataBaseDirectory, "TestData", "Json");
        private static readonly string OutputPath = Path.Combine(Path.GetTempPath(), "XliffWriterTests");

        static XliffWriterTests()
        {
            BaseParser.KeepEntryOrder = true;

            XliffParser.Use();
            JsonParser.Use();
        }

        private static string GetTestDataDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDirectory = Path.GetDirectoryName(assembly.Location) ?? string.Empty;
            var testDirectory = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", ".."));
            return Path.Combine(testDirectory, "Tlumach.Tests");
        }

        public XliffWriterTests()
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

        private void AssertValidXliffFormat(string filePath)
        {
            Assert.True(File.Exists(filePath), $"File {filePath} should exist");

            var content = File.ReadAllText(filePath);
            Assert.NotEmpty(content);

            // Verify it's valid XML
            var doc = XDocument.Load(filePath);
            var root = doc.Root;
            Assert.NotNull(root);
            Assert.Equal("xliff", root.Name.LocalName);

            // Check required attributes
            Assert.NotNull(root.Attribute("version"));
            Assert.NotNull(root.Attribute("srcLang"));
        }

        [Fact]
        public void ShouldHaveRequiredProperties()
        {
            var writer = new XliffWriter();

            // Verify properties are settable
            writer.SourceFile = "test.json";
            writer.TargetFile = "target.xlf";

            Assert.Equal("test.json", writer.SourceFile);
            Assert.Equal("target.xlf", writer.TargetFile);
        }

        [Fact]
        public void ShouldHaveCorrectFormat()
        {
            var writer = new XliffWriter();

            Assert.Equal("XLIFF", writer.FormatName);
            Assert.Equal(".xlfcfg", writer.ConfigExtension);
            Assert.Equal(".xlf", writer.TranslationExtension);
        }

        [Fact]
        public void ShouldThrowOnMultipleCultures()
        {
            // Create minimal translation manager with translations
            var trans = new Translation(locale: "en");
            trans.Add("TEST", new TranslationEntry("test", "value"));

            var manager = TranslationManager.Empty;
            var writer = new XliffWriter();

            using (var stream = new MemoryStream())
            {
                Assert.Throws<TlumachException>(() =>
                {
                    writer.WriteTranslations(manager, [new CultureInfo("en"), new CultureInfo("de")], stream);
                });
            }
        }
    }
}
