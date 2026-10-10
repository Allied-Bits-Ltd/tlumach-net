---
title: Tlumach for .NET
description: Tlumach.NET is a flexible library that provides translation and localization support to all kinds of .NET applications.
---
# Welcome to Tlumach!

Tlumach.NET is a flexible library that provides translation and localization support to all kinds of .NET applications: from desktop WinForms, WPF, UWP, WinUI, and console to mobile MAUI and Avalonia to server MVC, Razor Pages, and Blazor.
<br>
Additionally, Tlumach includes the Tlumach Tools utility that can be used in CI/CD scenarios to validate and convert translation files.
<br>

To get started, please visit the [Getting Started section](articles/index.md#GettingStarted) of the documentation.

## Why Tlumach 

Tlumach supports different formats of translation files and works with multiple languages and locales concurrently (even within one thread).
Also, Tlumach can use translations stored in external files, resources, or in custom locations, which makes maintenance of translations easier than dealing with resource compilation and language DLLs.
Finally, the application language can be switched without restarting the application.
And if you are bound to .resx format, Tlumach supports .resx files in their source form (no compilation required).

## Downloads

**Tlumach:**
* [Tlumach .NET and Writers on NuGet.Org](https://www.nuget.org/profiles/AlliedBits)
* [Tlumach source code on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net)

**Tlumach Tools:**
* [Tlumach Tools source code and binaries on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-tools)
* [Tlumach Tools for Windows / .NET Framework 4.8.1](/files/TlumachToolsWindows.zip)
* [Tlumach Tools cross-platform / .NET 10](/files/TlumachToolsWNet10.zip)

**Tlumach extensions**
* [Tlumach.NET extension for Visual Studio 2026 and Visual Studio 2022](https://marketplace.visualstudio.com/items?itemName=AlliedBitsLtd.TlumachExtensionVisualStudio)
* [Tlumach.NET extension for VS Code](https://marketplace.visualstudio.com/items?itemName=AlliedBitsLtd.tlumach)

<br>

## Help and Support

For general discussions and suggestions, you are welcome to use the [Discussions section](https://github.com/Allied-Bits-Ltd/tlumach-net/discussions).

If you need help with issues or want to report a bug, [please open an issue](https://github.com/Allied-Bits-Ltd/tlumach-net/issues/new/choose) and include the necessary details (including the relevant configuration and translation files) to help us better understand the problem. Providing this information will aid in resolving the issue effectively.

## Features

The features of Tlumach include:

* Integration with XAML (in WPF, UWP, WinUI, Uno Platform, MAUI, and Avalonia projects) via bindings to provide localized UI. The markup extension is provided for easy integration.
* Integration with Windows Forms: translation keys can be assigned to controls in the Visual Studio Designer via the `TranslationProvider` component or bound in code, with automatic update of the controls when the language is switched.
* Dependency Injection support and integration with Microsoft.Extensions.Localization.
* Integration with Blazor (Server, WebAssembly, static SSR, and Hybrid): components and helpers that show translations in the language of each user, a culture selector, and storage of the chosen language in a cookie or in the local storage of the browser.
* Integration with ASP.NET Core MVC and Razor Pages: `IViewLocalizer` and `IHtmlLocalizer`, tag helpers, a culture selector, and localized model binding messages and display names.
* Localization of the messages and display names of Data Annotations validation attributes.
* Integration with [FluentValidation](https://docs.fluentvalidation.net): localized built-in messages, messages and names of individual rules, error codes, and display names of properties.
* Integration with the [Scriban](https://github.com/scriban/scriban), [Fluid](https://github.com/sebastienros/fluid) (Liquid), and [Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net) template engines for localized emails, reports, and documents rendered concurrently in different languages.
* Localization of the built-in texts of [MudBlazor](https://mudblazor.com) and [Syncfusion Blazor](https://www.syncfusion.com/blazor-components) components in the language of each user.
* Low-level use via the translation manager or by accessing generated translation units, which enable syntax checking in design time.
* The Generator class to generate source code with translation units for static use and for XAML UIs during compilation of the project.
* Suitable for server and web applications thanks to the possibility to obtain translations for different languages/locales concurrently, even within one thread.
* Support for on-the-fly switching of current language/locale with automatic update of the UI.
* Automatic fallback to the basic locale (e.g., "de-AT" -> "de-DE") translation or to the default translation if a translation for a particular key is not available in the locale-specific translation.
* Handling of translation files in JSON, Arb (JSON with additional features, used in Dart/Flutter), simple INI, TOML, CSV and TSV, XLIFF 2.2 (bitext format with source and target), .NET ResX files, and Apple-specific .xcstrings (XCode String Catalog) format.
* Loading of translations from assembly resources, from disk files, or from a custom source (e.g., a database) via events.
* Smart search for localized files using ISO 639-1 names (e.g., "de" or "hr") and using RFC 5646 locale identifiers (e.g., "de-AT", "pt-BR"). It is also possible to specify custom names for files with translations via a configuration file or to provide translation files via events, making it possible to fetch the translations from the network.
* Support for multiple translation sets in one project. For example, you can keep server log strings in one file and client messages in another.
* The ability to pick a best-match translation for a list provided by the caller (usually, a browser client of a web application).
* Each translation set can have a hierarchy of groups of translation entries, enabling easy management of translations (depending on the source format).
* Automatic recognition and support for templated strings in Arb and .NET formats. This includes support for .NET- and Arb-style placeholders and support of main Unicode and ICU features ("number", "select", "plural", "selectordinal", "date", "time", "datetime" placeholder kinds) in Arb-style placeholders. (Note: placeholder style is independent of the file format, i.e., you can use Arb-style placeholders in a ResX or TOML translation file.)
* The possibility to control the found translation entries or provide entries for missing keys via events (it may be necessary if an application should use some phrases configured by a user rather than from translations).
* Writer classes for export of translations in all supported formats (useful when you need to automate convertion between translation formats). 
* Compatibility with AOT compilation.

## Tlumach is Used

Tlumach is known to be used in the following applications:

* [ABCalc](https://alliedbits.com/abcalc/) - an advanced programmable calculator
