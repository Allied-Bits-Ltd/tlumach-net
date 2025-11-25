# Intro

Tlumach.NET is a flexible library that provides translation and localization support to all kinds of .NET applications: from desktop WinForms, WPF, UWP, WinUI, and console to mobile MAUI and Avalonia to server Razor and Blazor.


## Table of Contents
* [Configuration Files](config-file.md): Structure and format of a configuration file
* [Translation Files and Formats](files-formats.md): Everything related to translation files, their formats, and supported locations
* [Strings and Translations](strings.md): Everything about the fundamentals - text strings and translation units
* [Templates and Placeholders](placeholders.md): What are templated translation units and how to use placeholders in translation units efficiently
* [Generator](generator.md): What Generator is and when you use it
* [Language management](language-management.md): How to list and switch languages
* [Integration with XAML](xaml.md): Binding of XAML elements to generated translation units for automatic updates of the UI
- [Glossary](glossary.md): The list of most frequent terms in this documentation.

<a name="GettingStarted"></a>
## The Ways to Use Tlumach

Tlumach can be used in different ways depending on your application type and the way the localized text is to be used:

1. XAML-based desktop and mobile .NET applications, use in XAML UIs. You can bind XAML attributes to translation units as shown below. This way, when the language is switched in the translation manager, UI elements get updated automatically.
2. Websites and Web, console, and server applications which output the text from code or web files. There, you can access generated <xref:Tlumach.TranslationUnit> instances in code to pick the text for current or specific locale. The use of generated <xref:Tlumach.TranslationUnit> objects ensures that there is no mistake made when referencing the text.
3. Any application. The basic way to access translations is to create and use an instance of the <xref:Tlumach.TranslationManager> class to retrieve specific translation units by key (a simple string). This class is always available and used internally by <xref:Tlumach.TranslationUnit> objects.

Each of the ways is documented below.

## Getting Started

It is a good idea to check [out] the samples that can be found in [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples) before proceeding with the below instructions.

Choose the desired way of using Tlumach to read specific Getting Started instructions:
- [Getting Started for integration with WPF](getting-started-wpf.md)
- [Getting Started for integration with UWP](getting-started-uwp.md)
- [Getting Started for integration with WinUI](getting-started-winui.md)
- [Getting Started for integration with UWP](getting-started-uwp.md)
- [Getting Started for integration with MAUI](getting-started-maui.md)
- [Getting Started for integration with Avalonia](getting-started-avalonia.md)
- [Getting Started for work with generated translation units](getting-started-manual.md) (recommended for web, server, and console applications)
- [Getting Started for work via TranslationManager](getting-started-tm.md) (for fine control over the process and for creating translation tools)
