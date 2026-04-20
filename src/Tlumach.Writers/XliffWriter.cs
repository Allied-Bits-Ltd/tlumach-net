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

    /// <summary>
    /// Gets or sets the string that separates individual segments within a single translation value.
    /// When writing, a value containing this separator is split into multiple XLIFF &lt;segment&gt; elements.
    /// Defaults to a tab character, matching <see cref="XliffParser.SegmentSeparator"/>.
    /// </summary>
    public string SegmentSeparator { get; set; } = "\t";

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
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.2";
        XDocument doc = new();
        string srcLang = translation.Locale ?? CultureInfo.InvariantCulture.Name;
        XElement root = new(ns + "xliff",
            new XAttribute("version", "2.2"),
            new XAttribute("srcLang", srcLang));

        doc.Add(root);

        string filename = SourceFile ?? translation.OriginalFilename ?? "strings";
        XElement fileElement = new(ns + "file",
            new XAttribute("id", filename));

        root.Add(fileElement);

        List<TranslationEntry> entryList = GetSortedEntries(translation);

        foreach (var entry in entryList)
        {
            XElement unit = new(ns + "unit",
                new XAttribute("id", entry.Key));

            if (!string.IsNullOrEmpty(entry.Comment))
                unit.Add(BuildNotesElement(ns, entry.Comment!));

            string sourceValue = ShouldWriteReference(entry) ? "@" + entry.Reference ?? string.Empty : entry.Text ?? string.Empty;
            AddSegmentsToUnit(ns, unit, sourceValue, targetValue: null);

            fileElement.Add(unit);
        }

        using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
        doc.Save(writer, SaveOptions.None);
    }

    private void InternalWriteXliffBitext(Translation sourceTranslation, Translation targetTranslation, Stream stream)
    {
        XNamespace ns = "urn:oasis:names:tc:xliff:document:2.2";
        XDocument doc = new();

        string srcLang = sourceTranslation.Locale ?? CultureInfo.InvariantCulture.Name;
        string trgLang = targetTranslation.Locale ?? "unknown";

        XElement root = new(ns + "xliff",
            new XAttribute("version", "2.2"),
            new XAttribute("srcLang", srcLang),
            new XAttribute("trgLang", trgLang));

        doc.Add(root);

        string filename = SourceFile ?? sourceTranslation.OriginalFilename ?? "strings";
        XElement fileElement = new(ns + "file",
            new XAttribute("id", filename));

        root.Add(fileElement);

        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(sourceTranslation.Keys);
        allKeys.UnionWith(targetTranslation.Keys);

        var sortedKeys = allKeys.OrderBy(k => k, new HierarchicalKeyComparer()).ToList();

        foreach (var key in sortedKeys)
        {
            XElement unit = new(ns + "unit",
                new XAttribute("id", key));

            string? commentText = null;
            if (targetTranslation.TryGetValue(key, out var targetEntry) && !string.IsNullOrEmpty(targetEntry.Comment))
                commentText = targetEntry.Comment;

            if (commentText is not null)
                unit.Add(BuildNotesElement(ns, commentText));

            string sourceText = string.Empty;
            if (sourceTranslation.TryGetValue(key, out var sourceEntry))
                sourceText = ShouldWriteReference(sourceEntry) ? "@" + sourceEntry.Reference ?? string.Empty : sourceEntry.Text ?? string.Empty;

            string? targetText = null;
            if (targetEntry is not null)
                targetText = ShouldWriteReference(targetEntry) ? "@" + targetEntry.Reference ?? string.Empty : targetEntry.Text ?? string.Empty;

            AddSegmentsToUnit(ns, unit, sourceText, targetText);

            fileElement.Add(unit);
        }

        using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
        doc.Save(writer, SaveOptions.None);
    }

    private void AddSegmentsToUnit(XNamespace ns, XElement unit, string sourceValue, string? targetValue)
    {
        string sep = SegmentSeparator;
        string[] sourceParts = !string.IsNullOrEmpty(sep) && sourceValue.IndexOf(sep, StringComparison.Ordinal) >= 0
            ? sourceValue.Split(new[] { sep }, StringSplitOptions.None)
            : new[] { sourceValue };

        string[]? targetParts = null;
        if (targetValue is not null)
        {
            targetParts = !string.IsNullOrEmpty(sep) && targetValue.IndexOf(sep, StringComparison.Ordinal) >= 0
                ? targetValue.Split(new[] { sep }, StringSplitOptions.None)
                : new[] { targetValue };
        }

        int count = Math.Max(sourceParts.Length, targetParts?.Length ?? 0);
        for (int i = 0; i < count; i++)
        {
            string src = i < sourceParts.Length ? sourceParts[i] : string.Empty;
            XElement segment = new(ns + "segment", new XElement(ns + "source", src));

            if (targetParts is not null)
            {
                string tgt = i < targetParts.Length ? targetParts[i] : string.Empty;
                segment.Add(new XElement(ns + "target", tgt));
            }

            unit.Add(segment);
        }
    }

    private static XElement BuildNotesElement(XNamespace ns, string comment)
    {
        return new XElement(ns + "notes",
            new XElement(ns + "note", comment));
    }
}
