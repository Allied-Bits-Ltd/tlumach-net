using System.Globalization;

using Tlumach;
using Tlumach.Base;

Console.WriteLine("Tlumach sample with no use of translation units\n");

IniParser.Use();
ArbParser.Use();
TomlParser.Use();
JsonParser.Use();

TranslationManager translationManager = new TranslationManager("sample.cfg");
translationManager.LoadFromDisk = true;

IList<string> culturesInConfig = translationManager.ListCulturesInConfiguration();

Console.WriteLine("Cultures declared in the configuration files: ");
foreach (var cultureInConf in culturesInConfig)
{
    Console.WriteLine(cultureInConf);
}

Console.WriteLine();

IList<string> filesOnDisk = translationManager.ListTranslationFiles(null, translationManager.DefaultConfiguration?.DefaultFile ?? "strings.arb");

Console.WriteLine("Files on the disk: ");
foreach (var fileOnDisk in filesOnDisk)
{
    Console.WriteLine(fileOnDisk);
}

Console.WriteLine();

IList<CultureInfo> culturesOnDisk = TranslationManager.ListCultures(filesOnDisk);

Console.WriteLine("Cultures of files on the disk: ");
foreach (var cultureOnDisk in culturesOnDisk)
{
    Console.WriteLine(cultureOnDisk.DisplayName);
}

Console.WriteLine();

Console.WriteLine("Hello in default culture: ");
Console.WriteLine(translationManager.GetValue("hello").Text);

Console.WriteLine("Hello in specific culture (de-DE): ");
Console.WriteLine(translationManager.GetValue("hello", new CultureInfo("de-DE")).Text);

Console.WriteLine("Switching the default culture...");
translationManager.CurrentCulture = new CultureInfo("hr");

Console.WriteLine($"Hello in current culture ({translationManager.CurrentCulture.Name}): ");
Console.WriteLine(translationManager.GetValue("hello").Text);

Console.WriteLine();
