using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for Apple String Catalog files (.xcstrings).
/// A single output file contains translations for all supplied locales.
/// </summary>
public class StringCatWriter : BaseJsonWriter
{
    public override string FormatName => "StringCatalog";

    public override string ConfigExtension => ".jsoncfg";

    public override string TranslationExtension => ".xcstrings";

    // WriteConfiguration() is inherited from BaseJsonWriter.
    // WriteTranslation() is inherited from BaseJsonWriter and delegates to InternalWriteTranslations([culture]).

    /// <inheritdoc/>
    /// <remarks>
    /// Overrides the base implementation (which throws) to support writing all locales into a single .xcstrings file.
    /// </remarks>
    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        InternalWriteTranslations(translationManager, cultures, stream);
    }

    protected override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));
        if (cultures is null || cultures.Count == 0)
            throw new ArgumentException("At least one culture must be provided.", nameof(cultures));

        // Determine the source language
        string sourceLanguage = translationManager.DefaultConfiguration?.DefaultFileLocale
            ?? cultures.First().Name;

        // Collect (culture, translation) pairs, skipping missing translations
        var pairs = cultures
            .Select(c => (culture: c, translation: translationManager.GetTranslation(c)))
            .Where(p => p.translation is not null)
            .ToList();

        // Union of all keys across all translations
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, translation) in pairs)
            foreach (string k in translation!.Keys)
                allKeys.Add(k);

        var sortedKeys = allKeys
            .OrderBy(static k => k, new HierarchicalKeyComparer())
            .ToList();

        int step = IndentationStep;
        string i1 = new(' ', step);
        string i2 = new(' ', step * 2);
        string i3 = new(' ', step * 3);
        string i4 = new(' ', step * 4);
        string i5 = new(' ', step * 5);
        string i6 = new(' ', step * 6);

        var sb = new StringBuilder();

        sb.Append("{\n");
        sb.Append(i1).Append("\"sourceLanguage\" : \"").Append(Utils.JsonEncode(sourceLanguage)).Append("\",\n");
        sb.Append(i1).Append("\"strings\" : {\n");

        bool firstKey = true;
        foreach (string key in sortedKeys)
        {
            if (!firstKey)
                sb.Append(",\n");
            firstKey = false;

            sb.Append(i2).Append('"').Append(Utils.JsonEncode(key)).Append("\" : {\n");

            // Collect comment from the first translation that has one for this key
            string? comment = null;
            foreach (var (_, translation) in pairs)
            {
                if (translation!.TryGetValue(key, out TranslationEntry? e) && !string.IsNullOrEmpty(e.Comment))
                {
                    comment = e.Comment;
                    break;
                }
            }

            // Check whether at least one locale has a value for this key
            bool hasAnyLocalization = pairs.Any(p => p.translation!.TryGetValue(key, out _));

            if (comment is not null)
            {
                sb.Append(i3).Append("\"comment\" : \"").Append(Utils.JsonEncode(comment)).Append('"');
                sb.Append(hasAnyLocalization ? ",\n" : "\n");
            }

            if (hasAnyLocalization)
            {
                sb.Append(i3).Append("\"localizations\" : {\n");

                bool firstLang = true;
                foreach (var (culture, translation) in pairs)
                {
                    if (!translation!.TryGetValue(key, out TranslationEntry? entry))
                        continue;

                    string value = ShouldWriteReference(entry) ? "@" + (entry.Reference ?? string.Empty) : entry.Text ?? string.Empty;

                    if (!firstLang)
                        sb.Append(",\n");
                    firstLang = false;

                    sb.Append(i4).Append('"').Append(Utils.JsonEncode(culture.Name)).Append("\" : {\n");
                    sb.Append(i5).Append("\"stringUnit\" : {\n");
                    sb.Append(i6).Append("\"state\" : \"translated\",\n");
                    sb.Append(i6).Append("\"value\" : \"").Append(Utils.JsonEncode(value)).Append("\"\n");
                    sb.Append(i5).Append("}\n");
                    sb.Append(i4).Append('}');
                }

                sb.Append('\n');
                sb.Append(i3).Append("}\n");
            }

            sb.Append(i2).Append('}');
        }

        if (sortedKeys.Count > 0)
            sb.Append('\n');

        sb.Append(i1).Append("},\n");
        sb.Append(i1).Append("\"version\" : \"1.0\"\n");
        sb.Append('}');

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }
}
