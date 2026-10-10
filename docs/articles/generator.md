# Generator

Tlumach.NET includes an analyzer that loads the [translation set](glossary.md) and creates C# code for a static class. This static class contains an instance of <xref:Tlumach.TranslationManager>, an instance of <xref:Tlumach.Base.TranslationConfiguration>, instances of <xref:Tlumach.TranslationUnit> classes, and string constants for keys of translation units.

## When Generator is Needed

You need Generator if you want to bind XAML elements to [generated translation units](glossary.md#GeneratedUnit) so that the text of your UI is fetched from translation files and updated automatically when you change the current locale in <xref:Tlumach.TranslationManager>. You can also use generated translation units from code - using them instead of retrieving text strings by key ensures that you don't make a typo in the key.

Another case when you may need Generator is when you want to access translated strings in one of lower-level keys but don't want to use strings for keys (e.g., when using [Dependency Injection](di.md)). For this, you can configure Generator to generate only string constants for keys (this is done using the "onlyDeclareKeys" setting in the [configuration file](config-file.md)), and then you use the generated string constants to get syntax checking of your use of the keys.

## How Generator Works

Generator is called by the compiler toolchain for the files marked as Additional Files in your project. Generator creates the auto-generated source code, which is then built by the toolchain as a part of the project. The compiled project gets referenced by your main projects.

For this scheme to work, you need a Translation Project.

Generator runs with version 4.8 or later of the C# compiler, i.e. with the .NET 8 SDK or later and with Visual Studio 2022 17.8 or later, including the compiler that Visual Studio uses for projects in the old (non-SDK) format on .NET Framework. The code that it generates compiles with C# 7.3, the default language version of .NET Framework and .NET Standard 2.0 projects, and later versions.

<a name="TranslationProject"></a>
## Translation Project Setup

To set up a translation project that Generator will use, take the following steps:
1. Create a project with translations and reference it from your main application project(s). This created project does not need to include any source code, its purpose is to be separate from the projects that access the generated code.
2. Include the [configuration file](config-file.md) to this project with translations. This project may also contain translation files as resources, but this is not required - Generator will pick the default file from the disk, and locale-specific files are needed only in runtime.

Visual Studio IDE will pick your configuration file and make it a part of the project automatically; you just need to set the "Build Action" property of this file to "C# Analyzer additional file".

If you manage the project via a text editor (e.g. in VS Code), add the configuration file to your project as follows:

```xml
<ItemGroup>
    <AdditionalFiles Include="Sample.cfg" />
</ItemGroup>
```

This will let Generator see this file as a configuration file.

### Set Options

The configuration file must include `generatedNamespace` and `generatedClass` settings. These define the name and the namespace of the class that gets generated.
These names are the ones you will use in your .NET application to access the [generated translation units](glossary.md).

Most options of the [configuration file](config-file.md) that control generation can also be set as an MSBuild property, whose name is the name of the option with a `TlumachGenerator` prefix (the properties are listed below; `onlyDeclareKeys` has no such property and can only be set in the configuration file). A property
has to be made visible to the compiler explicitly, because the NuGet package ships no .props file:

```xml
<PropertyGroup>
    <TlumachGeneratorCreateStringAccessors>true</TlumachGeneratorCreateStringAccessors>
</PropertyGroup>
<ItemGroup>
    <CompilerVisibleProperty Include="TlumachGeneratorCreateStringAccessors" />
</ItemGroup>
```

The property and the option of the configuration file are combined with a logical or: the configuration file can switch an option on, never off. The available properties are
`TlumachGeneratorUsingNamespace`, `TlumachGeneratorExtraParsers`, `TlumachGeneratorDelayedUnitCreation`, `TlumachGeneratorCreateFilledMethods`, `TlumachGeneratorUseContextCulture`, `TlumachGeneratorCreateStringAccessors`,
`TlumachGeneratorStringAccessorsClass`, `TlumachGeneratorStringAccessorsCulture`.

Setting `TlumachGeneratorUseContextCulture` to `true` makes the static constructor of the generated class set the <xref:Tlumach.TranslationManager.UseContextCulture> property of its `TranslationManager` instance to `true`, so the generated translation units read the ambient culture of the thread instead of the culture explicitly set on the manager.

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
    <PackageReference Include="AlliedBits.Tlumach" Version="2.*" />
</ItemGroup>
```

For WinUI (and Uno Platform), UWP, and Avalonia, reference the integration package of the platform instead, as described below: the class generated for these platforms uses the `TranslationUnit` class of the integration. Every Tlumach package passes Generator on to the projects that reference it, so the integration package brings both the core package and Generator.

## Diagnostics

When Generator fails to process a configuration file, it reports the error **TLUMACHGEN001** ("Failed while processing _file_: _message_") and generates no code for that configuration file. This happens, for example, when the configuration file cannot be loaded, when the default file referenced by it is missing or cannot be parsed, or when the configuration file does not contain the `generatedNamespace` or `generatedClass` setting. When the failure is a parse error with a known position, the diagnostic points to the file and the position of the error (in the default file or in the configuration file); otherwise, it points to the configuration file.

## Framework-specific Configuration

The following instructions apply for the case when you want to use XAML bindings to generated translation units. If you access the generated translation units only from your code, then the additional steps below are not necessary.

### WPF (Windows Presentation Foundation)

If you reference Tlumach source code, add _Tlumach.WPF.csproj_ to your solution and add the following lines to the project with translations:

```xml
<ItemGroup>
    <!-- This is needed only when you reference Tlumach source code -->
    <ProjectReference Include="Tlumach\src\Tlumach.WPF\Tlumach.WPF.csproj" />
</ItemGroup>
```

### UWP

Tell Generator to specify the namespace of the TranslationUnit class: you need the class from the _Tlumach.UWP_ assembly rather than from _Tlumach_ assembly. For this, add the following to your project with translations:

```xml
<PropertyGroup>
    <TlumachGeneratorUsingNamespace>Tlumach.UWP</TlumachGeneratorUsingNamespace>
</PropertyGroup>
<ItemGroup>
    <!-- Makes the property visible to analyzers/generators -->
    <CompilerVisibleProperty Include="TlumachGeneratorUsingNamespace" />
</ItemGroup>
<ItemGroup>
    <!-- With NuGet: the integration package, which brings the core package and Generator with it -->
    <PackageReference Include="AlliedBits.Tlumach.UWP" Version="2.*" />
    <!-- Or, if you reference Tlumach source code, a reference to the project instead:
    <ProjectReference Include="Tlumach\src\Tlumach.UWP\Tlumach.UWP.csproj" />
    -->
</ItemGroup>
```

The project with translations must then target `net9.0-windows10.0.17763.0` or later; _Tlumach.UWP_ is not built for .NET Standard.

If you reference Tlumach source code, also add _Tlumach.UWP.csproj_ to your solution.

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
    <!-- With NuGet: the integration package, which brings the core package and Generator with it -->
    <PackageReference Include="AlliedBits.Tlumach.WinUI" Version="2.*" />
    <!-- Or, if you reference Tlumach source code, a reference to the project instead:
    <ProjectReference Include="Tlumach\src\Tlumach.WinUI\Tlumach.WinUI.csproj" />
    -->
</ItemGroup>
```

The project with translations must then target `net9.0` or later; _Tlumach.WinUI_ is not built for .NET Standard.

If you reference Tlumach source code, also add _Tlumach.WinUI.csproj_ to your solution.

### MAUI

If you reference Tlumach source code, add _Tlumach.MAUI.csproj_ to your solution and add the following lines to the project with translations:

```xml
<ItemGroup>
    <!-- This is needed only when you reference Tlumach source code -->
    <ProjectReference Include="Tlumach\src\Tlumach.MAUI\Tlumach.MAUI.csproj" />
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
    <!-- With NuGet: the integration package, which brings the core package and Generator with it -->
    <PackageReference Include="AlliedBits.Tlumach.Avalonia" Version="2.*" />
    <!-- Or, if you reference Tlumach source code, a reference to the project instead:
    <ProjectReference Include="Tlumach\src\Tlumach.Avalonia\Tlumach.Avalonia.csproj" />
    -->
</ItemGroup>
```

The project with translations must then target `net9.0` or later; _Tlumach.Avalonia_ is not built for .NET Standard.

If you reference Tlumach source code, also add _Tlumach.Avalonia.csproj_ to your solution.
