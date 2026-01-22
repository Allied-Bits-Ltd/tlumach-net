# Change Log

This document provides information about the changes and new features in Tlumach.

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
