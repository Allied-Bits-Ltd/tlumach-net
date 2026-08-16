// <copyright file="ConfigurationLocaleTests.cs" company="Allied Bits Ltd.">
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
    /// Verifies that the locale of the default file is read from the canonical "defaultLocale" key
    /// and from the deprecated "defaultFileLocale" alias that older documentation advertised.
    /// </summary>
    [Trait("Category", "Parser")]
    [Trait("Category", "Configuration")]
    public class ConfigurationLocaleTests
    {
        private static TranslationConfiguration Parse(BaseParser parser, string content)
        {
            TranslationConfiguration? config = parser.ParseConfiguration(content, Assembly.GetExecutingAssembly());
            Assert.NotNull(config);
            return config!;
        }

        private static string IniConfig(string localeKey)
            => $"defaultFile=sample.ini\n{localeKey}=en-UK\n";

        private static string TomlConfig(string localeKey)
            => $"defaultFile = \"sample.toml\"\n{localeKey} = \"en-UK\"\n";

        private static string JsonConfig(string localeKey)
            => $"{{\n    \"defaultFile\": \"sample.json\",\n    \"{localeKey}\": \"en-UK\"\n}}";

        private static string XmlConfig(string localeKey)
            => $"<root>\n    <defaultFile>sample.resx</defaultFile>\n    <{localeKey}>en-UK</{localeKey}>\n</root>";

        [Theory]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE)]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS)]
        public void ShouldReadDefaultLocaleFromIniConfig(string localeKey)
        {
            Assert.Equal("en-UK", Parse(new IniParser(), IniConfig(localeKey)).DefaultFileLocale);
        }

        [Theory]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE)]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS)]
        public void ShouldReadDefaultLocaleFromTomlConfig(string localeKey)
        {
            Assert.Equal("en-UK", Parse(new TomlParser(), TomlConfig(localeKey)).DefaultFileLocale);
        }

        [Theory]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE)]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS)]
        public void ShouldReadDefaultLocaleFromJsonConfig(string localeKey)
        {
            Assert.Equal("en-UK", Parse(new JsonParser(), JsonConfig(localeKey)).DefaultFileLocale);
        }

        [Theory]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE)]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS)]
        public void ShouldReadDefaultLocaleFromArbConfig(string localeKey)
        {
            Assert.Equal("en-UK", Parse(new ArbParser(), JsonConfig(localeKey)).DefaultFileLocale);
        }

        [Theory]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE)]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS)]
        public void ShouldReadDefaultLocaleFromResxConfig(string localeKey)
        {
            Assert.Equal("en-UK", Parse(new ResxParser(), XmlConfig(localeKey)).DefaultFileLocale);
        }

        [Theory]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE)]
        [InlineData(TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS)]
        public void ShouldReadDefaultLocaleFromXliffConfig(string localeKey)
        {
            Assert.Equal("en-UK", Parse(new XliffParser(), XmlConfig(localeKey)).DefaultFileLocale);
        }

        // The canonical key wins when a configuration file happens to carry both names.

        [Fact]
        public void ShouldPreferCanonicalKeyOverAliasInKeyValueConfig()
        {
            var content = $"defaultFile=sample.ini\n{TranslationConfiguration.KEY_DEFAULT_LOCALE}=en-UK\n{TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS}=de-AT\n";
            Assert.Equal("en-UK", Parse(new IniParser(), content).DefaultFileLocale);
        }

        [Fact]
        public void ShouldPreferCanonicalKeyOverAliasInJsonConfig()
        {
            var content = $"{{\n    \"defaultFile\": \"sample.json\",\n    \"{TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS}\": \"de-AT\",\n    \"{TranslationConfiguration.KEY_DEFAULT_LOCALE}\": \"en-UK\"\n}}";
            Assert.Equal("en-UK", Parse(new JsonParser(), content).DefaultFileLocale);
        }

        [Fact]
        public void ShouldPreferCanonicalKeyOverAliasInXmlConfig()
        {
            var content = $"<root>\n    <defaultFile>sample.resx</defaultFile>\n    <{TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS}>de-AT</{TranslationConfiguration.KEY_DEFAULT_LOCALE_ALIAS}>\n    <{TranslationConfiguration.KEY_DEFAULT_LOCALE}>en-UK</{TranslationConfiguration.KEY_DEFAULT_LOCALE}>\n</root>";
            Assert.Equal("en-UK", Parse(new ResxParser(), content).DefaultFileLocale);
        }
    }
}
