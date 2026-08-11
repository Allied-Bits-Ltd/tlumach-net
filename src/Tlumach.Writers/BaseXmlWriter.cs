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

        // Every element is named after the key constant, which is what BaseXMLParser looks for and what the writers of the other formats use. The names are case-sensitive to XElement.Element,
        // so a name that differs in case only, however natural it looks in XML, produces a file that the parser cannot read back.
        if (!string.IsNullOrEmpty(config.DefaultFile))
            root.Add(new XElement(TranslationConfiguration.KEY_DEFAULT_FILE, config.DefaultFile));

        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            root.Add(new XElement(TranslationConfiguration.KEY_DEFAULT_LOCALE, config.DefaultFileLocale));

        if (!string.IsNullOrEmpty(config.Namespace))
            root.Add(new XElement(TranslationConfiguration.KEY_GENERATED_NAMESPACE, config.Namespace));

        if (!string.IsNullOrEmpty(config.ClassName))
            root.Add(new XElement(TranslationConfiguration.KEY_GENERATED_CLASS, config.ClassName));

        root.Add(new XElement(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, config.DelayedUnitsCreation ? "true" : "false"));
        root.Add(new XElement(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, config.OnlyDeclareKeys ? "true" : "false"));

        // The options below are written only when they carry a value other than the default, so that a configuration that does not use them is written back unchanged.
        if (config.CreateFilledMethods)
            root.Add(new XElement(TranslationConfiguration.KEY_CREATE_FILLED_METHODS, "true"));

        if (config.CreateStringAccessors)
            root.Add(new XElement(TranslationConfiguration.KEY_CREATE_STRING_ACCESSORS, "true"));

        if (!string.IsNullOrEmpty(config.StringAccessorsClass))
            root.Add(new XElement(TranslationConfiguration.KEY_STRING_ACCESSORS_CLASS, config.StringAccessorsClass));

        if (!string.IsNullOrEmpty(config.StringAccessorsCulture))
            root.Add(new XElement(TranslationConfiguration.KEY_STRING_ACCESSORS_CULTURE, config.StringAccessorsCulture));

        if (config.TextProcessingMode.HasValue)
            root.Add(new XElement(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, config.TextProcessingMode.ToString() ?? string.Empty));

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
