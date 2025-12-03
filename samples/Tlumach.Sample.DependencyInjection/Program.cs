using System.Globalization;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

using Tlumach;
using Tlumach.Base;
using Tlumach.Sample;
using Tlumach.Extensions.Localization;

namespace Tlumach.Sample.DependencyInjection;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Initialize the parsers, because in this sample, the parsers are not initialized automatically.
        ArbParser.Use();
        TomlParser.Use();

        TranslationManager manager = new TranslationManager(new TranslationConfiguration(Assembly.GetExecutingAssembly(), "sample.cfg", null, TextFormat.Arb));

        // Just to have a predictable culture in the sample.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
                services.AddTlumachLocalization(
                    options => { options.TranslationManager = manager; },
                    // This is only for illustration - if you comment out the assignment of TranslationManager, Tlumach will reach the Strings class and take TranslationManager from there.
                    provider => provider.AddContext("Tlumach.Sample.Strings", new TlumachLocalizationOptions() { /*TranslationManager = Strings.TranslationManager,*/ })
                )
            )
            .Build();

        // You can use untyped localizer
        var genericLocalizer = host.Services.GetRequiredService<IStringLocalizer>();
        Console.WriteLine(genericLocalizer["HelloName", "John Doe"]);

        // Typed localizer. It is created using the options, defined above.
        var localizer = host.Services.GetRequiredService<IStringLocalizer<Strings>>();
        Console.WriteLine(localizer["Welcome"]);

        await host.StopAsync();
    }
}
