# Tlumach.NET Extension for Visual Studio

Generate type-safe localization code and navigate seamlessly between generated identifiers and their source translation definitions.

## Features

### Run Generator
Execute the [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) code generator on-demand to create C# source files from translation files in your projects. The generator produces strongly-typed translation classes, eliminating magic strings and enabling IntelliSense support for all your translations.

- **On-demand generation** — Run the generator whenever you update your translation files
- **Project-aware** — Automatically detects projects with Tlumach configuration files
- **Batch support** — Run the generator across your entire solution with a single command
- **Output tracking** — View generation progress and messages in the Output window

### Validate Translations
Check every translation file of a project before the application runs into it. The generator reads only the configuration and the default translation file, so a broken translation otherwise shows up only at run time - as a parse error or as a silent fallback to the default language.

- **Finds all translation files** — The default file, the files named in the `translations` section of the configuration, the files that the translation manager finds by name next to the default file (`Strings_de.json`, `Strings.de.resx`, in any supported format, also when linked into that folder), and every locale of a CSV, TSV, Apple String Catalog, or XLIFF file
- **Checks them as the application loads them** — The same parsers and text processing mode as at run time, and every text with placeholders is formatted once, so errors such as an ICU `plural` without `other` are found now and not when the text is first shown
- **Reports in the Error List** — Errors and warnings with file, line, and column; double-click to go to the problem
- **Points out mistakes that load fine** — ResX files embedded in a way that the translation manager cannot read, keys that the default file or a translation lacks, files that are never loaded because another file takes precedence for their locale, files named in the configuration that do not exist, empty files, and unknown locale names

### Add New Item Templates
Create Tlumach translation and configuration files straight from **Project** → **Add** → **New Item**, under the **Tlumach** category of the C# item templates.

- **Translation files** — JSON, ARB, INI, TOML, CSV, TSV, XLIFF, Apple String Catalog, and ResX. The new file is added to the project as an **Embedded Resource**, which is where the translation manager loads translations from.
- **Configuration files** — INI (`.cfg`), JSON (`.jsoncfg`), TOML (`.tomlcfg`), ARB (`.arbcfg`), ResX (`.resxcfg`), and XLIFF (`.xlfcfg`). The new file is added as a **C# analyzer additional file** (an `AdditionalFiles` item), which is what makes the Tlumach source generator pick it up.
- **Ready to edit** — Each file comes with a few sample translation units, and a configuration file is pre-filled with the namespace of the project and a `generatedClass` of `Strings`.
- **ResX handled correctly** — A ResX translation file is created with a duplicate `.resx` extension and the `Non-Resx` type, so that the .NET toolchain embeds it verbatim instead of compiling it into a satellite assembly.

### Go To Definition
Navigate instantly from any generated translation identifier back to its original definition in the translation file. Click on a translation key in your code and jump directly to the source text that generated it.

- **Smart navigation** — Works with generated translation properties and classes
- **Format support** — Compatible with all Tlumach-supported formats (JSON, ARB, INI, TOML, CSV, TSV, ResX, Xliff, Apple String Catalog)
- **Quick discovery** — Explore how translations are structured and maintained

## Installation

1. Open Visual Studio 2026 or 2022 (Community, Professional, or Enterprise edition)
2. Go to **Extensions** → **Manage Extensions**
3. Search for "Tlumach.NET Generator"
4. Click **Download** and restart Visual Studio

## Getting Started

### Setup Your Project
1. Install the [AlliedBits.Tlumach NuGet package](https://www.nuget.org/packages/AlliedBits.Tlumach)
2. Add a Tlumach configuration file and translation files to your project as described in [the documentation](https://alliedbits.com/tlumach/articles/index.html#getting-started)

### Generate Code
- **Right-click on a project** and select **Run Tlumach Generator**
- **Right-click on your solution** and select **Run Tlumach Generator (All projects)** to process multiple projects
- Access these commands via the **Extensions** → **Tlumach** menu

### Validate Translations
- **Right-click on a project** and select **Validate Translations**
- **Right-click on your solution** and select **Validate Translations (All Projects)**, or select several projects or solution folders and choose **Validate Translations (Selected Projects)**
- Access these commands via the **Extensions** → **Tlumach** menu
- The problems appear in the **Error List**; the **Tlumach Generator** pane of the **Output** window lists every file that was checked

### Navigate to Definitions
- **Click on any generated translation identifier** in your editor
- **Right-click and select Go To Definition** or press **F12**
- You'll be taken directly to the translation entry in the original translation file 

## Requirements

- **Visual Studio 2026** or **Visual Studio 2022** (version 17.14 or later)
- **AlliedBits.Tlumach NuGet package** installed in your project
- Translation files in one or several projects of yours

## Documentation

For comprehensive guides, examples, and API documentation, visit [Tlumach.NET web site](https://github.com/AlliedBits/tlumach-net), download the [NuGet Package](https://www.nuget.org/packages/AlliedBits.Tlumach), or browse the [source code repository](https://github.com/Allied-Bits-Ltd/tlumach-net).

## Support

Found an issue or have a feature request? Open an issue on the [GitHub repository](https://github.com/AlliedBits/tlumach-net/issues).

## About Tlumach.NET

**Tlumach.NET** is a flexible library that provides translation and localization support to all kinds of .NET applications: from desktop WinForms, UWP, WPF, WinUI, and console to mobile MAUI and Avalonia to server Razor and Blazor. It supports a number of file formats used in translation and localization of applications.
