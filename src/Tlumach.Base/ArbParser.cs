// <copyright file="ArbParser.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tlumach.Base
{
    internal class ArbParser : BaseFileParser
    {
        private const string ARB_KEY_LOCALE = "@@locale";
        private const string ARB_KEY_CONTEXT = "@@context";

        static ArbParser()
        {
            // We register the parser for both configuration files and translation files.
            // This approach enables us to use configuration and translations in different formats.
            FileFormats.RegisterConfigParser(".arbcfg", Factory);
            FileFormats.RegisterParser(".arb", Factory);
        }

        private static BaseFileParser Factory() => new ArbParser();

        public override bool CanHandleExtension(string fileExtension)
        {
            return fileExtension.Equals(".arb", StringComparison.OrdinalIgnoreCase);
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent)
        {
            try
            {
                JObject? configObj = JObject.Parse(fileContent);

                //if (!configObj.TryGetValue(TranslationConfiguration.KEY_DEFAULT_FILE, out JToken? defaultFileToken) || defaultFileToken.Type != JTokenType.String)
                //    return TranslationConfiguration.Empty;

                string? defaultFile = configObj.Value<string>(TranslationConfiguration.KEY_DEFAULT_FILE);
                string? defaultLocale = configObj.Value<string>(TranslationConfiguration.KEY_DEFAULT_LOCALE);
                string? generatedNamespace = configObj.Value<string>(TranslationConfiguration.KEY_GENERATED_NAMESPACE);
                string? generatedClassName = configObj.Value<string>(TranslationConfiguration.KEY_GENERATED_CLASS);

                TranslationConfiguration result = new TranslationConfiguration(defaultFile, generatedNamespace, generatedClassName, defaultLocale);

                // If the configuration contains the Translations section, parse it
                if (configObj.TryGetValue(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, out JToken? translationsToken) && translationsToken is JObject translationsObject)
                {
                    // Enumerate properties
                    foreach (JProperty prop in translationsObject.Properties())
                    {
                        string lang = prop.Name.Trim();
                        if (lang.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK))
                            lang = TranslationConfiguration.KEY_TRANSLATION_DEFAULT;
                        else
                            lang = lang.ToUpperInvariant();
                        if (prop.Value.Type == JTokenType.String)
                        {
                            string value = prop.Value.ToString().Trim();
                            if (result.Translations.ContainsKey(lang))
                                throw new GenericParserException($"Duplicate translation reference '{prop.Name}' specified in the list of translations");
                            result.Translations.Add(lang, value);
                        }
                        else
                        {
                            throw new GenericParserException($"Translation reference '{prop.Name}' is not a string");
                        }
                    }
                }

                return result;
            }
            catch (JsonReaderException ex)
            {
                int pos = GetAbsolutePosition(fileContent, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }

        /*public override bool IsValidConfigFile(string fileContent, out TranslationConfiguration? configuration)
        {
            configuration = InternalLoadConfig(fileContent);

            if ((configuration is not null) && !string.IsNullOrEmpty(configuration.DefaultFile) && File.Exists(configuration.DefaultFile))
            {
                return true;
            }

            return false;
        }*/

        public override Translation? LoadTranslation(string translationText)
        {
            try
            {
                JObject? jsonObj = JObject.Parse(translationText);

                Translation result =  InternalLoadTranslationEntryFromJSON(jsonObj, null, string.Empty);

                return result;
            }
            catch (JsonReaderException ex)
            {
                int pos = GetAbsolutePosition(translationText, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of the translation has failed", ex);
            }
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
        {
            try
            {
                JObject? jsonObj = JObject.Parse(content);

                TranslationTree result = new();

                InternalLoadTreeNodeFromJSON(jsonObj, result, result.RootNode);

                return result;
            }
            catch (JsonReaderException ex)
            {
                int pos = GetAbsolutePosition(content, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }

        private static int GetAbsolutePosition(string text, int lineNumber, int linePosition)
        {
            // LineNumber and LinePosition are 1-based
            int currentLine = 1;
            int index = 0;

            while (currentLine < lineNumber && index < text.Length)
            {
                if (text[index] == '\n')
                {
                    currentLine++;
                }

                index++;
            }

            // Add position within the target line (minus 1 because LinePosition is 1-based)
            return index + (linePosition - 1);
        }

        private Translation InternalLoadTranslationEntryFromJSON(JObject jsonObj, Translation? translation, string groupName)
        {
            if (translation is null)
            {
                string? locale = jsonObj.Value<string>(ARB_KEY_LOCALE);
                string? context = jsonObj.Value<string>(ARB_KEY_CONTEXT);
                translation = new Translation(locale, context);
            }

            TranslationEntry entry;

            // Enumerate string properties
            foreach (var prop in jsonObj.Properties().Where(p => p.Value.Type == JTokenType.String))
            {
                string key = prop.Name.Trim();
                // todo: decide what to do with the keys that start with an @
                /*
                if (key.StartsWith("@"))
                    continue;
                */

                if (!string.IsNullOrEmpty(groupName))
                    key = groupName + "_" + key;

                if (translation.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");
                string? value = prop.Value.Value<string>();

                entry = new(value);
                translation.Add(key, entry);
            }

            // todo: implement
            return translation;
        }

        private void InternalLoadTreeNodeFromJSON(JObject jsonObj, TranslationTree tree, TranslationTreeNode parentNode)
        {
            // Enumerate string properties, which will be keys
            foreach (var prop in jsonObj.Properties().Where(p => p.Value.Type == JTokenType.String))
            {
                string key = prop.Name.Trim();
                // This should not happen but someone may misunderstand the format and use @ with string properties instead of objects
                if (key.StartsWith("@"))
                    continue;

                if (parentNode.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate key '{key}' specified");
                parentNode.Keys.Add(key);
            }

            // Enumerate object properties, which will be groups
            foreach (var prop in jsonObj.Properties().Where(p => p.Value.Type == JTokenType.Object))
            {
                string name = prop.Name.Trim();

                // Skip child JSON nodes which, in ARB format, are supplementary information about an entry.
                // This information is used when loading phrases, but not when building a tree.
                if (name.StartsWith("@"))
                    continue;

                if (parentNode.ChildNodes.Keys.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate group name '{name}' specified");

                var jsonChild = (JObject)prop.Value;

                var childNode = parentNode.MakeNode(name);
                if (childNode is null)
                    throw new GenericParserException($"Group '{name}' could not be used to build a tree of translation entries");

                InternalLoadTreeNodeFromJSON(jsonChild, tree, childNode);
            }
        }
    }
}
