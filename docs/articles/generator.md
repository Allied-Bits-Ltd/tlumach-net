# Generator

Tlumach.NET includes an analyzer that loads the [translation set](glossary.md) and creates C# code for a static class. This class contains an instance of <xref:Tlumach.TranslationManager>, an instance of <xref:Tlumach.Base.TranslationConfiguration>, and instances of <xref:Tlumach.TranslationUnit> and <xref:Tlumach.TemplatedTranslationUnit> classes.

## When Generator is Needed

You need Generator if you want to bind XAML elements to [generated translation units](glossary.md#GeneratedUnit) so that the text of your UI is fetched from translation files and updated automatically when you change the current locale in <xref:Tlumach.TranslationManager>. You can also use generated translation units from code - using them instead of retrieving text strings by key ensures that you don't make a typo in the key.

<a name="TranslationProject"></a>
## Project Setup

For Generator to work, you must take the following steps:
1. Create a project with translations and reference it from your main application project(s). This created project does not need to include any source code, its purpose is to be separate from the projects that access the generated code.
2. Include the [configuration file](config-file.md) to this project with translations. This project may also contain translation files as resources, but this is not required - Generator will pick the default file from the disk, and locale-specific files are needed only in runtime.

Visual Studio IDE will pick your configuration file and make it a part of the project automatically; you just need to set the "Build Action" property of this file to "C# Analyzer additional file".

If you manage the project via a text editor (e.g. in VS Code), add the configuration file to your project as follows:

```xml
<ItemGroup>
    <AdditionalFiles Include="Sample.cfg" />
</ItemGroup>
```

and in the properties of this file in the Visual Studio IDE, set "Build Action" to "C# Analyzer additional file".

This will let Generator see this file as configuration.

### Set Options

The configuration file must include `generatedNamespace` and `generatedClass` settings. These define the name and the namespace of the class that gets generated.
These names are the ones you will use in your .NET application to access the [generated translation units](glossary.md).

### Reference the Generator project

If you reference Tlumach source code, add the following references to your project with translations:

```xml
<ItemGroup>
    <ProjectReference Include="Tlumach\src\Tlumach.Generator\Tlumach.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <ProjectReference Include="Tlumach\src\Tlumach.Base\Tlumach.Base.csproj" />
    <ProjectReference Include="Tlumach\src\Tlumach\Tlumach.csproj" />
</ItemGroup>
```

and include the mentioned projects to your solution. Do not reference _Tlumach.Generator_ project in your main projects - it is only referenced from the project with translations.

If you reference a NuGet package, add the following package reference to your project with translations:

```xml
<ItemGroup>
    <PackageReference Include="Tlumach" Version="1.*" />
</ItemGroup>
```

## Framework-specific Configuration

The following instructions apply for the case when you want to use XAML bindings to generated translation units. If you access the generated translation units only from your code, then the additional steps below are not necessary.

### WPF (Windows Presentation Foundation)

If you reference Tlumach source code, add _Tlumach.WPF.csproj_ to your solution and add the following lines to the project with translations:

<ItemGroup>
    <!-- This is needed only when you reference Tlumach source code -->
    <ProjectReference Include="Tlumach\src\Tlumach.WinUI\Tlumach.WPF.csproj" />
</ItemGroup>
```

### WinUI

Tell Generator to specify the namespace of the TranslationUnit class: you need the class from the _Tlumach.WinUI_ assembly rather than from _Tlumach_ assembly. For this, add the following to your project with translations:

```xml
<PropertyGroup>
    <TlumachGeneratorUsingNamespace>Tlumach.WinUI</TlumachGeneratorUsingNamespace>
</PropertyGroup>
<ItemGroup>
    <!-- Makes the property visible to analyzers/generators -->
    <CompilerVisibleProperty Include="TlumachGeneratorUsingNamespace" />
</ItemGroup>
<ItemGroup>
    <!-- This is needed only when you reference Tlumach source code -->
    <ProjectReference Include="Tlumach\src\Tlumach.WinUI\Tlumach.WinUI.csproj" />
</ItemGroup>
```

and, if you reference Tlumach source code, add _Tlumach.WinUI.csproj_ to your solution.

### MAUI

If you reference Tlumach source code, add _Tlumach.MAUI.csproj_ to your solution and add the following lines to the project with translations:

<ItemGroup>
    <!-- This is needed only when you reference Tlumach source code -->
    <ProjectReference Include="Tlumach\src\Tlumach.WinUI\Tlumach.MAUI.csproj" />
</ItemGroup>
```

### Avalonia

Tell Generator to specify the namespace for the TranslationUnit class: you need the class from the _Tlumach.Avalonia_ assembly rather than from _Tlumach_ assembly. For this, add the following to your project with translations:

```xml
<PropertyGroup>
    <TlumachGeneratorUsingNamespace>Tlumach.Avalonia</TlumachGeneratorUsingNamespace>
</PropertyGroup>
<ItemGroup>
    <!-- Makes the property visible to analyzers/generators -->
    <CompilerVisibleProperty Include="TlumachGeneratorUsingNamespace" />
</ItemGroup>
<ItemGroup>
    <!-- This is needed only when you reference Tlumach source code -->
    <ProjectReference Include="Tlumach\src\Tlumach.WinUI\Tlumach.Avalonia.csproj" />
</ItemGroup>
```
and, if you reference Tlumach source code, add _Tlumach.Avalonia.csproj_ to your solution.
