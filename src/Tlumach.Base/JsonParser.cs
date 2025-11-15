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

namespace Tlumach.Base
{
    /// <summary>
    /// The parser for simpler JSON translation files.
    /// </summary>
    public class JsonParser : BaseJsonParser
    {
        /// <summary>
        /// Gets or sets the text processing mode to use when recognizing template strings in translation entries.
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

        protected override TextFormat GetEscapeMode()
        {
            return TextProcessingMode;
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".json", StringComparison.OrdinalIgnoreCase);
        }

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.
        protected override Translation InternalLoadTranslationEntriesFromJSON(JsonElement jsonObj, Translation? translation, string groupName)
        {
            // When processing the top level, pick the metadata (locale, context, author, last modified) values if they are present
            translation ??= new Translation(locale: null);

            // Enumerate string properties
            InternalEnumerateStringPropertiesOfJSONObject(jsonObj, translation, groupName);

            // Enumerate JSON properties that are objects - they either contain extra information about entries or they are child groups
            InternalEnumerateObjectPropertiesOfJSONObject(jsonObj, translation, groupName);

            return translation;
        }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

        private void InternalEnumerateStringPropertiesOfJSONObject(JsonElement jsonObj, Translation translation, string groupName)
        {
            foreach (var prop in jsonObj.EnumerateObject().Where(static p => p.Value.ValueKind == JsonValueKind.String))
            {
                TranslationEntry? entry;
                string key;

                string? value;
                string? reference = null;

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
                else
                {
                    // ... or add a new one
                    entry = new(key, value, escapedText: null, reference: null);
                    translation.Add(key.ToUpperInvariant(), entry);
                }

                if (value is not null)
                    entry.IsTemplated = IsTemplatedText(value);
                entry.Reference = reference;
            }
        }

        private void InternalEnumerateObjectPropertiesOfJSONObject(JsonElement jsonObj, Translation translation, string groupName)
        {
            foreach (var prop in jsonObj.EnumerateObject().Where(static p => p.Value.ValueKind == JsonValueKind.Object))
            {
                string name = prop.Name.Trim();

                var jsonChild = prop.Value;

                // We have a group - use recursive handling
                InternalLoadTranslationEntriesFromJSON(jsonChild, translation, (!string.IsNullOrEmpty(groupName)) ? groupName + "." + name : name);
            }
        }

        internal override bool IsTemplatedText(string text)
        {
            return StringHasParameters(text, TextProcessingMode);
        }
    }
}
