# Getting Started

## Integration with MudBlazor

[MudBlazor](https://mudblazor.com) components contain built-in texts: the filter menus and the pager of the data grid, the pager of the table, the date picker, the close button of dialogs, and
about 150 more. Tlumach provides these texts from an ordinary translation, in the language of each user of a Blazor application, and MudBlazor shows its English text for every key that the
translation lacks. This article sets the integration up step by step in a Blazor Web App with Interactive Server rendering, using the sample `samples/Tlumach.Sample.MudBlazor` as the example. The
details are in [Localization of MudBlazor](component-suites-mudblazor.md).

The integration is in the `Tlumach.MudBlazor` assembly (.NET 9 and .NET 10), namespace `Tlumach.MudBlazor`, which is shipped as the separate package `AlliedBits.Tlumach.MudBlazor`. The package
requires MudBlazor 9 and brings `MudBlazor`, `AlliedBits.Tlumach.Blazor`, and the core `AlliedBits.Tlumach` package with it. It builds on [Tlumach.Blazor](getting-started-blazor.md), whose
per-user culture it follows; this article assumes that you know the basics of Tlumach.Blazor.

### 1. Add Tlumach to your project

a) via NuGet

Add package references to "AlliedBits.Tlumach.MudBlazor" and, for request localization and the culture endpoint on the server, "AlliedBits.Tlumach.AspNetCore" to your project:

```cmd
dotnet add package AlliedBits.Tlumach.MudBlazor
dotnet add package AlliedBits.Tlumach.AspNetCore
```

or, in the project file:

```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.MudBlazor" Version="2.*" />
    <PackageReference Include="AlliedBits.Tlumach.AspNetCore" Version="2.*" />
</ItemGroup>
```

b) with Source Code

Add the _Tlumach.Base_, _Tlumach_, _Tlumach.Web_, _Tlumach.Extensions.Localization_, _Tlumach.Blazor_, _Tlumach.AspNetCore_, and _Tlumach.MudBlazor_ projects to your solution and reference them
from your project, and reference _Tlumach.Generator_ as an analyzer, as shown in [Generator](generator.md).

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

The texts of MudBlazor are .NET composite format strings with indexed placeholders, such as `"{0}-{1} of {2}"`. The integration formats them itself, whatever text processing mode the translation
set uses, so the translations must keep these placeholders (see [Placeholders](component-suites-mudblazor.md#placeholders)).

Add the files to the project:

```xml
<ItemGroup>
    <AdditionalFiles Include="Translations\Strings.cfg" />
    <EmbeddedResource Include="Translations\Strings.json" />
    <EmbeddedResource Include="Translations\Strings_de.json" />
</ItemGroup>
```

When the translation files are embedded resources, they are looked up as `<AssemblyName>.<path>`; leave `RootNamespace` unset or set it to the name of the assembly.

The keys of MudBlazor live in the `MudBlazor` group of the translation. "Strings.json" has only the texts of the application, so MudBlazor shows its English texts for English users:

```json
{
    "Title": "Customers",
    "ColumnName": "Name"
}
```

"Strings_de.json" translates a few keys of MudBlazor, too:

```json
{
    "Title": "Kunden",
    "ColumnName": "Name",
    "MudBlazor": {
        "MudDataGrid_Contains": "enthält",
        "MudDataGridPager_RowsPerPage": "Zeilen pro Seite:",
        "MudDataGridPager_InfoFormat": "{0}-{1} von {2}"
    }
}
```

The keys are those of MudBlazor; see [Keys and Translations](component-suites-mudblazor.md#keys-and-translations) for where to find them and for a dedicated translation set made of the .resx files
of the community project MudBlazor.Translations, which translates all keys into more than 40 languages.

### 3. Registration

`Program.cs`:

```csharp
using System.Globalization;
using MudBlazor.Services;
using MyApp.Translations;
using Tlumach.AspNetCore;
using Tlumach.Blazor;
using Tlumach.MudBlazor;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE")];
    options.DefaultCulture = options.SupportedCultures[0];
    options.DefaultManager = Strings.TranslationManager;
    options.Persistence = TlumachCulturePersistence.Cookie;
});
builder.Services.AddTlumachMudBlazor();

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();   // before the endpoints
app.UseAntiforgery();
app.MapStaticAssets();
app.MapTlumachCultureEndpoint();       // "/tlumach/culture" by default
app.MapRazorComponents<MyApp.Components.App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
```

`AddTlumachMudBlazor` makes MudBlazor take its texts from Tlumach. Without options, it uses the `DefaultManager` of `AddTlumachBlazor` and the `MudBlazor` group; see
[Registration](component-suites-mudblazor.md#registration) for the options. The order of the calls does not matter. In a Blazor Web App with WebAssembly components, call `AddTlumachMudBlazor` in the
client project, too, as with `AddTlumachBlazor` (see [Getting Started for integration with Blazor](getting-started-blazor.md)).

### 4. App, imports, and layout

MudBlazor needs an interactive render mode for its popovers, dialogs, and pickers, which include the filter menus of the data grid. In `Components/App.razor`, make the routes interactive, render the
culture into the `lang` attribute, and add the files of MudBlazor:

```razor
<!DOCTYPE html>
<html lang="@CultureInfo.CurrentUICulture.Name">
<head>
    <meta charset="utf-8" />
    <base href="/" />
    <link href="@Assets["_content/MudBlazor/MudBlazor.min.css"]" rel="stylesheet" />
    <ImportMap />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
    <script src="@Assets["_content/MudBlazor/MudBlazor.min.js"]"></script>
</body>
</html>
```

`Components/_Imports.razor`:

```razor
@using System.Globalization
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using global::MudBlazor
@using Tlumach.Blazor
@using MyApp.Translations
```

Write `@using global::MudBlazor` rather than `@using MudBlazor`: in a project whose namespace contains a `MudBlazor` segment, the latter refers to that namespace (see
[Namespaces](component-suites-mudblazor.md#namespaces)).

`Components/Layout/MainLayout.razor` adds the providers of MudBlazor and the culture selector of Tlumach.Blazor:

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<TlumachCultureSelector />
@Body
```

### 5. A page

`Components/Pages/Home.razor`:

```razor
@page "/"
@inherits TlumachComponentBase

<MudText Typo="Typo.h5">@T(Strings.Title)</MudText>

<MudDataGrid T="Person" Items="@People" Filterable="true" FilterMode="DataGridFilterMode.ColumnFilterMenu" RowsPerPage="10">
    <Columns>
        <PropertyColumn Property="p => p.Name" Title="@T(Strings.ColumnName)" />
    </Columns>
    <PagerContent>
        <MudDataGridPager T="Person" />
    </PagerContent>
</MudDataGrid>

@code {
    private sealed record Person(string Name);

    private static readonly Person[] People = [new("Ada"), new("Grace"), new("Linus")];
}
```

The page derives from `TlumachComponentBase`, so it re-renders when the user switches the language, and the MudBlazor components inside it re-render with it. In German, the pager shows
"Zeilen pro Seite:" and "1-3 von 3"; the texts that the translation lacks stay in English.

### 6. Try it

Start the application, open the home page, and switch the language with the selector. A few texts, such as "Rows per page:", are set when a component is created and keep their language until the
page is reloaded; switch with `<TlumachCultureSelector ForceReload="true" />` to update them, too (see
[Switching the Language Live](component-suites-mudblazor.md#switching-the-language-live)).

### Where next

* [Localization of MudBlazor](component-suites-mudblazor.md): the options, the keys, a dedicated translation set made of .resx files, how a text is found, the culture in each render mode, live
  switching, and trimming.
* [Getting Started for integration with Blazor](getting-started-blazor.md): the culture of each user, persistence, WebAssembly, and Blazor Hybrid.
* The sample `samples/Tlumach.Sample.MudBlazor` shows a data grid, a table, and a date picker in English, German, and Ukrainian, with the texts of MudBlazor in a dedicated set made of .resx files.
