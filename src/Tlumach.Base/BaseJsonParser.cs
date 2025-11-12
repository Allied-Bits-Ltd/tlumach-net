// <copyright file="BaseJsonParser.cs" company="Allied Bits Ltd.">
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tlumach.Base
{
    /// <summary>
    /// The base parser for JSON-formatted configuration and translation files.
    /// </summary>
    public abstract class BaseJsonParser : BaseFileParser
    {
        public override TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly)
        {
            try
            {
                JObject? configObj = JObject.Parse(fileContent);

                string? defaultFile = configObj.Value<string>(TranslationConfiguration.KEY_DEFAULT_FILE)?.Trim();
                string? defaultLocale = configObj.Value<string>(TranslationConfiguration.KEY_DEFAULT_LOCALE)?.Trim();
                string? generatedNamespace = configObj.Value<string>(TranslationConfiguration.KEY_GENERATED_NAMESPACE)?.Trim();
                string? generatedClassName = configObj.Value<string>(TranslationConfiguration.KEY_GENERATED_CLASS)?.Trim();

                TranslationConfiguration result = new TranslationConfiguration(assembly, defaultFile ?? string.Empty, generatedNamespace, generatedClassName, defaultLocale, GetTemplateEscapeMode());

                if (string.IsNullOrEmpty(defaultFile))
                    return result;

                // If the configuration contains the Translations section, parse it
                if (configObj.TryGetValue(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, StringComparison.OrdinalIgnoreCase, out JToken? translationsToken) && translationsToken is JObject translationsObject)
                {
                    // Enumerate properties
                    foreach (JProperty prop in translationsObject.Properties())
                    {
                        string lang = prop.Name.Trim();
                        if (lang.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal))
                            lang = TranslationConfiguration.KEY_TRANSLATION_OTHER;
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

        public override Translation? LoadTranslation(string translationText)
        {
            try
            {
                JObject? jsonObj = JObject.Parse(translationText);

                return InternalLoadTranslationEntryFromJSON(jsonObj, null, string.Empty);
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

        protected abstract Translation InternalLoadTranslationEntryFromJSON(JObject jsonObj, Translation? translation, string groupName);

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

        private void InternalLoadTreeNodeFromJSON(JObject jsonObj, TranslationTree tree, TranslationTreeNode parentNode)
        {
            // Enumerate string properties, which will be keys
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
            {
                string key = prop.Name.Trim();

                bool? skipStringPropertyKey = ShouldSkipStringProperty(key);
                if (skipStringPropertyKey is null)
                    throw new GenericParserException($"Invalid key '{key}' encountered");
                if (skipStringPropertyKey == true)
                    continue;

                string? value = prop.Value.Value<string>();

                if (value is null)
                    throw new GenericParserException($"The value of the key '{key}' is not a string");

                if (parentNode.Keys.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate key '{key}' specified");
                parentNode.Keys.Add(key, new TranslationTreeLeaf(key, IsTemplatedText(value)));
            }

            // Enumerate object properties, which will be groups
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object))
            {
                string name = prop.Name.Trim();

                // Skip child JSON nodes which, in ARB format, are supplementary information about an entry.
                // This information is used when loading phrases, but not when building a tree.

                bool? skipObjectPropertyKey = ShouldSkipObjectProperty(name);
                if (skipObjectPropertyKey is null)
                    throw new GenericParserException($"Invalid group '{name}' encountered");
                if (skipObjectPropertyKey == true)
                    continue;

                if (parentNode.ChildNodes.Keys.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate group name '{name}' specified");

                var jsonChild = prop.Value.Value<JObject>();
                if (jsonChild is null)
                    throw new GenericParserException($"The value of the key '{name}' is not an object");

                var childNode = parentNode.MakeNode(name);
                if (childNode is null)
                    throw new GenericParserException($"Group '{name}' could not be used to build a tree of translation entries");

                InternalLoadTreeNodeFromJSON(jsonChild, tree, childNode);
            }
        }

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.
        protected internal virtual bool? ShouldSkipStringProperty(string key)
        {
            if (key.Length == 0)
                return null;
            else
                return false;
        }

        protected internal virtual bool? ShouldSkipObjectProperty(string key)
        {
            if (key.Length == 0)
                return null;
            else
                return false;
        }
    }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

}
