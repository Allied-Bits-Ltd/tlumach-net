# Getting Started

## Use via Dependency Injection

**1. Add Tlumach to your project**:

a) via NuGet

Add a package reference to "AlliedBits.Tlumach.Extensions.Localization" to your project (it brings the core package, "AlliedBits.Tlumach", with it)

* via NuGet package manager GUI in Visual Studio

* via the command line:

```cmd
dotnet add package AlliedBits.Tlumach.Extensions.Localization
```

* using the text editor - add the following reference to your project:
```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.Extensions.Localization" Version="2.*" />
</ItemGroup>
```

b) with Source Code

- Check out Tlumach from the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net)
- Add _Tlumach.Base_, _Tlumach_, and _Tlumach.Extensions.Localization_ projects to your solution and reference them from your project(s).

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

But then, you will need to set <xref:Tlumach.Base.BaseTranslationManager.LoadFromDisk> property to true (you will create an instance of <xref:Tlumach.TranslationManager> in your code as described below).

**5. Initialize parsers**

Before you can load a configuration and create a translation manager, you need to initialize the parsers for the formats you are using.
In our sample, we use [INI format](files-formats.md) for settings and TOML format for translations. So, you need to call the Use method of those two parsers:

```c#
using Tlumach.Base;
...
IniParser.Use();
TomlParser.Use();
```

**6. Setup Dependency Injection**

```c#
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Tlumach;
using Tlumach.Extensions.Localization;
...
var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
                services.AddTlumachLocalization(
                    // These are the default settings. They are used when you request IStringLocalizer without context.
                    options => 
                        { 
                            options.DefaultFile = "strings.toml";
                        }
                )
            )
            .Build();
```

<xref:Tlumach.Extensions.Localization.TlumachLocalizationOptions.DefaultFile> is the name of the default translation file (not of the configuration file). The file is loaded from the embedded resources of the assembly set in <xref:Tlumach.Extensions.Localization.TlumachLocalizationOptions.Assembly> or, when that property is not set, of the application. To use the settings of "strings.cfg" instead, create an instance of <xref:Tlumach.TranslationManager> with `new TranslationManager(typeof(Program).Assembly, "strings.cfg")` and assign it to the <xref:Tlumach.Extensions.Localization.TlumachLocalizationOptions.TranslationManager> property as shown below.

If files are located on the disk, the procedure is a bit more complicated - you need to create an instance of <xref:Tlumach.TranslationManager> class and use it for initialization:

```c#
TranslationManager manager = new TranslationManager("strings.cfg");
manager.LoadFromDisk = true;
...
var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
                services.AddTlumachLocalization(
                    // These are the default settings. They are used when you request IStringLocalizer without context.
                    options => 
                        { 
                            options.TranslationManager = manager; 
                        }
                )
            )
            .Build();

```

**7. Use translations in your code**

The "hello" string from "strings.toml" can be loaded via DI as follows:


```c#
var genericLocalizer = host.Services.GetRequiredService<IStringLocalizer>();
string helloValue = genericLocalizer["hello"];
```

If you used [Generator](generator.md) in your project, you can access strings with syntax checking: 

```c#
var genericLocalizer = host.Services.GetRequiredService<IStringLocalizer>();
string helloValue = genericLocalizer[Strings.helloKey];
```

`Strings.helloKey` is a string constant with the key "hello" (Generator names the members after the keys, keeping their case). Generator also creates `Strings.hello`, an instance of <xref:Tlumach.TranslationUnit>. Do not pass the unit to a localizer: its implicit conversion to string yields the translated text and not the key (see [Using Tlumach via Dependency Injection](di.md)). Read the text from the unit directly with <xref:Tlumach.TranslationUnit.CurrentValue> instead. You can disable generation of <xref:Tlumach.TranslationUnit>, if you don't need them, via a [configuration file](config-file.md).

DI classes return the string that corresponds to the culture defined in `CultureInfo.CurrentCulture` property.

To access the translation for a specific culture (e.g., in a server application), retrieve an instance of IStringLocalizer for that locale:

```c#
var genericLocalizer = ((TlumachStringLocalizer)host.Services.GetRequiredService<IStringLocalizer>()).WithCulture(new CultureInfo("de-DE"));
string helloValue = genericLocalizer["hello"];
```

The cast works when `IStringLocalizer` is registered by `AddTlumachLocalization` alone. `AddTlumachBlazor` of Tlumach.Blazor replaces this registration with its own per-user localizer, which is not a <xref:Tlumach.Extensions.Localization.TlumachStringLocalizer>, so the cast throws `InvalidCastException` there; in Blazor, that localizer already returns the strings in the culture of each user (see [Getting Started for integration with Blazor](getting-started-blazor.md)).

Remember that you need [locale-specific files](glossary.md#LocaleSpecificFile) for other languages. For this, read about [Translation Files and Formats](files-formats.md)
