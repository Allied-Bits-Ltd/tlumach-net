// <copyright file="XliffParser.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Xml.Linq;

#if GENERATOR
namespace Tlumach.Generator;
#else
namespace Tlumach.Base;
#endif

/// <summary>
/// Parser for XLIFF 2.2 (eXtensible Localization Interchange File Format).
/// XLIFF is a bitext format that combines source and target translations in a single file.
/// </summary>
public class XliffParser : BaseXMLParser
{
    /// <summary>
    /// Gets or sets the source filename to be associated with parsed translations.
    /// Used when parsing XLIFF files to track the original source file reference.
    /// </summary>
    public static string? SourceFilename { get; set; }

    private static BaseParser Factory() => new XliffParser();

    /// <summary>
    /// Gets or sets the text processing mode to use when recognizing template strings in translation entries.
    /// </summary>
    public static TextFormat TextProcessingMode { get; set; }

    /// <summary>
    /// Gets or sets the string used to join multiple XLIFF segments into a single translation value.
    /// Defaults to a tab character.
    /// </summary>
    public string SegmentSeparator { get; set; } = "\t";

    static XliffParser()
    {
        TextProcessingMode = TextFormat.None;
        FileFormats.RegisterParser(".xlf", Factory);
        FileFormats.RegisterParser(".xliff", Factory);
        FileFormats.RegisterConfigParser(".xlfcfg", Factory);
        FileFormats.RegisterConfigParser(".xmlcfg", Factory);
    }

    /// <summary>
    /// Initializes the parser class, making it available for use.
    /// </summary>
    public static void Use()
    {
        // The role of this method is just to exist so that calling it executes a static constructor of this class.
    }

    public override bool CanHandleExtension(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return false;

        return fileExtension.Equals(".xlf", StringComparison.OrdinalIgnoreCase) ||
               fileExtension.Equals(".xliff", StringComparison.OrdinalIgnoreCase);
    }

    protected override TranslationTree? InternalLoadTranslationStructure(string content, TextFormat? textProcessingMode)
    {
        if (textProcessingMode is not null)
            TextProcessingMode = textProcessingMode.Value;

        try
        {
            XDocument doc = XDocument.Load(new StringReader(content));
            XElement? root = doc.Root;

            if (root is null)
                throw new GenericParserException("The XLIFF file has no XML root node.");

            XNamespace ns = root.Name.Namespace;
            TranslationTree result = new();

            foreach (var fileElement in root.Elements(ns + "file"))
            {
                ProcessUnitsForStructure(fileElement, result, textProcessingMode, ns);
            }

            return result;
        }
        catch (Exception ex) when (!(ex is GenericParserException) && !(ex is TextParseException))
        {
            throw new GenericParserException($"Failed to parse XLIFF file: {ex.Message}", ex);
        }
    }

    private void ProcessUnitsForStructure(XElement fileElement, TranslationTree result, TextFormat? textProcessingMode, XNamespace ns)
    {
        foreach (var unit in GetAllUnits(fileElement, ns))
        {
            string? unitId = (string?)unit.Attribute("id");
            if (string.IsNullOrEmpty(unitId))
                continue;

            string sourceText = GetSegmentText(unit, ns, isSource: true, SegmentSeparator);
            bool hasTemplate = IsTemplatedText(sourceText, textProcessingMode);
            var leaf = new TranslationTreeLeaf(unitId!, hasTemplate);
            result.RootNode.Keys[unitId!] = leaf;
        }
    }

    protected internal override Translation InternalLoadTranslationEntriesFromXML(XElement parentNode, CultureInfo? culture, Translation? translation, string groupName, TextFormat? textProcessingMode)
    {
        string? srcLang = (string?)parentNode.Attribute("srcLang");
        string? targetLang = (string?)parentNode.Attribute("trgLang");

        if (string.IsNullOrEmpty(srcLang))
            throw new GenericParserException("XLIFF document must have 'srcLang' attribute on root element.");

        translation ??= new Translation(locale: null, keepEntryOrder: KeepEntryOrder);

        string? targetLanguageToExtract = null;
        bool isSourceLanguage = false;

        if (culture is not null && srcLang!.Length > 0)
        {
            if (CultureMatch(culture.Name, srcLang!))
            {
                isSourceLanguage = true;
                targetLanguageToExtract = srcLang;
            }
            else if (!string.IsNullOrEmpty(targetLang) && CultureMatch(culture.Name, targetLang!))
            {
                isSourceLanguage = false;
                targetLanguageToExtract = targetLang;
            }
            else
            {
                return translation;
            }
        }
        else
        {
            targetLanguageToExtract = srcLang;
            isSourceLanguage = true;
        }

        translation.Locale = targetLanguageToExtract;

        XNamespace ns = parentNode.Name.Namespace;

        foreach (var fileElement in parentNode.Elements(ns + "file"))
        {
            ProcessUnitsForTranslation(fileElement, translation, isSourceLanguage, srcLang!, targetLang, textProcessingMode, ns);
        }

        return translation;
    }

    private void ProcessUnitsForTranslation(XElement fileElement, Translation translation, bool isSourceLanguage, string srcLang, string? targetLang, TextFormat? textProcessingMode, XNamespace ns)
    {
        foreach (var unit in GetAllUnits(fileElement, ns))
        {
            string? unitId = (string?)unit.Attribute("id");
            if (string.IsNullOrEmpty(unitId))
                continue;

            string sourceText = GetSegmentText(unit, ns, isSource: true, SegmentSeparator);
            string? targetText = GetSegmentText(unit, ns, isSource: false, SegmentSeparator);
            if (string.IsNullOrEmpty(targetText))
                targetText = null;

            string? text = isSourceLanguage ? sourceText : targetText;

            if (text is null)
                continue;

            string? pairedText = isSourceLanguage ? targetText : sourceText;

            TranslationEntry entry = new(unitId!, text: text, escapedText: null, reference: null);

            if (!string.IsNullOrEmpty(pairedText))
                entry.SourceText = pairedText;

            // <notes><note> container (XLIFF 2.x) or direct <note> child (XLIFF 2.0 simple)
            var notesElement = unit.Element(ns + "notes");
            if (notesElement is not null)
            {
                var sb = new StringBuilder();
                foreach (var note in notesElement.Elements(ns + "note"))
                {
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(note.Value);
                }

                if (sb.Length > 0)
                    entry.Comment = sb.ToString();
            }
            else
            {
                var noteElement = unit.Element(ns + "note");
                if (noteElement is not null)
                    entry.Comment = noteElement.Value;
            }

            if (textProcessingMode.HasValue)
                entry.ContainsPlaceholders = IsTemplatedText(text, textProcessingMode);

            translation.Add(unitId!.ToUpperInvariant(), entry);
        }
    }

    /// <summary>
    /// Recursively yields all &lt;unit&gt; elements inside a parent element, descending into &lt;group&gt; children.
    /// </summary>
    private static IEnumerable<XElement> GetAllUnits(XElement parent, XNamespace ns)
    {
        foreach (var unit in parent.Elements(ns + "unit"))
            yield return unit;

        foreach (var group in parent.Elements(ns + "group"))
            foreach (var unit in GetAllUnits(group, ns))
                yield return unit;
    }

    /// <summary>
    /// Extracts text from a &lt;unit&gt; by concatenating all &lt;segment&gt; and &lt;ignorable&gt; child texts
    /// using <paramref name="separator"/>, or falling back to a direct &lt;source&gt;/&lt;target&gt; child when no segments exist.
    /// </summary>
    private static string GetSegmentText(XElement unit, XNamespace ns, bool isSource, string separator)
    {
        string elementName = isSource ? "source" : "target";

        var sb = new StringBuilder();
        bool hasSegments = false;

        foreach (var container in unit.Elements(ns + "segment").Concat(unit.Elements(ns + "ignorable")))
        {
            hasSegments = true;
            var elem = container.Element(ns + elementName);
            if (elem is null)
                continue;

            string part = GetInlineText(elem);
            if (part.Length == 0)
                continue;

            if (sb.Length > 0)
                sb.Append(separator);

            sb.Append(part);
        }

        if (hasSegments)
            return sb.ToString();

        // No segment wrappers — direct child element (XLIFF 2.0 simple style)
        var directElement = unit.Element(ns + elementName);
        return directElement is not null ? GetInlineText(directElement) : string.Empty;
    }

    /// <summary>
    /// Returns the text content of an element, expanding &lt;ph&gt; inline elements using their <c>equiv</c> attribute.
    /// </summary>
    private static string GetInlineText(XElement element)
    {
        var sb = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
                sb.Append(textNode.Value);
            else if (node is XElement child && child.Name.LocalName == "ph")
                sb.Append(child.Attribute("equiv")?.Value ?? string.Empty);
        }

        return sb.ToString();
    }

    private static bool CultureMatch(string cultureName, string languageCode)
    {
        if (string.IsNullOrEmpty(cultureName) || string.IsNullOrEmpty(languageCode))
            return false;

        if (cultureName.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
            return true;

        if (cultureName.StartsWith(languageCode + "-", StringComparison.OrdinalIgnoreCase))
            return true;

        if (languageCode.Contains('-') && cultureName.Equals(languageCode.Split('-')[0], StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
