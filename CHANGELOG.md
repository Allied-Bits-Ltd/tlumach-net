# Change Log

This document provides information about the changes and new features in Tlumach.

- [NEW] Added Dependency Injection support.
- [NEW] Generator now emits key names as string constants.
- [NEW] It is possible to skip generation of `TranslationUnit` instances (and just use key name constants).
- [NEW] Added optional caching of values to the `TranslationUnit` class.

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
