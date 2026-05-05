# Tlumach.NET for VS Code

Generate type-safe localization code directly from VS Code by running the [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) code generator on-demand from the Explorer context menu.

## Features

### Run Generator

Execute the Tlumach.NET code generator to create C# source files from your translation files. The generator produces strongly-typed translation classes, eliminating magic strings and enabling IntelliSense support for all your translations.

- **On-demand generation** — Run the generator whenever you update your translation files
- **Project-aware** — Automatically detects projects with Tlumach configuration files
- **Batch support** — Run the generator across all projects in your workspace with a single command
- **Live output** — View generation progress and diagnostic messages in the Output panel

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

Generation output and any diagnostics are streamed live to the **Tlumach** Output channel.

## Requirements

- **VS Code** 1.95.0 or later
- **.NET runtime** installed and available on `PATH`
- **AlliedBits.Tlumach NuGet package** installed in your project
- Translation files in one or more projects

## Documentation

For comprehensive guides, examples, and API documentation, visit the [Tlumach.NET website](https://alliedbits.com/tlumach), download the [NuGet package](https://www.nuget.org/packages/AlliedBits.Tlumach), or browse the [source code repository](https://github.com/Allied-Bits-Ltd/tlumach-net).

## Support

Found an issue or have a feature request? Please report it on [GitHub Issues](https://github.com/Allied-Bits-Ltd/tlumach-net/issues).

## About Tlumach.NET

**Tlumach.NET** is a flexible library that provides translation and localization for .NET applications. It supports multiple file formats (JSON, ARB, INI, TOML, CSV, TSV, ResX), runtime language switching, XAML framework integrations, and Roslyn-based compile-time code generation for type-safe translation access.
