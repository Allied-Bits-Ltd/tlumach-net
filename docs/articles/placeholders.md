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

If you use [Generator](generator.md) and generated translation units, Generator will create instances of descendants of the <xref:Tlumach.BaseTranslationUnit> class. The <xref:Tlumach.BaseTranslationUnit> class contains the <xref:Tlumach.BaseTranslationUnit.ContainsPlaceholders> property and a set of `GetValue()` methods which let you retrieve the text with placeholders replaced with actual values.

If you use <xref:Tlumach.TranslationManager> directly, it also contains `GetValue()` methods which let you retrieve the text with placeholders replaced with actual values.

Both sets of `GetValue()` methods support multiple ways to pass the values. Please pay attention to the exact type of argument(s), because the "catch-all" overload of `GetValue()` methods accepts `object` as a parameter, and if your argument type does not match the type of the method parameter exactly, this catch-all overload may be called instead.

Tlumach makes an attempt to find a value in the set of provided values regardless of the way these values are passed, but search by index is not possible in a dictionary which is not ordered, while search by name is not possible in an array of objects. Thus, choose the format according to the form of placeholders in your translations.

Since version 1.1, you can bind XAML controls to units with placeholders, but you need to take extra steps to provide actual data for the placeholders in such units. Read the details in the [corresponding section of this documentation](strings.md#TemplatesInXAML).

Dependency Injection classes fully support placeholders, however, you can pass only the list of objects to the .NET interfaces, which limits the ability to process named placeholders.

Since version 1.6.2, Generator can optionally create subclasses of the <xref:Tlumach.TranslationUnit> class and include `Filled()` methods there. These methods include typed parameters that make it easy to pass properly typed values for placeholders instead of using dictionaries or objects with properties.
To enable the generation of these methods, set the "createFilledMethods" option in the [configuration](config-file.md) to `true` or set the "TlumachGeneratedCreateFilledMethods" option in the translation project file to `True` as follows:+1:

```xml
<PropertyGroup>
    <TlumachGeneratedCreateFilledMethods>True</TlumachGeneratedCreateFilledMethods>
</PropertyGroup>
<ItemGroup>
    <!-- Makes the property visible to analyzers/generators -->
    <CompilerVisibleProperty Include="TlumachGeneratedCreateFilledMethods" />
</ItemGroup>
```

The benefit of the "Filled" methods is that they are a bit faster than other ways of passing values for the placeholders, and the syntax is checked at compile time:

```c#
  // Assuming that "HelloMessage" is generated from the "Hello {name}!" translation string
  string filled = HelloMesage.Filled("Alice"); // Produces "Hello Alice!"
  
  // Assuming that the ResultMessage" is generated from the "{0} items added." translation string
  string result = ResultMessage.Filled(5); // Produces "5 items added." but only if the placeholder was recognized as numeric / integer; otherwise, you'd need to call "Filled(5.ToString());"
```

### TextFormat and text processing modes

When determining whether something in curly braces is a placeholder, Tlumach makes use of the static `TextProcessingMode` property of the parser class.
This property is initialized to different values depending on the format, and not every parser has it. E.g., the INI parser sets this mode to `None` by default, Arb parser sets it to `Arb`, and .resx parser always uses `DotNet` mode (as .resx is a .NET-specific format). The .xcstrings parser uses `Apple` mode.

Another use of <xref:Tlumach.Base.TextFormat> and the static `TextProcessingMode` property is to figure out whether the strings may come escaped and should be un-escaped when the translation file is loaded.

In runtime, the text processing mode can be changed via the <xref:Tlumach.Base.TranslationConfiguration.TextProcessingMode> property.

The possible modes are
* **None** - No decoding of characters takes place, and placeholders are not detected.
* **BackslashEscaping** - Strings may contain any characters, but "unsafe" characters should be prepended with a backslash ("\\"), and encoded characters are supported. This is the format used in C++, JSON strings, and TOML basic strings. Placeholders are not supported in this format.
* **Arb** - Curly braces are used to denote placeholders according to the rules defined for Arb files (those used in Dart language and Flutter framework) including the "use-escaping: true" setting: single quote characters may be used to quote curly braces so that quoted braces are not considered placeholder boundaries.
* **ArbNoEscaping** - Curly braces are used to denote placeholders according to the rules defined for Arb files (those used in Dart language and Flutter framework). Unlike Arb mode, quote characters (') are not considered as escape symbols.
* **DotNet** - Curly braces are used to denote placeholders according to the .NET rules used by String.Format(). Text is considered to be optionally escaped, and an attempt is made to un-escape it according to `BackslashEscaping` rules. Curly braces that are not placeholder boundaries should be duplicated like in .NET strings.
* **Apple** - placeholders are denoted, similar to the C++ syntax, by the percent character (%) followed by the format specifier. The list of specifiers is defined by Apple in its String Catalog format and formats used for localizaiton before String Catalog.

The use of .NET format specifiers requires that `TextProcessingMode` is set to `DotNet`. The use of ICU format specifiers is possible when you use named placeholders.

### Supported placeholder formats

In both `DotNet` and `Arb` modes, Tlumach will try to walk through the templated text and replace every placeholder, be it a numeric or a literal one. It will then process each placeholder with respect to ICU directives, if they are present in the placeholder.

When handling translation units coming from Arb files, Tlumach recognizes string, numeric, and date-time placeholders and supports most of Arb format specifiers.

In ICU-compatible placeholders, Tlumach supports `select`, `selectordinal`, `plural`, `number`, `date`, `time`, and `datetime` kinds. Sophisticated format specifiers embedded into ICU-compatible placeholders are not fully supported, but the corresponding improvements are in the plan.
