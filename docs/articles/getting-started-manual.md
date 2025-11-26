# Getting Started

## Work with generated translation units

**1. Add Tlumach to your project**:

a) via NuGet

Add a package reference to "Tlumach" to your project

* via NuGet package manager GUI in Visual Studio

* via the command line:

```cmd
dotnet add package Tlumach
```

* using the text editor - add the following reference to your project:
```xml
<ItemGroup>
    <PackageReference Include="Tlumach" Version="1.*" />
</ItemGroup>
```

b) with Source Code

- Check out Tlumach from the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net)
- Add _Tlumach.Base_ and _Tlumach_ projects to your solution and reference them from your project(s).

**2. Create a configuration file**

Please see the detailed description of the [configuration file here](config-file.md).

A simple configuration file for a start looks like this:

```ini
defaultFile=strings.toml
generatedClass=Strings
generatedNamespace=Tlumach.Sample
```

Save it to "strings.cfg".

The "strings.toml" file is a [default file](glossary.md), i.e., a file with [strings](strings.md) that will be retrieved by default.

**3. Create a default translation file**

Here is the minimal translation file in [TOML format](files-formats.md):

```toml
hello="Hello!"
```

Save it to "strings.toml".

**4. Create and set up a [translation project](glossary.md)**

This is a C# project separate from your main code project(s); this translation project does not need any source code in it.

Please see the detailed description of setting up a [translation project here](generator.md#TranslationProject).

The minimal addition you need to make to the new empty project are the "strings.cfg" file and the "strings.toml" file from the previous step.

Add "strings.cfg" to the project as an additional file. You can use the IDE for this or edit the project file as text and add these lines:

```xml
<ItemGroup>
    <AdditionalFiles Include="strings.cfg" />
</ItemGroup>
```

Next, add "strings.toml" to the project as Embedded Resource:

```xml
<ItemGroup>
    <EmbeddedResource Include="strings.toml" />
<ItemGroup>
```

Alternatively, if you plan to load translations from the disk, you can add a file as Content, but then, you will need to set <xref:Tlumach.TranslationManager.LoadFromDisk> property to true. The TranslationManager instance will be accessible to you as a static object named "Tlumach.Sample.Strings.TranslationManager".

**5. Reference the translation project in your main project**

**6. Build translation project**

This step is needed so that Generator creates the source code with [generated translation units](glossary.md#GeneratedUnit), which you will reference in your code.

**7. Use generated translation units in your code**

The "hello" string from "string.toml" is available in your main project as a static object named "Tlumach.Sample.Strings.hello", and you need its CurrentValue property:

```c#
string helloValue = Tlumach.Sample.Strings.hello.CurrentValue;
```

or

```c#
using Tlumach.Sample;
...
string helloValue = Strings.hello.CurrentValue;
```

or just

```c#
using static Tlumach.Sample.Strings;
...
string helloValue = hello.CurrentValue;
```

**Translations of string**

To retrieve the value for a different culture (e.g., in a server application), use the <xref:Tlumach.TranslationUnit.GetValue(System.Globalization.CultureInfo)> method:

```c#
using Tlumach.Sample;
...
CultureInfo deCulture = new CultureInfo("de-DE");
string helloValueDE = Strings.hello.GetValue(deCulture);
```

To switch current language (the one used for the <xref:Tlumach.TranslationUnit.CurrentValue> property), assign a new value to <xref:Tlumach.TranslationManager.CurrentCulture>:

```c#
using Tlumach.Sample;
...
CultureInfo deCulture = new CultureInfo("de-DE");
Strings.TranslationManager.CurrentCulture = deCulture;
```

Remember that you need [locale-specific files](glossary.md#LocaleSpecificFile) for other languages. For this, read about [Translation Files and Formats](files-formats.md)
