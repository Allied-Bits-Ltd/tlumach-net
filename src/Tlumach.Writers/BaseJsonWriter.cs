using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Tlumach.Writers;

using System.Globalization;

using Tlumach.Base;

/// <summary>
/// The base class for the writers of JSON-based formats. Each translation file of these formats holds one language, unless a writer overrides <see cref="WriteTranslations"/>.
/// <para>Configuration files are written in the JSON configuration format.</para>
/// </summary>
public abstract class BaseJsonWriter : BaseWriter
{
    /// <summary>
    /// Gets or sets the number of spaces to add for each level of indentation. The default value is 2.
    /// <para>Only <see cref="JsonWriter"/> and <see cref="StringCatWriter"/> honor this property, in the translation files they write. <see cref="ArbWriter"/> and <see cref="BaseWriter.WriteConfiguration"/> use a fixed indentation.</para>
    /// </summary>
    public int IndentationStep { get; set; } = 2;

    /// <inheritdoc/>
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

        // The options below are written only when they carry a value other than the default, so that a configuration that does not use them is written back unchanged.
        if (config.CreateFilledMethods)
            WriteJsonProperty(TranslationConfiguration.KEY_CREATE_FILLED_METHODS, "true", false, sb);
        if (config.CreateStringAccessors)
            WriteJsonProperty(TranslationConfiguration.KEY_CREATE_STRING_ACCESSORS, "true", false, sb);
        if (!string.IsNullOrEmpty(config.StringAccessorsClass))
            WriteJsonProperty(TranslationConfiguration.KEY_STRING_ACCESSORS_CLASS, config.StringAccessorsClass!, false, sb);
        if (!string.IsNullOrEmpty(config.StringAccessorsCulture))
            WriteJsonProperty(TranslationConfiguration.KEY_STRING_ACCESSORS_CULTURE, config.StringAccessorsCulture!, false, sb);

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

    /// <summary>
    /// Appends a property with a string value to the configuration file being written.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property. It is encoded as a JSON string.</param>
    /// <param name="isFirst">Whether this is the first property of its object; if not, a comma precedes the property.</param>
    /// <param name="sb">The builder of the file.</param>
    protected virtual void WriteJsonProperty(string name, string value, bool isFirst, StringBuilder sb)
    {
        if (!isFirst)
            sb.Append(",\n");
        sb.Append("  \"").Append(name).Append("\": ");

        // Utils.JsonEncode escapes the content of a string but does not delimit it, so the quotes have to be added here, as every other caller of it does. Without them the written file is not valid
        // JSON and cannot be read back.
        sb.Append('"').Append(Utils.JsonEncode(value)).Append('"');
    }
}
