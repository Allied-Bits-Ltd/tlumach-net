using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Tlumach.Writers;

using System.Globalization;

using Tlumach.Base;

public abstract class BaseJsonWriter : BaseWriter
{
    /// <summary>
    /// Gets or sets the number of spaces to add for each level of indentation. The default value is 2.
    /// </summary>
    public int IndentationStep { get; set; } = 2;

    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        TranslationConfiguration? config = translationManager.DefaultConfiguration;
        if (config is null)
            throw new TlumachException(BaseWriter.ErrNoConfigInTranslationManager);

        StringBuilder sb = new();
        sb.Append("{\n");

        bool isFirstProperty = true;

        if (!string.IsNullOrEmpty(config.DefaultFile))
        {
            WriteJsonProperty(TranslationConfiguration.KEY_DEFAULT_FILE, config.DefaultFile, isFirstProperty, sb);
            isFirstProperty = false;
        }

        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
        {
            WriteJsonProperty(TranslationConfiguration.KEY_DEFAULT_LOCALE, config.DefaultFileLocale!, isFirstProperty, sb);
            isFirstProperty = false;
        }

        if (!string.IsNullOrEmpty(config.Namespace))
        {
            WriteJsonProperty(TranslationConfiguration.KEY_GENERATED_NAMESPACE, config.Namespace!, isFirstProperty, sb);
            isFirstProperty = false;
        }

        if (!string.IsNullOrEmpty(config.ClassName))
        {
            WriteJsonProperty(TranslationConfiguration.KEY_GENERATED_CLASS, config.ClassName!, isFirstProperty, sb);
            isFirstProperty = false;
        }

        WriteJsonProperty(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, config.DelayedUnitsCreation ? "true" : "false", isFirstProperty, sb);
        isFirstProperty = false;

        WriteJsonProperty(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, config.OnlyDeclareKeys ? "true" : "false", isFirstProperty, sb);

        if (config.TextProcessingMode.HasValue)
        {
            WriteJsonProperty(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, config.TextProcessingMode.ToString() ?? string.Empty, false, sb);
        }

        if (config.Translations.Count > 0)
        {
            sb.Append(",\n");
            sb.Append("  \"").Append(TranslationConfiguration.KEY_SECTION_TRANSLATIONS).Append("\": {\n");

            bool isFirstTranslation = true;
            foreach (KeyValuePair<string, string> kvp in config.Translations)
            {
                WriteJsonProperty(kvp.Key, kvp.Value, isFirstTranslation, sb);
                isFirstTranslation = false;
            }

            sb.Append("\n  }");
        }

        sb.Append("\n}");

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

    public virtual void WriteJsonProperty(string name, string value, bool isFirst, StringBuilder sb)
    {
        if (!isFirst)
            sb.Append(",\n");
        sb.Append("  \"").Append(name).Append("\": ");
        sb.Append(Utils.JsonEncode(value));
    }
}
