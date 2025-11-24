# Managing Languages

## Initial Setup

To start, you need a [configuration file](config-file.md) and one or more [translation files](glossary.md#TranslationFile). These files may come in a number of supported [formats](files-formats.md) and can be created in any text editor or translation tool.

You will likely either include the files as .NET resources or just include them as content files with your project and load them from the disk. One more option is to keep files in a custom location and handle the <xref:Tlumach.TranslationManager.OnFileContentNeeded> event to provide the contents of the translation files.

## Collecting Available Languages

In the simplest case, your code knows what languages you support. However, it may be more convenient to keep language and translation management in one place, in <xref:Tlumach.TranslationManager>.

This class has methods that let you enumerate available translations so that you can present them as options for a user to choose from:

* <xref:Tlumach.TranslationManager.ListCulturesInConfiguration> - this method returns the list of references to translation files explicitly provided in the configuration file. You can use the <xref:Tlumach.TranslationManager.ListCultures> method to convert the list of filenames into the list of cultures.
* <xref:Tlumach.TranslationManager.ListTranslationFiles> - this method returns the list of translation files in the assembly or on the disk depending on the parameters and the settings of the translation manager. You can use the <xref:Tlumach.TranslationManager.ListCultures> method to convert the list of filenames into the list of cultures.
* <xref:Tlumach.TranslationManager.ListCultures> - converts the filename lists obtained using the above-mentioned methods into the list of CultureInfo objects, suitable for further use. Note that this method lists the cultures based on filenames - for CSV and TSV formats, it does not currently scan the files for included translations.

You can refer to the sample projects to see how handy these methods are in presenting the list of translations in your UI.

## Switching Current Language

Switching current language makes sense in the applications that produce text in one language at a time (mostly, desktop and mobile applications). If you have a server application that serves client requests, you will likely use the language of the client, and the "current language" concept will not be applicable to your scenario.

If you use generated translation units (directly or by binding XAML elements to these units), the language of the interface is defined by the <xref:Tlumach.TranslationManager.CurrentCulture> property. Assign the culture which you want to be used in your UI to this property.

If you access <xref:Tlumach.TranslationManager> directly by calling its `GetValue()` methods, you can also use the <xref:Tlumach.TranslationManager.CurrentCulture> property, but this does not give you a huge benefit, because the methods that return a translation unit either accept a culture as a parameter or call an overload which accepts it and pass the value of the property there.
