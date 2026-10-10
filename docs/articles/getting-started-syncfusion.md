# Getting Started

## Integration with Syncfusion Blazor

[Syncfusion Blazor](https://www.syncfusion.com/blazor-components) components contain built-in texts: the pager of the grid, the filter menus, the search box, the text of an empty grid, the "Today"
button of the date picker, and about 3,000 more. Tlumach provides these texts from an ordinary translation, in the language of each user of a Blazor application, and Syncfusion shows its English
text for every key that the translation lacks. This article sets the integration up step by step in a Blazor Web App with Interactive Server rendering, using the sample
`samples/Tlumach.Sample.Syncfusion` as the example. The details are in [Localization of Syncfusion Blazor](component-suites-syncfusion.md).

The integration is in the `Tlumach.Syncfusion.Blazor` assembly (.NET 9 and .NET 10), namespace `Tlumach.Syncfusion.Blazor`, which is shipped as the separate package
`AlliedBits.Tlumach.Syncfusion.Blazor`. The package requires `Syncfusion.Blazor.Core` 29.1.33 or later and brings `Syncfusion.Blazor.Core`, `AlliedBits.Tlumach.Blazor`, and the core
`AlliedBits.Tlumach` package with it. It builds on [Tlumach.Blazor](getting-started-blazor.md), whose per-user culture it follows; this article assumes that you know the basics of Tlumach.Blazor.

The license of Syncfusion components is not affected: register the license key of Syncfusion as usual. Building and testing work without a key; at run time, the components show a trial banner
without one.

### 1. Add Tlumach to your project

a) via NuGet

Add package references to "AlliedBits.Tlumach.Syncfusion.Blazor" and, for request localization and the culture endpoint on the server, "AlliedBits.Tlumach.AspNetCore" to your project. The
components themselves are in the packages of Syncfusion, e.g. `Syncfusion.Blazor.Grid` for the grid, and the themes are in `Syncfusion.Blazor.Themes`:

```cmd
dotnet add package AlliedBits.Tlumach.Syncfusion.Blazor
dotnet add package AlliedBits.Tlumach.AspNetCore
dotnet add package Syncfusion.Blazor.Grid
dotnet add package Syncfusion.Blazor.Themes
```

or, in the project file, add the packages of Tlumach:

```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.Syncfusion.Blazor" Version="2.*" />
    <PackageReference Include="AlliedBits.Tlumach.AspNetCore" Version="2.*" />
</ItemGroup>
```

together with the packages of Syncfusion, all of one version.

b) with Source Code

Add the _Tlumach.Base_, _Tlumach_, _Tlumach.Web_, _Tlumach.Extensions.Localization_, _Tlumach.Blazor_, _Tlumach.AspNetCore_, and _Tlumach.Syncfusion.Blazor_ projects to your solution and
reference them from your project, and reference _Tlumach.Generator_ as an analyzer, as shown in [Generator](generator.md).

### 2. Translations

Create a translation project with Generator as described in [Generator](generator.md), or keep the translations in the application project itself, as the sample does. The configuration file, here
"Translations/Strings.cfg":

```ini
defaultFile=Strings.json
defaultLocale=en
generatedNamespace=MyApp.Translations
generatedClass=Strings
textProcessingMode=DotNet

[translations]
de=Strings_de.json
```

Add the files to the project:

```xml
<ItemGroup>
    <AdditionalFiles Include="Translations\Strings.cfg" />
    <EmbeddedResource Include="Translations\Strings.json" />
    <EmbeddedResource Include="Translations\Strings_de.json" />
</ItemGroup>
```

When the translation files are embedded resources, they are looked up as `<AssemblyName>.<path>`; leave `RootNamespace` unset or set it to the name of the assembly.

The keys of Syncfusion live in the `Syncfusion` group of the translation. "Strings.json" has only the texts of the application, so Syncfusion shows its English texts for English users:

```json
{
    "Title": "Customers",
    "ColumnName": "Name"
}
```

"Strings_de.json" translates a few keys of Syncfusion, too:

```json
{
    "Title": "Kunden",
    "ColumnName": "Name",
    "Syncfusion": {
        "Grid_EmptyRecord": "Keine Datensätze vorhanden",
        "Pager_CurrentPageInfo": "Seite {0} von {1}",
        "Pager_TotalItemsInfo": "({0} Einträge)"
    }
}
```

The texts keep the placeholders that Syncfusion fills itself, such as `{0}`. See [Keys and Translations](component-suites-syncfusion.md#keys-and-translations) for where to find the keys, and
[The Official Translations of Syncfusion](component-suites-syncfusion.md#the-official-translations-of-syncfusion) for a second translation set, made of the official .resx files of Syncfusion,
that provides the texts which the application's translation lacks.

### 3. Registration

`Program.cs`:

```csharp
using System.Globalization;
using MyApp.Translations;
using Syncfusion.Blazor;
using Syncfusion.Licensing;
using Tlumach.AspNetCore;
using Tlumach.Blazor;
using Tlumach.Syncfusion.Blazor;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// A license key from user secrets or the environment variable Syncfusion__LicenseKey.
string? licenseKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(licenseKey))
    SyncfusionLicenseProvider.RegisterLicense(licenseKey);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSyncfusionBlazor();
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE")];
    options.DefaultCulture = options.SupportedCultures[0];
    options.DefaultManager = Strings.TranslationManager;
    options.Persistence = TlumachCulturePersistence.Cookie;
});
builder.Services.AddTlumachSyncfusionBlazor();

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();   // before the endpoints
app.UseAntiforgery();
app.MapStaticAssets();
app.MapTlumachCultureEndpoint();       // "/tlumach/culture" by default
app.MapRazorComponents<MyApp.Components.App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
```

`AddTlumachSyncfusionBlazor` replaces the localizer that `AddSyncfusionBlazor` registers, so Syncfusion takes its texts from Tlumach. Without options, it uses the `DefaultManager` of
`AddTlumachBlazor` and the `Syncfusion` group; see [Registration](component-suites-syncfusion.md#registration) for the options. The order of the calls does not matter. Do not register another
`ISyncfusionStringLocalizer`, such as the `SyncfusionLocalizer` class of the Syncfusion documentation: the last registration wins. In a Blazor Web App with WebAssembly components, call
`AddTlumachSyncfusionBlazor` in the client project, too, as with `AddTlumachBlazor` (see [Getting Started for integration with Blazor](getting-started-blazor.md)).

### 4. App, imports, and layout

Syncfusion components need an interactive render mode for their popups, which include the filter menus of the grid and the calendar of the date picker. In `Components/App.razor`, make the routes
interactive, render the culture into the `lang` attribute, and add a theme and the script of Syncfusion:

```razor
<!DOCTYPE html>
<html lang="@CultureInfo.CurrentUICulture.Name">
<head>
    <meta charset="utf-8" />
    <base href="/" />
    <link href="@Assets["_content/Syncfusion.Blazor.Themes/bootstrap5.css"]" rel="stylesheet" />
    <ImportMap />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src="@Assets["_content/Syncfusion.Blazor.Core/scripts/syncfusion-blazor.min.js"]" type="text/javascript"></script>
</body>
</html>
```

`Components/_Imports.razor`:

```razor
@using System.Globalization
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using global::Syncfusion.Blazor
@using global::Syncfusion.Blazor.Grids
@using Tlumach.Blazor
@using MyApp.Translations
```

`Components/Layout/MainLayout.razor` adds the culture selector of Tlumach.Blazor:

```razor
@inherits LayoutComponentBase

<TlumachCultureSelector ForceReload="true" />
@Body
```

`ForceReload="true"` reloads the page in the new language. Syncfusion formats numbers and dates with the culture of the thread, and a few components read some texts once, when they are created, so
Syncfusion recommends a reload after the culture changes, and so does Tlumach (see [Switching the Language](component-suites-syncfusion.md#switching-the-language)).

### 5. A page

`Components/Pages/Home.razor`:

```razor
@page "/"
@inherits TlumachComponentBase

<h1>@T(Strings.Title)</h1>

<SfGrid TValue="Person" DataSource="@People" AllowPaging="true">
    <GridColumns>
        <GridColumn Field="@nameof(Person.Name)" HeaderText="@T(Strings.ColumnName)" />
    </GridColumns>
</SfGrid>

<SfGrid TValue="Person" DataSource="@NoPeople">
    <GridColumns>
        <GridColumn Field="@nameof(Person.Name)" HeaderText="@T(Strings.ColumnName)" />
    </GridColumns>
</SfGrid>

@code {
    public sealed record Person(string Name);

    private static readonly List<Person> People = [new("Ada"), new("Grace"), new("Linus")];
    private static readonly List<Person> NoPeople = [];
}
```

In German, the pager of the first grid shows "Seite 1 von 1" and "(3 Einträge)", and the second grid shows "Keine Datensätze vorhanden"; the texts that the translation lacks stay in English.

### 6. Try it

Start the application, open the home page, and switch the language with the selector. To store a license key for development, run `dotnet user-secrets init` and then
`dotnet user-secrets set Syncfusion:LicenseKey <key>` in the folder of the project.

### Where next

* [Localization of Syncfusion Blazor](component-suites-syncfusion.md): the options, the keys, the official translations of Syncfusion as a fallback, how a text is found, the culture in each render
  mode, switching the language, and trimming.
* [Getting Started for integration with Blazor](getting-started-blazor.md): the culture of each user, persistence, WebAssembly, and Blazor Hybrid.
* The sample `samples/Tlumach.Sample.Syncfusion` shows a grid with paging, a filter menu, and a search box, an empty grid, a date picker, and a numeric text box in English, German, and Ukrainian.
