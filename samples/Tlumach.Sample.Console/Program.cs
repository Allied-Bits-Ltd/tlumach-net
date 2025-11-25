using System.Globalization;

using Tlumach;
using Tlumach.Sample;

Console.WriteLine("Tlumach sample\n");

IList<string> culturesInConfig = Strings.TranslationManager.ListCulturesInConfiguration();

Console.WriteLine("Cultures declared in the configuration files: ");
foreach (var cultureInConf in culturesInConfig)
{
    Console.WriteLine(cultureInConf);
}

Console.WriteLine();

// This is how you can enumerate the files in resources or on the disk and obtain the cultures when the config file does not specify translations explicitly.
// This approach is useful for files on the disk, when you want to let users add translations for new languages by putting these translations to some disk directory.
IList<string> filesInResources = Strings.TranslationManager.ListTranslationFiles(typeof(Strings).Assembly, Strings.TranslationManager.DefaultConfiguration?.DefaultFile ?? "strings.arb");

Console.WriteLine("Files in resources: ");
foreach (var fileInRes in filesInResources)
{
    Console.WriteLine(fileInRes);
}

Console.WriteLine();

IList<CultureInfo> culturesInResources = TranslationManager.ListCultures(filesInResources);

Console.WriteLine("Cultures of files in resources: ");
foreach (var cultureInRes in culturesInResources)
{
    Console.WriteLine(cultureInRes.DisplayName);
}

Console.WriteLine();

Console.WriteLine("Hello in default culture: ");
Console.WriteLine(Strings.Hello.CurrentValue);

Console.WriteLine("Hello in specific culture (de-DE): ");
Console.WriteLine(Strings.Hello.GetValue(new CultureInfo("de-DE")));

Console.WriteLine("Switching the default culture...");
Strings.TranslationManager.CurrentCulture = new CultureInfo("hr");

Console.WriteLine($"Hello in current culture ({Strings.TranslationManager.CurrentCulture.Name}): ");
Console.WriteLine(Strings.Hello.CurrentValue);

Console.WriteLine();
