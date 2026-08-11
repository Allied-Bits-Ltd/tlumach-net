// <copyright file="ConfigurationWriterTests.cs" company="Allied Bits Ltd.">
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
using System.IO;
using System.Reflection;
using System.Text;

using Tlumach.Base;
using Tlumach.Writers;

namespace Tlumach.WriterTests
{
    /// <summary>
    /// Tests that a configuration written by a writer can be read back by the parser of the same format, with every setting preserved.
    /// <para>Every format names its entries with the key constants of <see cref="TranslationConfiguration"/>, so a writer that spells a name differently produces a file that its own parser cannot read.
    /// These tests pin that agreement for each format that supports configuration files.</para>
    /// </summary>
    [Trait("Category", "Writer")]
    [Trait("Category", "Configuration")]
    public class ConfigurationWriterTests
    {
        static ConfigurationWriterTests()
        {
            ResxParser.Use();
            XliffParser.Use();
            JsonParser.Use();
            IniParser.Use();
            TomlParser.Use();
        }

        public static TheoryData<string, BaseWriter, BaseParser> Formats() => new()
        {
            { "RESX (XML)", new ResxWriter(), new ResxParser() },
            { "XLIFF (XML)", new XliffWriter(), new XliffParser() },
            { "JSON", new JsonWriter(), new JsonParser() },
            { "INI", new IniWriter(), new IniParser() },
            { "TOML", new TomlWriter(), new TomlParser() },
        };

        [Theory]
        [MemberData(nameof(Formats))]
        public void ShouldReadBackEverySettingItWrote(string format, BaseWriter writer, BaseParser parser)
        {
            TranslationConfiguration original = new(
                assembly: null,
                defaultFile: "Strings.resx",
                @namespace: "Sample.Translations",
                className: "Strings",
                defaultFileLocale: "en-US",
                textProcessingMode: TextFormat.DotNet,
                delayedUnitCreation: true,
                onlyDeclareKeys: false,
                createFilledMethods: true,
                createStringAccessors: true,
                stringAccessorsClass: "Labels",
                stringAccessorsCulture: "ambient");

            original.Translations.Add("DE", "Strings.de.resx");
            original.Translations.Add("DE-AT", "Strings.de-AT.resx");

            TranslationConfiguration restored = WriteAndParse(writer, parser, original);

            Assert.Equal(original.DefaultFile, restored.DefaultFile);
            Assert.Equal(original.DefaultFileLocale, restored.DefaultFileLocale);
            Assert.Equal(original.Namespace, restored.Namespace);
            Assert.Equal(original.ClassName, restored.ClassName);
            Assert.Equal(original.TextProcessingMode, restored.TextProcessingMode);
            Assert.Equal(original.DelayedUnitsCreation, restored.DelayedUnitsCreation);
            Assert.Equal(original.OnlyDeclareKeys, restored.OnlyDeclareKeys);
            Assert.Equal(original.CreateFilledMethods, restored.CreateFilledMethods);
            Assert.Equal(original.CreateStringAccessors, restored.CreateStringAccessors);
            Assert.Equal(original.StringAccessorsClass, restored.StringAccessorsClass);
            Assert.Equal(original.StringAccessorsCulture, restored.StringAccessorsCulture);

            Assert.Equal(original.Translations.Count, restored.Translations.Count);
            foreach (KeyValuePair<string, string> translation in original.Translations)
                Assert.Equal(translation.Value, restored.Translations[translation.Key]);
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ShouldNotInventSettingsThatWereNotSet(string format, BaseWriter writer, BaseParser parser)
        {
            TranslationConfiguration original = new(
                assembly: null,
                defaultFile: "Strings.resx",
                @namespace: "Sample.Translations",
                className: "Strings",
                defaultFileLocale: null,
                textProcessingMode: TextFormat.None,
                delayedUnitCreation: false,
                onlyDeclareKeys: false,
                createFilledMethods: false);

            TranslationConfiguration restored = WriteAndParse(writer, parser, original);

            Assert.False(restored.DelayedUnitsCreation);
            Assert.False(restored.OnlyDeclareKeys);
            Assert.False(restored.CreateFilledMethods);
            Assert.False(restored.CreateStringAccessors);
            Assert.True(string.IsNullOrEmpty(restored.StringAccessorsClass));
            Assert.True(string.IsNullOrEmpty(restored.StringAccessorsCulture));
        }

        private static TranslationConfiguration WriteAndParse(BaseWriter writer, BaseParser parser, TranslationConfiguration original)
        {
            TranslationManager manager = new(original);

            using MemoryStream stream = new();
            writer.WriteConfiguration(manager, stream);

            // Read the bytes the way the library reads a file from disk: with a StreamReader that detects a byte order mark. The XML writer emits one, and a caller that decoded the bytes by hand would
            // keep it and then fail to parse the very first character.
            stream.Position = 0;
            using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string text = reader.ReadToEnd();
            Assert.False(string.IsNullOrWhiteSpace(text));

            TranslationConfiguration? restored = parser.ParseConfiguration(text, Assembly.GetExecutingAssembly());
            Assert.NotNull(restored);

            return restored;
        }
    }
}
