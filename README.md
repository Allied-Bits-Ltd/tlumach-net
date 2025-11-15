![Tlumach](Tlumach.png "Tlumach")

# Tlumach

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Allied-Bits-ltd/Tlumach/build-test.yml "GitHub Actions Workflow Status")](https://img.shields.io/github/actions/workflow/status/Allied-Bits-ltd/Tlumach/build-test.yml)

[![NuGet](https://img.shields.io/nuget/v/AlliedBits.Tlumach.svg)](https://www.nuget.org/packages/AlliedBits.Tlumach) [![downloads](https://img.shields.io/nuget/dt/AlliedBits.Tlumach)](https://www.nuget.org/packages/AlliedBits.Tlumach)

Tlumach.NET is a flexible library that provides translation and localization support to all kinds of .NET applications: from desktop WinForms, WPF, WinUI, and console to mobile MAUI and Avalonia to server Razor and Blazor.

The goal of the library is to support different formats of translation files and to support different languages and locales concurrently (even within one thread).

## Features

The features of Tlumach include:

* Integration with XAML (in WPF, WinUI, MAUI, and Avalonia projects) via bindings to provide localized UI. The markup extension is provided for easy integration.
* The Generator class to generate source code with translation units for static use and for XAML UIs during compilation of the project.
* Use via the translation manager or by accessing generated translation units, which enable syntax checking in design time.
* Support for on-the-fly switching of current language/locale with automatic update of the UI (for XAML UIs).
* Automatic fallback to the basic locale (e.g., "de-AT" -> "de-DE") translation or to the default translation if a translation for a particular key is not available in the locale-specific translation.
* Suitable for server and web applications thanks to the possibility to obtain translations for different languages/locales concurrently, even within one thread.
* Handling of translation files in JSON, Arb (JSON with additional features, used in Dart/Flutter), simple INI, TOML, CSV and TSV, and .NET resx files.
* Loading of translations from assembly resources, from disk files, or from a custom source (via events).
* Smart search for localized files using ISO 639-1 names (e.g., "de" or "hr") and using RFC 5646 locale identifiers (e.g., "de-AT", "pt-BR"). It is also possible to specify custom names for files with translations via a configuration file or to provide translation files via events, making it possible to fetch the translations from the network.
* Support for multiple translation sets in one project. For example, you can keep server log strings in one file and client messages in another.
* Each translation set can have a hierarchy of groups of translation entries, enabling easy management of translations.
* Automatic recognition and support for templated strings in Arb and .NET formats. This includes support for .NET- and Arb-style placeholders and support of main ICU features ("select" and "plural" types) in Arb-style placeholders.
* The possibility to control the found translation entries or provide entries for missing keys via events (may be necessary if an application should use some phrases configured by a user rather than from translations).

## Supported platforms and frameworks

* .NET 9.0 and later (a dedicated set of assemblies is provided)
* Anything that loads .NET Standard 2.0 assemblies

...on any operating system capable of running .NET 9+ or loading .NET Standard 2.0 assemblies.

## Documentation

Tlumach documentation can be conveniently read in the [Tlumach section of Allied Bits site](https://alliedbits.com/tlumach).
The source files of the documentation are available in the [Tlumach repository](https://github.com/Allied-Bits-Ltd/tlumach), in the "docs" directory.

## Help and support

For general discussions and suggestions, you are welcome to use the [Discussions section](https://github.com/Allied-Bits-Ltd/tlumach/discussions).

If you need help with issues or want to report a bug, [please open an issue](https://github.com/Allied-Bits-Ltd/tlumach/issues/new/choose) and include the necessary details (including the relevant configuration and translation files) to help us better understand the problem. Providing this information will aid in resolving the issue effectively.




