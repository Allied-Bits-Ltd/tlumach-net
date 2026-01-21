// <copyright file="TomlParserTests.cs" company="Allied Bits Ltd.">
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "KeyValue")]
    [Trait("Category", "TOML")]
    public class TomlParserTests
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\TOML";

        static TomlParserTests()
        {
            TomlParser.Use();
        }

        [Theory]
        [InlineData("ConfigInvalidJunk.tomlcfg")]
        [InlineData("ConfigInvalidKey.tomlcfg")]
        [InlineData("ConfigInvalidSection.tomlcfg")]
        [InlineData("ConfigInvalidSectionName.tomlcfg")]
        [InlineData("ConfigInvalidSeparator.tomlcfg")]
        [InlineData("ConfigInvalidLine.tomlcfg")]
        public void ShouldFailOnInvalidConfig(string configFile)
        {
            Assert.Throws<ParserFileException>(() => new TranslationManager(Path.Combine(TestFilesPath, configFile)));
        }

        [Theory]
        [InlineData("ConfigInvalidJunk.tomlcfg", 3, 6)]
        [InlineData("ConfigInvalidKey.tomlcfg", 2, 8)]
        [InlineData("ConfigInvalidSection.tomlcfg", 4, 9)]
        [InlineData("ConfigInvalidSectionName.tomlcfg", 4, 2)]
        [InlineData("ConfigInvalidSeparator.tomlcfg", 2, 13)]
        [InlineData("ConfigInvalidLine.tomlcfg", 2, 28)]
        public void ShouldFailOnInvalidConfigWithPositionCheck(string configFile, int lineNumber, int columnNumber)
        {
            try
            {
                _ = new TranslationManager(Path.Combine(TestFilesPath, configFile));
                Assert.Fail("An exception has not been thrown");
            }
            catch (Exception ex)
            {
                Assert.True(ex is ParserFileException);
                Assert.NotNull(ex.InnerException);
                Assert.True(ex.InnerException is TextParseException);
                TextParseException? tex = ex.InnerException as TextParseException;
                Assert.NotNull(tex);
                Assert.Equal(lineNumber, tex.LineNumber);
                Assert.Equal(columnNumber, tex.ColumnNumber);
            }
        }

        [Fact]
        public void ShouldLoadValidConfigWithTranslations()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.tomlcfg"));
            Assert.Equal("Strings.toml", manager.DefaultConfiguration?.DefaultFile);
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE-AT"), "de-AT translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE"), "de translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("other"), "translation for 'other' not found");
        }

        [Fact]
        public void ShouldLoadValidConfigWithGroups()
        {
            TomlParser? parser = FileFormats.GetConfigParser(".tomlcfg") as TomlParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigWithGroups.tomlcfg"), string.Empty, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("StringsWithGroups.toml", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("hello"));
            Assert.NotNull(tree.FindNode("ui"));
            Assert.True(tree.RootNode.ChildNodes.ContainsKey("logs"));
            TranslationTreeNode? node = tree.FindNode("logs.server");
            Assert.NotNull(node);
            Assert.True(node.Keys.ContainsKey("started"));
        }

        [Fact]
        public void ShouldGetKeyFromGroup()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithGroups.tomlcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithGroups.toml", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("sk"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Started", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRef()
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithRef.tomlcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithRef.toml", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("sk"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyWithRefMissing()
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithRefMissing.tomlcfg"));
            manager.OnReferenceNotResolved += (sender, args) => { args.Text = null; };
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            Assert.Equal("StringsWithRefMissing.toml", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("de"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Logging has been started.", entry.Text);
        }

        [Theory]
        [InlineData("Basic", "A basic string\twith some escaping")]
        [InlineData("MultilineBasic", "A basic string\twith some \nescaping")]
        [InlineData("MultilineBasicWithSpaces", "A basic string\twith some \n  escaping")]
        [InlineData("MultilineBasicWithEscaping", "A basic string\twith some escaping")]
        [InlineData("MultilineBasicWithEscaping2", "A basic string\twith some escaping")]
        [InlineData("MultilineBasicWithNL", "A basic string\twith some\nescaping")]
        [InlineData("Literal", "A basic\tstring")]
        [InlineData("LiteralWithQuote", "A basic 'string'\t")]
        [InlineData("MultilineLiteral", "A\tbasic\nstring")]
        public void ShouldGetKeyWithComplexStrings(string keyName, string expected)
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithComplexStrings.tomlcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue(keyName);
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal(expected, entry.Text);
        }

        [Theory]
        [InlineData("EmptyLiteral")]
        [InlineData("EmptyBasic")]
        [InlineData("EmptyMultilineLiteral")]
        [InlineData("EmptyMultilineBasic")]
        public void ShouldGetKeyWithComplexStringsEmpty(string keyName)
        {
            BaseParser.RecognizeFileRefs = true;
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithComplexStrings.tomlcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            TranslationEntry entry = manager.GetValue(keyName);
            Assert.NotNull(entry.Text);
            Assert.Equal(0, entry.Text.Length);
        }

        [Fact]
        public void ShouldCountLinesRight()
        {
            try
            {
                var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithDuplicates.tomlcfg"));
                manager.LoadFromDisk = true;
                manager.TranslationsDirectory = TestFilesPath;
                TranslationEntry entry = manager.GetValue("fake");

                Assert.Fail("The loading of the translation should have ended with an exception, but it has not.");
            }
            catch (TextFileParseException ex)
            {
                var pex = ex.InnerException as Tlumach.Base.TextParseException;
                Assert.NotNull(pex);

                Assert.Equal(21, pex.LineNumber);
            }
        }
    }
}
