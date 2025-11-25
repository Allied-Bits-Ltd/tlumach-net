# Glossary

* [**Configuration file**](config-file.md) - the file that specifies the name of a default file, optionally specifies information for Generator and may include references to locale-specific files.
* [**Default file**](files-formats.md) - the file that contains keys and translation units which are loaded by default (when no specific locale is set).
<a name="GeneratedUnit"></a>
* **Generated translation units** - instances of <xref:Tlumach.TranslationUnit> or <xref:Tlumach.TemplatedTranslationUnit> class, declared in the source code created by _Generator_ when it processes a translation project.
* [**Generator**](generator.md) - the class (.NET analyzer) that processes a configuration file and a default file and generates C# source code with _generated translation units_.
<a name="LocaleSpecificFile"></a>
* **Locale-specific file** - a file with a set of translation units for one language (or several languages if a file is in CSV or TSV format); this file must contain a language or locale indicator included into its name.
* [**Placeholder**](placeholders.md) - a word or a number enclosed in curly braces that gets replaced with a parameter value in runtime.
* [**Templated translation unit**](strings.md#StringTypes) - a translation unit whose text contains [placeholders](placeholders.md). Such units are processed differently from regular texts.
<a name="TranslationFile"></a>
* [**Translation file**](files-formats.md) - a collection of translation units contained in one file. A translation file can be either a default file or a locale-specific file. A file in CSV and TSV format can be both at the same time as it can include multiple translations.
* [**Translation project**](generator.md#TranslationProject) - a project in your solution that includes a _translation set_ (or at least a configuration file or files).
<a name="TranslationUnit"></a>
* [**Translation unit**](strings.md) - a phrase or a sentence that gets translated with an associated _key_; this key is used to address the sentence as well as to name the _generated translation unit_.
* **Translation set** - a _configuration file_ and one or more _translation files_, where both the default file and locale-specific files contain the same set of translation units (locale-specific files may omit some translation units present in the default file).
