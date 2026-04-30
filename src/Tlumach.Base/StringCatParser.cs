// <copyright file="StringCatParser.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Text.Json;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// Parser for Apple String Catalog files (.xcstrings).
    /// A single .xcstrings file contains translations for all locales in the format:
    /// strings → key → localizations → lang → stringUnit → value.
    /// </summary>
    public class StringCatParser : BaseJsonParser
    {
        private const string SC_STRINGS = "strings";
        private const string SC_COMMENT = "comment";
        private const string SC_LOCALIZATIONS = "localizations";
        private const string SC_STRING_UNIT = "stringUnit";
        private const string SC_VALUE = "value";
        private const string SC_VARIATIONS = "variations";
        private const string SC_PLURAL = "plural";

        private static readonly string[] PluralForms = ["other", "one", "many", "few", "two", "zero"];

        /// <summary>
        /// Gets or sets the text processing mode used when recognising placeholder strings.
        /// </summary>
        public static TextFormat TextProcessingMode { get; set; }

        private static BaseParser Factory() => new StringCatParser();

        static StringCatParser()
        {
            TextProcessingMode = TextFormat.Apple;

            // .jsoncfg is already registered by JsonParser; the first-wins guard silently ignores duplicates.
            FileFormats.RegisterConfigParser(".jsoncfg", Factory);
            FileFormats.RegisterParser(".xcstrings", Factory);
        }

        /// <summary>
        /// Initializes the parser class, making it available for use.
        /// </summary>
        public static void Use()
        {
            // Calling this method triggers the static constructor.
        }

        protected override TextFormat GetTextProcessingMode() => TextProcessingMode;

        public override bool CanHandleExtension(string fileExtension)
            => !string.IsNullOrEmpty(fileExtension) &&
               fileExtension.Equals(".xcstrings", StringComparison.OrdinalIgnoreCase);

        // -------------------------------------------------------------------------
        // Translation loading — DOM path
        // -------------------------------------------------------------------------

        public override Translation? LoadTranslation(string translationText, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            if (PopulateKeyLocations)
                return LoadTranslationWithLocations(translationText, culture, textProcessingMode);

            try
            {
                var doc = JsonDocument.Parse(translationText);
                JsonElement root = doc.RootElement;

                var translation = new Translation(locale: culture?.Name, keepEntryOrder: KeepEntryOrder);

                if (!root.TryGetProperty(SC_STRINGS, out JsonElement stringsEl) ||
                    stringsEl.ValueKind != JsonValueKind.Object)
                    return translation;

                TextFormat mode = textProcessingMode ?? TextProcessingMode;

                foreach (JsonProperty stringEntry in stringsEl.EnumerateObject())
                {
                    string key = stringEntry.Name;

                    if (stringEntry.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    string? comment = null;
                    if (stringEntry.Value.TryGetProperty(SC_COMMENT, out JsonElement commentEl))
                        comment = commentEl.GetString()?.Trim();

                    if (!stringEntry.Value.TryGetProperty(SC_LOCALIZATIONS, out JsonElement locsEl) ||
                        locsEl.ValueKind != JsonValueKind.Object)
                        continue;

                    if (!TryGetLocalizationValue(locsEl, culture?.Name, out string? text))
                        continue;

                    bool isTemplated = text is not null && IsTemplatedText(text, mode);
                    var entry = new TranslationEntry(key, text, escapedText: null, reference: null);
                    entry.ContainsPlaceholders = isTemplated;
                    if (comment is not null)
                        entry.Comment = comment;

                    translation.Add(key.ToUpperInvariant(), entry);
                }

                return translation;
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

        // -------------------------------------------------------------------------
        // Translation loading — streaming path (key location tracking)
        // -------------------------------------------------------------------------

        protected override Translation? LoadTranslationWithLocations(string translationText, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(translationText);
            long[] byteLineStarts = BuildByteLineStartsTable(utf8);
            var translation = new Translation(locale: culture?.Name, keepEntryOrder: KeepEntryOrder);
            TextFormat mode = textProcessingMode ?? TextProcessingMode;

            // First pass: DOM — extract comment and translated text for each key.
            var keyContent = new Dictionary<string, (string? comment, string? text)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var doc = JsonDocument.Parse(translationText);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty(SC_STRINGS, out JsonElement stringsEl) &&
                    stringsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty stringEntry in stringsEl.EnumerateObject())
                    {
                        if (stringEntry.Value.ValueKind != JsonValueKind.Object)
                            continue;

                        string? comment = null;
                        if (stringEntry.Value.TryGetProperty(SC_COMMENT, out JsonElement commentEl))
                            comment = commentEl.GetString()?.Trim();

                        string? text = null;
                        if (stringEntry.Value.TryGetProperty(SC_LOCALIZATIONS, out JsonElement locsEl) &&
                            locsEl.ValueKind == JsonValueKind.Object)
                        {
                            TryGetLocalizationValue(locsEl, culture?.Name, out text);
                        }

                        if (text is not null)
                            keyContent[stringEntry.Name] = (comment, text);
                    }
                }
            }
            catch (JsonException ex)
            {
                int pos = GetAbsolutePosition(translationText, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
                throw new TextParseException(ex.Message, pos, pos, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
            }

            if (keyContent.Count == 0)
                return translation;

            // Second pass: streaming — find the byte offset of each key name inside "strings".
            var readerOptions = new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
            var reader = new Utf8JsonReader(utf8, readerOptions);

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return translation;

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string? rootProp = reader.GetString();
                if (!SC_STRINGS.Equals(rootProp, StringComparison.Ordinal))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
                        reader.Skip();
                    continue;
                }

                // Found "strings" — walk its keys
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    break;

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        continue;

                    string key = reader.GetString() ?? string.Empty;
                    long keyByteOffset = reader.TokenStartIndex + 1; // +1 to skip the opening "

                    // Skip the value object; we have the content from the DOM pass
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartObject)
                        reader.Skip();

                    if (!keyContent.TryGetValue(key, out var data))
                        continue;

                    var (comment, text) = data;

                    var (line, col) = GetLineAndColumnFromByteOffset(byteLineStarts, keyByteOffset);
                    var location = new KeyLocation(line, col, (int)keyByteOffset);

                    bool isTemplated = IsTemplatedText(text!, mode);
                    var entry = new TranslationEntry(key, text, escapedText: null, reference: null, keyLocation: location);
                    entry.ContainsPlaceholders = isTemplated;
                    if (comment is not null)
                        entry.Comment = comment;

                    translation.Add(key.ToUpperInvariant(), entry);
                }

                break;
            }

            return translation;
        }

        // -------------------------------------------------------------------------
        // Translation structure loading
        // -------------------------------------------------------------------------

        protected override TranslationTree? InternalLoadTranslationStructure(string content, TextFormat? textProcessingMode)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;
                TextFormat mode = textProcessingMode ?? TextProcessingMode;
                var result = new TranslationTree();

                if (!root.TryGetProperty(SC_STRINGS, out JsonElement stringsEl) ||
                    stringsEl.ValueKind != JsonValueKind.Object)
                    return result;

                foreach (JsonProperty stringEntry in stringsEl.EnumerateObject())
                {
                    string key = stringEntry.Name;

                    if (stringEntry.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    bool hasTemplate = false;

                    if (stringEntry.Value.TryGetProperty(SC_LOCALIZATIONS, out JsonElement locsEl) &&
                        locsEl.ValueKind == JsonValueKind.Object)
                    {
                        foreach (JsonProperty locProp in locsEl.EnumerateObject())
                        {
                            if (TryGetLocalizationValue(locsEl, locProp.Name, out string? v) && v is not null)
                            {
                                if (IsTemplatedText(v, mode))
                                {
                                    hasTemplate = true;
                                    break;
                                }
                            }
                        }
                    }

                    result.RootNode.Keys[key] = new TranslationTreeLeaf(key, hasTemplate);
                }

                return result;
            }
            catch (JsonException ex)
            {
                int pos = GetAbsolutePosition(content, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
                throw new TextParseException(ex.Message, pos, pos, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of the translation structure has failed", ex);
            }
        }

        // -------------------------------------------------------------------------
        // Required abstract override — fallback path with no culture context
        // -------------------------------------------------------------------------

#pragma warning disable CA1062
        protected override Translation InternalLoadTranslationEntriesFromJSON(JsonElement jsonObj, Translation? translation, string groupName, TextFormat? textProcessingMode)
        {
            translation ??= new Translation(locale: null, keepEntryOrder: KeepEntryOrder);
            TextFormat mode = textProcessingMode ?? TextProcessingMode;

            if (!jsonObj.TryGetProperty(SC_STRINGS, out JsonElement stringsEl) ||
                stringsEl.ValueKind != JsonValueKind.Object)
                return translation;

            foreach (JsonProperty stringEntry in stringsEl.EnumerateObject())
            {
                string key = stringEntry.Name;

                if (stringEntry.Value.ValueKind != JsonValueKind.Object)
                    continue;

                string? comment = null;
                if (stringEntry.Value.TryGetProperty(SC_COMMENT, out JsonElement commentEl))
                    comment = commentEl.GetString()?.Trim();

                string? text = null;
                if (stringEntry.Value.TryGetProperty(SC_LOCALIZATIONS, out JsonElement locsEl) &&
                    locsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty locProp in locsEl.EnumerateObject())
                    {
                        if (TryGetLocalizationValue(locsEl, locProp.Name, out text))
                            break;
                    }
                }

                bool isTemplated = text is not null && IsTemplatedText(text, mode);
                var entry = new TranslationEntry(key, text, escapedText: null, reference: null);
                entry.ContainsPlaceholders = isTemplated;
                if (comment is not null)
                    entry.Comment = comment;

                translation.Add(key.ToUpperInvariant(), entry);
            }

            return translation;
        }
#pragma warning restore CA1062

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Tries to find a translated value for <paramref name="langCode"/> inside a localizations JSON object.
        /// Supports exact match, language-prefix fallback, simple stringUnit, and plural variations.
        /// </summary>
        private static bool TryGetLocalizationValue(JsonElement locsEl, string? langCode, out string? value)
        {
            value = null;

            if (string.IsNullOrEmpty(langCode))
            {
                // No culture requested — grab the first available value
                foreach (JsonProperty locProp in locsEl.EnumerateObject())
                {
                    if (TryExtractStringUnitValue(locProp.Value, out value))
                        return true;
                }

                return false;
            }

            // 1. Exact match
            foreach (JsonProperty locProp in locsEl.EnumerateObject())
            {
                if (locProp.Name.Equals(langCode, StringComparison.OrdinalIgnoreCase))
                    return TryExtractStringUnitValue(locProp.Value, out value);
            }

            // 2. Language-prefix match: "fr" for "fr-FR" or vice-versa
            string langOnly = langCode!.Contains('-') ? langCode.Split('-')[0] : langCode;
            foreach (JsonProperty locProp in locsEl.EnumerateObject())
            {
                string locLang = locProp.Name.Contains('-') ? locProp.Name.Split('-')[0] : locProp.Name;
                if (locLang.Equals(langOnly, StringComparison.OrdinalIgnoreCase))
                    return TryExtractStringUnitValue(locProp.Value, out value);
            }

            return false;
        }

        private static bool TryExtractStringUnitValue(JsonElement locEl, out string? value)
        {
            value = null;

            if (locEl.ValueKind != JsonValueKind.Object)
                return false;

            // Simple stringUnit case
            if (locEl.TryGetProperty(SC_STRING_UNIT, out JsonElement unitEl) &&
                unitEl.ValueKind == JsonValueKind.Object &&
                unitEl.TryGetProperty(SC_VALUE, out JsonElement valEl))
            {
                value = valEl.GetString();
                return true;
            }

            // Plural variations case
            if (locEl.TryGetProperty(SC_VARIATIONS, out JsonElement varsEl) &&
                varsEl.ValueKind == JsonValueKind.Object &&
                varsEl.TryGetProperty(SC_PLURAL, out JsonElement pluralEl) &&
                pluralEl.ValueKind == JsonValueKind.Object)
            {
                foreach (string form in PluralForms)
                {
                    if (pluralEl.TryGetProperty(form, out JsonElement formEl) &&
                        formEl.TryGetProperty(SC_STRING_UNIT, out JsonElement formUnit) &&
                        formUnit.TryGetProperty(SC_VALUE, out JsonElement formVal))
                    {
                        value = formVal.GetString();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
