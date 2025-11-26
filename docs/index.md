# Welcome to Tlumach!

Tlumach.NET is a flexible library that provides translation and localization support to all kinds of .NET applications: from desktop WinForms, WPF, UWP, WinUI, and console to mobile MAUI and Avalonia to server Razor and Blazor.
<br>

To get started, please visit the [Getting Started section](https://alliedbits.com/tlumach/articles/index.md#GettingStarted) of the documentation.

<br>

## Useful Links
* [Tlumach on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net)
* [Allied Bits on NuGet.Org](https://www.nuget.org/profiles/AlliedBits)

<br>

## Help and support

For general discussions and suggestions, you are welcome to use the [Discussions section](https://github.com/Allied-Bits-Ltd/tlumach/discussions).

If you need help with issues or want to report a bug, [please open an issue](https://github.com/Allied-Bits-Ltd/tlumach/issues/new/choose) and include the necessary details (including the relevant configuration and translation files) to help us better understand the problem. Providing this information will aid in resolving the issue effectively.

## Features

The features of Tlumach include:

* Integration with XAML (in WPF, UWP, WinUI, MAUI, and Avalonia projects) via bindings to provide localized UI. The markup extension is provided for easy integration.
* Use via the translation manager or by accessing generated translation units, which enable syntax checking in design time.
* The Generator class to generate source code with translation units for static use and for XAML UIs during compilation of the project.
* Suitable for server and web applications thanks to the possibility to obtain translations for different languages/locales concurrently, even within one thread.
* Support for on-the-fly switching of current language/locale with automatic update of the UI (for XAML UIs).
* Automatic fallback to the basic locale (e.g., "de-AT" -> "de-DE") translation or to the default translation if a translation for a particular key is not available in the locale-specific translation.
* Handling of translation files in JSON, Arb (JSON with additional features, used in Dart/Flutter), simple INI, TOML, CSV and TSV, and .NET ResX files.
* Loading of translations from assembly resources, from disk files, or from a custom source (via events).
* Smart search for localized files using ISO 639-1 names (e.g., "de" or "hr") and using RFC 5646 locale identifiers (e.g., "de-AT", "pt-BR"). It is also possible to specify custom names for files with translations via a configuration file or to provide translation files via events, making it possible to fetch the translations from the network.
* Support for multiple translation sets in one project. For example, you can keep server log strings in one file and client messages in another.
* Each translation set can have a hierarchy of groups of translation entries, enabling easy management of translations (depending on the source format).
* Automatic recognition and support for templated strings in Arb and .NET formats. This includes support for .NET- and Arb-style placeholders and support of main ICU features ("number", "select", and "plural" types) in Arb-style placeholders. (Note: placeholder style is independent of the file format, i.e., you can use Arb-style placeholders in a ResX or TOML translation file.)
* The possibility to control the found translation entries or provide entries for missing keys via events (it may be necessary if an application should use some phrases configured by a user rather than from translations).
