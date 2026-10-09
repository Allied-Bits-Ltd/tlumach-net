# Localization of MudBlazor

## Overview

[MudBlazor](https://mudblazor.com) components contain built-in texts: the operators and buttons of the data grid filters ("contains", "Filter value", "Clear"), the pager ("Rows per page:", "1-10 of 32",
"Next page"), the date picker ("Previous month", "Open"), the close button of dialogs, the "Required" message of form fields, and about 150 more. MudBlazor shows them in English and lets an
application supply translations through its localization API. Tlumach can provide them from an ordinary translation, in the language of each user of a Blazor application.

The integration is shipped as the separate package `AlliedBits.Tlumach.MudBlazor`, so that MudBlazor is not forced on applications that do not use it. The package requires MudBlazor 9 and .NET 9 or
later, and it builds on [Tlumach.Blazor](getting-started-blazor.md), whose per-user culture it follows.

## Registration

Call <xref:Tlumach.MudBlazor.TlumachMudBlazorServiceCollectionExtensions.AddTlumachMudBlazor*> in the project that registers MudBlazor (the server project, the client project of a Blazor Web App, or
both, as with `AddTlumachBlazor`):

```csharp
using MudBlazor.Services;
using Tlumach.Blazor;
using Tlumach.MudBlazor;

builder.Services.AddMudServices();
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [new("en-US"), new("de-DE"), new("uk-UA")];
    options.DefaultManager = Strings.TranslationManager;
});
builder.Services.AddTlumachMudBlazor();
```

The order of the three calls does not matter. `AddTlumachMudBlazor` also calls `AddTlumachBlazor`, whose options can still be set with a call of its own. The options of
<xref:Tlumach.MudBlazor.TlumachMudBlazorOptions> are:

| Option | Default | Meaning |
|---|---|---|
| `TranslationManager` | `null` | The translation set that holds the texts of MudBlazor. `null` means `TlumachBlazorOptions.DefaultManager`. Without either, the first component that needs a text fails with an `InvalidOperationException`. |
| `Group` | `"MudBlazor"` | The group, in which the keys of MudBlazor are stored. A key is looked up as `{Group}.{key}`, or as `{key}` when the group is `null` or empty. |

## Keys and Translations

The keys are those of MudBlazor, for example `MudDataGrid_Contains`, `MudDataGridPager_RowsPerPage`, `MudDataGridPager_InfoFormat`, `MudTablePager_NextPage`, or `MudBaseDatePicker_PrevMonth`. Their
English texts are in [LanguageResource.resx](https://github.com/MudBlazor/MudBlazor/blob/dev/src/MudBlazor/Resources/LanguageResource.resx) in the source of MudBlazor, which is also the list of all keys;
MudBlazor does not expose the list as public API, and new versions add keys.

### In the Application's Translation

By default, the keys live in the `MudBlazor` group of the application's translation. In a JSON file, that is an object at the root:

```json
{
    "Title": "Kunden",
    "MudBlazor": {
        "MudDataGrid_Contains": "enthält",
        "MudDataGridPager_RowsPerPage": "Zeilen pro Seite:",
        "MudDataGridPager_InfoFormat": "{0}-{1} von {2}"
    }
}
```

Only the texts that the application uses need to be translated; for the others, MudBlazor shows its English text (see [How a Text Is Found](#how-a-text-is-found)). An English translation may contain
MudBlazor keys, too, to change the texts of MudBlazor for English users.

### In a Dedicated Set Made of .resx Files

The community project [MudBlazor.Translations](https://github.com/MudBlazor/Translations) (MIT License) maintains translations of the keys into more than 40 languages, as `LanguageResource.{culture}.resx`
files. They have the keys at the root, so they can form a dedicated translation set that the ResX parser of Tlumach reads:

1. Copy `LanguageResource.resx` from MudBlazor and the `LanguageResource.{culture}.resx` files that you need from MudBlazor.Translations into the project, for example into a `Translations` folder, and keep
   their license notices with them. Check the completeness of each language: some are complete, others are partial, and the missing texts are shown in English.
2. Add a configuration file, `Translations/MudBlazor.cfg`:

   ```ini
   defaultFile=LanguageResource.resx
   defaultLocale=en
   textProcessingMode=DotNet

   [translations]
   de=LanguageResource.de.resx
   uk=LanguageResource.uk.resx
   ```

3. Embed the files. A .resx file is compiled into a satellite assembly by default, so it must be marked as a plain resource, and its name must keep the extension:

   ```xml
   <ItemGroup>
       <EmbeddedResource Include="Translations\MudBlazor.cfg" />
       <EmbeddedResource Update="Translations\LanguageResource.resx;Translations\LanguageResource.de.resx;Translations\LanguageResource.uk.resx">
           <Type>Non-Resx</Type>
           <WithCulture>false</WithCulture>
           <LogicalName>$(AssemblyName).Translations.%(Filename)%(Extension)</LogicalName>
       </EmbeddedResource>
   </ItemGroup>
   ```

4. Load the set and register it with `Group = null`:

   ```csharp
   IniParser.Use();
   ResxParser.Use();
   TranslationManager mudBlazorTexts = new(typeof(Program).Assembly, "Translations/MudBlazor.cfg");
   builder.Services.AddSingleton(mudBlazorTexts);
   builder.Services.AddTlumachMudBlazor(options =>
   {
       options.TranslationManager = mudBlazorTexts;
       options.Group = null;
   });
   ```

The files can also be converted into the format of the application's translation with the [writers](writers.md) and moved into its `MudBlazor` group.

Do not call `AddMudTranslations()` of MudBlazor.Translations in the same application: it replaces the localization interceptor of MudBlazor, which turns the Tlumach integration off, and it follows the
culture of the thread instead of the culture of the user.

### Placeholders

The texts of MudBlazor are .NET composite format strings, such as `"{0}-{1} of {2}"` or `"Previous month {0}"`. The integration formats them with `string.Format` in the culture of the user, whatever
text processing mode the translation set uses, so the translations must keep the indexed placeholders. A translation that is not a valid format string for the values that MudBlazor passes is treated as
missing, and MudBlazor shows its English text instead of failing.

## How a Text Is Found

MudBlazor reads every built-in text through its localization interceptor. `AddTlumachMudBlazor` registers <xref:Tlumach.MudBlazor.TlumachMudLocalizer> as the `MudLocalizer` of MudBlazor and replaces
the interceptor with MudBlazor's `DefaultLocalizationInterceptor` with `IgnoreDefaultEnglish` set. The default interceptor of MudBlazor shows its English texts whenever the culture of the thread is an
English one, without asking the localizer; with the setting, it asks Tlumach for every text and in every culture. For a key:

1. The text is looked up in the translation set for the culture of the user, with the usual fallback of Tlumach: the culture, its parent culture, then the default file.
2. If the key is not found, MudBlazor shows its built-in English text.

The registration uses `Replace`, so `AddTlumachMudBlazor` and `AddMudServices` can be called in any order. A later call of `AddLocalizationInterceptor` or `AddMudTranslations` replaces the interceptor and
turns the integration off.

## Culture

The texts follow `TlumachCultureState.LocalizerCulture`, the culture of the user, rather than the culture of the thread:

| Render mode | Culture |
|---|---|
| Interactive Server | The culture of the circuit. After a live switch with `SetCultureAsync`, it is the chosen culture, also in later events of the circuit, whose thread still has the culture that the circuit started with. The users of one server do not see each other's language. |
| Static server-side rendering and prerendering | The culture of the request, set by `UseTlumachRequestLocalization`. |
| Interactive WebAssembly | The culture of the application, loaded by `LoadTlumachCultureAsync`. |

MudBlazor needs an interactive render mode for its popovers, dialogs, and pickers, which includes the filter menus of the data grid and the page size selector of the pagers.

## Switching the Language Live

MudBlazor reads almost all texts while it renders, so a component shows the new language as soon as it re-renders. After `SetCultureAsync`, the components that take the cascading `TlumachCulture`
value re-render, for example a page derived from `TlumachComponentBase`, and the MudBlazor components inside them re-render with them. A page whose MudBlazor components should switch the language
live should therefore derive from `TlumachComponentBase` or declare a `[CascadingParameter] TlumachCulture` property. A popup that is open during the switch shows the new language on its next render.

A few texts are set once, when a component is created, and keep their language until the component is created again:

| Component | Texts |
|---|---|
| `MudTablePager`, `MudDataGridPager` | `RowsPerPageString` ("Rows per page:"), `AllItemsText` ("All") |
| `MudDatePicker`, `MudTimePicker`, `MudColorPicker` | `AdornmentAriaLabel` (the label of the button that opens the picker) |

To switch them, too, either set the parameters from Tlumach, for example `RowsPerPageString="@T(Strings.RowsPerPage)"` in a `TlumachComponentBase`, or switch with
`SetCultureAsync(culture, forceReload: true)`, which reloads the page in the new language.

The names of months and days in `MudDatePicker` and the formatting of numbers and dates in the data grid come from the `Culture` parameter of the components, not from the localizer. Bind it to the
culture of the user, for example `Culture="@Culture.Culture"` in a `TlumachComponentBase`.

## Namespaces

The namespace of MudBlazor is `MudBlazor`. In a project whose namespace contains a `MudBlazor` segment, such as `Contoso.MudBlazor`, `@using MudBlazor` in `_Imports.razor` refers to that namespace instead,
and the enumerations of MudBlazor (`Typo`, `Color`, ...) are not found. Write `@using global::MudBlazor` there.

## Trimming and NativeAOT

`Tlumach.MudBlazor` uses no reflection and is marked as trimming- and AOT-compatible, so it can be used in Blazor WebAssembly applications that are trimmed. Load the dedicated translation set from a
configuration file or a class created by Generator, as shown above.

## Sample

The `samples/Tlumach.Sample.MudBlazor` application (Interactive Server) shows a data grid with filter menus and a pager, a table with a pager, and a date picker in English, German, and Ukrainian, with
the texts of MudBlazor in a dedicated set made of the .resx files of MudBlazor and MudBlazor.Translations. The Ukrainian translation of the community is partial, so some texts stay in English. The
language can be switched live or with a reload.
