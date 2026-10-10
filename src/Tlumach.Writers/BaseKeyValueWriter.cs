using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// The base class for the writers of key-value formats (INI, TOML). Each translation file of these formats holds one language; the part of a key before its last dot becomes the name of a section.
/// </summary>
public abstract class BaseKeyValueWriter : BaseWriter
{
    /// <inheritdoc/>
    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        TranslationConfiguration? config = translationManager.DefaultConfiguration;
        if (config is null)
            throw new TlumachException(BaseWriter.ErrNoConfigInTranslationManager);

        StringBuilder sb = new();

        if (!string.IsNullOrEmpty(config.DefaultFile))
            WriteKeyValueLine(TranslationConfiguration.KEY_DEFAULT_FILE, config.DefaultFile, sb);
        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            WriteKeyValueLine(TranslationConfiguration.KEY_DEFAULT_LOCALE, config.DefaultFileLocale!, sb);
        if (!string.IsNullOrEmpty(config.Namespace))
            WriteKeyValueLine(TranslationConfiguration.KEY_GENERATED_NAMESPACE, config.Namespace!, sb);
        if (!string.IsNullOrEmpty(config.ClassName))
            WriteKeyValueLine(TranslationConfiguration.KEY_GENERATED_CLASS, config.ClassName!, sb);

        WriteKeyValueLine(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, config.DelayedUnitsCreation ? "true" : "false", sb);
        WriteKeyValueLine(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, config.OnlyDeclareKeys ? "true" : "false", sb);

        // The options below are written only when they carry a value other than the default, so that a configuration that does not use them is written back unchanged.
        if (config.CreateFilledMethods)
            WriteKeyValueLine(TranslationConfiguration.KEY_CREATE_FILLED_METHODS, "true", sb);
        if (config.CreateStringAccessors)
            WriteKeyValueLine(TranslationConfiguration.KEY_CREATE_STRING_ACCESSORS, "true", sb);
        if (!string.IsNullOrEmpty(config.StringAccessorsClass))
            WriteKeyValueLine(TranslationConfiguration.KEY_STRING_ACCESSORS_CLASS, config.StringAccessorsClass!, sb);
        if (!string.IsNullOrEmpty(config.StringAccessorsCulture))
            WriteKeyValueLine(TranslationConfiguration.KEY_STRING_ACCESSORS_CULTURE, config.StringAccessorsCulture!, sb);

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

    /// <inheritdoc/>
    public override void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream)
    {
        InternalWriteTranslations(translationManager, [culture], stream);
    }

    /// <summary>
    /// Always throws an exception, as the format does not support multiple languages in one file. Use <see cref="WriteTranslation"/> instead.
    /// </summary>
    /// <param name="translationManager">The translation manager from which the translations should be picked.</param>
    /// <param name="cultures">The list of cultures to write.</param>
    /// <param name="stream">The stream to write the resulting file to.</param>
    /// <exception cref="TlumachException">Always.</exception>
    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        throw new TlumachException(string.Format(CultureInfo.InvariantCulture, BaseWriter.ErrSingleFileFormatS1, FormatName));
    }

    /// <inheritdoc/>
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

        List<TranslationEntry> entryList = GetSortedEntries(translation);

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

    /// <summary>
    /// Appends the header of a section to the file being written.
    /// </summary>
    /// <param name="key">The name of the section.</param>
    /// <param name="stringBuilder">The builder of the file.</param>
    protected abstract void WriteSection(string key, StringBuilder stringBuilder);

    /// <summary>
    /// Appends a line with a key and its value to the file being written, quoting and escaping them as the format requires.
    /// </summary>
    /// <param name="key">The key (within the current section).</param>
    /// <param name="value">The value.</param>
    /// <param name="stringBuilder">The builder of the file.</param>
    protected abstract void WriteKeyValueLine(string key, string value, StringBuilder stringBuilder);
}
