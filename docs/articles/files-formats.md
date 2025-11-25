# Translation Files and Formats

## Default and Locale-specific Files

A reference to a [default file](glossary.md) is specified in the [configuration file](config-file.md). The name of the default file should not include the name of the locale (a locale of the default file can be specified in the `defaultFileLocale` setting in the configuration file).

Locale-specific files should have names in the following format:

```
{base}{separator}{suffix}.{file extension}
```
where
* _base_ is the name of the default file without an extension,
* _separator_ is '.' (a dot) for .resx and '_' (an underscore character) for other formats (this can be customized via the `LocaleSeparatorChar` class property of most parser classes),
* _suffix_ is the name of the locale or the code of the language (see below),
* _file extension_ tells the translation manager, which parser to choose.

### Locale and Language Names

Tlumach works with full locale names (RFC 5646 locale identifiers), which have the form

```
{language code}-{region code}
```

* language code - ISO 639 language code: A two-letter (or sometimes three-letter) lowercase code representing the language (e.g., "en" for English, "fr" for French).
* region code - ISO 3166 region code: A two-letter uppercase code representing the country or region (e.g., "US" for the United States, "GB" for Great Britain).

However, it accepts just a language code in file names and, in the case of CSV and TSV files, in column names. When a file with just a language code in the name is loaded, the translation manager resolves this language code into a basic locale ("de-DE" for "de" and so on) and uses this basic locale in the case where a locale-specific translation is not available. E.g., if some "de-AT"-specific translation unit is not found, the one from "de" or "de-DE" will be picked.

Note that a language code and a region code are joined with '-' (hyphen). In Arb format, there is an optional placeholder parameter named 'locale', in which one can specify the name of the locale to use for formatting a value for a placeholder. That locale names use '_' (underscore) for a separator. When Tlumach processes such placeholders, it is aware of the difference and converts the separator.

## File Formats

Tlumach can parse language files in the following formats:

* **INI** - a simple key-value format, in which keys and values are separated with a "=" or, optionally, with a semicolon. Values come as is, without escaping or quote characters. The end of line is the end of the value. This is a format well known from Windows' ini files.
* **TOML** - a more advanced key-value format where values are enclosed in single quotes or double quotes. See the description of the format on the [TOML site](https://toml.io/en/v1.0.0#string). TOML is a good format for translations as it is easy to manage and quite powerful when it comes to translations.
* **JSON** - a simple key-value JSON-based format, where all translation units are stored as the properties of the root JSON object.
* **Arb** - a more advanced JSON-based format introduced by Google for Dart / Flutter. This format supports describing properties of translation files and individual units; also, it allows sophisticated [placeholders](placeholders.md) (although, with Tlumach, you can have sophisticated placeholders with almost any format).
* **ResX** - .NET native format. Tlumach handles it without compilation, as a text file. This makes it a bit tricky (albeit possible) to add such files into resources of a .NET application, so this format is more appropriate when translations come from the disk in runtime.
* **CSV** - Comma-separated files, where each file may include multiple translations. The <xref:Tlumach.Base.CsvParser> parser supports a semicolon or other character as a separator via the <xref:Tlumach.Base.CsvParser.SeparatorChar> property. For tabs as separators, see `TSV` format below. Hint: Excel uses a _semicolon_ as a separator for CSV file export.
* **TSV** - Tab-separated files, where each file may include multiple translations. Works similarly to CSV, but as Tab is not normally used in texts, individual values ("cells") don't have to be quoted.

***Important***: parsers must be initialized before they can be used. Read the corresponding section below.

## File Location

By default, <xref:Tlumach.TranslationManager> loads translation files only from assembly resources. The assembly to load the resources from is specified via <xref:Tlumach.Base.TranslationConfiguration.Assembly> property of the <xref:Tlumach.Base.TranslationConfiguration> class, which is accessible via the <xref:Tlumach.TranslationManager.DefaultConfiguration> property. If [Generator](generator.md) creates source code based on a configuration file, it initializes the <xref:Tlumach.Base.TranslationConfiguration> to reference the assembly, to which the configuration file belongs.

It is possible to remove the reference to the assembly and load files from the disk or even via an event instead. To let the translation manager load files from the disk, set <xref:Tlumach.TranslationManager.LoadFromDisk> property to true. You can optionally specify a directory that will be used as a hint when searching for files via the <xref:Tlumach.TranslationManager.TranslationsDirectory> property.

To load a file from the custom location or to override default loading, you can use the <xref:Tlumach.TranslationManager.OnFileContentNeeded> event.

When loading files, the translation manager will first fire the event, then try the disk (if enabled), and finally check assembly resources.

## References to External Files

It is possible to have some value come not from the translation file, but from an external file. If <xref:Tlumach.Base.BaseParser.RecognizeFileRefs> property is set to `true`, the parsers will recognize references to external files contained in the values. A reference is a text string that starts with the `@` (at) character:

Example:
```ini
Hello=@hello.txt
```

Here, the text will be loaded from an external file named 'hello.txt'. The text is loaded from this file on-demand, when the translation unit is used the first time during an application session. Text loading follows the rules described above for translation files.

## Format-specific notes

### INI

Files in this format support comments. <xref:Tlumach.Base.IniParser> uses ';' (semicolon) as a comment character, but you can change this via the <xref:Tlumach.Base.IniParser.LineCommentChar> property.

### TOML

Files in TOML format support comments; '#' is used for a line comment character.

Tlumach loads only string values and will report an error if the value is not a valid TOML string.

### JSON

JSON parser does not support comments because JSON format does not include them, and .NET built-in parser doesn't support comments (Newtonsoft.Json package does, but we don't use it in Tlumach).

Tlumach loads only string properties.

### Arb

Arb format does not support comments (same as JSON above).

Tlumach loads only string properties.

### CSV and TSV

Files in these formats may contain multiple languages. I.e., a [default file](glossary.md) may also contain some or all translations that your application needs.

The first column of a CSV or TSV file must be the column with keys (in both default and locale-specific files). The first line must contain column headers, i.e., the names/captions of columns (the name of the first column is ignored). The columns after the first one may contain locale-specific translation units in any order. The locale of a column is recognized by its header; additionally, <xref:Tlumach.Base.BaseTableParser> has <xref:Tlumach.Base.BaseTableParser.OnCultureNameMatchCheck> event that you can use to translate a column caption into a locate name (useful when translating say "German" to "de-DE").

In addition to translation units, a Description column is supported. It is recognized by matching the column caption with the <xref:Tlumach.Base.BaseTableParser.DescriptionColumnCaption> property, which is set to "Description" by default. A description is stored in the <xref:Tlumach.Base.TranslationEntry.Description> property (you can get a <xref:Tlumach.Base.TranslationEntry> instance from <xref:Tlumach.TranslationManager> by calling its GetEntry() method).

### ResX

ResX files are a bit hard to include to resources in their text form. As soon as .NET toolchain detects a dot in the name of the file which is marked as Embedded Resource, it considers the file to be a localized resource, and includes it to a satellite assembly instead of the main one. I.e., "strings.pl.resx", even when you specify that it is not a compilable resource, will be included into "YourAssembly.pl.dll" and not "YourAssembly.dll"; additionally, the extension will be removed. The only solution is to include it with a duplicate extension, for example as follows:

```xml
<EmbeddedResource Update="sample.pl.resx.resx">
    <Type>Non-Resx</Type>
</EmbeddedResource>
```

will give you the "sample.pl.resx" file in the resources of your main assembly.

**Note** that the attribute name is "Update" and not "include". This is because .NET will see the ".resx" extension and will add the file to the list of files to be processed automatically. Thus, the "Update" name is needed to update the existing entry; otherwise, you'll get a compilation error.

## Parser Initialization

If you use [Generator](generator.md), you don't need to do anything as the generator puts initialization to the generated code _for formats that it detects from the configuration file and referenced files_. If you stick to one or two formats for your configuration file and translation files, you are fine. If you expect files in other formats to be present on the disk, from where your application and the translation manager will pick them, read below.

If you initialize and use <xref:Tlumach.TranslationManager> directly, you also need instructions below.

Parsers are not initialized by default. To initialize the required parser classes, call the Use() static method of the corresponding parser class, namely:

```c#
ArbParser.Use();
CsvParser.Use();
IniParser.Use();
JsonParser.Use();
TomlParser.Use();
TsvParser.Use();
ResxParser.Use();
```

These methods don't do anything per se, but they make the compiler invoke static constructors of the corresponding classes, and those constructors do the heavy lifting.
