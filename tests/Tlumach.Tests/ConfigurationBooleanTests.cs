// <copyright file="ConfigurationBooleanTests.cs" company="Allied Bits Ltd.">
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

using System.Reflection;

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Verifies that the boolean settings of a configuration (delayedUnitsCreation, onlyDeclareKeys, createFilledMethods, createStringAccessors)
    /// are accepted both as native booleans of JSON and TOML and as strings.
    /// </summary>
    [Trait("Category", "Parser")]
    [Trait("Category", "Configuration")]
    public class ConfigurationBooleanTests
    {
        private static TranslationConfiguration Parse(BaseParser parser, string content)
        {
            TranslationConfiguration? config = parser.ParseConfiguration(content, Assembly.GetExecutingAssembly());
            Assert.NotNull(config);
            return config!;
        }

        private static void AssertSettings(TranslationConfiguration config, bool expected)
        {
            Assert.Equal(expected, config.DelayedUnitsCreation);
            Assert.Equal(expected, config.OnlyDeclareKeys);
            Assert.Equal(expected, config.CreateFilledMethods);
            Assert.Equal(expected, config.CreateStringAccessors);
        }

        private static string JsonConfig(string value)
            => "{\n" +
               "    \"defaultFile\": \"sample.json\",\n" +
               $"    \"delayedUnitsCreation\": {value},\n" +
               $"    \"onlyDeclareKeys\": {value},\n" +
               $"    \"createFilledMethods\": {value},\n" +
               $"    \"createStringAccessors\": {value}\n" +
               "}";

        private static string TomlConfig(string value, string eol = "\n")
            => "defaultFile = \"sample.toml\"" + eol +
               $"delayedUnitsCreation = {value}" + eol +
               $"onlyDeclareKeys = {value}" + eol +
               $"createFilledMethods = {value}" + eol +
               $"createStringAccessors = {value}" + eol;

        public static TheoryData<string, bool> JsonValues => new()
        {
            { "true", true },
            { "false", false },
            { "\"true\"", true },
            { "\"false\"", false },
            { "\"True\"", true },
            { "null", false },
        };

        public static TheoryData<string, bool> TomlValues => new()
        {
            { "true", true },
            { "false", false },
            { "true   ", true },
            { "false\t", false },
            { "\"true\"", true },
            { "\"false\"", false },
            { "'true'", true },
        };

        [Theory]
        [MemberData(nameof(JsonValues))]
        public void ShouldReadBooleanSettingsFromJsonConfig(string value, bool expected)
        {
            AssertSettings(Parse(new JsonParser(), JsonConfig(value)), expected);
        }

        [Theory]
        [MemberData(nameof(JsonValues))]
        public void ShouldReadBooleanSettingsFromArbConfig(string value, bool expected)
        {
            AssertSettings(Parse(new ArbParser(), JsonConfig(value)), expected);
        }

        [Theory]
        [MemberData(nameof(JsonValues))]
        public void ShouldReadBooleanSettingsFromStringCatalogConfig(string value, bool expected)
        {
            AssertSettings(Parse(new StringCatParser(), JsonConfig(value)), expected);
        }

        [Theory]
        [MemberData(nameof(TomlValues))]
        public void ShouldReadBooleanSettingsFromTomlConfig(string value, bool expected)
        {
            AssertSettings(Parse(new TomlParser(), TomlConfig(value)), expected);
        }

        [Theory]
        [MemberData(nameof(TomlValues))]
        public void ShouldReadBooleanSettingsFromTomlConfigWithCrLf(string value, bool expected)
        {
            AssertSettings(Parse(new TomlParser(), TomlConfig(value, "\r\n")), expected);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void ShouldReadBooleanAtEndOfTomlConfigWithoutEol(string value, bool expected)
        {
            TranslationConfiguration config = Parse(new TomlParser(), $"defaultFile = \"sample.toml\"\ndelayedUnitsCreation = {value}");
            Assert.Equal(expected, config.DelayedUnitsCreation);
        }

        [Fact]
        public void ShouldReadTranslationsAfterBooleanInTomlConfig()
        {
            TranslationConfiguration config = Parse(new TomlParser(), "defaultFile = \"sample.toml\"\ndelayedUnitsCreation = false\n\n[translations]\nde = \"sample_de.toml\"\n");
            Assert.False(config.DelayedUnitsCreation);
            Assert.Equal("sample_de.toml", config.Translations["DE"]);
        }

        [Theory]
        [InlineData("truex")]
        [InlineData("falsey")]
        [InlineData("yes")]
        public void ShouldRejectUnquotedNonBooleanInTomlConfig(string value)
        {
            Assert.ThrowsAny<Exception>(() => new TomlParser().ParseConfiguration($"defaultFile = \"sample.toml\"\ndelayedUnitsCreation = {value}\n", Assembly.GetExecutingAssembly()));
        }
    }
}
