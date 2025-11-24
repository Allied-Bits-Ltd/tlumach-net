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
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// The base parser for JSON-formatted configuration and translation files.
    /// </summary>
    public abstract class BaseJsonParser : BaseParser
    {
        /// <summary>
        /// Gets or sets the character that is used to separate the locale name from the base name in the names of locale-specific translation files.
        /// </summary>
        public static char LocaleSeparatorChar { get; set; } = '_';

        public override char GetLocaleSeparatorChar()
        {
            return LocaleSeparatorChar;
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly)
        {
            try
            {
                var doc = JsonDocument.Parse(fileContent);
                JsonElement configObj = doc.RootElement;

                JsonElement jsonValue;

                string? defaultFile = null;
                string? defaultLocale = null;
                string? generatedNamespace = null;
                string? generatedClassName = null;
                string? textProcessingModeStr = null;
                string? delayedUnitCreationStr = null;

                if (configObj.TryGetProperty(TranslationConfiguration.KEY_DEFAULT_FILE, out jsonValue))
                    defaultFile = jsonValue.GetString()?.Trim();
                if (configObj.TryGetProperty(TranslationConfiguration.KEY_DEFAULT_LOCALE, out jsonValue))
                    defaultLocale = jsonValue.GetString()?.Trim();
                if (configObj.TryGetProperty(TranslationConfiguration.KEY_GENERATED_NAMESPACE, out jsonValue))
                    generatedNamespace = jsonValue.GetString()?.Trim();
                if (configObj.TryGetProperty(TranslationConfiguration.KEY_GENERATED_CLASS, out jsonValue))
                    generatedClassName = jsonValue.GetString()?.Trim();

                if (configObj.TryGetProperty(TranslationConfiguration.KEY_DELAYED_UNIT_CREATION, out jsonValue))
                    delayedUnitCreationStr = jsonValue.GetString()?.Trim();

                if (configObj.TryGetProperty(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, out jsonValue))
                    textProcessingModeStr = jsonValue.GetString()?.Trim();

                TextFormat textProcessingMode = DecodeTextProcessingMode(textProcessingModeStr) ?? GetTextProcessingMode();

                TranslationConfiguration result = new TranslationConfiguration(assembly, defaultFile ?? string.Empty, generatedNamespace, generatedClassName, defaultLocale, textProcessingMode, "true".Equals(delayedUnitCreationStr, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(defaultFile))
                    return result;

                // If the configuration contains the Translations section, parse it
                if (configObj.TryGetProperty(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, out jsonValue) && jsonValue.ValueKind == JsonValueKind.Object)
                {
                    // Enumerate properties
                    foreach (JsonProperty prop in jsonValue.EnumerateObject())
                    {
                        string lang = prop.Name.Trim();
                        if (lang.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal))
                            lang = TranslationConfiguration.KEY_TRANSLATION_OTHER;
                        else
                            lang = lang.ToUpperInvariant();
                        if (prop.Value.ValueKind == JsonValueKind.String)
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
            catch (JsonException ex)
            {
                int pos = GetAbsolutePosition(fileContent, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
                throw new TextParseException(ex.Message, pos, pos, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }

        public override Translation? LoadTranslation(string translationText, CultureInfo? culture)
        {
            try
            {
                var doc = JsonDocument.Parse(translationText);
                JsonElement jsonObj = doc.RootElement;

                return InternalLoadTranslationEntriesFromJSON(jsonObj, null, string.Empty);
            }
            catch (JsonException ex)
            {
                int pos = GetAbsolutePosition(translationText, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
                throw new TextParseException(ex.Message, pos, pos, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of the translation has failed", ex);
            }
        }

        protected abstract Translation InternalLoadTranslationEntriesFromJSON(JsonElement jsonObj, Translation? translation, string groupName);

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                JsonElement jsonObj = doc.RootElement;

                TranslationTree result = new();

                InternalLoadTreeNodeFromJSON(jsonObj, result, result.RootNode);

                return result;
            }
            catch (JsonException ex)
            {
                int pos = GetAbsolutePosition(content, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
                throw new TextParseException(ex.Message, pos, pos, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }

        private void InternalLoadTreeNodeFromJSON(JsonElement jsonObj, TranslationTree tree, TranslationTreeNode parentNode)
        {
            // Enumerate string properties, which will be keys
            foreach (var prop in jsonObj.EnumerateObject().Where(static p => p.Value.ValueKind == JsonValueKind.String))
            {
                string key = prop.Name.Trim();

                bool? skipStringPropertyKey = ShouldSkipStringProperty(key);
                if (skipStringPropertyKey is null)
                    throw new GenericParserException($"Invalid key '{key}' encountered");
                if (skipStringPropertyKey == true)
                    continue;

                if (parentNode.Keys.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate key '{key}' specified");

                string? value = prop.Value.GetString();

                if (value is null)
                    throw new GenericParserException($"The value of the key '{key}' is not a string");

                parentNode.Keys.Add(key, new TranslationTreeLeaf(key, IsTemplatedText(value)));
            }

            // Enumerate object properties, which will be groups
            foreach (var prop in jsonObj.EnumerateObject().Where(static p => p.Value.ValueKind == JsonValueKind.Object))
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

                var jsonChild = prop.Value;
                //if (jsonChild is null)
                //    throw new GenericParserException($"The value of the key '{name}' is not an object");

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
