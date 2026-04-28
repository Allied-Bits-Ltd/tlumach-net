# Change Log

This document provides information about the changes and new features in Tlumach.

---
Version: 1.6.0  
Date: April 22, 2026

- [IMPORTANT] Some refactoring - some of the members of `TranslationManager` were moved to the ancestor class, `BaseTranslationManager`.
- [NEW] Now, the Generator writes the source value of the text to the documentation comments, making it possible to see the text value by hovering the mouse cursor over a constant. This does not work for text that is loaded dynamically (from references or via events). 
- [NEW] Added the extensions for Visual Studio and VS Code. The extensions let you run Generator without building a translation project or projects. Also, you can navigate to the original location of the constant in the main/default translation file by using the navigate to symbol functionality of the IDEs. 

---
Version: 1.5.0.1  
Date: April 21, 2026

- [FIX] Renamed the `placeholderValues` parameter of the `TranslationUnit.GetValue` overloads to indicate the type of the parameter. This is necessary for avoiding ambiguities when calling "GetValue([ someValue ])".

---
Version: 1.5  
Date: April 20, 2026

- [NEW] Added the writer classes for all formats. These classes can be used in the creation of various tools related to translations (conversion, export/import, etc.), and they are the basis for Tlumach Tools. Writer classes go to the dedicated NuGet package.
- [NEW] Added the parser and writer for XLIFF file format.
- [NEW] Added a static list of all `TranslationManager` instances (`TranslationManager.TranslationManagers` property) for easier update of properties of several managers.
- [NEW] Added the overload of the `LoadTranslation` method to the `TranslationManager` class that loads a translation by culture and expanded the `GetTranslation` method to optionally load the translation if it is not loaded yet.
- [NEW] Added the `LoadDefaultTranslation` method to the `TranslationManager` class for use in the file conversion scenarios.
- [FIX] Fixed the line counter in CSV, TSV, INI, and TOML parsers so that when an error occurs, the line number is reported correctly.

---
Version: 1.2.3.4  
Date: March 29, 2026

- [FIX] Tlumach.Generator is built against Microsoft.CodeAnalysis.CSharp version 5.0.0 now in order to be usable in environments with a bit older SDKs.

---
Version: 1.2.3.3  
Date: March 16, 2026

- [FIX] A duplicate curly quote was emitted as duplicate in DotNet mode (only one quote should be emitted). 

---
Version: 1.2.3.2  
Date: January 22, 2026

- [FIX] In the case of an error reported by Generator, the row and column reported by the IDE was offset by one. 

---
Version: 1.2.3.1  
Date: January 17, 2026

- [IMPORTANT] `TranslationUnit` classes can now be assigned to a string (this will assign the value of the `CurrentValue` property); the `ToString` method also returns the value of the `CurrentValue` property (previously, it returned the key).

---
Version: 1.2.3  
Date: January 10, 2026

- [NEW] Added the `OnReferenceNotResolved` event to TranslationManager.
- [NEW] Added the `OnTranslationFileNotFound` event to TranslationManager.
- [NEW] Added the `CacheDefaultTranslations` property to TranslationManager.
- [FIX] If a reference could not be resolved, an `ArgumentException` could occur. Now, an unresolved reference is by default returned "as is", and this behavior can be overridden using the `OnReferenceNotResolved` event.
- [FIX] When the default translation was loaded because some translation unit could not be found in a locale-specific translation, the loaded default translation could in some cases take the place of the current locale-specific translation.

---
Version: 1.2.2.3  
Date: January 9, 2026

- [FIX] If the same key was used in different sections in a TOML or INI file, it was erroneously treated as a duplicate.

---
Version: 1.2.2.2  
Date: December 25, 2025

- [FIX] `UntranslatedUnit` in the Avalonia package returned _null_ in `CurrentValue`.

---
Version: 1.2.2.1  
Date: December 24, 2025

- [FIX] The NuGet package did not include all assemblies in some libs directories, and this prevented the build toolchain from picking the right assemblies when packing an Android application.

---
Version: 1.2.2  
Date: December 20, 2025

- [NEW] Minor improvements in the Generator in its handling of configuration files and translation files that reside in a subdirectory of a project and get included into the assembly as resources.
- [FIX] Slightly improved the work with numeric placeholders in DotNet text processing mode - now, if format specifiers come out of order ("{1}:{0}"), the value from the ordered containers is picked by the format specifier and not by the ordinal position of the placeholder.

---
Version: 1.2.1  
Date: December 13, 2025

- [NEW] Added `UntranslatedUnit` class that lets one create a fake translation unit from a value coming from the application (this may be necessary when the UI operates with lists of translation units).
- [FIX] Removed a shortcut way to format a string with .NET formatter as it fails when a string contains named parameters.

---
Version: 1.2.0  
Date: December 6, 2025

- [NEW] Added Dependency Injection support.
- [NEW] Generator now emits key names as string constants.
- [NEW] It is possible to skip generation of `TranslationUnit` instances (and just use key name constants).
- [NEW] Added optional caching of values to the `TranslationUnit` class.
- [NEW] Added AOT compatibility flag to the main assemblies.
- [NEW] Added the `Comment` property to the `TranslationEntry` class. CSV/TSV and ResX parsers now pick comments from the translation files.

---
Version: 1.1.0  
Date: November 30, 2025

- [IMPORTANT] The TranslationEntry.`IsTemplated` property has been renamed to `ContainsPlaceholders`.
- [NEW] Now, you can bind XAML controls to translation units with placeholders. This requires that the application provide values for such units. Please, refer to the documentation for the details.
- [NEW] Added support for "selectordinal" (only for English presently), "date", "time", and "datetime" placeholder kinds to the ICU fragment parser.
- [FIX] Improvements in the handling of complex cases in placeholders.
- [FIX] The `textProcessingMode` value from a configuration file was used in code generation but not during the initial analysis of the default translation file.

---
Version: 1.0.1  
Date: November 26, 2025

- [FIX] Fixed loading of default translation files from a subdirectory, when both the config file and the translation file resided in the same _sub_directory.
- [FIX] TOML parser falsely marked some units as templated.

---
Version: 1.0.0  
Date: November 26, 2025

- [NEW] Initial public release.
