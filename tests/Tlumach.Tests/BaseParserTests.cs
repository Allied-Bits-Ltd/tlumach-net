// <copyright file="BaseParserTests.cs" company="Allied Bits Ltd.">
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "Base")]
    public class BaseParserTests
    {

        private const string TestFilesPath = "..\\..\\..\\TestData\\Base";

        static BaseParserTests()
        {
            TomlParser.Use();
            JsonParser.Use();
        }

        [Theory]
        [InlineData(TextFormat.BackslashEscaping, "TextFormat.BackslashEscaping")]
        [InlineData(TextFormat.DotNet, "TextFormat.DotNet")]
        public void ShouldConvertEscapeModeToStringRight(TextFormat textProcessingMode, string expected)
        {
            TranslationConfiguration config = new(null, string.Empty, null, null, null, textProcessingMode);
            Assert.Equal(expected, config.GetEscapeModeFullName());
        }

        [Theory]
        [ClassData(typeof(TemplateExpressionTestData))]
        public void ShouldDetectTemplatedString(string input, TextFormat escaping, bool? expected)
        {
            ArbParser.TextProcessingMode = escaping;

            if (expected is null)
                Assert.Throws<GenericParserException>(() => ArbParser.StringHasParameters(input, escaping));
            else
                Assert.Equal(expected, ArbParser.StringHasParameters(input, escaping));
        }

        [Fact]
        public void ShouldLoadValidConfigWithGroupsTOML()
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
        public void ShouldLoadValidConfigWithGroupsJSON()
        {

            JsonParser? parser = FileFormats.GetConfigParser(".jsoncfg") as JsonParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigWithGroups.jsoncfg"), string.Empty, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("StringsWithGroups.json", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("hello"));
            Assert.NotNull(tree.FindNode("ui"));
            Assert.True(tree.RootNode.ChildNodes.ContainsKey("logs"));
            TranslationTreeNode? node = tree.FindNode("logs.server");
            Assert.NotNull(node);
            Assert.True(node.Keys.ContainsKey("started"));
        }

        // for https://github.com/Allied-Bits-Ltd/tlumach-net/issues/7
        [Fact]
        public void Issue7ShouldNotRevertOthersToDefaultAfterSomeNotFound()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigIssue7.tomlcfg"));
            manager.LoadFromDisk = true;
            manager.TranslationsDirectory = TestFilesPath;
            manager.CurrentCulture = new System.Globalization.CultureInfo("uk-UK");
            TranslationUnit unitHello = new TranslationUnit(manager, manager.DefaultConfiguration, "hello", false);
            TranslationUnit unitWelcome = new TranslationUnit(manager, manager.DefaultConfiguration, "welcome", false);
            TranslationUnit unitGoodbye = new TranslationUnit(manager, manager.DefaultConfiguration, "goodbye", false);

            string hello = unitHello.CurrentValue;
            string welcome = unitWelcome.CurrentValue;
            string goodbye = unitGoodbye.CurrentValue;

            Assert.Equal("До побачення", goodbye);
        }
    }
}
