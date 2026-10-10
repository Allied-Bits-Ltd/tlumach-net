using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// The base class for the writers of table formats (CSV, TSV). A file of these formats has a row for each key and a column for each language, followed by the description and comments columns.
/// <para>Table formats have no configuration files.</para>
/// </summary>
public abstract class BaseTableWriter : BaseWriter
{
    /// <summary>
    /// Always throws an exception, as table formats have no configuration files. Use a writer of another format (e.g., INI or TOML) to write the configuration.
    /// </summary>
    /// <param name="translationManager">The translation manager whose configuration should be written.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    /// <exception cref="TlumachException">Always.</exception>
    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        throw new TlumachException("Table writers do not support configuration files. Use INI or TOML format for configuration.");
    }

    /// <summary>
    /// Writes a table with one language column.
    /// </summary>
    /// <param name="translationManager">The translation manager from which the translations should be picked.</param>
    /// <param name="culture">The culture to write.<para>Set this parameter to <see cref="CultureInfo.InvariantCulture"/> to signal that the default translation should be written.</para></param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    public override void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream)
    {
        InternalWriteTranslations(translationManager, [culture], stream);
    }

    /// <summary>
    /// Writes a table with a column for each of the cultures. The column of the default translation, if it is among the cultures, comes first; the keys of the first column define the rows.
    /// </summary>
    /// <param name="translationManager">The translation manager from which the translations should be picked.</param>
    /// <param name="cultures">The list of cultures to write.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    /// <exception cref="TlumachException"><paramref name="cultures"/> is empty, or there is no translation for one of the cultures.</exception>
    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        InternalWriteTranslations(translationManager, cultures, stream);
    }

    /// <inheritdoc/>
    protected override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));

        if (cultures.Count == 0)
            throw new TlumachException("At least one culture must be specified.");

        StringBuilder sb = new();

        List<CultureInfo> cultureList = [..cultures];
        List<Translation> translationList = [];

        string? defaultLocale = translationManager.DefaultConfiguration?.DefaultFileLocale;

        if (defaultLocale is null)
        {
            // Retrieve the default translation
            var sourceTranslation = translationManager.GetTranslation(CultureInfo.InvariantCulture, tryLoadMissing: true);
            defaultLocale = sourceTranslation?.Locale;
        }

        if (defaultLocale?.Length == 0)
            defaultLocale = null;

        CultureInfo? defaultCulture = null;

        foreach (CultureInfo culture in cultureList)
        {
            Translation? translation = translationManager.GetTranslation(culture);

            // A translation manager keeps the default translation (e.g. of a JSON default file whose locale is "en") as the translation of the invariant culture, not under the name
            // of its locale, so the default culture requested by its name is taken from there
            if (translation is null && defaultLocale is not null && culture.Name.Equals(defaultLocale, StringComparison.OrdinalIgnoreCase))
                translation = translationManager.GetTranslation(CultureInfo.InvariantCulture, tryLoadMissing: true);

            if (translation is null)
                throw new TlumachException(string.Format(BaseWriter.ErrNoTranslationForCultureS1, culture.Name));

            if ((defaultLocale is not null && (translation.Locale?.Equals(defaultLocale, StringComparison.OrdinalIgnoreCase) == true)) || (translation.Locale?.Length == 0))
            {
                translationList.Insert(0, translation);
                defaultCulture = culture;
            }
            else
            {
                translationList.Add(translation);
            }
        }

        // If we moved a translation to the beginning of the list, we need to do the same with the culture, to which the translation belongs.
        if (defaultCulture is not null)
        {
            cultureList.Remove(defaultCulture);
            cultureList.Insert(0, defaultCulture);
        }

        // The rows are the keys of the first translation, which is the default one when it is among the written cultures. A key that only other cultures have is not written,
        // as the default translation defines the set of keys.
        List<TranslationEntry> entryList = GetSortedEntries(translationList[0]);

        WriteHeaderRow(cultureList, defaultLocale, sb);
        WriteDataRows(entryList, translationList, cultureList, sb);

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    private void WriteHeaderRow(List<CultureInfo> cultures, string? defaultLocale, StringBuilder sb)
    {
        WriteCell(string.Empty, sb);

        foreach (CultureInfo culture in cultures)
        {
            // A column must have a locale name, which the invariant culture lacks; its translation is the default one, whose locale is used instead when it is known
            WriteCell(culture.Name.Length == 0 && defaultLocale is not null ? defaultLocale : culture.Name, sb);
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
                        value = '@' + translationEntry.Reference ?? string.Empty;
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

    /// <summary>
    /// Appends a cell with the given value to the current row, quoting and escaping the value as the format requires.
    /// </summary>
    /// <param name="value">The value of the cell.</param>
    /// <param name="sb">The builder of the file.</param>
    protected abstract void WriteCell(string value, StringBuilder sb);

    /// <summary>
    /// Ends the current row.
    /// </summary>
    /// <param name="sb">The builder of the file.</param>
    protected abstract void EndRow(StringBuilder sb);
}
