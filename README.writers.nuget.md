AlliedBits.Tlumach.Writers is a supplementary package for [Tlumach.NET](https://github.com/Allied-Bits-Ltd/tlumach-net) that contains the translation file writer classes (the `Tlumach.Writers` namespace). These classes can be used to export translations and convert them between translation file formats: JSON, ARB, INI, TOML, CSV, TSV, ResX, XLIFF, and Apple String Catalog (.xcstrings). They are also the basis for the [Tlumach Tools](https://github.com/Allied-Bits-Ltd/tlumach-tools) utility.

## Supported platforms and frameworks

* .NET 10.0 (a dedicated set of assemblies is provided)
* .NET 9.0 (a dedicated set of assemblies is provided)
* Anything that loads .NET Standard 2.0 assemblies

...on any operating system capable of running .NET 9+ or loading .NET Standard 2.0 assemblies.

## Documentation

Tlumach documentation can be conveniently read in the [Tlumach section of Allied Bits site](https://alliedbits.com/tlumach).
The source files of the documentation are available in the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net), in the "docs" directory.

## Help and support

For general discussions and suggestions, you are welcome to use the [Discussions section](https://github.com/Allied-Bits-Ltd/tlumach-net/discussions).

If you need help with issues or want to report a bug, [please open an issue](https://github.com/Allied-Bits-Ltd/tlumach-net/issues/new/choose) and include the necessary details (including the relevant configuration and translation files) to help us better understand the problem. Providing this information will aid in resolving the issue effectively.

## Getting started

A writer takes the translations from a `TranslationManager` and writes one culture to a stream. For example, to write the German translation of a generated `Strings` class to a .resx file and the default translation to a JSON file:

```csharp
using System.Globalization;
using System.IO;
using Tlumach;
using Tlumach.Writers;

TranslationManager manager = Strings.TranslationManager; // or any TranslationManager instance

var resxWriter = new ResxWriter();
using (var stream = File.Create("Strings_de.resx"))
    resxWriter.WriteTranslation(manager, new CultureInfo("de"), stream);

var jsonWriter = new JsonWriter();
using (var stream = File.Create("Strings.json"))
    jsonWriter.WriteTranslation(manager, CultureInfo.InvariantCulture, stream);
```

`WriteConfiguration` writes the configuration file of the translation set, and the CSV, TSV, and String Catalog writers can write several cultures into one file with `WriteTranslations`. See the [Writers](https://alliedbits.com/tlumach/articles/writers.html) article of the documentation for the details.

The package depends on the core [AlliedBits.Tlumach](https://www.nuget.org/packages/AlliedBits.Tlumach) package of exactly the same version. All packages of Tlumach must have the same version.
