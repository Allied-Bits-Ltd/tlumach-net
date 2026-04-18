using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

public abstract class BaseTableWriter : BaseWriter
{
    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        throw new TlumachException("Table writers do not support configuration files. Use INI or TOML format for configuration.");
    }

    public override void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream)
    {
        InternalWriteTranslations(translationManager, [culture], stream);
    }

    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        InternalWriteTranslations(translationManager, cultures, stream);
    }

    protected override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));

        if (cultures.Count == 0)
            throw new TlumachException("At least one culture must be specified.");

        StringBuilder sb = new();

        List<CultureInfo> cultureList = [..cultures];
        List<Translation> translationList = [];
        List<TranslationEntry> allEntries = [];
        HashSet<string> allKeys = [];

        foreach (CultureInfo culture in cultureList)
        {
            Translation? translation = translationManager.GetTranslation(culture);
            if (translation is null)
                throw new TlumachException(string.Format(BaseWriter.ErrNoTranslationForCultureS1, culture.Name));

            translationList.Add(translation);

            foreach (TranslationEntry entry in translation.Values)
            {
                if (!allKeys.Contains(entry.Key.ToUpperInvariant()))
                {
                    allKeys.Add(entry.Key.ToUpperInvariant());
                    allEntries.Add(entry);
                }
            }
        }

        List<TranslationEntry> entryList;
        if (translationList[0].OrderedEntries is not null)
        {
            entryList = translationList[0].OrderedEntries!;
        }
        else
        {
            entryList = allEntries;
            entryList.Sort(TranslationEntry.CompareByHierarchicalKey);
        }

        WriteHeaderRow(cultureList, sb);
        WriteDataRows(entryList, translationList, cultureList, sb);

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    private void WriteHeaderRow(List<CultureInfo> cultures, StringBuilder sb)
    {
        WriteCell(string.Empty, sb);

        foreach (CultureInfo culture in cultures)
        {
            WriteCell(culture.Name, sb);
        }

        WriteCell(BaseTableParser.DescriptionColumnCaption, sb);
        WriteCell(BaseTableParser.CommentsColumnCaption, sb);

        EndRow(sb);
    }

    private void WriteDataRows(List<TranslationEntry> entries, List<Translation> translations, List<CultureInfo> cultures, StringBuilder sb)
    {
        foreach (TranslationEntry entry in entries)
        {
            WriteCell(entry.Key, sb);

            foreach (Translation translation in translations)
            {
                string value = string.Empty;
                if (translation.TryGetValue(entry.Key.ToUpperInvariant(), out TranslationEntry? translationEntry))
                {
                    if (ShouldWriteReference(translationEntry))
                        value = '@' + (translationEntry.Reference ?? string.Empty);
                    else
                        value = translationEntry.Text ?? string.Empty;
                }

                WriteCell(value, sb);
            }

            WriteCell(entry.Description ?? string.Empty, sb);
            WriteCell(entry.Comment ?? string.Empty, sb);

            EndRow(sb);
        }
    }

    protected abstract void WriteCell(string value, StringBuilder sb);

    protected abstract void EndRow(StringBuilder sb);

    protected abstract bool ShouldWriteReference(TranslationEntry entry);
}
