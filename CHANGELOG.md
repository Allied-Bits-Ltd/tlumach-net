# Change Log

This document provides information about the changes and new features in Tlumach.

---
Version: 1.13.0
Date: September 4, 2026

- [NEW] FluentValidation is supported through the new `AlliedBits.Tlumach.FluentValidation` package (.NET 9 and .NET 10, FluentValidation 12). `TlumachLanguageManager`, assigned to `ValidatorOptions.Global.LanguageManager` or installed with `AddTlumachFluentValidation`, takes the built-in messages of FluentValidation and the messages of error codes from a group of a Tlumach translation (`FluentValidation` by default, set by `FluentValidationGroup`), and falls back to the messages built into FluentValidation: the Tlumach text for the culture comes first, then the built-in message for the culture, then the default file of Tlumach, then the built-in English message. The placeholders of FluentValidation are kept as they are. The `WithMessage` and `WithName` extension methods take the message and the display name of a rule from a translation unit or a key and read it when the rule is validated, and `TlumachDisplayNameResolver` provides display names from a translation group. The culture follows `CurrentUICulture`, as in FluentValidation, or `TranslationManager.CurrentCulture`. A new sample, `samples/Tlumach.Sample.FluentValidation`, shows the messages in three languages. See "Localization of FluentValidation".
- [NEW] Server-side templates are supported through three new packages for .NET 9 and .NET 10: `AlliedBits.Tlumach.Scriban` (Scriban 7.2.2 or later within 7.x), `AlliedBits.Tlumach.Fluid` (Fluid 2.40 or later within 2.x, Liquid), and `AlliedBits.Tlumach.HandlebarsNet` (Handlebars.Net 2.1 or later within 2.x). Each adds `t`, which returns a translation by its key or for a translation unit with named, indexed, and ICU (`plural`, `select`) placeholder values, and `t_html` for translations that contain trusted HTML, whose values are HTML-encoded (numbers, dates, and times are formatted for the culture and not encoded). The culture comes from the template, then from the render (`PushCulture` in Scriban, `TemplateContext.CultureInfo` in Fluid, `@culture` data in Handlebars), then from the translation manager, so emails for many users can be rendered concurrently, each in its own language. The output follows the escaping of each engine and is never encoded twice, also when `WebEncodeValues` is set. A missing key returns the key by default, or an empty string, or throws, or is handled by a callback. New samples, `samples/Tlumach.Sample.Scriban`, `samples/Tlumach.Sample.Fluid`, and `samples/Tlumach.Sample.HandlebarsNet`, render an email in three languages. See "Template Engines".
- [NEW] `TemplateTranslator` in the new `Tlumach.Templating` namespace is the engine-neutral part of the template engine integrations: it resolves the culture of a call, looks up a key or a translation unit, fills the placeholders with the values of the call, encodes the values of HTML translations, and handles missing keys. It can be used to integrate other template engines.
- [NEW] `TranslationManager.GetValueWithSource` returns a translation entry together with its source (`TranslationEntrySource`): the requested culture, its basic culture, the default translation, or none.
- [FIX] With `CacheDefaultTranslations` enabled (the default), a value that a lookup had taken from the basic culture or from the default translation and cached in the translation of the requested culture was treated as the culture's own text by later lookups. `foundForCulture` was then `true` for a value from the default translation, and a lookup for several language IDs could return the value cached for an earlier language, whether taken from its basic culture or from the default translation, instead of the own text of a later one. Cached values are now recognized as such.
- [FIX] An entry supplied through the `Entry` property in an `OnTranslationValueNeeded` handler is now reported as found for the culture (`foundForCulture` is `true`), as a text supplied through the `Text` property already was.
- [NEW] Windows Forms applications are supported through the new `Tlumach.WinForms` assembly, built for .NET Framework 4.7.2, .NET 9, and .NET 10. The `TranslationProvider` component, placed on a form in the Visual Studio Designer, adds the **TranslationKey** and **ToolTipKey** properties to controls, tool strip items, and list view column headers and updates their texts when `TranslationManager.CurrentCulture` changes, also when it changes in a background thread; optionally, it switches the form to the right-to-left layout for right-to-left languages. The `BindTranslation` extension methods bind properties of components to translation units in code, including units with placeholders. The package now also has the `net472`, `net9.0-windows7.0`, and `net10.0-windows7.0` folders; the latter two are used by applications that target `net9.0-windows` or `net10.0-windows` without a Windows SDK version (the default for Windows Forms and WPF applications), which previously got the plain `net9.0`/`net10.0` assemblies and, as a result, no `Tlumach.WPF` assembly. A new sample, `samples/Tlumach.Sample.WinForms`, shows both ways of localization. See "Getting Started for integration with Windows Forms".
- [NEW] **Run Tlumach Generator (All Projects)** is now offered alongside **Run Tlumach Generator (Selected Projects)** in every Solution Explorer context menu that a selection of several nodes produces - a solution folder, two or more solution folders, two or more projects, a mix of projects and solution folders, and the solution node together with projects. Version 1.12.0 replaced the solution-wide command with the selection-scoped one in those menus, so processing the whole solution took a click on the solution node first. The selection-scoped command remains the first of the two.
- [NEW] Uno Platform applications are supported through the `Tlumach.WinUI` assembly. Uno implements the WinUI API on Android, iOS, macOS, Linux, and WebAssembly, and `Tlumach.WinUI` uses no WinUI types, so the assembly is now also built for plain `net9.0` and `net10.0` and included in the `net9.0`, `net10.0`, Android, iOS, and Mac Catalyst folders of the package. An Uno application uses `Tlumach.WinUI` exactly as a WinUI application does, including `<TlumachGeneratorUsingNamespace>Tlumach.WinUI</TlumachGeneratorUsingNamespace>` in translation projects. In XAML, Uno does not track changes in `x:Bind` paths that start with a static member, so the translation units should be bound through instance properties; the "Integration with XAML" topic explains this. A new sample, `samples/Tlumach.Sample.Uno`, targets Android, iOS, WebAssembly, Desktop (Skia), and Windows.
- [FIX] The `Tlumach.WinUI` project passed its references to the Windows App SDK and Windows SDK build tools on to the projects that reference it, which forced their version on the application. Uno.Sdk, for example, brings its own Windows App SDK version, and restoring an Uno application that referenced the `Tlumach.WinUI` project failed with a package downgrade error. The references are now private to `Tlumach.WinUI`, which does not use any Windows App SDK types. The NuGet package was not affected, as it never declared these dependencies.
- [NEW] `Tlumach.WinUI.TranslationUnit` raises its `PropertyChanged` event on the UI thread. WinUI and Uno Platform require bindings to be updated on the UI thread, so changing `TranslationManager.CurrentCulture` or calling `NotifyPlaceholdersUpdated()` from a background thread used to fail or to leave the controls unchanged. The unit captures the synchronization context of the thread on which it is created or, when that thread has none (for example, when the generated class is initialized on a worker thread), of the thread on which XAML first reads `CurrentValue`. `CurrentValue` reflects the new text immediately; only the notification is posted to the UI thread.
- [FIX] The `XamlTranslateCore` class, used by the `Translate` markup extension in MAUI, subscribed to the `OnCultureChanged` event of the previous translation manager again instead of unsubscribing from it when its `Unit` property was assigned a new value. Each reassignment left an extra handler behind, which kept the old objects alive and caused redundant UI updates on language changes.
- [NEW] Blazor applications are supported through the new `Tlumach.Blazor` assembly (.NET 9 and .NET 10), which works with Blazor Server, Blazor WebAssembly (in a Blazor Web App and standalone), static server-side rendering, and Blazor Hybrid. The scoped `TlumachCultureState` service keeps the culture of each user, so the users of a Blazor Server application no longer share the process-wide `TranslationManager.CurrentCulture`; `SetCultureAsync` switches the language live or, with `forceReload`, reloads the page in the new language. The `TlumachText` component renders a translation unit or a key, with values for named or indexed placeholders, and re-renders when the language changes; when a translation is rendered as markup (`AsMarkup`, `TlumachCulture.Markup`), the translation is trusted HTML, but string placeholder values are HTML-encoded (a `MarkupString` value inserts trusted HTML); `TlumachComponentBase` adds the `T` and `TFrom` methods for attributes and code; `TlumachCultureSelector` lets the user choose the language and works without interactivity, too (in a prerendered interactive page, it turns from the SSR form into a `select` when the page becomes interactive). Injected `IStringLocalizer` and `IStringLocalizer<T>` follow the culture of the user; `AddTlumachBlazor` registers them as scoped services and replaces the earlier registrations, including those of `AddLocalization()`, so a singleton service should not inject `IStringLocalizer<T>` (scope validation rejects it in Development); a localizer that is resolved outside a user's scope anyway follows the culture of the current request or circuit, as before. The culture is stored in the ASP.NET Core culture cookie or in the local storage of the browser (`Cookie | LocalStorage` is allowed; when the culture is loaded, the cookie store goes first, and the local storage is used only when the page has no `lang` attribute); no JavaScript file is needed. The new `TlumachCultureOptions.FindSupportedCulture(string)` overload (inherited by `TlumachBlazorOptions`) matches a culture name against `SupportedCultures` without growing the culture cache of the process: the name of a predefined culture is matched like the culture itself by the `CultureInfo` overload, and any other name with string operations only (the exact name, then the name with the trailing subtags removed, then a culture of the same language); with an empty `SupportedCultures` list, only the names of predefined cultures are accepted, and an empty name is never accepted. A new sample, `samples/Tlumach.Sample.Blazor`, shows a Blazor Web App with static, Interactive Server, and Interactive WebAssembly pages. See "Getting Started for integration with Blazor".
- [NEW] The new `Tlumach.AspNetCore` assembly (.NET 9 and .NET 10, in the `net9.0` and `net10.0` folders of the package) provides `UseTlumachRequestLocalization`, which configures the request localization of ASP.NET Core from `TlumachCultureOptions` (registered by `AddTlumachCultures` or `AddTlumachBlazor`) with the culture cookie first (set `SupportedCultures`: with an empty list, the middleware honors only the current culture of the server), and `MapTlumachCultureEndpoint`, which stores the chosen culture in the cookie. The cookie is `HttpOnly`, `SameSite=Lax`, and `Secure` on HTTPS, and it is marked as essential, because the culture is a functional preference that is written even when a cookie-consent policy has not been accepted. The `redirectUri` of the endpoint must be a local path (other values, including those with control or non-ASCII characters, lead to the root of the application), and an unsupported culture yields 400. Both GET and POST change only the culture cookie and carry no antiforgery token, by design. The assembly does not depend on `Tlumach.Blazor`, so an application without Blazor can use it. It is also included in the Windows folders of the package (`net9.0-windows7.0`, `net9.0-windows10.0.19041.0`, `net9.0-windows10.0.26100.0`, and their `net10.0` counterparts), together with `Tlumach.Web` and `Tlumach.AspNetCore.Mvc`, but not in the Android, iOS, and Mac Catalyst folders, where ASP.NET Core does not run.
- [NEW] The new `Tlumach.Web` assembly (.NET 9 and .NET 10) holds the web core that does not depend on ASP.NET Core and is safe to use in WebAssembly: `TlumachCultureOptions` (`SupportedCultures`, `DefaultCulture`, `CultureEndpoint`, `FindSupportedCulture`, `ResolveInitialCulture`), the base class of `TlumachBlazorOptions`, and `AddTlumachCultures`, which registers the options without Blazor services. `AddTlumachBlazor` and `AddTlumachCultures` can be called in any order in an application that uses both MVC and Blazor; they share one set of cultures; `TlumachWebServiceCollectionExtensions.FindRegisteredOptions` is public and returns the options instance that is registered in a service collection. `UseTlumachRequestLocalization` and `MapTlumachCultureEndpoint` of `Tlumach.AspNetCore` read `TlumachCultureOptions`. See "Getting Started for integration with Blazor".
- [NEW] ASP.NET Core MVC and Razor Pages are supported through the new `Tlumach.AspNetCore.Mvc` assembly (.NET 9 and .NET 10), which does not depend on Blazor and works with `AddControllersWithViews()` and with `AddRazorPages()` alone. `AddTlumachViewLocalization` registers Tlumach-backed `IHtmlLocalizer`, `IHtmlLocalizer<T>`, and `IViewLocalizer` (regardless of the order relative to `AddViewLocalization`): the keys of a view start with its path (`/Views/Home/Index.cshtml` has the keys `Views.Home.Index.*`) with a fallback to shared keys, the translation is trusted HTML and the values of the placeholders are encoded (`IHtmlContent` values are inserted as they are), all Tlumach placeholders and ICU `plural`/`select` work, and `TranslationManager.WebEncodeValues` is ignored, so the output is never encoded twice. The `tlumach-key` and `tlumach-unit` attributes (with `tlumach-arg-*`, `tlumach-args`, and `tlumach-culture`), the `<tlumach-text>` element, and `Html.Tlumach` for generated translation units show translations without code in a view; a key of a tag helper is also looked up with the prefix of the main view, so that it works in the `@section` blocks of a view. `<tlumach-culture-selector>` lets users choose the language without JavaScript and uses the culture endpoint. `AddTlumachModelBindingMessages` takes the 11 messages of model binding from the `ModelBinding.*` keys and falls back to the English text of MVC, and `AddTlumachDisplayNames` gives properties without `[Display]` a display name from `DisplayNames.{type}.{property}` and `DisplayNames.{property}`, with key styles (`RelativeTypeName`, `TypeName`, `FullTypeName`) that keep the `InputModel` and `CreateModel` types of scaffolded pages apart, and a `Debug` log of the keys that were tried. Two new samples, `samples/Tlumach.Sample.Mvc` and `samples/Tlumach.Sample.RazorPages` (without controllers), show the features in three languages. See "Localization of MVC and Razor Pages" and the getting-started articles for MVC and Razor Pages.
- [NEW] `TemplateTranslator.TryTranslate` and `TryTranslateMarkup` return `false` for a missing key or entry instead of applying the missing-key handling, so that integrations can handle the misses themselves (a fallback key, a message of the platform). They are used by the MVC integration.
- [NEW] The constructor of `CultureChangedEventArgs` is public, so that other code can raise culture change events with it.
- [NEW] The `TranslationManagerResolver` class of `Tlumach.Extensions.Localization` (it finds the translation manager from `TlumachLocalizationOptions` or from a class created by Tlumach Generator) and the `TlumachStringLocalizer(TranslationManager)` and `TlumachStringLocalizer(TranslationManager, TextFormat?)` constructors are public, so that other integrations (`Tlumach.AspNetCore.Mvc`) can use them.
- [FIX] `AddTlumachLocalization` replaced `IStringLocalizer` and `IStringLocalizer<T>` registrations that were made before it. It now adds its registrations only when there are none, so the culture-aware localizers of `AddTlumachBlazor` are kept regardless of the order of the calls. If `AddLocalization()` is called before `AddTlumachLocalization()`, the framework's `StringLocalizer<T>` stays registered (it still delegates to the Tlumach localizer factory).
- [FIX] `TranslationManager.TranslationManagers` returned the internal list itself, which is changed under a lock whenever a translation manager is created or disposed. Code that enumerated it, e.g. to set `CurrentCulture` on all managers, could fail with "Collection was modified" or read an inconsistent list when another thread created or disposed a manager meanwhile. The property now returns a copy taken under the same lock; the copy does not reflect managers created or disposed after the call, so read the property again to get the current set.
- [FIX] The .NET 9 and .NET 10 folders of the package, including their Windows, Android, iOS, and Mac Catalyst variants, declared no dependencies, although `Tlumach.Extensions.Localization`, `Tlumach.Web`, and the assemblies built on them reference `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Localization.Abstractions` version 10, also in their .NET 9 builds. An application that did not reference these packages itself could not use the assemblies, and in an ASP.NET Core 9 application, whose shared framework has version 9 of the abstractions, a call to `AddTlumachCultures` failed to compile with error CS1705. The package now depends on both packages (version 10.0.0 or later) in the .NET 9 and .NET 10 folders. The frameworks of the integrations (ASP.NET Core, Blazor, Avalonia, MAUI, WPF, Windows Forms, WinUI, and UWP) are still not declared, because an application that uses an integration references its framework already, and all integrations share the folders of the package. Also, `Tlumach.AspNetCore` and `Tlumach.AspNetCore.Mvc` were missing from the `net9.0-windows10.0.19041.0`, `net9.0-windows10.0.26100.0`, `net10.0-windows10.0.19041.0`, and `net10.0-windows10.0.26100.0` folders, so an ASP.NET Core application that targets a Windows version did not get them; they are now included in all Windows folders.

---
Version: 1.12.0
Date: September 3, 2026

- [NEW] The `TranslationManager` and `TranslationUnit` classes got the overloaded `GetValue` methods that accept lists of language identifiers instead of a single CultureInfo. This approach enables applications to retrieve whatever translation is available for a user's request with multiple accepted languages. This feature is the most useful when serving client requests in a web application.
- [NEW] The Visual Studio extension offers the generator commands on more Solution Explorer nodes. **Run Tlumach Generator (All Projects)** used to appear only in the context menus of the solution node and of a single project. It is now joined by a new command, **Run Tlumach Generator (Selected Projects)**, in the context menu of a solution folder and in the context menus that Visual Studio shows for a multiple selection: two or more solution folders, two or more projects, a mix of projects and solution folders, and the solution node together with projects. The new command runs the generator only for the projects that the selection covers, where a selected solution folder contributes every project nested under it, recursively, and the solution node contributes every project in the solution; a project that the selection reaches twice, through a folder and on its own, is processed once. The command is hidden when the selection covers no project the generator can run for. It is also available from **Extensions > Tlumach**, where it acts on the selection of the active window.

---
Version: 1.11.0
Date: August 20, 2026

- [NEW] The `TranslationManager` class got the `UseContextCulture` property. This property tells translation units and Translation Manager to use the culture of the current thread context rather than the fixed culture of the translation manager.
- [NEW] The Visual Studio extension contributes item templates for every Tlumach file format to the **Add New Item** dialog, under a "Tlumach" category of the C# templates. A translation file (JSON, ARB, INI, TOML, CSV, TSV, XLIFF, Apple String Catalog, ResX) is added to the project as an Embedded Resource; a configuration file (`.cfg`, `.jsoncfg`, `.tomlcfg`, `.arbcfg`, `.resxcfg`, `.xlfcfg`) is added as a C# analyzer additional file (`AdditionalFiles`). A ResX translation file is created with a duplicate `.resx` extension and the `Non-Resx` type so that the toolchain embeds it verbatim.
- [NEW] The VS Code extension offers the same set of files through the **Tlumach: New Translation File...** and **Tlumach: New Configuration File...** commands, available from the Command Palette and from the Explorer context menu of a folder. The created file is registered in the nearest `.csproj` with the matching item type.
- [NEW] The list of formats and the template contents used by both editor integrations live in `src/Shared/FileTemplates`, so they are maintained in one place.
- [NEW] The icon that the Add New Item dialog shows for the Tlumach item templates is monochrome, in the style of the other item templates of Visual Studio 2026. The icons of the commands in the project and solution context menus keep the Tlumach teal and orange, as does the logo that identifies the extension in the marketplace and in the Extensions manager.
- [FIX] The locale of the default file is read by the parsers from the `defaultLocale` setting, but the documentation named the setting `defaultFileLocale` (the name of the `TranslationConfiguration.DefaultFileLocale` property). A configuration file written against the documentation therefore had its locale silently ignored. The documentation now names the setting `defaultLocale`, and all parsers additionally accept `defaultFileLocale` as a deprecated alias, so existing configuration files keep working; `defaultLocale` wins when both are present, and it is the only name the writers produce.
- [FIX] The default translation was loaded with the invariant culture even when the configuration declared `defaultLocale`. For the formats that keep several locales in one file - Apple String Catalog, XLIFF, CSV, and TSV - the parser then fell back to whichever locale came first in the file, so a String Catalog with a German entry ahead of the English one returned the German text for the default culture. The declared locale is now used when the default file is loaded.
- [FIX] The "Configuration" section of the XLIFF documentation showed `.xlfcfg` files in the INI syntax, with `[DefaultConfiguration]` and `[Translations]` sections. `XliffParser` is an XML parser, so a `.xlfcfg` file is XML; the topic now shows the correct syntax. The `.xlfcfg` files among the test data carried the same wrong example and were corrected as well.

---
Version: 1.10.1
Date: August 11, 2026

- [NEW] `BaseTranslationUnit.CurrentTemplate` returns the unprocessed template of the unit for the current culture of the translation manager. It is the counterpart of `CurrentValue` for a caller that formats the text itself, and it is what the generated accessors now read.
- [FIX] The string accessors generated because of `createStringAccessors` returned the **processed** value, so the positional placeholders of a validation message were stripped whenever `textProcessingMode` was `DotNet` or `Arb`: an attribute that localizes through `ErrorMessageResourceType` received `The  field is required.` instead of `The {0} field is required.`, and every message that carries an argument was quietly emptied. An accessor now returns the text unprocessed. A display name, and any other text without placeholders, is unaffected. The tests of the accessors used a configuration without a `textProcessingMode`, where the placeholder engine is off, which is why the defect did not show.

---
Version: 1.10.0 
Date: August 11, 2026

Localization of data annotations. Tlumach now offers three ways to localize the messages and the display names of validation attributes: a set of localized validation attributes for validation anywhere, string accessors generated for the attributes that localize through a resource type, and a corrected `IStringLocalizer` implementation on which the localization of data annotations of ASP.NET Core works as it stands. See the new "Localization of Data Annotations" topic in the documentation.

- [NEW] A new assembly, `Tlumach.DataAnnotations`, provides validation attributes that take their error message from a class created by Generator: `TlumachRequiredAttribute`, `TlumachStringLengthAttribute`, `TlumachRangeAttribute`, `TlumachRegularExpressionAttribute`, `TlumachCompareAttribute`, `TlumachEmailAddressAttribute`, and `TlumachPhoneAttribute`, plus `TlumachValidationAttribute` to derive own attributes from. An annotation names the localization class and the key, as in `[TlumachRequired(typeof(Strings), Strings.emailRequiredKey)]`. An attribute without a translation behaves exactly like the attribute of the framework it derives from.  
- [NEW] `TlumachValidationDefaults` sets the culture used by those attributes for the whole process and lets a trimmed application register a translation manager for a localization class explicitly.  
- [NEW] The `createStringAccessors` option of the configuration file, and the `TlumachGeneratorCreateStringAccessors` MSBuild property, tell Generator to emit a nested class of `public static string` properties, one per key, in the generated class and in every generated group class. Those properties are what `DisplayAttribute`, which is sealed, and the `ErrorMessageResourceType` mechanism require, so `[Display(ResourceType = typeof(Strings.Texts), Name = Strings.emailLabelKey)]` now works with a class generated by Tlumach. The option is off by default, and generated code is unchanged when it is absent.  
- [NEW] The `stringAccessorsClass` option, and the `TlumachGeneratorStringAccessorsClass` property, change the name of that class from the default "Texts".  
- [NEW] The `stringAccessorsCulture` option, and the `TlumachGeneratorStringAccessorsCulture` property, choose the culture that the accessors read: "manager", the default, reads the culture of the translation manager, and "ambient" reads the culture of the thread, which a web application that localizes requests needs.  
- [NEW] `TranslationConfiguration` exposes the `CreateStringAccessors`, `StringAccessorsClass`, and `StringAccessorsCulture` properties and a new constructor overload that accepts them. The existing overloads are unchanged, which matters for parsers implemented outside Tlumach.  
- [IMPORTANT] `TlumachStringLocalizer` now reads `CultureInfo.CurrentCulture` at the moment of every call instead of capturing it when the localizer is created. A localizer created while an application starts follows the culture of each request instead of staying with the culture of the startup.  
- [IMPORTANT] `TlumachStringLocalizer.WithCulture` and `WithTextProcessingMode` now return a new localizer instead of changing the one whose method was called and returning it.  
- [IMPORTANT] When a key is present in no translation, `TlumachStringLocalizer` now returns the key itself as the value and sets `LocalizedString.ResourceNotFound`, as the implementation of the framework does; it used to return an empty string. `ResourceNotFound` is now set only when no text was found at all, and not when the text came from the default translation.  
- [IMPORTANT] The indexer of `TlumachStringLocalizer` that takes no arguments now returns the text of the entry unchanged instead of replacing the placeholder names with themselves, so that the value can be used as a format string. This is what `ValidationAttribute`, `IViewLocalizer`, and the model binding of ASP.NET Core expect. The indexer that takes arguments is unchanged.  
- [IMPORTANT] `LocalizedString.SearchedLocation` returned by `TlumachStringLocalizer` now names the default translation file instead of being empty.  
- [FIX] `TlumachStringLocalizer.GetAllStrings(true)` never returned for a culture with a parent, such as "de-AT" or "en-US": the parent of the culture was recomputed on every pass, so the keys of the parent translation were appended without end. Keys are now returned once each, with the value of the most specific culture of the chain and the default translation as the last fallback.  
- [FIX] With `onlyDeclareKeys`, the static constructor of every generated group class assigned translation units that were never declared, so the generated code did not compile as soon as a translation contained a group. The generated class itself was not affected.  
- [FIX] The writers no longer lose the `createFilledMethods` setting when they write a configuration file. That setting, and the three new ones, are written only when they carry a value other than the default, so a configuration that does not use them is written back unchanged.  
- [FIX] The XML writer named the entries of a configuration file in PascalCase, as in `DefaultFile`, while the XML parser looks for the camelCase names that every other format uses, as in `defaultFile`, and XML element names are case-sensitive. A configuration file written for the RESX or the XLIFF format could therefore not be read back at all. The writer now names every entry after the same key constant that the parser and the writers of the other formats use.  
- [IMPORTANT] The JSON and the ARB writers did not put quotation marks around the string values they wrote, so a configuration file or a translation file they produced was not valid JSON and could only be read by the lenient reader of Tlumach itself. The values are now quoted. Files written by earlier versions are still read as before; files written by this version are accepted by any JSON reader, including the tools of other vendors that consume ARB.  
- [FIX] The Visual Studio extension now passes the `createFilledMethods` setting, and the three new settings, to the generator it runs in process. Only `usingNamespace`, `extraParsers`, and `delayedUnitCreation` used to reach it, so generation inside Visual Studio could differ from generation during a build.  

---
Version: 1.9.1 
Date: August 9, 2026

- [FIX] In some project configurations (where the base project directory was also the directory of the translation and configuration files), the Generator failed when invoked from the Visual Studio extension (generation during project building worked fine).  

---
Version: 1.9.0 
Date: August 7, 2026

Performance work on the translation-lookup and template-processing paths. Unless listed as `[IMPORTANT]`, the changes below do not alter behaviour.

- [PERF] `TranslationManager.GetValue` no longer uppercases the key and the culture name before probing. Both dictionaries already compare their keys with `StringComparer.OrdinalIgnoreCase`, so the conversions allocated two strings per lookup and changed nothing.
- [PERF] Loading a translation no longer happens while a lock covering the whole translation map is held. Each culture is now loaded under its own gate, so a slow load blocks only callers who want that same culture. Loading different cultures at the same time now scales with the number of cores instead of serializing.
- [PERF] Reading an already-loaded translation takes no lock on the translation map at all, and the default translation is now published through a `volatile` field rather than being read under a monitor. Resolving a file reference no longer holds the translation's lock.
- [PERF] `TranslationUnit` and other `BaseTranslationUnit` descendants reuse a single placeholder-resolver delegate instead of allocating a new one on every `GetValue()` call. The delegate still reads the placeholder cache and the `OnPlaceholderValueNeeded` subscription list live, so behaviour is unchanged.
- [PERF] Placeholder values are converted to text directly instead of through `string.Format(culture, "{0}", value)`, which parsed a composite format string on every placeholder.
- [PERF] In the `DotNet` text processing mode, the composite format string built from a placeholder's format specifier is cached per specifier instead of being concatenated for every placeholder evaluation.
- [PERF] `Utils.TryGetPropertyValue` builds and caches a property index per type. It previously called `Type.GetProperties`, which allocates a new array, once per placeholder, and scanned the result linearly. The resolution order is unchanged: an exact, case-sensitive match still wins over a case-insensitive one, and only public instance properties are considered.
- [PERF] The lookup of a placeholder's declaration in an `Arb` entry uses an indexed loop instead of a LINQ predicate, which allocated a closure and a delegate per placeholder.
- [PERF] In the `Arb` text processing modes, the closure used to process nested placeholder content is created at most once per template evaluation instead of once per placeholder.
- [PERF] Template processing reuses a small per-thread pool of `StringBuilder` instances. A pool rather than a single instance is required because processing re-enters itself for nested placeholder content.
- [PERF] The placeholder value cache of a translation unit uses `StringComparer.Ordinal` instead of `StringComparer.InvariantCulture`. The matching stays case-sensitive; the collation-based comparer routed every lookup through ICU.
- [PERF] `Utils.GetLeadingNonNegativeNumber` accumulates the value while scanning instead of allocating a substring and re-parsing it. The contract, including the answer for a digit run that does not fit in an `Int32`, is unchanged.
- [PERF] In the `Apple` text processing mode, specifier keys are no longer allocated per placeholder, and a value whose type does not match the specifier (for example, a string passed for `%d`) no longer raises and catches an exception. The text produced for such a value is unchanged.
- [IMPORTANT] `TranslationConfiguration.Translations` now compares its keys with `StringComparer.OrdinalIgnoreCase`. A configuration file that declares a locale in lowercase now matches, where previously it was silently ignored. Conversely, a configuration that declares two locales differing only by case (for example, both `de` and `DE`) is now reported as a duplicate.
- [IMPORTANT] The protected `BaseTranslationManager.Translations` property is now a `ConcurrentDictionary<string, Translation>` instead of a `Dictionary<string, Translation>`, and the protected `_defaultTranslation` field is now `volatile`. Code that derives from `BaseTranslationManager` and uses these members directly may need to be adjusted; in particular, `Translations.Remove(key)` becomes `Translations.TryRemove(key, out _)`.
- [IMPORTANT] When a placeholder value object exposes an indexer, the indexer is no longer considered a candidate for a placeholder named `Item`. Reading it without index arguments threw a `TargetParameterCountException`; such a placeholder is now reported as having no value.
- [FIX] In the `DotNet` text processing mode, a placeholder that carried a format specifier or an alignment did not work. The part following the placeholder name kept its leading colon, so `{0:N2}` was turned into the composite format `{0::N2}`; .NET then read `":N2"` as a custom numeric format made entirely of literal characters, and the value was dropped in favour of the text `":N2"`. An alignment was swallowed the same way, and `{0,10:N2}` produced neither alignment nor formatting. Specifiers and alignments now behave as they do in `string.Format`, so `{0:N2}`, `{0:X}`, `{0:yyyy-MM-dd}`, `{0,10}` and `{0,10:N2}` all produce the expected text. Note that translations which relied on the previous output will now render differently.
- [FIX] A `\uXXXX` escape in a templated text was decoded correctly but the reader then stepped one character short, so the last hexadecimal digit was emitted a second time: `A` produced `"A1"` instead of `"A"`. `Utils.UnescapeString` was not affected; the two paths now agree.
- [FIX] `FileFormats` read its parser registries without holding the lock that registration writes under. A parser lookup made while another thread was registering a parser could fail to find a parser that was in fact registered. The registries are now concurrent.

---
Version: 1.8.0  
Date: August 2, 2026

- [NEW] Added the `TranslationUnit.GetValueFrom<T>` and `TranslationEntry.ProcessTemplatedValueFrom<T>` overloads. They take the placeholder values from the public properties of a generic argument instead of an `object`, which lets the trimmer see and preserve those properties. Use these overloads instead of the `object`-based ones in applications published with trimming or NativeAOT (in particular, on iOS and Mac Catalyst, where full trimming is common) - otherwise the properties of anonymous types may be removed and the placeholders will not be substituted. Note that the lookup uses `typeof(T)`, so a value stored in a variable declared as `object` finds no properties; declare the variable with its concrete type.
- [NEW] Added the `Utils.TryGetPropertyValue` overload that accepts a `Type` known at compile time. It is the trimming-safe counterpart of the overload that accepts an `object`.
- [IMPORTANT] The `object`-based `TranslationUnit.GetValue`, `TranslationEntry.ProcessTemplatedValue`, and `Utils.TryGetPropertyValue` methods are now marked with `RequiresUnreferencedCode`. Applications that enable trim analysis will see an `IL2026` warning at the call sites, pointing to the overloads listed above. Applications that do not use trimming are not affected.
- [IMPORTANT] When an object is passed as the source of placeholder values and none of its properties match the placeholder name, the object itself is no longer substituted into the text. Previously, the `ToString()` of the whole object ended up in the translated string (for example, `{ Count = 5, Name = x }`), which silently corrupted the output. Now such a placeholder is reported as having no value, so the `OnPlaceholderValueNeeded` event can supply one. Passing a lone scalar value (a number, a string, a `bool`, a `char`, a `DateTime`, a `DateTimeOffset`, a `TimeSpan`, a `Guid`, or an enumeration member) for a single placeholder keeps working as before.
- [FIX] The `OnlyDeclareKeys` and `CreateFilledMethods` options did not work when specified in a simple key-value configuration file (.ini, .cfg). 

---
Version: 1.7.0  
Date: June 9, 2026

+ [NEW] Added the `WebEncodeValues` property to `TranslationManager`. When the property is set to `true`, translation units linked to this translation manager instance return the text which is safe for insertion into HTML web page sources.

---
Version: 1.6.3  
Date: May 10, 2026

- [FIX] A regression - `Tlumach.Avalonia.TranslationUnit` accidentally lost the constructor used by the generated code. 

---
Version: 1.6.2  
Date: May 8, 2026

- [NEW] Added optional generation of an individual class, a descendant of the TranslationUnit class, with the `Filled` method that accepts parameters named and typed after placeholders in the corresponding translation entry. This way, filling templated translation strings becomes easier as the syntax and types are checked at compile time. Also, this  way of passing the parameters is the fastest one, although it leads to the creation of extra classes and generation of additional code (one class with three methods per translation entry that contains placeholders). 
- [FIX] In the `Arb` text processing mode, integer placeholders were not substituted with a number if there was no format specified. 

---
Version: 1.6.0  
Date: May 1, 2026

- [IMPORTANT] Some refactoring - some of the members of `TranslationManager` were moved to the ancestor class, `BaseTranslationManager`.
- [NEW] Now, the Generator writes the source value of the text to the documentation comments, making it possible to see the text value by hovering the mouse cursor over a constant. This does not work for text that is loaded dynamically (from references or via events). 
- [NEW] Added the extensions for Visual Studio and VS Code. The extensions let you run Generator without building a translation project or projects. Also, in Visual Studio, you can navigate to the original location of the translation entry in the main/default translation file by using the Go To Definition" functionality of the IDEs. 
- [NEW] Added the parser and writer for Apple String Catalog file format.

---
Version: 1.5.0.1  
Date: April 21, 2026

- [FIX] Renamed the `placeholderValues` parameter of the `TranslationUnit.GetValue` overloads to indicate the type of the parameter. This is necessary for avoiding ambiguities when calling "GetValue([ someValue ])".

---
Version: 1.5  
Date: April 20, 2026

- [NEW] Added the writer classes for all formats. These classes can be used in the creation of various tools related to translations (conversion, export/import, etc.), and they are the basis for Tlumach Tools. Writer classes go to the dedicated NuGet package.
- [NEW] Added the parser and writer for XLIFF file format.
- [NEW] Added a static list of all `TranslationManager` instances (`TranslationManager.TranslationManagers` property) for easier update of properties of several managers.
- [NEW] Added the overload of the `LoadTranslation` method to the `TranslationManager` class that loads a translation by culture and expanded the `GetTranslation` method to optionally load the translation if it is not loaded yet.
- [NEW] Added the `LoadDefaultTranslation` method to the `TranslationManager` class for use in the file conversion scenarios.
- [FIX] Fixed the line counter in CSV, TSV, INI, and TOML parsers so that when an error occurs, the line number is reported correctly.

---
Version: 1.2.3.4  
Date: March 29, 2026

- [FIX] Tlumach.Generator is built against Microsoft.CodeAnalysis.CSharp version 5.0.0 now in order to be usable in environments with a bit older SDKs.

---
Version: 1.2.3.3  
Date: March 16, 2026

- [FIX] A duplicate curly quote was emitted as duplicate in DotNet mode (only one quote should be emitted). 

---
Version: 1.2.3.2  
Date: January 22, 2026

- [FIX] In the case of an error reported by Generator, the row and column reported by the IDE was offset by one. 

---
Version: 1.2.3.1  
Date: January 17, 2026

- [IMPORTANT] `TranslationUnit` classes can now be assigned to a string (this will assign the value of the `CurrentValue` property); the `ToString` method also returns the value of the `CurrentValue` property (previously, it returned the key).

---
Version: 1.2.3  
Date: January 10, 2026

- [NEW] Added the `OnReferenceNotResolved` event to TranslationManager.
- [NEW] Added the `OnTranslationFileNotFound` event to TranslationManager.
- [NEW] Added the `CacheDefaultTranslations` property to TranslationManager.
- [FIX] If a reference could not be resolved, an `ArgumentException` could occur. Now, an unresolved reference is by default returned "as is", and this behavior can be overridden using the `OnReferenceNotResolved` event.
- [FIX] When the default translation was loaded because some translation unit could not be found in a locale-specific translation, the loaded default translation could in some cases take the place of the current locale-specific translation.

---
Version: 1.2.2.3  
Date: January 9, 2026

- [FIX] If the same key was used in different sections in a TOML or INI file, it was erroneously treated as a duplicate.

---
Version: 1.2.2.2  
Date: December 25, 2025

- [FIX] `UntranslatedUnit` in the Avalonia package returned _null_ in `CurrentValue`.

---
Version: 1.2.2.1  
Date: December 24, 2025

- [FIX] The NuGet package did not include all assemblies in some libs directories, and this prevented the build toolchain from picking the right assemblies when packing an Android application.

---
Version: 1.2.2  
Date: December 20, 2025

- [NEW] Minor improvements in the Generator in its handling of configuration files and translation files that reside in a subdirectory of a project and get included into the assembly as resources.
- [FIX] Slightly improved the work with numeric placeholders in DotNet text processing mode - now, if format specifiers come out of order ("{1}:{0}"), the value from the ordered containers is picked by the format specifier and not by the ordinal position of the placeholder.

---
Version: 1.2.1  
Date: December 13, 2025

- [NEW] Added `UntranslatedUnit` class that lets one create a fake translation unit from a value coming from the application (this may be necessary when the UI operates with lists of translation units).
- [FIX] Removed a shortcut way to format a string with .NET formatter as it fails when a string contains named parameters.

---
Version: 1.2.0  
Date: December 6, 2025

- [NEW] Added Dependency Injection support.
- [NEW] Generator now emits key names as string constants.
- [NEW] It is possible to skip generation of `TranslationUnit` instances (and just use key name constants).
- [NEW] Added optional caching of values to the `TranslationUnit` class.
- [NEW] Added AOT compatibility flag to the main assemblies.
- [NEW] Added the `Comment` property to the `TranslationEntry` class. CSV/TSV and ResX parsers now pick comments from the translation files.

---
Version: 1.1.0  
Date: November 30, 2025

- [IMPORTANT] The TranslationEntry.`IsTemplated` property has been renamed to `ContainsPlaceholders`.
- [NEW] Now, you can bind XAML controls to translation units with placeholders. This requires that the application provide values for such units. Please, refer to the documentation for the details.
- [NEW] Added support for "selectordinal" (only for English presently), "date", "time", and "datetime" placeholder kinds to the ICU fragment parser.
- [FIX] Improvements in the handling of complex cases in placeholders.
- [FIX] The `textProcessingMode` value from a configuration file was used in code generation but not during the initial analysis of the default translation file.

---
Version: 1.0.1  
Date: November 26, 2025

- [FIX] Fixed loading of default translation files from a subdirectory, when both the config file and the translation file resided in the same _sub_directory.
- [FIX] TOML parser falsely marked some units as templated.

---
Version: 1.0.0  
Date: November 26, 2025

- [NEW] Initial public release.
