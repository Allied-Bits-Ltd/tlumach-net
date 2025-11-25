# Getting Started

## Work with Translation Manager

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

- Check out Tlumach from the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach)
- Add _Tlumach.Base_ and _Tlumach_ projects to your solution and reference them from your project(s).

**2. Create a configuration file**

Please see the detailed description of the [configuration file here](config-file.md).

A simple configuration file for a start looks like this:

```ini
defaultFile=strings.toml
```

Save it to "strings.cfg".

The "strings.toml" file is a [default file](glossary.md), i.e., a file with [strings](strings.md) that will be retrieved by default.

**3. Create a default translation file**

Here is the minimal translation file in [TOML format](files-formats.md):

```toml
hello="Hello!"
```

Save it to "strings.toml".

**4. Include translation files into your project**

The minimal addition you need to make to your project are the "strings.cfg" file and the "strings.toml" file from the previous step.

Add "strings.cfg" and "strings.toml" to the project as Embedded Resource. You can use the IDE for this or edit the project file as text and add these lines:

```xml
<ItemGroup>
    <EmbeddedResource Include="strings.cfg" />
    <EmbeddedResource Include="strings.toml" />
</ItemGroup>
```

Alternatively, if you plan to load translations from the disk, you can both files as Content and specify that they should be copied to the output directory:

```xml
<Content Include="strings.cfg">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
<Content Include="strings.toml">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
```

But then, you will need to set <xref:Tlumach.TranslationManager.LoadFromDisk> property to true (you will create an instance of <xref:Tlumach.TranslationManager> in your code as described below).

**5. Initialize parsers**

Before you can load a configuration and create a translation manager, you need to initialize the parsers for the formats you are using.
In our sample, we use [INI format](files-formats.md) for settings and TOML format for translations. So, you need to call the Use method of those two parsers:

```c#
IniParser.Use();
TomlParser.Use();
```

**6. Create TranslationManager**

If files are included as resources, just call <xref uid="Tlumach.TranslationManager.#ctor(System.Reflection.Assembly,System.String)">:

```c#
using System.Reflection;
...
TranslationManager translationManager = new TranslationManager(Assembly.GetExecutingAssembly(), "strings.cfg");
```

If files are located on the disk, use <xref uid="Tlumach.TranslationManager.#ctor(System.String)">:

```c#
TranslationManager translationManager = new TranslationManager("strings.cfg");
translationManager.LoadFromDisk = true;
```

**7. Use translations in your code**

The "hello" string from "string.toml" can be loaded via <xref:Tlumach.TranslationManager.GetValue(System.String)>:


```c#
string helloValue = translationManager.GetValue("hello").Text;
```

To access the translation for a specific culture (e.g., in a server application), use the <xref:Tlumach.TranslationManager.GetValue(System.String,System.Globalization.CultureInfo)> overload:

```c#
string helloValue = translationManager.GetValue("hello", new CultureInfo("de-DE")).Text;
```

To switch current language (the one used for the <xref:Tlumach.TranslationManager.GetValue(System.String)> method), assign a new value to <xref:Tlumach.TranslationManager.CurrentCulture>:

```c#
CultureInfo deCulture = new CultureInfo("de-DE");
translationManager.CurrentCulture = deCulture;
```

Remember that you need [locale-specific files](glossary.md#LocaleSpecificFile) for other languages. For this, read about [Translation Files and Formats](files-formats.md)
