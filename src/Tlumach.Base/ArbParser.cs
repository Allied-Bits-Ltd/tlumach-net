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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tlumach.Base
{
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

    public class ArbPlaceholder : Placeholder
    {
        public ArbPlaceholder(string name)
            : base(name)
        {
        }

        internal void SetType(string type)
        {
            Type = type;
        }

        internal void SetFormat(string format)
        {
            Format = format;
        }

        internal void SetExample(string example)
        {
            Example = example;
        }
    }

    public class ArbParser : BaseJsonParser
    {
        private const string ARB_KEY_LOCALE = "@@locale";
        private const string ARB_KEY_GLOBAL_CONTEXT = "@@context";
        private const string ARB_KEY_LAST_MODIFIED = "@@last_modified";
        private const string ARB_KEY_AUTHOR = "@@author";
        private const string ARB_KEY_DESCRIPTION = "description";
        private const string ARB_KEY_TYPE = "type";
        private const string ARB_KEY_CONTEXT = "context";
        private const string ARB_KEY_SOURCE_TEXT = "source_text";
        private const string ARB_KEY_SCREEN = "screen";
        private const string ARB_KEY_VIDEO = "video";
        private const string ARB_KEY_PLACEHOLDERS = "placeholders";
        private const string ARB_KEY_FORMAT = "format";
        private const string ARB_KEY_EXAMPLE = "example";
        private const string ARB_KEY_OPTIONAL_PARAMETERS = "optionalParameters";

        /// <summary>
        /// Gets or sets the escape mode to use when recognizing template strings in translation entries.
        /// </summary>
        public static TemplateStringEscaping TemplateEscapeMode { get; set; }

        static ArbParser()
        {
            TemplateEscapeMode = TemplateStringEscaping.Arb;

            // We register the parser for both configuration files and translation files.
            // This approach enables us to use configuration and translations in different formats.
            FileFormats.RegisterConfigParser(".arbcfg", Factory);
            FileFormats.RegisterParser(".arb", Factory);
        }

        /// <summary>
        /// Initializes the parser class, making it available for use.
        /// </summary>
        public static void Use()
        {
            // The role of this method is just to exist so that calling it executes a static constructor of this class.
        }

        protected override TemplateStringEscaping GetTemplateEscapeMode()
        {
            return TemplateEscapeMode;
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".arb", StringComparison.OrdinalIgnoreCase);
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

        private static BaseFileParser Factory() => new ArbParser();

        /// <summary>
        /// Extracts placeholder definitions from a JSON object and stores them in the translation entry.
        /// </summary>
        /// <param name="entry">The entry to put the definitions to.</param>
        /// <param name="jsonObj">The object to extract definitions from.</param>
        private static void InternalProcessEntryPlaceholderDefinitions(TranslationEntry entry, JObject jsonObj)
        {
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object))
            {
                string name = prop.Name.Trim();
                InternalAddSinglePlaceholderDefinition(entry, name, (JObject)prop.Value);
            }
        }

        private static void InternalAddSinglePlaceholderDefinition(TranslationEntry entry, string placeholderName, JObject jsonObj)
        {
            ArbPlaceholder placeholder = new(placeholderName);

            // Collect main and unrecognized parameters
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
            {
                string key = prop.Name.Trim();
                string? value = prop.Value.ToString();

                if (key.Equals(ARB_KEY_TYPE, StringComparison.OrdinalIgnoreCase))
                    placeholder.SetType(value);
                else
                if (key.Equals(ARB_KEY_FORMAT, StringComparison.OrdinalIgnoreCase))
                    placeholder.SetFormat(value);
                else
                if (key.Equals(ARB_KEY_EXAMPLE, StringComparison.OrdinalIgnoreCase))
                    placeholder.SetExample(value);
                else
                    placeholder.Properties[key] = value;
            }

            // Collect optional parameters
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object && p.Name.Trim().Equals(ARB_KEY_OPTIONAL_PARAMETERS, StringComparison.OrdinalIgnoreCase)))
            {
                JObject? childObj = prop.Value as JObject;
                if (childObj != null)
                {
                    foreach (var childProp in childObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
                    {
                        string key = childProp.Name.Trim();
                        string? value = childProp.Value.ToString();
                        placeholder.OptionalParameters[key] = value;
                    }
                }
            }

            // Add new placeholder to the entry
            entry.AddPlaceholder(placeholder);
        }

        private void InternalEnumerateStringPropertiesOfJSONObject(JObject jsonObj, Translation translation, string groupName)
        {
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
            {
                TranslationEntry? entry;
                string key;

                string? value;
                string? target = null;
                string? reference = null;

                key = prop.Name.Trim();

                // pick custom attributes of the translation file
                if (key.StartsWith("@@x-", StringComparison.Ordinal) && key.Length > 4)
                {
                    value = prop.Value.Value<string>() ?? string.Empty;
                    translation.CustomProperties.Add(key.Substring(4), value);
                    continue;
                }
#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
                int atIdx = key.IndexOf('@');
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

                if (atIdx == 0)
                    continue;
                else
                if (atIdx > 0 && atIdx < key.Length - 1) // the key contains a target for HTML
                {
                    target = key.Substring(atIdx + 1);
                    key = key.Substring(0, atIdx);
                }

                if (!string.IsNullOrEmpty(groupName))
                    key = groupName + "." + key;

                value = prop.Value.Value<string>();

                if (value is not null && IsReference(value))
                {
                    reference = value.Substring(1).Trim();
                    value = null;
                }

                // Pick an existing entry ...
                if (translation.TryGetValue(key, out entry))
                {
                    // Report duplicate entries but not always:
                    // we might have pre-allocated the entry if its properties object came before it for some reason.
                    if (!(entry.Text is null && entry.Reference is null))
                        throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");
                    else
                        entry.Text = value;
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
                entry.Target = target;
            }
        }

        private void InternalEnumerateObjectPropertiesOfJSONObject(JObject jsonObj, Translation translation, string groupName)
        {
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object))
            {
                TranslationEntry? entry;
                string key;

                string name = prop.Name.Trim();

                var jsonChild = (JObject)prop.Value;

                // Process objects that contain properties of the entries
#if NET9_0_OR_GREATER
                if (name.StartsWith('@'))
#else
                if (name.StartsWith("@", StringComparison.Ordinal))
#endif
                {
                    if (name.Length == 1)
                        continue;
                    name = name.Substring(1); // Strip leading @

                    // Determine the key of the entry
                    key = (!string.IsNullOrEmpty(groupName)) ? groupName + "." + name : name;

                    // Locate an entry, to which the properties belong. If the entry is not found, add one.
                    if (!translation.TryGetValue(key, out entry))
                        entry = new TranslationEntry(key, text: null, reference: null);

                    foreach (var childProp in jsonChild.Properties())
                    {
                        string childPropName = childProp.Name.Trim();

                        if (childProp.Value.Type == JTokenType.String)
                        {
                            if (childPropName.Equals(ARB_KEY_DESCRIPTION, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick description
                                entry.Description = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_TYPE, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick type
                                entry.Type = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_CONTEXT, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick context
                                entry.Context = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_SOURCE_TEXT, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick source text
                                entry.SourceText = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_SCREEN, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick screen[shot]
                                entry.Screen = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_VIDEO, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick video
                                entry.Video = (string?)childProp.Value;
                            }
                        }
                        else
                        if (childPropName.Equals(ARB_KEY_PLACEHOLDERS, StringComparison.OrdinalIgnoreCase) && childProp.Value.Type == JTokenType.Object)
                        {
                            // Pick placeholders
                            InternalProcessEntryPlaceholderDefinitions(entry, (JObject)childProp.Value);
                        }
                    }
                }
                else
                {
                    // We have a group - use recursive handling
                    InternalLoadTranslationEntryFromJSON(jsonChild, translation, (!string.IsNullOrEmpty(groupName)) ? groupName + "." + name : name);
                }
            }
        }

        protected override Translation InternalLoadTranslationEntryFromJSON(JObject jsonObj, Translation? translation, string groupName)
        {
            if (jsonObj is null)
                throw new ArgumentNullException(nameof(jsonObj));

            // When processing the top level, pick the metadata (locale, context, author, last modified) values if they are present
            if (translation is null)
            {
                string? locale = jsonObj.Value<string>(ARB_KEY_LOCALE);
                string? context = jsonObj.Value<string>(ARB_KEY_GLOBAL_CONTEXT);
                translation = new Translation(locale, context);

                string? value = jsonObj.Value<string>(ARB_KEY_AUTHOR);
                translation.Author = value;

                value = jsonObj.Value<string>(ARB_KEY_LAST_MODIFIED);

                translation.LastModified = Utils.ParseDateISO8601(value);
            }

            // Enumerate string properties
            InternalEnumerateStringPropertiesOfJSONObject(jsonObj, translation, groupName);

            // Enumerate JSON properties that are objects - they either contain extra information about entries or they are child groups
            InternalEnumerateObjectPropertiesOfJSONObject(jsonObj, translation, groupName);

            return translation;
        }

        protected internal override bool? ShouldSkipStringProperty(string key)
        {
            if (key?.Length > 0)
                return key[0] == '@';
            else
                return null;
        }

        protected internal override bool? ShouldSkipObjectProperty(string key)
        {
            if (key?.Length > 0)
                return key[0] == '@';
            else
                return null;
        }

        internal override bool IsTemplatedText(string text)
        {
            return StringHasParameters(text, TemplateEscapeMode);
        }
    }
}
