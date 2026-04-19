// <copyright file="XliffParserTests.cs" company="Allied Bits Ltd.">
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

using Tlumach;
using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "Xliff")]
    public class XliffParserTests
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\Xliff";

        static XliffParserTests()
        {
            XliffParser.Use();
        }

        [Fact]
        public void ShouldParseSourceLanguageFromXliff()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "simple_en_fr.xlf"));

            var translation = parser.LoadTranslation(xliffContent, new CultureInfo("en"), null);

            Assert.NotNull(translation);
            Assert.Equal("en", translation.Locale);
            Assert.True(translation.ContainsKey("GREETING"));
            Assert.Equal("Hello", translation["GREETING"].Text);
        }

        [Fact]
        public void ShouldParseTargetLanguageFromXliff()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "simple_en_fr.xlf"));

            var translation = parser.LoadTranslation(xliffContent, new CultureInfo("fr"), null);

            Assert.NotNull(translation);
            Assert.Equal("fr", translation.Locale);
            Assert.True(translation.ContainsKey("GREETING"));
            Assert.Equal("Bonjour", translation["GREETING"].Text);
        }

        [Fact]
        public void ShouldStorePairedLanguageInSourceText()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "simple_en_fr.xlf"));

            // Parse target language and check that source is stored in SourceText
            var translation = parser.LoadTranslation(xliffContent, new CultureInfo("fr"), null);

            Assert.NotNull(translation);
            Assert.True(translation.ContainsKey("GREETING"));
            Assert.Equal("Bonjour", translation["GREETING"].Text);
            Assert.Equal("Hello", translation["GREETING"].SourceText);
        }

        [Fact]
        public void ShouldParseXliffWithNotes()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "simple_en_fr.xlf"));

            var translation = parser.LoadTranslation(xliffContent, new CultureInfo("fr"), null);

            Assert.NotNull(translation);
            Assert.True(translation.ContainsKey("THANKS"));
            Assert.Equal("Polite greeting", translation["THANKS"].Comment);
        }

        [Fact]
        public void ShouldParseSourceOnlyXliff()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "source_only_en.xlf"));

            var translation = parser.LoadTranslation(xliffContent, new CultureInfo("en"), null);

            Assert.NotNull(translation);
            Assert.Equal("en", translation.Locale);
            Assert.True(translation.ContainsKey("GREETING"));
            Assert.Equal("Hello", translation["GREETING"].Text);
        }

        [Fact]
        public void ShouldReturnEmptyForUnsupportedCulture()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "simple_en_fr.xlf"));

            var translation = parser.LoadTranslation(xliffContent, new CultureInfo("de"), null);

            Assert.Null(translation.Locale);
        }

        [Fact]
        public void ShouldCanHandleXliffExtensions()
        {
            XliffParser parser = new();

            Assert.True(parser.CanHandleExtension(".xlf"));
            Assert.True(parser.CanHandleExtension(".xliff"));
            Assert.False(parser.CanHandleExtension(".json"));
            Assert.False(parser.CanHandleExtension(".resx"));
        }

        [Fact]
        public void ShouldHaveStaticSourceFilenameProperty()
        {
            var originalValue = XliffParser.SourceFilename;
            try
            {
                XliffParser.SourceFilename = "test.json";
                Assert.Equal("test.json", XliffParser.SourceFilename);
            }
            finally
            {
                XliffParser.SourceFilename = originalValue;
            }
        }
    }
}
