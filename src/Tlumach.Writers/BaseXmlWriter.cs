using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

using Tlumach.Base;

namespace Tlumach.Writers;

public abstract class BaseXmlWriter : BaseWriter
{
    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        TranslationConfiguration? config = translationManager.DefaultConfiguration;
        if (config is null)
            throw new TlumachException(BaseWriter.ErrNoConfigInTranslationManager);

        XDocument doc = new();
        XElement root = new("configuration");
        doc.Add(root);

        if (!string.IsNullOrEmpty(config.DefaultFile))
            root.Add(new XElement("DefaultFile", config.DefaultFile));

        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            root.Add(new XElement("DefaultLocale", config.DefaultFileLocale));

        if (!string.IsNullOrEmpty(config.Namespace))
            root.Add(new XElement("GeneratedNamespace", config.Namespace));

        if (!string.IsNullOrEmpty(config.ClassName))
            root.Add(new XElement("GeneratedClass", config.ClassName));

        root.Add(new XElement("DelayedUnitsCreation", config.DelayedUnitsCreation ? "true" : "false"));
        root.Add(new XElement("OnlyDeclareKeys", config.OnlyDeclareKeys ? "true" : "false"));

        if (config.TextProcessingMode.HasValue)
            root.Add(new XElement("TextProcessingMode", config.TextProcessingMode.ToString() ?? string.Empty));

        if (config.Translations.Count > 0)
        {
            XElement translationsElement = new(TranslationConfiguration.KEY_SECTION_TRANSLATIONS);
            foreach (var kvp in config.Translations)
            {
                XElement localeElement = new(TranslationConfiguration.KEY_LOCALE);
                localeElement.SetAttributeValue(TranslationConfiguration.KEY_ATTR_NAME, kvp.Key);
                localeElement.Value = kvp.Value;
                translationsElement.Add(localeElement);
            }

            root.Add(translationsElement);
        }

        using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
        {
            doc.Save(writer, SaveOptions.None);
        }
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

        InternalWriteXmlTranslations(translation, stream);
    }

    protected abstract void InternalWriteXmlTranslations(Translation translation, Stream stream);
}
