# Tlumach.NET Extension for Visual Studio

Generate type-safe localization code and navigate seamlessly between generated identifiers and their source translation definitions.

## Features

### Run Generator
Execute the [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) code generator on-demand to create C# source files from translation files in your projects. The generator produces strongly-typed translation classes, eliminating magic strings and enabling IntelliSense support for all your translations.

- **On-demand generation** — Run the generator whenever you update your translation files
- **Project-aware** — Automatically detects projects with Tlumach configuration files
- **Batch support** — Run the generator across your entire solution with a single command
- **Output tracking** — View generation progress and messages in the Output window

### Go To Definition
Navigate instantly from any generated translation identifier back to its original definition in the translation file. Click on a translation key in your code and jump directly to the source text that generated it.

- **Smart navigation** — Works with generated translation properties and classes
- **Format support** — Compatible with all Tlumach-supported formats (JSON, ARB, INI, TOML, CSV, TSV, ResX, Xliff)
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

### Navigate to Definitions
- **Click on any generated translation identifier** in your editor
- **Right-click and select Go To Definition** or press **F12**
- You'll be taken directly to the translation entry in the original translation file 

## Requirements

- **Visual Studio 2026** or **Visual Studio 2022** (version 17.0 or later)
- **AlliedBits.Tlumach NuGet package** installed in your project
- Translation files in one or several projects of yours

## Documentation

For comprehensive guides, examples, and API documentation, visit [Tlumach.NET web site](https://github.com/AlliedBits/tlumach-net), download the [NuGet Package](https://www.nuget.org/packages/AlliedBits.Tlumach), or browse the [source code repository](https://github.com/Allied-Bits-Ltd/tlumach-net).

## Support

Found an issue or have a feature request? Open an issue on the [GitHub repository](https://github.com/AlliedBits/tlumach-net/issues).

## About Tlumach.NET

**Tlumach.NET** is a flexible library that provides translation and localization support to all kinds of .NET applications: from desktop WinForms, UWP, WPF, WinUI, and console to mobile MAUI and Avalonia to server Razor and Blazor. It supports a number of file formats used in translation and localization of applications.
