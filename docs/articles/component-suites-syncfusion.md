# Localization of Syncfusion Blazor

## Overview

[Syncfusion Blazor](https://www.syncfusion.com/blazor-components) components contain built-in texts: the pager of the grid ("1 of 4 pages", "Go to next page"), the filter menus
("Starts With", "Filter", "Clear"), the search box, the text of an empty grid ("No records to display"), the "Today" button of the date picker, the tooltips of the numeric
text box, and about 3,000 more. Syncfusion shows them in English and lets an application supply translations through its `ISyncfusionStringLocalizer` interface. Tlumach can
provide them from an ordinary translation, in the language of each user of a Blazor application.

The integration is shipped as the separate package `AlliedBits.Tlumach.Syncfusion.Blazor`, so that Syncfusion is not forced on applications that do not use it. The package
requires `Syncfusion.Blazor.Core` 29.1.33 or later and .NET 9 or later, and it builds on [Tlumach.Blazor](getting-started-blazor.md), whose per-user culture it follows.

## Registration

Call <xref:Tlumach.Syncfusion.Blazor.TlumachSyncfusionBlazorServiceCollectionExtensions.AddTlumachSyncfusionBlazor*> in the project that registers Syncfusion (the server project,
the client project of a Blazor Web App, or both, as with `AddTlumachBlazor`):

```csharp
using Syncfusion.Blazor;
using Tlumach.Blazor;
using Tlumach.Syncfusion.Blazor;

builder.Services.AddSyncfusionBlazor();
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [new("en-US"), new("de-DE"), new("uk-UA")];
    options.DefaultManager = Strings.TranslationManager;
});
builder.Services.AddTlumachSyncfusionBlazor();
```

The order of the three calls does not matter. `AddTlumachSyncfusionBlazor` replaces the localizer that `AddSyncfusionBlazor` registers, and it also calls `AddTlumachBlazor`,
whose options can still be set with a call of its own. Do not register another `ISyncfusionStringLocalizer`, such as the `SyncfusionLocalizer` class of the Syncfusion
documentation, in the same application: the last registration wins. The options of <xref:Tlumach.Syncfusion.Blazor.TlumachSyncfusionBlazorOptions> are:

| Option | Default | Meaning |
|---|---|---|
| `TranslationManager` | `null` | The translation set that holds the texts of Syncfusion. `null` means `TlumachBlazorOptions.DefaultManager`. Without either, the first component that needs a text fails with an `InvalidOperationException`. |
| `Group` | `"Syncfusion"` | The group, in which the keys of Syncfusion are stored. A key is looked up as `{Group}.{key}`, or as `{key}` when the group is `null` or empty. |
| `FallbackTranslationManager` | `null` | An optional second translation set, for example one made of the official `.resx` files of Syncfusion (see [The Official Translations of Syncfusion](#the-official-translations-of-syncfusion)). |
| `FallbackGroup` | `null` | The group of the keys in the second set; `null` means the root, as in the official files. |

The license of Syncfusion components is not affected: register the license key of Syncfusion as usual (`SyncfusionLicenseProvider.RegisterLicense`). Building and testing work
without a key; at run time, the components show a trial banner without one.

## Keys and Translations

The keys are those of Syncfusion, in the form `{Component}_{Text}`, for example `Grid_EmptyRecord`, `Grid_Search`, `Grid_StartsWith`, `Pager_CurrentPageInfo`,
`Pager_NextPageTooltip`, `DatePicker_Today`, or `NumericTextBox_IncrementTitle`. The English texts and the list of all keys are in
[SfResources.resx](https://github.com/syncfusion/blazor-locale/blob/master/src/SfResources.resx) in the blazor-locale repository of Syncfusion.

By default, the keys live in the `Syncfusion` group of the application's translation. In a JSON file, that is an object at the root:

```json
{
    "Title": "Kunden",
    "Syncfusion": {
        "Grid_EmptyRecord": "Keine Datensätze vorhanden",
        "Grid_Search": "Suchen",
        "Pager_CurrentPageInfo": "Seite {0} von {1}"
    }
}
```

Only the texts that the application uses need to be translated; for the others, Syncfusion shows its English text (see [How a Text Is Found](#how-a-text-is-found)). An English
translation may contain Syncfusion keys, too, to change the texts of Syncfusion for English users.

The texts contain placeholders that Syncfusion fills itself, such as `{0}` in `Pager_CurrentPageInfo` or `${count}` in some selection texts. Tlumach returns the texts unchanged,
whatever text processing mode the translation set uses, so the translations must keep the placeholders as they are.

## The Official Translations of Syncfusion

Syncfusion publishes translations of its keys into about 35 languages as `SfResources.{culture}.resx` files in the
[blazor-locale](https://github.com/syncfusion/blazor-locale) repository. They have the keys at the root, so they can form a second translation set, read by the ResX parser of
Tlumach, that provides the texts which the application's translation lacks:

1. Download `SfResources.resx` and the `SfResources.{culture}.resx` files that you need. The repository states no license, so decide for yourself whether to distribute them
   with your application. The repository can lag the package: keys that a new version of Syncfusion adds are missing from it until it is updated, and they are shown in English.
   There is no Ukrainian translation.
2. Add a configuration file next to them, for example `SyncfusionLocale/Syncfusion.cfg`:

   ```ini
   defaultFile=SfResources.resx
   defaultLocale=en
   textProcessingMode=DotNet

   [translations]
   de=SfResources.de.resx
   fr=SfResources.fr.resx
   ```

3. Load the set from disk and register it as the fallback:

   ```csharp
   IniParser.Use();
   ResxParser.Use();
   string directory = Path.Combine(builder.Environment.ContentRootPath, "SyncfusionLocale");
   TranslationManager syncfusionTexts = new(Path.Combine(directory, "Syncfusion.cfg")) { LoadFromDisk = true, TranslationsDirectory = directory };
   builder.Services.AddSingleton(syncfusionTexts);
   builder.Services.AddTlumachSyncfusionBlazor(options => options.FallbackTranslationManager = syncfusionTexts);
   ```

   To embed the files in the assembly instead, mark them as plain resources, because a .resx file is compiled into a satellite assembly by default, and keep their extension:

   ```xml
   <ItemGroup>
       <EmbeddedResource Include="SyncfusionLocale\Syncfusion.cfg" />
       <EmbeddedResource Update="SyncfusionLocale\SfResources.resx;SyncfusionLocale\SfResources.de.resx">
           <Type>Non-Resx</Type>
           <WithCulture>false</WithCulture>
           <LogicalName>$(AssemblyName).SyncfusionLocale.%(Filename)%(Extension)</LogicalName>
       </EmbeddedResource>
   </ItemGroup>
   ```

   and load them with `new TranslationManager(typeof(Program).Assembly, "SyncfusionLocale/Syncfusion.cfg")`.

The official files repeat a few keys with the same text; the ResX parser of Tlumach skips such repetitions. A set made of the official files can also be used alone, as the
primary set with `Group = null`, or converted into the format of the application's translation with the [writers](writers.md).

## How a Text Is Found

Syncfusion components ask the localizer for every built-in text. <xref:Tlumach.Syncfusion.Blazor.TlumachSyncfusionLocalizer> looks a key up in this order:

1. The application's set, in the translation for the culture of the user or its parent culture (for example `de-AT`, then `de`).
2. The fallback set, in the translation for the culture of the user or its parent culture.
3. The application's set, in its default file.
4. The fallback set, in its default file (`SfResources.resx` of the official files).
5. The English text built into Syncfusion.

An empty text counts as missing. So a German text from the official files is preferred to an English text in the default file of the application, and a key that no translation
has is shown in Syncfusion's English. The `ResourceManager` property of the localizer is `null`; Syncfusion components do not use it.

## Culture

The texts follow `TlumachCultureState.LocalizerCulture`, the culture of the user, rather than the culture of the thread:

| Render mode | Culture |
|---|---|
| Interactive Server | The culture of the circuit, or the culture chosen with `SetCultureAsync`. The users of one server do not see each other's language. |
| Static server-side rendering and prerendering | The culture of the request, set by `UseTlumachRequestLocalization`. |
| Interactive WebAssembly | The culture of the application, loaded by `LoadTlumachCultureAsync`. |

Syncfusion components need an interactive render mode for their popups, which include the filter menus of the grid and the calendar of the date picker.

## Switching the Language

Syncfusion formats numbers and dates with the culture of the thread, and a few components read some texts once, when they are created (for example the placeholders of the
date picker mask and the buttons of the date range picker). Syncfusion therefore recommends reloading the page after the culture changes, and so does Tlumach: switch with
`SetCultureAsync(culture, forceReload: true)` or `<TlumachCultureSelector ForceReload="true" />`. After the reload, `UseTlumachRequestLocalization` sets the culture of the new
circuit, and all texts and formats are in the new language.

Without a reload, the components that take the cascading `TlumachCulture` value, for example a page derived from `TlumachComponentBase`, re-render, and the Syncfusion components
inside them show the texts that they read while rendering (the empty grid, the pager, the filter menu) in the new language. Formats and the texts read at creation keep the
old language until the page is reloaded.

## Trimming and NativeAOT

`Tlumach.Syncfusion.Blazor` uses no reflection and is marked as trimming- and AOT-compatible. Syncfusion's own assemblies carry no trimming annotations; follow the Syncfusion
documentation for trimmed Blazor WebAssembly applications.

## Sample

The `samples/Tlumach.Sample.Syncfusion` application (Interactive Server) shows a grid with paging, a filter menu, and a search box, an empty grid, a date picker, and a numeric
text box in English, German, and Ukrainian. The texts of Syncfusion come from the `Syncfusion` group of its JSON translations, which translates part of the keys. The official
German translation can be downloaded into its `SyncfusionLocale` folder, as described in the `README.md` there, and then provides the German texts that the group lacks. A
Syncfusion license key can be stored with `dotnet user-secrets set Syncfusion:LicenseKey <key>` in the sample's folder; without it, the trial banner of Syncfusion is shown.
