// <copyright file="ResxParser.cs" company="Allied Bits Ltd.">
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

using System.Globalization;
using System.Xml;
using System.Xml.Linq;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    public class ResxParser : BaseXMLParser
    {
        private static BaseParser Factory() => new ResxParser();

        /// <summary>
        /// Gets or sets the character that is used to separate the locale name from the base name in the names of locale-specific translation files.
        /// </summary>
        public static char LocaleSeparatorChar { get; set; } = '.';

        static ResxParser()
        {
            // We register the parser for both configuration files and translation files.
            // This approach enables us to use configuration and translations in different formats.
            FileFormats.RegisterConfigParser(".resxcfg", Factory);
            FileFormats.RegisterConfigParser(".xmlcfg", Factory);
            FileFormats.RegisterParser(".resx", Factory);
        }

        /// <summary>
        /// Initializes the parser class, making it available for use.
        /// </summary>
        public static void Use()
        {
            // The role of this method is just to exist so that calling it executes a static constructor of this class.
        }

        private static bool NodeHasPreserveAttr(XElement dataElement)
        {
            var attr = dataElement.Attribute(CXmlNamespace + "space");
            return attr?.Value.Equals("preserve", StringComparison.Ordinal) == true;
        }

        protected override TextFormat GetTextProcessingMode()
        {
            return TextFormat.DotNet;
        }

        public override char GetLocaleSeparatorChar()
        {
            return LocaleSeparatorChar;
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".resx", StringComparison.OrdinalIgnoreCase);
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content, TextFormat? textProcessingMode)
        {
            try
            {
                XDocument doc = XDocument.Load(new StringReader(content));

                XElement? root = doc.Root;

                if (root is null)
                    throw new GenericParserException("The translation file has no XML root node.");

                TranslationTree result = new();

                foreach (var data in root.Elements("data"))
                {
                    string? key;
                    string? value;

                    // Skip non-string typed entries (e.g., images) if a type is specified
                    var typeAttr = (string?)data.Attribute("type");
                    if (typeAttr is not null && !"System.String".StartsWith(typeAttr, StringComparison.Ordinal))
                        continue;

                    key = ((string?)data.Attribute("name"))?.Trim();

                    if (key?.Length > 0)
                    {
                        value = data.Element("value")?.Value;

                        if (value is null)
                            continue;

                        if (!NodeHasPreserveAttr(data))
                            value = value.Trim();

                        // Add an entry

                        if (result.RootNode.Keys.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                            throw new GenericParserException($"Duplicate key '{key}' specified");

                        result.RootNode.Keys.Add(key, new TranslationTreeLeaf(key, IsTemplatedText(value, textProcessingMode)));
                    }
                }

                return result;
            }
            catch (XmlException ex)
            {
                int pos = GetAbsolutePosition(content, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of the translation has failed", ex);
            }
        }

        protected override Translation? LoadTranslationWithLocations(string translationText, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            int[] lineStarts = BuildLineStartsTable(translationText);
            var translation = new Translation(locale: null, keepEntryOrder: KeepEntryOrder);

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            using var xmlReader = XmlReader.Create(new StringReader(translationText), settings);
            var lineInfo = (IXmlLineInfo)xmlReader;

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType != XmlNodeType.Element || !xmlReader.Name.Equals("data", StringComparison.Ordinal))
                    continue;

                // Skip non-string typed entries (e.g. images)
                string? typeAttr = xmlReader.GetAttribute("type");
                if (typeAttr is not null && !"System.String".StartsWith(typeAttr, StringComparison.Ordinal))
                    continue;

                string? key = xmlReader.GetAttribute("name")?.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                // Capture xml:space="preserve" before entering subtree
                string? xmlSpaceAttr = xmlReader.GetAttribute("space", "http://www.w3.org/XML/1998/namespace")
                    ?? xmlReader.GetAttribute("xml:space");
                bool preserveSpace = xmlSpaceAttr?.Equals("preserve", StringComparison.Ordinal) == true;

                // LinePosition for an element points to the first char of the element name (after '<').
                // The '<' is at LinePosition - 1.
                int line = lineInfo.LineNumber;
                int col = Math.Max(1, lineInfo.LinePosition - 1);
                int offset = GetOffsetFromLineAndColumn(lineStarts, line, col);
                var location = new KeyLocation(line, col, offset);

                string? value = null;
                string? comment = null;

                using (var subtree = xmlReader.ReadSubtree())
                {
                    while (subtree.Read())
                    {
                        if (subtree.NodeType == XmlNodeType.Element)
                        {
                            if (subtree.Name.Equals("value", StringComparison.Ordinal))
                                value = subtree.ReadElementContentAsString();
                            else if (subtree.Name.Equals("comment", StringComparison.Ordinal))
                                comment = subtree.ReadElementContentAsString();
                        }
                    }
                }

                if (value is null)
                    continue;

                if (!preserveSpace)
                    value = value.Trim();

                if (translation.TryGetValue(key!, out _))
                    throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");

                string? reference = null;
                if (IsReference(value))
                {
                    reference = value.Substring(1).Trim();
                    value = null;
                }

                var entry = new TranslationEntry(key!, value, escapedText: null, reference, location);
                entry.Comment = comment;
                if (value is not null)
                    entry.ContainsPlaceholders = IsTemplatedText(value, textProcessingMode);
                translation.Add(key!.ToUpperInvariant(), entry);
            }

            return translation;
        }

        protected internal override Translation InternalLoadTranslationEntriesFromXML(XElement parentNode, CultureInfo? culture, Translation? translation, string groupName, TextFormat? textProcessingMode)
        {
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance
            if (parentNode is null)
                throw new ArgumentNullException(nameof(parentNode));
#pragma warning restore CA1510

            // When processing the top level, pick the metadata (locale, context, author, last modified) values if they are present
            translation ??= new Translation(locale: null, keepEntryOrder: KeepEntryOrder);

            foreach (var data in parentNode.Elements("data"))
            {
                TranslationEntry? entry;

                string? key;
                string? value;
                string? comment;
                string? reference = null;

                // Skip non-string typed entries (e.g., images) if a type is specified
                var typeAttr = (string?)data.Attribute("type");
                if (typeAttr is not null && !"System.String".StartsWith(typeAttr, StringComparison.Ordinal))
                    continue;

                key = ((string?)data.Attribute("name"))?.Trim();

                if (key?.Length > 0)
                {
                    if (!string.IsNullOrEmpty(groupName))
                        key = groupName + "." + key;

                    value = data.Element("value")?.Value;

                    if (value is not null && !NodeHasPreserveAttr(data))
                        value = value.Trim();

                    if (value is not null && IsReference(value))
                    {
                        reference = value.Substring(1).Trim();
                        value = null;
                    }

                    comment = data.Element("comment")?.Value;

                    // Add an entry
                    if (translation.TryGetValue(key, out entry))
                        throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");

                    entry = new(key, value, escapedText: null, reference);
                    translation.Add(key.ToUpperInvariant(), entry);

                    entry.Comment = comment;

                    if (value is not null)
                        entry.ContainsPlaceholders = IsTemplatedText(value, textProcessingMode);
                }
            }

            return translation;
        }
    }
}
