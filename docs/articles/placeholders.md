# Templates and Placeholders

A [translation unit](glossary.md) may contain [placeholders](glossary.md) - words or numbers enclosed in curly braces - that get replaced with value provided in runtime.

Tlumach supports both numeric and literal placeholders. Numeric placeholders reference parameters by number (the index of the parameter in the list), while literal placeholders denote parameters by name.

Sample placeholders:
```
Hello {name}!
{0} items added.
```

## Use of Placeholders

Tlumach recognizes placeholders and marks translation units as templated according to the source file format and the value of the `TextProcessingMode` static property of the corresponding parser (see below about TextFormat and TextProcessingMode).

If you use [Generator](generator.md) and generated translation units, Generator will create instances of <xref:Tlumach.TemplatedTranslationUnit> class for templated translation units. The <xref:Tlumach.TemplatedTranslationUnit> class contains a set of `GetValue()` methods which let you retrieve the text with placeholders replaced with actual values.

If you use <xref:Tlumach.TranslationManager> directly, it also contains `GetValue()` methods which let you retrieve the text with placeholders replaced with actual values.

Both sets of `GetValue()` methods support multiple ways to pass the values. Please pay attention to the exact type of argument(s), because the "catch-all" overload of `GetValue()` methods accepts `object` as a parameter, and if your argument type does not match the type of the method parameter exactly, this catch-all overload may be called instead.

Tlumach makes an attempt to find a value in the set of provided values regardless of the way these values are passed, but search by index is not possible in a dictionary which is not ordered, while search by name is not possible in an array of objects. Thus, choose the format according to the form of placeholders in your translations.

### TextFormat and text processing modes

When determining whether something in Curly braces is a placeholder, Tlumach makes use of the static `TextProcessingMode` property of the parser class.
This property is initialized to different values depending on the format, and not every parser has it. E.g., the INI parser sets this mode to `None` by default, Arb parser sets it to `Arb`, and .resx parser always uses `DotNet` mode (as .resx is a .NET-specific format).

Another use of <xref:Tlumach.Base.TextFormat> and the static `TextProcessingMode` property is to figure out whether the strings may come escaped and should be un-escaped when the translation file is loaded.

In runtime, the text processing mode can be changed via the <xref:Tlumach.Base.TranslationConfiguration.TextProcessingMode> property.

The possible modes are
* **None** - No decoding of characters takes place, and placeholders are not detected.
* **BackslashEscaping** - Strings may contain any characters, but "unsafe" characters should be prepended with a backslash ("\\"), and encoded characters are supported. This is the format used in C++, JSON strings, and TOML basic strings. Placeholders are not supported in this format.
* **Arb** - Curly braces are used to denote placeholders according to the rules defined for Arb files (those used in Dart language and Flutter framework) including the "use-escaping: true" setting: single quote characters may be used to quote curly braces so that quoted braces are not considered placeholder boundaries.
* **ArbNoEscaping** - Curly braces are used to denote placeholders according to the rules defined for Arb files (those used in Dart language and Flutter framework). Unlike Arb mode, quote characters (') are not considered as escape symbols.
* **DotNet** - Curly braces are used to denote placeholders according to the .NET rules used by String.Format(). Text is considered to be optionally escaped, and an attempt is made to un-escape it according to BackslashEscaping rules. Curly braces that are not placeholder boundaries should be duplicated like in .NET strings.

The use of .NET format specifiers requires that TextProcessingMode is set to `DotNet`. The use of ICU format specifiers is possible when you use named placeholders.

### Supported placeholder formats

In both `DotNet` and `Arb` and `ArbNoEscaping` modes, Tlumach will try to walk through the templated text and replace every placeholder, be it a numeric or a literal one. It will then process each placeholder with respect to ICU directives, if they are present in the placeholder.

At the moment, .NET format specifiers are not detected (this is planned). If your translation uses templated strings with .NET format specifiers in the placeholders, you can take the template values as described in [this topic](strings.md#StringTypes) and format them in your code.

When handling translation units coming from Arb files, Tlumach recognizes string, numeric, and date-time placeholders and supports most of Arb format specifiers.

In ICU-compatible placeholders, Tlumach supports `select`, `plural`, and `number` kinds. Sophisticated format specifiers embedded into ICU-compatible placeholders are not fully supported, but the corresponding improvements are in the plan.
