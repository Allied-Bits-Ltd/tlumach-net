# XLIFF 2.2 Support in Tlumach

This guide covers XLIFF 2.2 (eXtensible Localization Interchange File Format) support in Tlumach, including parsing, writing, configuration, and best practices.

## What is XLIFF?

XLIFF 2.2 is a standardized XML-based format for localization and translation. Unlike single-language formats (JSON, RESX, etc.), XLIFF is a **bitext format** — it combines source and target translations in a single file, making it ideal for:

- Translation memory systems
- Computer-Aided Translation (CAT) tools
- Professional translation workflows
- Multi-locale content distribution

### Key Characteristics

- **Bitext format**: Both source and target translations in one file
- **Language pair metadata**: Source language (`srcLang`) and target language (`trgLang`) declared at the file level
- **File references**: Original source filename stored in `<file id="...">` elements
- **Unit-based structure**: Translation units (`<unit>` elements) contain source and target text, each wrapped in one or more `<segment>` elements
- **Multi-segment units**: A single `<unit>` can contain multiple `<segment>` children representing separate sentences or paragraphs
- **Extensible**: Supports metadata, notes, groups, inline placeholders, and translation phase tracking
- **Human-readable**: XML format is easy to inspect and edit

## File Structure

A typical XLIFF 2.2 file produced by Tlumach looks like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<xliff xmlns="urn:oasis:names:tc:xliff:document:2.2" version="2.2" srcLang="en" trgLang="fr">
  <file id="strings.json">
    <unit id="greeting">
      <segment>
        <source>Hello</source>
        <target>Bonjour</target>
      </segment>
    </unit>
    <unit id="thanks">
      <notes>
        <note>Polite greeting</note>
      </notes>
      <segment>
        <source>Thank you</source>
        <target>Merci</target>
      </segment>
    </unit>
    <unit id="description">
      <segment>
        <source>First sentence.</source>
        <target>Première phrase.</target>
      </segment>
      <segment>
        <source>Second sentence.</source>
        <target>Deuxième phrase.</target>
      </segment>
    </unit>
  </file>
</xliff>
```

### Element Breakdown

| Element | Attributes | Purpose |
|---------|-----------|---------|
| `<xliff>` | `version="2.2"`, `srcLang="en"`, `trgLang="fr"` | Root element; declares XLIFF version and language pair |
| `<file>` | `id="strings.json"` | File container; `id` references the original source filename |
| `<group>` | `id="..."`, `name="..."` | Optional grouping of related units; Tlumach discovers units at any nesting depth |
| `<unit>` | `id="greeting"` | Translation unit; `id` is the translation key |
| `<segment>` | (none) | Wraps a single segment's source and target text within a unit |
| `<source>` | (none) | Source language text (language specified by parent `<xliff srcLang>`) |
| `<target>` | (none) | Target language text (language specified by parent `<xliff trgLang>`) |
| `<notes>` | (none) | Container for one or more translator notes |
| `<note>` | `id`, `priority` (optional) | Individual translator note; mapped to `TranslationEntry.Comment` |
| `<ph>` | `equiv="..."` | Inline placeholder; replaced with the `equiv` attribute value when parsed |

## Configuration

XLIFF files in Tlumach are configured via `.xlfcfg` files, following the same pattern as other formats.

### Example Configuration File (`.xlfcfg`)

```ini
[DefaultConfiguration]

# The original source file reference
# This value is stored in the XLIFF <file id="..."> element
DefaultFile = strings.json

# The locale of the source language
# Must match the srcLang attribute in the XLIFF file
DefaultFileLocale = en

[Translations]

# Map target locales to XLIFF bitext files
# Each entry represents a source-to-target language pair

fr = strings_fr.xlf
de = strings_de.xlf
es = strings_es.xlf
```

### Configuration Notes

- **DefaultFile**: References the original source file. Used in the `<file id="...">` element when writing XLIFF.
- **DefaultFileLocale**: Must be `en` if your XLIFF's `srcLang="en"`. This tells Tlumach which language is the source.
- **[Translations] section**: Maps target locales to XLIFF files. Each line represents one bitext file containing a specific source-to-target language pair.

## Using XLIFF in Code

### Loading XLIFF Translations

```csharp
using Tlumach;

// Initialize XLIFF parser
XliffParser.Use();

// Load translations from XLIFF configuration
var manager = new TranslationManager("strings.xlfcfg");

// Access English (source)
manager.SetCulture(CultureInfo.GetCultureInfo("en"));
string greeting = manager.GetValue("GREETING");  // "Hello"

// Switch to French (target)
manager.SetCulture(CultureInfo.GetCultureInfo("fr"));
string greetingFr = manager.GetValue("GREETING");  // "Bonjour"
```

### Writing XLIFF Translations

```csharp
using Tlumach;
using Tlumach.Writers;

// Create a translation manager with both source and target translations
var manager = new TranslationManager("strings.xlfcfg");

// Get the XLIFF writer
var writer = new XliffWriter
{
    SourceFile = "strings.json",   // Optional; defaults to config DefaultFile
    TargetFile = "strings_fr.xlf"  // Optional; for reference/metadata
};

// Write bitext XLIFF file
using (var output = File.Create("output.xlf"))
{
    var frenchLocale = new CultureInfo("fr");
    writer.WriteTranslations(manager, new[] { frenchLocale }, output);
}
```

## Parsing Behavior

### Single-Language Extraction from Bitext

XLIFF files contain both source and target. When parsing, Tlumach extracts one language at a time:

```csharp
var parser = new XliffParser();
var xliffContent = File.ReadAllText("strings_en_fr.xlf");

// Extract English (source language)
var englishTranslation = parser.LoadTranslation(
    xliffContent, 
    new CultureInfo("en"), 
    null);
// Result: TranslationEntry["GREETING"].Text = "Hello"

// Extract French (target language)
var frenchTranslation = parser.LoadTranslation(
    xliffContent, 
    new CultureInfo("fr"), 
    null);
// Result: TranslationEntry["GREETING"].Text = "Bonjour"
//         TranslationEntry["GREETING"].SourceText = "Hello" (paired language)
```

### Key Points

- **Multiple invocations**: To load all languages from a bitext file, invoke the parser once per language.
- **Paired language**: When loading the target language, the source is automatically stored in `TranslationEntry.SourceText` for reference.
- **Unsupported languages**: If the XLIFF file doesn't contain the requested language, `LoadTranslation()` returns `null`.

### Segment Merging

XLIFF units can contain multiple `<segment>` children (e.g., one per sentence). The parser merges all segments within a unit into a single `TranslationEntry.Text` value, joining them with the `SegmentSeparator` string.

The default separator is a **tab character** (`"\t"`), which is unlikely to appear in natural-language text and round-trips cleanly through the writer.

```csharp
// Default: segments joined with tab
var parser = new XliffParser();
// parser.SegmentSeparator == "\t"  (default)

var translation = parser.LoadTranslation(xliffContent, new CultureInfo("en"), null);

// Unit with two segments becomes a single tab-separated value
string text = translation["DESCRIPTION"].Text;
// "First sentence.\tSecond sentence."
```

To use a different separator, set the property before parsing:

```csharp
var parser = new XliffParser { SegmentSeparator = " " };

string text = translation["DESCRIPTION"].Text;
// "First sentence. Second sentence."
```

The parser also handles:
- **`<ignorable>` elements**: treated as segments and included in the merged value
- **`<ph>` inline placeholders**: replaced with their `equiv` attribute value
- **`<group>` nesting**: units are discovered at any depth inside `<file>` elements

## Metadata Support

### Supported XLIFF Metadata

Tlumach maps XLIFF metadata to TranslationEntry properties:

| XLIFF Element | TranslationEntry Property |
|---------------|-------------------------|
| `<note>` | `Comment` |
| `<source>` (paired language) | `SourceText` |
| Translation phase | `Phase` (optional) |
| Change tracking | `ChangeDate`, `ChangeReason` (optional) |

### Example with Metadata

```csharp
var entry = translation["GREETING"];
entry.Comment;      // From XLIFF <note>: "Polite greeting"
entry.SourceText;   // From paired <source> when loading target
entry.Phase;        // "review" if set
entry.ChangeDate;   // Last modification timestamp
```

## Writing XLIFF Files

### XliffWriter Properties

```csharp
var writer = new XliffWriter
{
    // Optional: source filename for <file id="..."> element
    // Defaults to manager's DefaultFile if not set
    SourceFile = "strings.json",
    
    // Optional: target filename (metadata only, doesn't affect output location)
    TargetFile = "strings_fr.xlf",

    // Separator used to detect multi-segment values (default: tab character)
    // Must match XliffParser.SegmentSeparator to round-trip correctly
    SegmentSeparator = "\t"
};
```

### Segment Splitting

When writing, the `XliffWriter` checks each translation value for the `SegmentSeparator` string. If found, the value is split on the separator and each part is written as a separate `<segment>` element within the unit. Values without the separator produce a single segment.

```csharp
// Translation value contains a tab — written as two segments
var entry = new TranslationEntry("description",
    text: "First sentence.\tSecond sentence.");

// Produces:
// <unit id="DESCRIPTION">
//   <segment><source>First sentence.</source></segment>
//   <segment><source>Second sentence.</source></segment>
// </unit>
```

For bitext output, source and target are split independently and paired by position. If the counts differ, remaining parts are written with an empty counterpart.

> **Important**: `SegmentSeparator` on `XliffParser` and `XliffWriter` must be set to the same value for a lossless round-trip. Both default to `"\t"`.

### Writing Constraints

- **Single target culture**: XliffWriter accepts exactly one target locale per invocation
- **Source required**: Both source (default) and target translations must be available in the TranslationManager
- **Order preservation**: If `BaseParser.KeepEntryOrder = true`, entry order is preserved in output

### Round-Trip Preservation

XLIFF supports round-trip conversion (load → modify → write → load) with metadata preservation:

```csharp
// Load XLIFF
var parser = new XliffParser();
var source = parser.LoadTranslation(xliffContent, new CultureInfo("en"), null);
var target = parser.LoadTranslation(xliffContent, new CultureInfo("fr"), null);

// Add to manager
var manager = new TranslationManager(...);
manager.AddTranslation(source);
manager.AddTranslation(target);

// Write back
var writer = new XliffWriter();
using (var output = File.Create("output.xlf"))
{
    writer.WriteTranslations(manager, new[] { new CultureInfo("fr") }, output);
}

// Verify: parse output and check metadata is preserved
var reloaded = parser.LoadTranslation(File.ReadAllText("output.xlf"), new CultureInfo("fr"), null);
Assert.Equal("Bonjour", reloaded["GREETING"].Text);
Assert.Equal("Polite greeting", reloaded["GREETING"].Comment);
```

## Best Practices

### 1. File Naming

Use language codes in filenames for clarity:

```
strings_en.xlf        # Source only (English)
strings_en_fr.xlf     # English source → French target
strings_en_de.xlf     # English source → German target
```

### 2. Source File Reference

Always set `DefaultFile` to the original source file for traceability:

```ini
DefaultFile = strings.json  # Original source
```

This helps translation teams understand the file's origin.

### 3. Configuration Organization

Group bitext files by language pair:

```ini
[Translations]

# Romance languages
fr = strings_en_fr.xlf
es = strings_en_es.xlf
it = strings_en_it.xlf

# Germanic languages
de = strings_en_de.xlf
nl = strings_en_nl.xlf

# Other
ja = strings_en_ja.xlf
```

### 4. Comments and Notes

Add translator notes in XLIFF `<note>` elements:

```xml
<unit id="context_specific_term">
  <source>File</source>
  <target>Fichier</target>
  <note>In French, "Fichier" refers to both computer files and filing cabinets.
         Use "Dossier" only for document folders.</note>
</unit>
```

These are automatically preserved in `TranslationEntry.Comment`.

### 5. Metadata Usage

Leverage metadata properties for translation tracking:

```csharp
entry.Phase = "review";           // Translation phase
entry.ChangeDate = DateTime.Now;  // Last modification
entry.ChangeReason = "Client feedback incorporated"
```

## Cross-Format Conversion

### From JSON to XLIFF

```csharp
// Load JSON source and translations
XliffParser.Use();
JsonParser.Use();

var manager = new TranslationManager("config.jsoncfg");

// Write as XLIFF
var writer = new XliffWriter();
using (var output = File.Create("output.xlf"))
{
    writer.WriteTranslations(manager, new[] { new CultureInfo("fr") }, output);
}
```

### From XLIFF to Other Formats

```csharp
// Load XLIFF
var parser = new XliffParser();
var xliffContent = File.ReadAllText("strings.xlf");
var frenchTranslation = parser.LoadTranslation(xliffContent, new CultureInfo("fr"), null);

// Write as JSON
var jsonWriter = new JsonWriter();
using (var output = File.Create("strings_fr.json"))
{
    // ... (write using existing TranslationManager pattern)
}
```

## Troubleshooting

### "XLIFF document must have 'srcLang' attribute"

**Issue**: Your XLIFF file is missing the `srcLang` attribute on the root `<xliff>` element.

**Solution**: Add the attribute:

```xml
<xliff version="2.0" srcLang="en">  <!-- Add srcLang -->
```

### "WriteTranslations requires exactly one target culture"

**Issue**: You're passing multiple cultures to XliffWriter.

**Solution**: Write one XLIFF file per target language:

```csharp
var writer = new XliffWriter();

// Write French
using (var output = File.Create("strings_fr.xlf"))
{
    writer.WriteTranslations(manager, new[] { new CultureInfo("fr") }, output);
}

// Write German (separate call)
using (var output = File.Create("strings_de.xlf"))
{
    writer.WriteTranslations(manager, new[] { new CultureInfo("de") }, output);
}
```

### "LoadTranslation returned null"

**Issue**: The XLIFF file doesn't contain the requested language.

**Causes**:
- Language code mismatch (e.g., requesting "de" but XLIFF has `srcLang="en"` and `trgLang="fr"`)
- Unsupported culture code (e.g., "xyz" is not a valid BCP 47 code)

**Solution**: Verify the XLIFF `srcLang` and `trgLang` attributes match your requested culture:

```xml
<!-- Your XLIFF declares English and French -->
<xliff version="2.0" srcLang="en" trgLang="fr">

<!-- Request matching cultures -->
var en = parser.LoadTranslation(content, new CultureInfo("en"), null);  // ✓ Works
var fr = parser.LoadTranslation(content, new CultureInfo("fr"), null);  // ✓ Works
var de = parser.LoadTranslation(content, new CultureInfo("de"), null);  // ✗ Returns null
```

## API Reference

### XliffParser

```csharp
public class XliffParser : BaseXMLParser
{
    // Register XLIFF parser for .xlf and .xliff extensions
    public static void Use();

    // Get/set source filename context (for <file id="...">)
    public static string? SourceFilename { get; set; }

    // Get/set text processing mode for template recognition
    public static TextFormat TextProcessingMode { get; set; }

    // Separator used to join multiple <segment> values into one string (default: "\t")
    public string SegmentSeparator { get; set; }

    // Load a single language from XLIFF bitext
    public override Translation? LoadTranslation(
        string translationText,
        CultureInfo? culture,
        TextFormat? textProcessingMode);

    // Check if parser handles .xlf/.xliff extensions
    public override bool CanHandleExtension(string fileExtension);
}
```

### XliffWriter

```csharp
public class XliffWriter : BaseXmlWriter
{
    // Source filename for <file id="..."> element
    public string? SourceFile { get; set; }

    // Target filename (metadata only)
    public string? TargetFile { get; set; }

    // Separator that triggers segment splitting when writing (default: "\t")
    // Should match XliffParser.SegmentSeparator for round-trip fidelity
    public string SegmentSeparator { get; set; }

    // Write bitext XLIFF (source + target); accepts exactly one target culture
    public override void WriteTranslations(
        TranslationManager translationManager,
        IReadOnlyCollection<CultureInfo> targetLocales,
        Stream output);

    // Format properties
    public override string FormatName => "XLIFF";
    public override string ConfigExtension => ".xlfcfg";
    public override string TranslationExtension => ".xlf";
}
```

## Standards Compliance

Tlumach's XLIFF implementation adheres to:

- **XLIFF 2.2 specification** (OASIS standard)
- **RFC 5646** for BCP 47 language tags (srcLang, trgLang attributes)
- **XML 1.0** encoding (UTF-8)

## See Also

- [XLIFF 2.2 Official Specification](https://docs.oasis-open.org/xliff/xliff-core/v2.0/os/xliff-core-v2.0-os.html)
- [Configuration Guide](config-file.md)
