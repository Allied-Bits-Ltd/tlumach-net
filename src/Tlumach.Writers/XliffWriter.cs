using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for XLIFF 2.2 (eXtensible Localization Interchange File Format).
/// XLIFF is a bitext format that combines source and target translations in a single file.
/// </summary>
public class XliffWriter : BaseXmlWriter
{
    public override string FormatName => "XLIFF";

    public override string ConfigExtension => ".xlfcfg";

    public override string TranslationExtension => ".xlf";

    /// <summary>
    /// Gets or sets the source filename to use in the XLIFF file.
    /// If not set, defaults to the source translation's OriginalFilename or the configuration's DefaultFile.
    /// </summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// Gets or sets the target filename (used for reference/metadata only).
    /// </summary>
    public string? TargetFile { get; set; }

    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));

        if (cultures.Count != 1)
            throw new TlumachException("XLIFF writer requires exactly one target culture.");

        var targetCulture = cultures.First();

        // Retrieve source (default/invariant) translation
        var sourceTranslation = translationManager.GetTranslation(CultureInfo.InvariantCulture);
        if (sourceTranslation is null)
            throw new TlumachException("Source translation (invariant culture) is required for XLIFF output.");

        // Retrieve target translation
        var targetTranslation = translationManager.GetTranslation(targetCulture);
        if (targetTranslation is null)
            throw new TlumachException($"Target translation for culture '{targetCulture.Name}' is required for XLIFF output.");

        InternalWriteXliffBitext(sourceTranslation, targetTranslation, stream);
    }

    protected override void InternalWriteXmlTranslations(Translation translation, Stream stream)
    {
        // For single translation writing, output source-only XLIFF
        // This method is called when WriteTranslation (single culture) is used

        XDocument doc = new();
        string srcLang = translation.Locale ?? CultureInfo.InvariantCulture.Name;
        XElement root = new("xliff",
            new XAttribute("version", "2.0"),
            new XAttribute("srcLang", srcLang));

        doc.Add(root);

        // Create file element
        string filename = SourceFile ?? translation.OriginalFilename ?? "strings";
        XElement fileElement = new("file",
            new XAttribute("id", filename));

        root.Add(fileElement);

        // Get sorted entries
        List<TranslationEntry> entryList = GetSortedEntries(translation);

        // Add unit elements
        foreach (var entry in entryList)
        {
            XElement unit = new("unit",
                new XAttribute("id", entry.Key));

            // Add source element
            string sourceValue = ShouldWriteReference(entry) ? "@" + entry.Reference ?? string.Empty : entry.Text ?? string.Empty;
            XElement source = new("source", sourceValue);
            unit.Add(source);

            // Add note if comment exists
            if (!string.IsNullOrEmpty(entry.Comment))
            {
                XElement note = new("note", entry.Comment);
                unit.Add(note);
            }

            fileElement.Add(unit);
        }

        // Write document to stream
        using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
        {
            doc.Save(writer, SaveOptions.None);
        }
    }

    private void InternalWriteXliffBitext(Translation sourceTranslation, Translation targetTranslation, Stream stream)
    {
        XDocument doc = new();

        string srcLang = sourceTranslation.Locale ?? CultureInfo.InvariantCulture.Name;
        string trgLang = targetTranslation.Locale ?? "unknown";

        XElement root = new("xliff",
            new XAttribute("version", "2.0"),
            new XAttribute("srcLang", srcLang),
            new XAttribute("trgLang", trgLang));

        doc.Add(root);

        // Create file element
        string filename = SourceFile ?? sourceTranslation.OriginalFilename ?? "strings";
        XElement fileElement = new("file",
            new XAttribute("id", filename));

        root.Add(fileElement);

        // Get all unique keys from both source and target
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(sourceTranslation.Keys);
        allKeys.UnionWith(targetTranslation.Keys);

        // Sort keys hierarchically
        var sortedKeys = allKeys.OrderBy(k => k, new HierarchicalKeyComparer()).ToList();

        // Add unit elements
        foreach (var key in sortedKeys)
        {
            XElement unit = new("unit",
                new XAttribute("id", key));

            // Add source element
            string sourceText = string.Empty;
            if (sourceTranslation.TryGetValue(key, out var sourceEntry))
            {
                sourceText = ShouldWriteReference(sourceEntry) ? "@" + sourceEntry.Reference ?? string.Empty : sourceEntry.Text ?? string.Empty;
            }

            XElement source = new("source", sourceText);
            unit.Add(source);

            // Add target element if available
            if (targetTranslation.TryGetValue(key, out var targetEntry))
            {
                string targetText = ShouldWriteReference(targetEntry) ? "@" + targetEntry.Reference ?? string.Empty : targetEntry.Text ?? string.Empty;
                XElement target = new("target", targetText);
                unit.Add(target);

                // Add note if comment exists
                if (!string.IsNullOrEmpty(targetEntry.Comment))
                {
                    XElement note = new("note", targetEntry.Comment);
                    unit.Add(note);
                }
            }

            fileElement.Add(unit);
        }

        // Write document to stream
        using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
        {
            doc.Save(writer, SaveOptions.None);
        }
    }
}
