using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

internal class TomlWriter : BaseWriter
{
    public override string ConfigExtension => ".tomlcfg";

    public override string TranslationExtension => ".toml";

    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        TranslationConfiguration? config = translationManager.DefaultConfiguration;
        if (config is null)
            throw new TlumachException(BaseWriter.ErrNoConfigInTranslationManager);

        StringBuilder sb = new();

        if (!string.IsNullOrEmpty(config.DefaultFile))
            WriteTomlLine(TranslationConfiguration.KEY_DEFAULT_FILE, config.DefaultFile, sb);
        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            WriteTomlLine(TranslationConfiguration.KEY_DEFAULT_LOCALE, config.DefaultFileLocale, sb);
        if (!string.IsNullOrEmpty(config.Namespace))
            WriteTomlLine(TranslationConfiguration.KEY_GENERATED_NAMESPACE, config.Namespace, sb);
        if (!string.IsNullOrEmpty(config.ClassName))
            WriteTomlLine(TranslationConfiguration.KEY_GENERATED_CLASS, config.ClassName, sb);

        WriteTomlLine(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, config.DelayedUnitsCreation ? "true" : "false", sb);
        WriteTomlLine(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, config.OnlyDeclareKeys ? "true" : "false", sb);

        if (config.TextProcessingMode.HasValue)
            WriteTomlLine(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, config.TextProcessingMode.ToString() ?? string.Empty, sb);

        if (config.Translations.Count > 0)
        {
            sb.AppendLine($"[{TranslationConfiguration.KEY_SECTION_TRANSLATIONS}]");
            foreach (KeyValuePair<string, string> kvp in config.Translations)
                WriteTomlLine(kvp.Key, kvp.Value, sb);
        }

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    public override void WriteTranslation(TranslationManager translationManager, CultureInfo culture, Stream stream)
    {
        InternalWriteTranslations(translationManager, new[] { culture }, stream);
    }

    public override void WriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        throw new TlumachException(BaseWriter.ErrSingleFileFormat);
    }

    public override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        CultureInfo culture = cultures.First();
        Translation? translation = translationManager.GetTranslation(culture);

        if (translation is null)
            throw new TlumachException(string.Format(BaseWriter.ErrNoTranslationForCulture, culture.Name));

        StringBuilder sb = new();

        foreach (KeyValuePair<string, TranslationEntry> kvp in translation)
        {
            string key = kvp.Key;
            string? value = kvp.Value.Text;

            if (value is null)
                continue;

            WriteTomlLine(key, value, sb);
        }

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    void WriteTomlLine(string key, string value, StringBuilder stringBuilder)
    {
        throw new NotImplementedException();
    }
}
