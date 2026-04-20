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

            Assert.NotNull(translation);
            Assert.Null(translation!.Locale);
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

        [Fact]
        public void ShouldFailOnMalformedXliff()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "invalid_malformed.xlf"));
            Assert.Throws<TextParseException>(() => parser.LoadTranslation(xliffContent, new CultureInfo("de"), null));
        }

        [Fact]
        public void ShouldFailOnMalformedXliffWithPositionCheck()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "invalid_malformed.xlf"));
            try
            {
                parser.LoadTranslation(xliffContent, new CultureInfo("de"), null);
                Assert.Fail("An exception has not been thrown");
            }
            catch (Exception ex)
            {
                Assert.True(ex is TextParseException);
                TextParseException? tex = ex as TextParseException;
                Assert.NotNull(tex);
                Assert.Equal(11, tex.LineNumber);
                Assert.Equal(7, tex.ColumnNumber);
            }
        }

        [Fact]
        public void ShouldParseComprehensiveXliff()
        {
            XliffParser parser = new();
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "Comprehensive.xlf"));

            // --- Source language (en-US) ---
            var source = parser.LoadTranslation(xliffContent, new CultureInfo("en-US"), null);

            Assert.NotNull(source);
            Assert.Equal("en-US", source.Locale);

            // 11 webapp units + 8 email units = 19 total
            Assert.Equal(19, source.Count);

            // Units inside <group> elements are discovered
            Assert.True(source.ContainsKey("U-NAV-HOME"));
            Assert.Equal("Home", source["U-NAV-HOME"].Text);

            Assert.True(source.ContainsKey("U-NAV-ORDERS"));
            Assert.Equal("Orders", source["U-NAV-ORDERS"].Text);

            // Note from <notes><note> container
            Assert.Equal("Short menu label.", source["U-NAV-HOME"].Comment);

            // Unit from nested checkout group
            Assert.True(source.ContainsKey("U-CHECKOUT-TITLE"));
            Assert.Equal("Review and place your order", source["U-CHECKOUT-TITLE"].Text);

            // <ph equiv="..."/> inline placeholder expands to equiv value
            Assert.True(source.ContainsKey("U-CHECKOUT-ITEMS"));
            Assert.Equal("You have {count} items in your cart.", source["U-CHECKOUT-ITEMS"].Text);

            // Multi-segment unit: two <segment> children concatenated with the default tab separator
            Assert.True(source.ContainsKey("U-CHECKOUT-WARNING"));
            Assert.Equal(
                "Your delivery address is incomplete.\tAdd the missing apartment, suite, or floor number to avoid delays.",
                source["U-CHECKOUT-WARNING"].Text);

            // translate="no" unit is still included
            Assert.True(source.ContainsKey("U-CHECKOUT-LEGAL"));
            Assert.Equal("PCI-DSS-V4-CHECKOUT", source["U-CHECKOUT-LEGAL"].Text);

            // <ignorable> element is treated like a segment
            Assert.True(source.ContainsKey("U-CHECKOUT-COMINGSOON"));
            Assert.Equal("Coming soon", source["U-CHECKOUT-COMINGSOON"].Text);

            // Units from the second <file> element are discovered
            Assert.True(source.ContainsKey("U-EMAIL-SUBJECT"));
            Assert.Equal("Reset your password", source["U-EMAIL-SUBJECT"].Text);

            // Multiple <ph> placeholders in one segment
            Assert.True(source.ContainsKey("U-EMAIL-EXPIRY"));
            Assert.Equal("This link expires in {minutes} minutes at {time}.", source["U-EMAIL-EXPIRY"].Text);

            // Multi-segment email unit
            Assert.True(source.ContainsKey("U-EMAIL-BODY2"));
            Assert.Equal(
                "If you made this request, click the button below.\tIf you did not request a password reset, you can safely ignore this email.",
                source["U-EMAIL-BODY2"].Text);

            // --- Target language (sk-SK) ---
            var target = parser.LoadTranslation(xliffContent, new CultureInfo("sk-SK"), null);

            Assert.NotNull(target);
            Assert.Equal("sk-SK", target.Locale);

            // 2 units have no <target> (translate="no" source-only units) → 19 - 2 = 17
            Assert.Equal(17, target.Count);

            Assert.True(target.ContainsKey("U-NAV-HOME"));
            Assert.Equal("Domov", target["U-NAV-HOME"].Text);

            // Source language text stored as SourceText on target entries
            Assert.Equal("Home", target["U-NAV-HOME"].SourceText);

            // Note is available on target entries too
            Assert.Equal("Short menu label.", target["U-NAV-HOME"].Comment);

            // Multi-segment target concatenated with the default tab separator
            Assert.True(target.ContainsKey("U-CHECKOUT-WARNING"));
            Assert.Equal(
                "Vaša doručovacia adresa nie je úplná.\tDoplňte chýbajúce číslo bytu, apartmánu alebo poschodia, aby ste predišli oneskoreniu.",
                target["U-CHECKOUT-WARNING"].Text);

            // Target placeholder expansion
            Assert.True(target.ContainsKey("U-CHECKOUT-ITEMS"));
            Assert.Equal("V košíku máte {count} položiek.", target["U-CHECKOUT-ITEMS"].Text);

            // Units without a <target> are absent from the target translation
            Assert.False(target.ContainsKey("U-CHECKOUT-LEGAL"));
            Assert.False(target.ContainsKey("U-EMAIL-SIGNATURE"));
        }

        [Fact]
        public void ShouldRespectCustomSegmentSeparator()
        {
            // Verify that a custom separator is used instead of the default tab.
            var parser = new XliffParser { SegmentSeparator = " | " };
            var xliffContent = File.ReadAllText(Path.Combine(TestFilesPath, "Comprehensive.xlf"));

            var source = parser.LoadTranslation(xliffContent, new CultureInfo("en-US"), null);

            Assert.NotNull(source);
            Assert.Equal(
                "Your delivery address is incomplete. | Add the missing apartment, suite, or floor number to avoid delays.",
                source["U-CHECKOUT-WARNING"].Text);
        }

        [Fact]
        public void ShouldDefaultSegmentSeparatorToTab()
        {
            var parser = new XliffParser();
            Assert.Equal("\t", parser.SegmentSeparator);
        }
    }
}
