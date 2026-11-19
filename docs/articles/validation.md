# Validation of Translations

## Overview

The generator reads only the configuration and the default translation file. A mistake in another translation file therefore shows up only at run time: as a parse error when the file is loaded, as an exception when a text with placeholders is formatted, or as a silent fallback to the texts of the default file.

**Validation** finds such mistakes in advance. It finds the Tlumach configurations of a project and every translation file that they lead to, and checks that the library can load and process each of them, with the same parsers and the same text processing mode as at run time. Every text with placeholders is formatted once, so that errors in templates, such as an ICU `plural` without the `other` branch, are reported with the file and the position of the key.

The validation is available in two ways:

* The **Validate Translations** command of the Tlumach extensions for Visual Studio and VS Code, which reports the problems in the Error List or in the Problems panel.
* The <xref:Tlumach.Validation.TranslationValidator> class in the `Tlumach.Validation` namespace of the `Tlumach` assembly (the `AlliedBits.Tlumach` package), for tools and tests of your own.

## Validation During the Build

The generator validates the translation files of every configuration that it processes, so the problems also show up when the project is built, among the other warnings of the compiler. The problems that break the generated class (a configuration or a default file that cannot be read) remain the errors of the generator (TLUMACHGEN001); everything else that the validation finds is reported as a **warning**, with the codes of the table below, and the keys that the default file or a translation lacks (TLUMACHVAL007 and TLUMACHVAL012) as informational messages.

Like any other diagnostic, a code can be turned off in the project file or in `.editorconfig`:

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);TLUMACHVAL010</NoWarn>
</PropertyGroup>
```

```ini
[*.cs]
dotnet_diagnostic.TLUMACHVAL012.severity = none
```

The generator receives only the configuration from the compiler (as `AdditionalFiles`) and reads the translation files from the disk. An IDE therefore updates these diagnostics when the configuration changes or the project is built, and not while a translation file is being edited. The generator does not see the other files of the project, so it does not check how ResX files are embedded (TLUMACHVAL013) and does not look for translation files linked from other folders; the **Validate Translations** command does both.

## Validate Translations in the IDE

In Visual Studio, right-click a project in Solution Explorer and choose **Validate Translations**. **Validate Translations (All Projects)** and **Validate Translations (Selected Projects)** are in the same menus as the corresponding commands of the generator, and all three are in the **Extensions** → **Tlumach** menu. The problems appear in the **Error List**, and the **Tlumach Generator** pane of the **Output** window lists every file that was checked.

In VS Code, right-click a `.csproj` file or a Tlumach configuration file in the Explorer and choose **Validate Translations**, or right-click a `.sln` or `.slnx` file and choose **Validate Translations (All Projects)**. From the Command Palette, **Tlumach: Validate Translations** checks the project of the active editor. The problems appear in the **Problems** panel. The validation runs in a small .NET program that comes with the extension, so it needs .NET 8 or newer; it lists the files of a project with `dotnet msbuild`, so that the files added by wildcards, conditions, and imported files are found as in a build.

## Validation in Code

```csharp
using Tlumach.Validation;

// The files of the project with their MSBuild item types (an IDE or "dotnet msbuild -getItem" provides them)
var files = new List<ProjectFile>
{
    new ProjectFile(@"C:\App\Strings.jsoncfg", ProjectFile.AdditionalFilesItemType),
    new ProjectFile(@"C:\App\Strings.json", "EmbeddedResource"),
};

ProjectValidationReport report = TranslationValidator.ValidateProject(@"C:\App\App.csproj", files);

foreach (ValidationMessage message in report.Messages)
    Console.WriteLine(message); // C:\App\Strings_de.json(3,7): error TLUMACHVAL005: ...

return report.ErrorCount == 0 ? 0 : 1;
```

<xref:Tlumach.Validation.TranslationValidator.ValidateConfiguration*> validates one configuration without a project. <xref:Tlumach.Validation.ValidationReportWriter> writes a report as text.

## Which Files Are Checked

Every file of the project that has the extension of a configuration file (`.cfg`, `.jsoncfg`, `.arbcfg`, `.tomlcfg`, `.resxcfg`, `.xlfcfg`, `.xmlcfg`) is a candidate. A configuration included as `AdditionalFiles` is processed by the generator, and the generator's requirements are checked for it too. Another file is used only when it is a Tlumach configuration with a default file; otherwise it is ignored, because `.cfg` is a common extension.

For each configuration, the following translation files are checked:

* the default file, loaded both the way the generator loads it and the way the translation manager loads it;
* the files named in the `translations` section of the configuration;
* the files that the translation manager finds by name next to the default file: `{base}{separator}{culture}{extension}` in any supported format (for example, `Strings_de.json` or `Strings.de.resx` for `Strings.json`), including the files that the project links into that folder, and including ResX files with a duplicate extension (`Strings.de.resx.resx`, see [Translation Files and Formats](files-formats.md));
* every locale stored in a CSV, TSV, Apple String Catalog, or XLIFF file.

## Messages

| Code | Severity | Meaning |
|---|---|---|
| TLUMACHVAL001 | Error | The configuration cannot be read or parsed. |
| TLUMACHVAL002 | Error | The default translation file is missing, empty, or cannot be parsed. |
| TLUMACHVAL003 | Error | A configuration processed by the generator does not name the namespace or the class to generate. |
| TLUMACHVAL004 | Warning | A file named in the `translations` section does not exist; the translation manager shows the texts of the default file instead. |
| TLUMACHVAL005 | Error | A translation file cannot be parsed. |
| TLUMACHVAL006 | Error or Warning | The text of an entry cannot be formatted. It is a warning when formatting fails only with values of some types (for example, a .NET format specifier that strings ignore). |
| TLUMACHVAL007 | Info | A translation contains keys that the default file lacks; the generated class has no members for them. |
| TLUMACHVAL008 | Warning | A translation file is empty or yields no translation for its locale. |
| TLUMACHVAL009 | Warning | A locale in the `translations` section is not a known culture. |
| TLUMACHVAL010 | Warning | The translation manager never loads a file, because another file takes precedence for its locale. |
| TLUMACHVAL011 | Error | No parser reads the extension of a file named in the configuration. |
| TLUMACHVAL012 | Info | A translation lacks keys of the default file; the texts of the basic culture or of the default file are shown for them. |
| TLUMACHVAL013 | Warning | An embedded ResX file cannot be read by the translation manager (see below). |

## ResX Files Embedded as Resources

The translation manager reads a ResX file from the resources of an assembly as text, under a name that ends with `.resx`. MSBuild embeds a `.resx` file that way only under these conditions, which TLUMACHVAL013 checks:

* The `Type` metadata of the `EmbeddedResource` item is `Non-Resx`; otherwise the file is compiled into binary resources.
* The name of the file does not end with a culture, such as `Strings.de.resx`; such a file is moved into the satellite assembly of the culture, unless the item sets `WithCulture` to `false`. Name it `Strings.de.resx.resx` instead, or set `WithCulture` to `false` and `LogicalName`.
* The name of the resource keeps the `.resx` extension: the file has a duplicate extension (`Strings.de.resx.resx` gives the resource `Strings.de.resx`) or the item sets `LogicalName`. The generator reads the default file by the name in the configuration, so the default file keeps its name and uses `LogicalName`.

```xml
<ItemGroup>
    <EmbeddedResource Update="Strings.resx">
        <Type>Non-Resx</Type>
        <LogicalName>MyApp.Strings.resx</LogicalName>
    </EmbeddedResource>
    <EmbeddedResource Update="Strings.de.resx.resx">
        <Type>Non-Resx</Type>
    </EmbeddedResource>
</ItemGroup>
```
