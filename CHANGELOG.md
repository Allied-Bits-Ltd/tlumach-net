# Change Log

This document provides information about the changes and new features in Tlumach.

---
Version: 1.9.1 
Date: August 9, 2026

- [FIX] In some project configurations (where the base project directory was also the directory of the translation and configuration files), the Generator failed when invoked from the Visual Studio extension (generation during project building worked fine).  

---
Version: 1.9.0 
Date: August 7, 2026

Performance work on the translation-lookup and template-processing paths. Unless listed as `[IMPORTANT]`, the changes below do not alter behaviour.

- [PERF] `TranslationManager.GetValue` no longer uppercases the key and the culture name before probing. Both dictionaries already compare their keys with `StringComparer.OrdinalIgnoreCase`, so the conversions allocated two strings per lookup and changed nothing.
- [PERF] Loading a translation no longer happens while a lock covering the whole translation map is held. Each culture is now loaded under its own gate, so a slow load blocks only callers who want that same culture. Loading different cultures at the same time now scales with the number of cores instead of serializing.
- [PERF] Reading an already-loaded translation takes no lock on the translation map at all, and the default translation is now published through a `volatile` field rather than being read under a monitor. Resolving a file reference no longer holds the translation's lock.
- [PERF] `TranslationUnit` and other `BaseTranslationUnit` descendants reuse a single placeholder-resolver delegate instead of allocating a new one on every `GetValue()` call. The delegate still reads the placeholder cache and the `OnPlaceholderValueNeeded` subscription list live, so behaviour is unchanged.
- [PERF] Placeholder values are converted to text directly instead of through `string.Format(culture, "{0}", value)`, which parsed a composite format string on every placeholder.
- [PERF] In the `DotNet` text processing mode, the composite format string built from a placeholder's format specifier is cached per specifier instead of being concatenated for every placeholder evaluation.
- [PERF] `Utils.TryGetPropertyValue` builds and caches a property index per type. It previously called `Type.GetProperties`, which allocates a new array, once per placeholder, and scanned the result linearly. The resolution order is unchanged: an exact, case-sensitive match still wins over a case-insensitive one, and only public instance properties are considered.
- [PERF] The lookup of a placeholder's declaration in an `Arb` entry uses an indexed loop instead of a LINQ predicate, which allocated a closure and a delegate per placeholder.
- [PERF] In the `Arb` text processing modes, the closure used to process nested placeholder content is created at most once per template evaluation instead of once per placeholder.
- [PERF] Template processing reuses a small per-thread pool of `StringBuilder` instances. A pool rather than a single instance is required because processing re-enters itself for nested placeholder content.
- [PERF] The placeholder value cache of a translation unit uses `StringComparer.Ordinal` instead of `StringComparer.InvariantCulture`. The matching stays case-sensitive; the collation-based comparer routed every lookup through ICU.
- [PERF] `Utils.GetLeadingNonNegativeNumber` accumulates the value while scanning instead of allocating a substring and re-parsing it. The contract, including the answer for a digit run that does not fit in an `Int32`, is unchanged.
- [PERF] In the `Apple` text processing mode, specifier keys are no longer allocated per placeholder, and a value whose type does not match the specifier (for example, a string passed for `%d`) no longer raises and catches an exception. The text produced for such a value is unchanged.
- [IMPORTANT] `TranslationConfiguration.Translations` now compares its keys with `StringComparer.OrdinalIgnoreCase`. A configuration file that declares a locale in lowercase now matches, where previously it was silently ignored. Conversely, a configuration that declares two locales differing only by case (for example, both `de` and `DE`) is now reported as a duplicate.
- [IMPORTANT] The protected `BaseTranslationManager.Translations` property is now a `ConcurrentDictionary<string, Translation>` instead of a `Dictionary<string, Translation>`, and the protected `_defaultTranslation` field is now `volatile`. Code that derives from `BaseTranslationManager` and uses these members directly may need to be adjusted; in particular, `Translations.Remove(key)` becomes `Translations.TryRemove(key, out _)`.
- [IMPORTANT] When a placeholder value object exposes an indexer, the indexer is no longer considered a candidate for a placeholder named `Item`. Reading it without index arguments threw a `TargetParameterCountException`; such a placeholder is now reported as having no value.
- [FIX] In the `DotNet` text processing mode, a placeholder that carried a format specifier or an alignment did not work. The part following the placeholder name kept its leading colon, so `{0:N2}` was turned into the composite format `{0::N2}`; .NET then read `":N2"` as a custom numeric format made entirely of literal characters, and the value was dropped in favour of the text `":N2"`. An alignment was swallowed the same way, and `{0,10:N2}` produced neither alignment nor formatting. Specifiers and alignments now behave as they do in `string.Format`, so `{0:N2}`, `{0:X}`, `{0:yyyy-MM-dd}`, `{0,10}` and `{0,10:N2}` all produce the expected text. Note that translations which relied on the previous output will now render differently.
- [FIX] A `\uXXXX` escape in a templated text was decoded correctly but the reader then stepped one character short, so the last hexadecimal digit was emitted a second time: `A` produced `"A1"` instead of `"A"`. `Utils.UnescapeString` was not affected; the two paths now agree.
- [FIX] `FileFormats` read its parser registries without holding the lock that registration writes under. A parser lookup made while another thread was registering a parser could fail to find a parser that was in fact registered. The registries are now concurrent.

---
Version: 1.8.0  
Date: August 2, 2026

- [NEW] Added the `TranslationUnit.GetValueFrom<T>` and `TranslationEntry.ProcessTemplatedValueFrom<T>` overloads. They take the placeholder values from the public properties of a generic argument instead of an `object`, which lets the trimmer see and preserve those properties. Use these overloads instead of the `object`-based ones in applications published with trimming or NativeAOT (in particular, on iOS and Mac Catalyst, where full trimming is common) - otherwise the properties of anonymous types may be removed and the placeholders will not be substituted. Note that the lookup uses `typeof(T)`, so a value stored in a variable declared as `object` finds no properties; declare the variable with its concrete type.
- [NEW] Added the `Utils.TryGetPropertyValue` overload that accepts a `Type` known at compile time. It is the trimming-safe counterpart of the overload that accepts an `object`.
- [IMPORTANT] The `object`-based `TranslationUnit.GetValue`, `TranslationEntry.ProcessTemplatedValue`, and `Utils.TryGetPropertyValue` methods are now marked with `RequiresUnreferencedCode`. Applications that enable trim analysis will see an `IL2026` warning at the call sites, pointing to the overloads listed above. Applications that do not use trimming are not affected.
- [IMPORTANT] When an object is passed as the source of placeholder values and none of its properties match the placeholder name, the object itself is no longer substituted into the text. Previously, the `ToString()` of the whole object ended up in the translated string (for example, `{ Count = 5, Name = x }`), which silently corrupted the output. Now such a placeholder is reported as having no value, so the `OnPlaceholderValueNeeded` event can supply one. Passing a lone scalar value (a number, a string, a `bool`, a `char`, a `DateTime`, a `DateTimeOffset`, a `TimeSpan`, a `Guid`, or an enumeration member) for a single placeholder keeps working as before.
- [FIX] The `OnlyDeclareKeys` and `CreateFilledMethods` options did not work when specified in a simple key-value configuration file (.ini, .cfg). 

---
Version: 1.7.0  
Date: June 9, 2026

+ [NEW] Added the `WebEncodeValues` property to `TranslationManager`. When the property is set to `true`, translation units linked to this translation manager instance return the text which is safe for insertion into HTML web page sources.

---
Version: 1.6.3  
Date: May 10, 2026

- [FIX] A regression - `Tlumach.Avalonia.TranslationUnit` accidentally lost the constructor used by the generated code. 

---
Version: 1.6.2  
Date: May 8, 2026

- [NEW] Added optional generation of an individual class, a descendant of the TranslationUnit class, with the `Filled` method that accepts parameters named and typed after placeholders in the corresponding translation entry. This way, filling templated translation strings becomes easier as the syntax and types are checked at compile time. Also, this  way of passing the parameters is the fastest one, although it leads to the creation of extra classes and generation of additional code (one class with three methods per translation entry that contains placeholders). 
- [FIX] In the `Arb` text processing mode, integer placeholders were not substituted with a number if there was no format specified. 

---
Version: 1.6.0  
Date: May 1, 2026

- [IMPORTANT] Some refactoring - some of the members of `TranslationManager` were moved to the ancestor class, `BaseTranslationManager`.
- [NEW] Now, the Generator writes the source value of the text to the documentation comments, making it possible to see the text value by hovering the mouse cursor over a constant. This does not work for text that is loaded dynamically (from references or via events). 
- [NEW] Added the extensions for Visual Studio and VS Code. The extensions let you run Generator without building a translation project or projects. Also, in Visual Studio, you can navigate to the original location of the translation entry in the main/default translation file by using the Go To Definition" functionality of the IDEs. 
- [NEW] Added the parser and writer for Apple String Catalog file format.

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
