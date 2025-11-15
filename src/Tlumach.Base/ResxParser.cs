using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tlumach.Base
{
    public class ResxParser : BaseXMLParser
    {
        private static BaseParser Factory() => new ResxParser();

        static ResxParser()
        {
            // We register the parser for both configuration files and translation files.
            // This approach enables us to use configuration and translations in different formats.
            FileFormats.RegisterConfigParser(".resxcfg", Factory);
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
            var attr = dataElement.Attribute(cXmlNamespace + "space");
            return attr?.Value.Equals("preserve", StringComparison.Ordinal) == true;
        }

        protected override TextFormat GetEscapeMode()
        {
            return TextFormat.DotNet;
        }

        public override char GetLocaleSeparatorChar()
        {
            return '.';
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".resx", StringComparison.OrdinalIgnoreCase);
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
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
                        value = data.Value;

                        if (value is null)
                            continue;

                        if (!NodeHasPreserveAttr(data))
                            value = value.Trim();

                        // Add an entry

                        if (result.RootNode.Keys.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                            throw new GenericParserException($"Duplicate key '{key}' specified");

                        result.RootNode.Keys.Add(key, new TranslationTreeLeaf(key, IsTemplatedText(value)));
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

        protected override Translation InternalLoadTranslationEntriesFromXML(XElement parentNode, Translation? translation, string groupName)
        {
            if (parentNode is null)
                throw new ArgumentNullException(nameof(parentNode));

            // When processing the top level, pick the metadata (locale, context, author, last modified) values if they are present
            translation ??= new Translation(locale: null);

            foreach (var data in parentNode.Elements("data"))
            {
                TranslationEntry? entry;

                string? key;
                string? value;
                string? reference = null;

                // Skip non-string typed entries (e.g., images) if a type is specified
                var typeAttr = (string?) data.Attribute("type");
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

                    // Add an entry
                    if (translation.TryGetValue(key, out entry))
                        throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");

                    entry = new(key, value, escapedText: null, reference);
                    translation.Add(key.ToUpperInvariant(), entry);

                    if (value is not null)
                        entry.IsTemplated = IsTemplatedText(value);
                }
            }

            return translation;
        }
    }
}
