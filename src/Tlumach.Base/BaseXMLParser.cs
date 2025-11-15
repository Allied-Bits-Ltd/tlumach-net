using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Tlumach.Base
{
    public abstract class BaseXMLParser : BaseParser
    {
        // "xml" namespace is special: must be explicitly referenced
        protected static XNamespace cXmlNamespace = "http://www.w3.org/XML/1998/namespace";

        public override Translation? LoadTranslation(string translationText, CultureInfo? culture)
        {
            try
            {
                XDocument doc = XDocument.Load(new StringReader(translationText));

                XElement? root = doc.Root;

                if (root is null)
                    throw new GenericParserException("The translation file has no XML root node.");

                return InternalLoadTranslationEntriesFromXML(root, null, string.Empty);
            }
            catch (XmlException ex)
            {
                int pos = GetAbsolutePosition(translationText, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of the translation has failed", ex);
            }
        }

        protected abstract Translation InternalLoadTranslationEntriesFromXML(XElement parentNode, Translation? translation, string groupName);

        public override TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly)
        {
            try
            {
                XDocument doc = XDocument.Load(new StringReader(fileContent));

                if (doc.Root is null)
                    throw new GenericParserException("The configuration file has no XML root node.");

                string? defaultFile = doc.Root.Element(TranslationConfiguration.KEY_DEFAULT_FILE)?.Value.Trim();
                string? defaultLocale = doc.Root.Element(TranslationConfiguration.KEY_DEFAULT_LOCALE)?.Value.Trim();
                string? generatedNamespace = doc.Root.Element(TranslationConfiguration.KEY_GENERATED_NAMESPACE)?.Value.Trim();
                string? generatedClassName = doc.Root.Element(TranslationConfiguration.KEY_GENERATED_CLASS)?.Value.Trim();

                TranslationConfiguration result = new TranslationConfiguration(assembly, defaultFile ?? string.Empty, generatedNamespace, generatedClassName, defaultLocale, GetEscapeMode());

                if (string.IsNullOrEmpty(defaultFile))
                    return result;

                XElement? translationsNode = doc.Root?.Element(TranslationConfiguration.KEY_SECTION_TRANSLATIONS);

                // If the configuration contains the Translations section, parse it
                if (translationsNode is not null)
                {
                    // Enumerate nodes
                    foreach (var item in translationsNode.Elements())
                    {
                        if (!item.Name.ToString().Equals(TranslationConfiguration.KEY_LOCALE, StringComparison.OrdinalIgnoreCase))
                            throw new GenericParserException($"Unexpected '{item.Name}' tag in the '{TranslationConfiguration.KEY_SECTION_TRANSLATIONS}' sections");

                        string? lang = item.Attribute(TranslationConfiguration.KEY_ATTR_NAME)?.Value.Trim();

                        if (string.IsNullOrEmpty(lang))
                            throw new GenericParserException($"The '{TranslationConfiguration.KEY_ATTR_NAME}' attribute is missing from the '{TranslationConfiguration.KEY_LOCALE}' node");

                        if (lang!.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal))
                            lang = TranslationConfiguration.KEY_TRANSLATION_OTHER;
                        else
                            lang = lang.ToUpperInvariant();

                        if (result.Translations.ContainsKey(lang))
                            throw new GenericParserException($"Duplicate translation reference '{item.Attribute(TranslationConfiguration.KEY_ATTR_NAME)?.Value}' specified in the list of translations");

                        string value = item.Value.ToString().Trim();
                        result.Translations.Add(lang, value);
                    }
                }

                return result;
            }
            catch (XmlException ex)
            {
                int pos = GetAbsolutePosition(fileContent, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }
    }
}
