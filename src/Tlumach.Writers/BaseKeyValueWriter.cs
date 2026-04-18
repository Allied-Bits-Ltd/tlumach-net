using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

public abstract class BaseKeyValueWriter : BaseWriter
{
    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        TranslationConfiguration? config = translationManager.DefaultConfiguration;
        if (config is null)
            throw new TlumachException(BaseWriter.ErrNoConfigInTranslationManager);

        StringBuilder sb = new();

        if (!string.IsNullOrEmpty(config.DefaultFile))
            WriteKeyValueLine(TranslationConfiguration.KEY_DEFAULT_FILE, config.DefaultFile, sb);
        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            WriteKeyValueLine(TranslationConfiguration.KEY_DEFAULT_LOCALE, config.DefaultFileLocale, sb);
        if (!string.IsNullOrEmpty(config.Namespace))
            WriteKeyValueLine(TranslationConfiguration.KEY_GENERATED_NAMESPACE, config.Namespace, sb);
        if (!string.IsNullOrEmpty(config.ClassName))
            WriteKeyValueLine(TranslationConfiguration.KEY_GENERATED_CLASS, config.ClassName, sb);

        WriteKeyValueLine(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, config.DelayedUnitsCreation ? "true" : "false", sb);
        WriteKeyValueLine(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, config.OnlyDeclareKeys ? "true" : "false", sb);

        if (config.TextProcessingMode.HasValue)
            WriteKeyValueLine(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, config.TextProcessingMode.ToString() ?? string.Empty, sb);

        if (config.Translations.Count > 0)
        {
            // Write a section name
            WriteSection(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, sb);

            foreach (KeyValuePair<string, string> kvp in config.Translations)
                WriteKeyValueLine(kvp.Key, kvp.Value, sb);
        }

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    public override void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream)
    {
        InternalWriteTranslations(translationManager, [culture], stream);
    }

    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        throw new TlumachException(BaseWriter.ErrSingleFileFormatS1);
    }

    protected override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));

        CultureInfo culture = cultures.First();
        Translation? translation = translationManager.GetTranslation(culture);

        if (translation is null)
            throw new TlumachException(string.Format(BaseWriter.ErrNoTranslationForCultureS1, culture.Name));

        StringBuilder sb = new();

        string currentGroup = string.Empty;

        List<TranslationEntry> entryList;

        if (translation.OrderedEntries is not null)
        {
            entryList = translation.OrderedEntries!;
        }
        else
        {
            entryList = translation.Values.ToList();
            entryList.Sort(TranslationEntry.CompareByHierarchicalKey);
        }

        foreach (var entry in entryList)
        {
            string key = entry.Key;

            (string section, string keyName) = GetSectionAndKeyName(key);

            if (section.Length > 0 && !section.Equals(currentGroup, StringComparison.OrdinalIgnoreCase))
            {
                WriteSection(section, sb);
                currentGroup = section;
            }

            if (ShouldWriteReference(entry))
                WriteKeyValueLine(keyName, '@' + entry.Reference ?? string.Empty, sb);
            else
                WriteKeyValueLine(keyName, entry.Text ?? string.Empty, sb);
        }

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    protected abstract void WriteSection(string key, StringBuilder stringBuilder);

    protected abstract void WriteKeyValueLine(string key, string value, StringBuilder stringBuilder);

    protected abstract bool ShouldWriteReference(TranslationEntry entry);
}
