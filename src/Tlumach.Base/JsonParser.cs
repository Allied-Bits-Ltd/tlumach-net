// <copyright file="JsonParser.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Text.Json;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// The parser for simpler JSON translation files.
    /// </summary>
    public class JsonParser : BaseJsonParser
    {
        /// <summary>
        /// Gets or sets the text processing mode to use when decoding potentially escaped strings and when recognizing template strings in translation entries.
        /// </summary>
        public static TextFormat TextProcessingMode { get; set; }

        private static BaseParser Factory() => new JsonParser();

        static JsonParser()
        {
            TextProcessingMode = TextFormat.DotNet;

            // We register the parser for both configuration files and translation files.
            // This approach enables us to use configuration and translations in different formats.
            FileFormats.RegisterConfigParser(".jsoncfg", Factory);
            FileFormats.RegisterParser(".json", Factory);
        }

        /// <summary>
        /// Initializes the parser class, making it available for use.
        /// </summary>
        public static void Use()
        {
            // The role of this method is just to exist so that calling it executes a static constructor of this class.
        }

        protected override TextFormat GetTextProcessingMode()
        {
            return TextProcessingMode;
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".json", StringComparison.OrdinalIgnoreCase);
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content, TextFormat? textProcessingMode)
        {
            if (textProcessingMode is not null)
                JsonParser.TextProcessingMode = textProcessingMode.Value;
            return base.InternalLoadTranslationStructure(content, textProcessingMode);
        }

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.
        protected override Translation InternalLoadTranslationEntriesFromJSON(JsonElement jsonObj, Translation? translation, string groupName, TextFormat? textProcessingMode)
        {
            // When processing the top level, pick the metadata (locale, context, author, last modified) values if they are present
            translation ??= new Translation(locale: null, keepEntryOrder: KeepEntryOrder);

            // Enumerate string properties
            InternalEnumerateStringPropertiesOfJSONObject(jsonObj, translation, groupName, textProcessingMode);

            // Enumerate JSON properties that are objects - they either contain extra information about entries or they are child groups
            InternalEnumerateObjectPropertiesOfJSONObject(jsonObj, translation, groupName, textProcessingMode);

            return translation;
        }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

        private void InternalEnumerateStringPropertiesOfJSONObject(JsonElement jsonObj, Translation translation, string groupName, TextFormat? textProcessingMode)
        {
            foreach (var prop in jsonObj.EnumerateObject().Where(static p => p.Value.ValueKind == JsonValueKind.String))
            {
                TranslationEntry? entry;
                string key;

                string? escapedValue = null;
                string? value;
                string? reference = null;
                bool isTemplated = false;

                key = prop.Name.Trim();

                if (!string.IsNullOrEmpty(groupName))
                    key = groupName + "." + key;

                value = prop.Value.GetString();

                if (value is not null && IsReference(value))
                {
                    reference = value.Substring(1).Trim();
                    value = null;
                }

                // Pick an existing entry ...
                if (translation.TryGetValue(key, out entry))
                {
                    throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");
                }

                // ... or add a new one

                if (value is not null)
                {
                    isTemplated = IsTemplatedText(value, textProcessingMode);
                    if (TextProcessingMode == TextFormat.BackslashEscaping || TextProcessingMode == TextFormat.DotNet)
                    {
                        escapedValue = value;
                        value = Utils.UnescapeString(value);
                    }
                }

                entry = new(key, value, escapedText: escapedValue, reference: reference);

                translation.Add(key.ToUpperInvariant(), entry);

                entry.ContainsPlaceholders = isTemplated;
            }
        }

        private void InternalEnumerateObjectPropertiesOfJSONObject(JsonElement jsonObj, Translation translation, string groupName, TextFormat? textProcessingMode)
        {
            foreach (var prop in jsonObj.EnumerateObject().Where(static p => p.Value.ValueKind == JsonValueKind.Object))
            {
                string name = prop.Name.Trim();

                var jsonChild = prop.Value;

                // We have a group - use recursive handling
                InternalLoadTranslationEntriesFromJSON(jsonChild, translation, (!string.IsNullOrEmpty(groupName)) ? groupName + "." + name : name, textProcessingMode);
            }
        }

        protected override Translation? LoadTranslationWithLocations(string translationText, System.Globalization.CultureInfo? culture, TextFormat? textProcessingMode)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(translationText);
            long[] byteLineStarts = BuildByteLineStartsTable(utf8);
            var translation = new Translation(locale: null, keepEntryOrder: KeepEntryOrder);

            var readerOptions = new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
            var reader = new Utf8JsonReader(utf8, readerOptions);

            StreamEnumerateJsonTranslations(ref reader, utf8, byteLineStarts, translation, string.Empty, textProcessingMode);

            return translation;
        }

        private void StreamEnumerateJsonTranslations(ref Utf8JsonReader reader, byte[] utf8, long[] byteLineStarts, Translation translation, string groupName, TextFormat? textProcessingMode)
        {
            // Expect StartObject
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return;

            string? pendingKeyName = null;
            long pendingKeyByteOffset = 0;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        pendingKeyName = reader.GetString()?.Trim();
                        pendingKeyByteOffset = reader.TokenStartIndex + 1; // skip opening "
                        break;

                    case JsonTokenType.String when pendingKeyName != null:
                    {
                        string key = string.IsNullOrEmpty(groupName) ? pendingKeyName : groupName + "." + pendingKeyName;

                        if (translation.TryGetValue(key, out _))
                            throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");

                        var (line, col) = GetLineAndColumnFromByteOffset(byteLineStarts, pendingKeyByteOffset);
                        var location = new KeyLocation(line, col, (int)pendingKeyByteOffset);

                        string? rawValue = reader.GetString();
                        string? reference = null;
                        string? value = rawValue;
                        string? escapedValue = null;
                        bool isTemplated = false;

                        if (value is not null && IsReference(value))
                        {
                            reference = value.Substring(1).Trim();
                            value = null;
                        }

                        if (value is not null)
                        {
                            isTemplated = IsTemplatedText(value, textProcessingMode);
                            if (TextProcessingMode == TextFormat.BackslashEscaping || TextProcessingMode == TextFormat.DotNet)
                            {
                                escapedValue = value;
                                value = Utils.UnescapeString(value);
                            }
                        }

                        var entry = new TranslationEntry(key, value, escapedText: escapedValue, reference: reference, keyLocation: location);
                        entry.ContainsPlaceholders = isTemplated;
                        translation.Add(key.ToUpperInvariant(), entry);

                        pendingKeyName = null;
                        break;
                    }

                    case JsonTokenType.StartObject when pendingKeyName != null:
                    {
                        // This property's value is an object — treat it as a group
                        string childGroup = string.IsNullOrEmpty(groupName) ? pendingKeyName : groupName + "." + pendingKeyName;
                        pendingKeyName = null;

                        // Process the child object inline (reader is already past StartObject)
                        StreamEnumerateJsonObjectContents(ref reader, utf8, byteLineStarts, translation, childGroup, textProcessingMode);
                        break;
                    }

                    case JsonTokenType.EndObject:
                        return;

                    default:
                        pendingKeyName = null;
                        break;
                }
            }
        }

        private void StreamEnumerateJsonObjectContents(ref Utf8JsonReader reader, byte[] utf8, long[] byteLineStarts, Translation translation, string groupName, TextFormat? textProcessingMode)
        {
            // reader is positioned on the StartObject token already consumed by the caller's switch;
            // we just need to read property/value pairs until EndObject
            string? pendingKeyName = null;
            long pendingKeyByteOffset = 0;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        pendingKeyName = reader.GetString()?.Trim();
                        pendingKeyByteOffset = reader.TokenStartIndex + 1;
                        break;

                    case JsonTokenType.String when pendingKeyName != null:
                    {
                        string key = string.IsNullOrEmpty(groupName) ? pendingKeyName : groupName + "." + pendingKeyName;

                        if (translation.TryGetValue(key, out _))
                            throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");

                        var (line, col) = GetLineAndColumnFromByteOffset(byteLineStarts, pendingKeyByteOffset);
                        var location = new KeyLocation(line, col, (int)pendingKeyByteOffset);

                        string? value = reader.GetString();
                        string? reference = null;
                        string? escapedValue = null;
                        bool isTemplated = false;

                        if (value is not null && IsReference(value))
                        {
                            reference = value.Substring(1).Trim();
                            value = null;
                        }

                        if (value is not null)
                        {
                            isTemplated = IsTemplatedText(value, textProcessingMode);
                            if (TextProcessingMode == TextFormat.BackslashEscaping || TextProcessingMode == TextFormat.DotNet)
                            {
                                escapedValue = value;
                                value = Utils.UnescapeString(value);
                            }
                        }

                        var entry = new TranslationEntry(key, value, escapedText: escapedValue, reference: reference, keyLocation: location);
                        entry.ContainsPlaceholders = isTemplated;
                        translation.Add(key.ToUpperInvariant(), entry);

                        pendingKeyName = null;
                        break;
                    }

                    case JsonTokenType.StartObject when pendingKeyName != null:
                    {
                        string childGroup = string.IsNullOrEmpty(groupName) ? pendingKeyName : groupName + "." + pendingKeyName;
                        pendingKeyName = null;
                        StreamEnumerateJsonObjectContents(ref reader, utf8, byteLineStarts, translation, childGroup, textProcessingMode);
                        break;
                    }

                    case JsonTokenType.EndObject:
                        return;

                    default:
                        pendingKeyName = null;
                        break;
                }
            }
        }
    }
}
