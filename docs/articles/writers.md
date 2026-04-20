# Writers

## Overview

While [parsers](files-formats.md) read translation files in various formats and load them into memory, **writers** do the opposite — they serialize translations from the <xref:Tlumach.TranslationManager> back to files in different formats. Writers enable you to export translations, convert between formats, implement translation workflows, and support round-trip editing (read → modify → write).

Tlumach provides writers for all [supported file formats](files-formats.md): JSON, ARB, INI, TOML, CSV, TSV, ResX, and XLIFF. Like parsers, writers follow a hierarchical architecture with format-specific base classes and concrete implementations.

## Writer Architecture

All writers inherit from a common base class hierarchy that organizes implementations by file format family:

```
BaseWriter (abstract)
├── BaseJsonWriter (abstract)
│   ├── JsonWriter
│   └── ArbWriter
├── BaseKeyValueWriter (abstract)
│   ├── IniWriter
│   └── TomlWriter
├── BaseTableWriter (abstract)
│   ├── CsvWriter
│   └── TsvWriter
└── BaseXmlWriter (abstract)
    ├── ResxWriter
    └── XliffWriter
```

### BaseWriter

The root <xref:Tlumach.Writers.BaseWriter> class defines the writer contract:

- **FormatName** — Display name of the format (e.g., "JSON", "INI")
- **ConfigExtension** — File extension for configuration files (e.g., ".jsoncfg")
- **TranslationExtension** — File extension for translation files (e.g., ".json")
- **WriteConfiguration()** — Serializes the translation configuration
- **WriteTranslation(culture)** — Writes a single culture's translations to a file
- **WriteTranslations(cultures)** — Writes multiple cultures' translations in one file (table formats only)

### Format-Specific Base Classes

Format families share common behavior through specialized base classes:

- **BaseJsonWriter** — Handles JSON and ARB formats; supports indentation control via the `IndentationStep` property (default: 2 spaces)
- **BaseKeyValueWriter** — Handles INI and TOML formats; organizes translations into sections and key-value pairs
- **BaseTableWriter** — Handles CSV and TSV formats; writes translations as rows in a tabular structure
- **BaseXmlWriter** — Handles ResX and XLIFF formats; works with XML DOM structures

## Basic Usage

### Creating and Using a Writer

To write translations, instantiate the appropriate writer, then call one of its methods:

```csharp
using Tlumach;
using Tlumach.Writers;
using System.Globalization;

// Assume you have a TranslationManager instance
var translationManager = new TranslationManager();
// ... load translations ...

// Write a single culture to JSON
var jsonWriter = new JsonWriter();
using (var fileStream = File.Create("translations.en.json"))
{
    jsonWriter.WriteTranslation(translationManager, new CultureInfo("en"), fileStream);
}

// Write the configuration file
using (var fileStream = File.Create("translations.jsoncfg"))
{
    jsonWriter.WriteConfiguration(translationManager, fileStream);
}
```

### Single-Culture vs. Multi-Culture Formats

Most formats (JSON, ARB, INI, TOML, ResX, XLIFF) store **one culture per file**. For these formats, call <xref:Tlumach.Writers.BaseWriter.WriteTranslation(Tlumach.TranslationManager,System.Globalization.CultureInfo,System.IO.Stream)> once per culture:

```csharp
var cultures = new[] { new CultureInfo("en"), new CultureInfo("de") };
foreach (var culture in cultures)
{
    var filename = culture.Name == "en" 
        ? "strings.json" 
        : $"strings_{culture.Name}.json";
    using (var stream = File.Create(filename))
    {
        jsonWriter.WriteTranslation(translationManager, culture, stream);
    }
}
```

Table formats (CSV, TSV) support **multiple cultures in one file**. Use <xref:Tlumach.Writers.BaseWriter.WriteTranslations(Tlumach.TranslationManager,System.Collections.Generic.IReadOnlyCollection{System.Globalization.CultureInfo},System.IO.Stream)>:

```csharp
var csvWriter = new CsvWriter();
var cultures = new[] { new CultureInfo("en"), new CultureInfo("de"), new CultureInfo("fr") };
using (var stream = File.Create("all_translations.csv"))
{
    csvWriter.WriteTranslations(translationManager, cultures, stream);
}
```

### Writing the Invariant Culture

To write the default translation (the one without a specific locale), pass <xref:System.Globalization.CultureInfo.InvariantCulture>:

```csharp
using (var stream = File.Create("default.json"))
{
    jsonWriter.WriteTranslation(translationManager, CultureInfo.InvariantCulture, stream);
}
```

## Format-Specific Notes

### JSON

<xref:Tlumach.Writers.JsonWriter> serializes translations as a hierarchical JSON object. Dot-separated keys create nested structures:

```json
{
  "App": {
    "Title": "My Application",
    "Settings": {
      "Language": "Language"
    }
  },
  "Menu": {
    "File": "File",
    "Edit": "Edit"
  }
}
```

Control indentation with the <xref:Tlumach.Writers.BaseJsonWriter.IndentationStep> property:

```csharp
var writer = new JsonWriter { IndentationStep = 4 };
```

The `IndentationStep` property also applies to configuration files written by `WriteConfiguration()`.

### ARB

<xref:Tlumach.Writers.ArbWriter> writes ARB (Application Resource Bundle) format, which extends JSON with metadata support. ARB is commonly used in translation workflows, especially with tools like Google Translator Toolkit.

```json
{
  "appTitle": "My Application",
  "@@locale": "en",
  "@appTitle": {
    "context": "application title"
  }
}
```

Like `JsonWriter`, the `IndentationStep` property controls JSON indentation.

### INI

<xref:Tlumach.Writers.IniWriter> writes Windows INI format. Dot-separated keys become section headers:

```ini
[App.Settings]
Language=Language
Theme=Theme

[Menu]
File=File
Edit=Edit
```

### TOML

<xref:Tlumach.Writers.TomlWriter> writes TOML format, which is human-friendly and supports advanced string features:

```toml
[App]
Title = "My Application"

[App.Settings]
Language = "Language"
Theme = "Theme"

[Menu]
File = "File"
Edit = "Edit"
```

The writer automatically handles string quoting and escaping according to TOML syntax rules.

### CSV

<xref:Tlumach.Writers.CsvWriter> writes comma-separated values with proper escaping. Each row represents one translation unit, and columns hold the key and one or more culture translations.

Control the separator character via the <xref:Tlumach.Writers.CsvWriter.SeparatorChar> property (default: comma):

```csharp
var csvWriter = new CsvWriter { SeparatorChar = ';' };
```

Note: Excel typically exports CSV with a semicolon as separator.

### TSV

<xref:Tlumach.Writers.TsvWriter> writes tab-separated values, similar to `CsvWriter` but using tabs as separators. TSV is often preferred over CSV for translation workflows because tabs are rarely used within text content and minimal escaping is needed.

### ResX

<xref:Tlumach.Writers.ResxWriter> writes .NET ResX format (XML). This format is native to the .NET ecosystem and is useful when integrating translations with .NET resource systems:

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="AppTitle" xml:space="preserve">
    <value>My Application</value>
  </data>
  <data name="Menu.File" xml:space="preserve">
    <value>File</value>
  </data>
</root>
```

Each culture's translations are written to a separate file with the naming pattern `filename.culture.resx` (e.g., `strings.en.resx`, `strings.de.resx`).

### XLIFF

<xref:Tlumach.Writers.XliffWriter> writes XLIFF 2.2 (XML Localization Interchange File Format), a standardized bitext format used in professional translation workflows. XLIFF files contain both source and target language text side-by-side, making them ideal for translation services and translation memory systems.

```xml
<xliff version="2.0" srcLang="en" trgLang="de">
  <file id="strings">
    <unit id="AppTitle">
      <segment>
        <source>My Application</source>
        <target>Meine Anwendung</target>
      </segment>
    </unit>
  </file>
</xliff>
```

The `SourceFile` and `TargetFile` properties of the writer control additional metadata in the XLIFF output. For more detailed information, see the [XLIFF Guide](XLIFF.md).

## Common Scenarios

### Format Conversion

Convert translations from one format to another:

```csharp
// Load from INI
IniParser.Use();
var translationManager = new TranslationManager();
translationManager.LoadFromDisk = true;
translationManager.LoadDefaultTranslation("translations.ini");

// Write to JSON
var jsonWriter = new JsonWriter();
var cultureInfo = new CultureInfo("en");
using (var stream = File.Create("translations.json"))
{
    jsonWriter.WriteTranslation(translationManager, cultureInfo, stream);
}
```

### Batch Export

Export translations for multiple cultures and formats:

```csharp
var cultures = new[] { new CultureInfo("en"), new CultureInfo("de"), new CultureInfo("fr") };
var exportDirectory = "exports";
Directory.CreateDirectory(exportDirectory);

var jsonWriter = new JsonWriter();
var csvWriter = new CsvWriter();

// Export to JSON (one file per culture)
foreach (var culture in cultures)
{
    var filename = Path.Combine(exportDirectory, $"strings_{culture.Name}.json");
    using (var stream = File.Create(filename))
    {
        jsonWriter.WriteTranslation(translationManager, culture, stream);
    }
}

// Also export all at once to CSV
using (var stream = File.Create(Path.Combine(exportDirectory, "all_strings.csv")))
{
    csvWriter.WriteTranslations(translationManager, cultures, stream);
}
```

### Round-Trip Editing

Read translations, modify them, and write them back:

```csharp
// Load
JsonParser.Use();
var translationManager = new TranslationManager();
translationManager.LoadFromDisk = true;
translationManager.LoadDefaultTranslation("strings.json");

// Modify (e.g., via UI or programmatically)
var translation = translationManager.GetTranslation(new CultureInfo("en"));
if (translation.TryGetValue("AppTitle", out var entry))
{
    // Modify the translation text
    // ...
}

// Write back
var jsonWriter = new JsonWriter();
using (var stream = File.Create("strings_updated.json"))
{
    jsonWriter.WriteTranslation(translationManager, new CultureInfo("en"), stream);
}
```

### Exporting for Translation Services

Prepare translations for external translation services (e.g., converting to XLIFF for professional translation):

```csharp
// Load current translations
JsonParser.Use();
var translationManager = new TranslationManager();
translationManager.LoadFromDisk = true;
translationManager.LoadDefaultTranslation("strings.json");

// Export to XLIFF for translation
var xliffWriter = new XliffWriter();
var sourceLanguage = new CultureInfo("en");
var targetLanguage = new CultureInfo("de");

using (var stream = File.Create("strings_de.xliff"))
{
    xliffWriter.WriteTranslation(translationManager, targetLanguage, stream);
}
```

### Configuration File Export

Export the translation configuration:

```csharp
var jsonWriter = new JsonWriter();
using (var stream = File.Create("translations.jsoncfg"))
{
    jsonWriter.WriteConfiguration(translationManager, stream);
}
```

The configuration file includes settings like the default file, default locale, and generated class namespace.

## Advanced Topics

### Implementing a Custom Writer

To create a writer for a custom or unsupported format, extend the appropriate base class:

```csharp
using Tlumach.Base;
using Tlumach.Writers;

public class CustomYamlWriter : BaseWriter
{
    public override string FormatName => "YAML";
    public override string ConfigExtension => ".yamlcfg";
    public override string TranslationExtension => ".yaml";

    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        // Implement configuration serialization
    }

    public override void WriteTranslations(TranslationManager translationManager, 
        IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        throw new NotSupportedException("YAML format does not support multiple cultures per file.");
    }

    public override void WriteTranslation(TranslationManager translationManager, 
        CultureInfo culture, Stream stream)
    {
        var translation = translationManager.GetTranslation(culture)
            ?? throw new TlumachException($"No translation found for culture {culture.Name}");

        // Implement translation serialization to YAML
    }

    protected override void InternalWriteTranslations(TranslationManager translationManager,
        IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        throw new NotSupportedException("YAML format does not support multiple cultures per file.");
    }
}
```

### Format-Specific Configuration

Many writers expose properties to customize output:

- **JsonWriter** and **ArbWriter** — `IndentationStep` (default: 2)
- **CsvWriter** — `SeparatorChar` (default: ',')
- **XliffWriter** — `SourceFile`, `TargetFile` (metadata properties)

Example:

```csharp
var csvWriter = new CsvWriter { SeparatorChar = ';' };
var jsonWriter = new JsonWriter { IndentationStep = 4 };
```

### Error Handling

Writers throw exceptions if the operation is not supported for the format. For example, calling `WriteTranslations()` on a `JsonWriter` (which supports only one culture per file) throws a <xref:Tlumach.Base.TlumachException>:

```csharp
var jsonWriter = new JsonWriter();
var cultures = new[] { new CultureInfo("en"), new CultureInfo("de") };

try
{
    using (var stream = File.Create("translations.json"))
    {
        // This throws an exception because JSON does not support multiple cultures per file
        jsonWriter.WriteTranslations(translationManager, cultures, stream);
    }
}
catch (TlumachException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Preserving Entry Order

By default, writers sort entries hierarchically by key. To preserve the original entry order from parsing, set the <xref:Tlumach.Base.BaseParser.KeepEntryOrder> property to `true` before parsing:

```csharp
BaseParser.KeepEntryOrder = true;
JsonParser.Use();
// ... load translations ...
```

## Related Topics

- [Translation Files and Formats](files-formats.md) — Overview of supported file formats and parsers
- [Strings and Translations](strings.md) — Working with translation units and the TranslationManager
- [Templates and Placeholders](placeholders.md) — Formatting and parameterizing translation text
- [Language Management](language-management.md) — Handling locales and language fallback
- [XLIFF Guide](XLIFF.md) — Detailed information about XLIFF format support
