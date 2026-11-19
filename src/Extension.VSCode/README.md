# Tlumach.NET for VS Code

Generate type-safe localization code directly from VS Code by running the [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) code generator on-demand from the Explorer context menu.

## Features

### Run Generator

Execute the Tlumach.NET code generator to create C# source files from your translation files. The generator produces strongly-typed translation classes, eliminating magic strings and enabling IntelliSense support for all your translations.

- **On-demand generation** — Run the generator whenever you update your translation files
- **Project-aware** — Automatically detects projects with Tlumach configuration files
- **Batch support** — Run the generator across all projects in your workspace with a single command
- **Live output** — View generation progress and diagnostic messages in the Output panel

### Validate Translations

Check every translation file of a project before the application runs into it. The generator reads only the configuration and the default translation file, so a broken translation otherwise shows up only at run time - as a parse error or as a silent fallback to the default language.

- **Finds all translation files** — The default file, the files named in the `translations` section of the configuration, the files that the translation manager finds by name next to the default file (`Strings_de.json`, `Strings.de.resx`, in any supported format, also when linked into that folder), and every locale of a CSV, TSV, Apple String Catalog, or XLIFF file
- **Checks them as the application loads them** — The same parsers and text processing mode as at run time, and every text with placeholders is formatted once, so errors such as an ICU `plural` without `other` are found now and not when the text is first shown
- **Reports in the Problems panel** — Errors and warnings with file, line, and column; the **Tlumach Generator** Output channel lists every file that was checked
- **Points out mistakes that load fine** — ResX files embedded in a way that the translation manager cannot read, keys that the default file or a translation lacks, files that are never loaded because another file takes precedence for their locale, files named in the configuration that do not exist, empty files, and unknown locale names

### New Tlumach Files

VS Code has no "Add New Item" dialog, so the equivalent is offered as two commands, available from the Command Palette and from the Explorer context menu of a folder:

- **Tlumach: New Translation File...** — pick a format (JSON, ARB, INI, TOML, CSV, TSV, XLIFF, Apple String Catalog, or ResX) and a file name. The file is created with a few sample translation units and registered in the nearest `.csproj` as an `EmbeddedResource`.
- **Tlumach: New Configuration File...** — pick a format (INI `.cfg`, JSON `.jsoncfg`, TOML `.tomlcfg`, ARB `.arbcfg`, ResX `.resxcfg`, or XLIFF `.xlfcfg`) and a file name. The file is pre-filled with the project's root namespace and registered in the nearest `.csproj` as an `AdditionalFiles` item, which is what makes the Tlumach source generator pick it up.

A ResX translation file is created with a duplicate `.resx` extension and marked `Non-Resx`, so that the .NET toolchain embeds it verbatim instead of compiling it into a satellite assembly.

## Installation

### From VS Code Marketplace

1. Open VS Code
2. Go to the **Extensions** view (`Ctrl+Shift+X`)
3. Search for **Tlumach.NET**
4. Click **Install**

## Getting Started

### Set Up Your Project

1. Install the [AlliedBits.Tlumach NuGet package](https://www.nuget.org/packages/AlliedBits.Tlumach) in your project
2. Add a Tlumach configuration file and translation files to your project as described in [the documentation](https://alliedbits.com/tlumach/articles/index.html#getting-started)

### Generate Code

- **Right-click a `.csproj` file** in the Explorer and select **Run Tlumach Generator** to process that project
- **Right-click a `.sln` file** in the Explorer and select **Run Tlumach Generator (All Projects)** to process every project in the solution

Generation output and any diagnostics are streamed live to the **Tlumach Generator** Output channel.

### Validate Translations

- **Right-click a `.csproj` file or a Tlumach configuration file** in the Explorer and select **Validate Translations** to check that project
- **Right-click a `.sln` or `.slnx` file** in the Explorer and select **Validate Translations (All Projects)** to check every project in the workspace
- From the Command Palette, **Tlumach: Validate Translations** checks the project of the active editor (or asks which project to check)

The problems appear in the **Problems** panel and stay there until the project is validated again. The validation runs in a small .NET program that comes with the extension; it lists the files of a project with `dotnet msbuild`, so that the files added by wildcards, conditions, and imported files are found as in a build.

## Requirements

- **VS Code** 1.95.0 or later
- **.NET 8 or newer** installed and available on `PATH` (or set the **Tlumach: Dotnet Path** setting); the validation of translations needs it, and the .NET SDK lets it list the files of a project exactly as MSBuild does
- **AlliedBits.Tlumach NuGet package** installed in your project
- Translation files in one or more projects

## Documentation

For comprehensive guides, examples, and API documentation, visit the [Tlumach.NET website](https://alliedbits.com/tlumach), download the [NuGet package](https://www.nuget.org/packages/AlliedBits.Tlumach), or browse the [source code repository](https://github.com/Allied-Bits-Ltd/tlumach-net).

## Support

Found an issue or have a feature request? Please report it on [GitHub Issues](https://github.com/Allied-Bits-Ltd/tlumach-net/issues).

## About Tlumach.NET

**Tlumach.NET** is a flexible library that provides translation and localization for .NET applications. It supports multiple file formats (JSON, ARB, INI, TOML, CSV, TSV, ResX, XLIFF, Apple String Catalog), runtime language switching, XAML framework integrations, and Roslyn-based compile-time code generation for type-safe translation access.
