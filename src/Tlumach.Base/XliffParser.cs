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

            TranslationTree result = new();

            // Process all units in all files
            foreach (var fileElement in root.Elements("file"))
            {
                ProcessUnitsForStructure(fileElement, result, textProcessingMode);
            }

            return result;
        }
        catch (Exception ex) when (!(ex is GenericParserException) && !(ex is TextParseException))
        {
            throw new GenericParserException($"Failed to parse XLIFF file: {ex.Message}", ex);
        }
    }

    private void ProcessUnitsForStructure(XElement fileElement, TranslationTree result, TextFormat? textProcessingMode)
    {
        var units = fileElement.Elements("unit");

        foreach (var unit in units)
        {
            string? unitId = (string?)unit.Attribute("id");
            if (string.IsNullOrEmpty(unitId))
                continue;

            var sourceElement = unit.Element("source");
            if (sourceElement != null)
            {
                string text = sourceElement.Value ?? string.Empty;
                bool hasTemplate = IsTemplatedText(text, textProcessingMode);
                var leaf = new TranslationTreeLeaf(unitId!, hasTemplate);
                result.RootNode.Keys[unitId!] = leaf;
            }
        }
    }

    protected internal override Translation InternalLoadTranslationEntriesFromXML(XElement parentNode, CultureInfo? culture, Translation? translation, string groupName, TextFormat? textProcessingMode)
    {
        /*// This method is called from the base class parsing. For XLIFF, we handle the full structure in LoadTranslation.
        // Return empty translation as the main parsing happens in LoadTranslation override.
        return translation ?? new Translation(locale: null);*/

        // Get source and target language codes
        string? srcLang = (string?)parentNode.Attribute("srcLang");
        string? targetLang = (string?)parentNode.Attribute("trgLang");

        if (string.IsNullOrEmpty(srcLang))
            throw new GenericParserException("XLIFF document must have 'srcLang' attribute on root element.");

        translation ??= new Translation(locale: null, keepEntryOrder: KeepEntryOrder);

        // Determine which language to extract
        string? targetLanguageToExtract = null;
        bool isSourceLanguage = false;

        if (culture is not null && srcLang!.Length > 0)
        {
            // Check if requested culture matches source language
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
                // No matching language found
                return translation;
            }
        }
        else
        {
            // If no culture specified, default to source language
            targetLanguageToExtract = srcLang;
            isSourceLanguage = true;
        }

        translation.Locale = targetLanguageToExtract;

        // Process all files and units
        foreach (var fileElement in parentNode.Elements("file"))
        {
            //string? fileId = (string?)fileElement.Attribute("id");
            ProcessUnitsForTranslation(fileElement, translation, isSourceLanguage, srcLang!, targetLang, textProcessingMode);
        }

        return translation;
    }

    /*public override Translation? LoadTranslation(string translationText, CultureInfo? culture, TextFormat? textProcessingMode)
    {
        try
        {
            XDocument doc = XDocument.Load(new StringReader(translationText));
            XElement? root = doc.Root;

            if (root is null)
                return null;

            // Get source and target language codes
            string? srcLang = (string?)root.Attribute("srcLang");
            string? targetLang = (string?)root.Attribute("trgLang");

            if (string.IsNullOrEmpty(srcLang))
                throw new GenericParserException("XLIFF document must have 'srcLang' attribute on root element.");

            // Determine which language to extract
            string? targetLanguageToExtract = null;
            bool isSourceLanguage = false;

            if (culture is not null && srcLang!.Length > 0)
            {
                // Check if requested culture matches source language
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
                    // No matching language found
                    return null;
                }
            }
            else
            {
                // If no culture specified, default to source language
                targetLanguageToExtract = srcLang;
                isSourceLanguage = true;
            }

            Translation result = new(locale: targetLanguageToExtract, keepEntryOrder: KeepEntryOrder);

            // Process all files and units
            foreach (var fileElement in root.Elements("file"))
            {
                //string? fileId = (string?)fileElement.Attribute("id");
                ProcessUnitsForTranslation(fileElement, result, isSourceLanguage, srcLang!, targetLang, textProcessingMode);
            }

            return result;
        }
        catch (Exception ex) when (!(ex is GenericParserException) && !(ex is TextParseException))
        {
            throw new GenericParserException($"Failed to parse XLIFF file: {ex.Message}", ex);
        }
    }*/

    private void ProcessUnitsForTranslation(XElement fileElement, Translation translation, bool isSourceLanguage, string srcLang, string? targetLang, TextFormat? textProcessingMode)
    {
        var units = fileElement.Elements("unit");

        foreach (var unit in units)
        {
            string? unitId = (string?)unit.Attribute("id");
            if (string.IsNullOrEmpty(unitId))
                continue;

            var sourceElement = unit.Element("source");
            var targetElement = unit.Element("target");

            string? sourceText = sourceElement?.Value;
            string? targetText = targetElement?.Value;

            // Extract the requested language text
            string? text = isSourceLanguage ? sourceText : targetText;

            if (text is null)
                continue;

            // Store the paired language (source if extracting target, target if extracting source)
            string? pairedText = isSourceLanguage ? targetText : sourceText;

            TranslationEntry entry = new(unitId!, text: text, escapedText: null, reference: null);

            // Store the paired language as SourceText for reference
            if (!string.IsNullOrEmpty(pairedText))
            {
                entry.SourceText = pairedText;
            }

            // Process notes as comments
            var noteElement = unit.Element("note");
            if (noteElement is not null)
            {
                entry.Comment = noteElement.Value;
            }

            // Check for placeholders
            if (textProcessingMode.HasValue)
            {
                entry.ContainsPlaceholders = IsTemplatedText(text, textProcessingMode);
            }

            translation.Add(unitId!.ToUpperInvariant(), entry);
        }
    }

    private static bool CultureMatch(string cultureName, string languageCode)
    {
        if (string.IsNullOrEmpty(cultureName) || string.IsNullOrEmpty(languageCode))
            return false;

        // Exact match
        if (cultureName.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if the culture's language matches (e.g., "en" matches "en-US")
        if (cultureName.StartsWith(languageCode + "-", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if language code matches culture's language part
        if (languageCode.Contains('-') && cultureName.Equals(languageCode.Split('-')[0], StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
