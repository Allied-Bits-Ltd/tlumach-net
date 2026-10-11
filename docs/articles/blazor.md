# Blazor integration

## Overview

`Tlumach.Blazor` localizes Blazor applications of every hosting model: Blazor Server (Interactive Server), Blazor WebAssembly (in a Blazor Web App and standalone), static server-side rendering (SSR), and Blazor Hybrid. The language of each user can be switched while the application runs, and the components show the new language at once.

The central problem it solves is the culture of a user. In a Blazor Server application, all users share one process, so the process-wide <xref:Tlumach.TranslationManager.CurrentCulture> and the `CurrentValue` of generated translation units cannot hold the language of a user. `Tlumach.Blazor` keeps the culture of each user in the scoped <xref:Tlumach.Blazor.TlumachCultureState> service and always retrieves texts for that culture explicitly. Where one user owns the process (Blazor WebAssembly, Blazor Hybrid), the same API can also switch the process-wide culture.

This article is the reference. For a step-by-step introduction, read [Getting Started for integration with Blazor](getting-started-blazor.md). The sample is `samples/Tlumach.Sample.Blazor` (with `Tlumach.Sample.Blazor.Client` and `Tlumach.Sample.Blazor.Translation`) in the [repository](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples).

## Packages and assemblies

The assemblies target .NET 9 and .NET 10 and ship no JavaScript or CSS files.

| Assembly | Package | Contents |
|---|---|---|
| `Tlumach.Blazor` | `AlliedBits.Tlumach.Blazor` | `AddTlumachBlazor`, <xref:Tlumach.Blazor.TlumachBlazorOptions>, <xref:Tlumach.Blazor.TlumachCultureState>, the components (`TlumachText`, `TlumachCultureSelector`, `TlumachComponentBase`), the culture stores, and `LoadTlumachCultureAsync`. |
| `Tlumach.Web` | `AlliedBits.Tlumach.Web` | <xref:Tlumach.Web.TlumachCultureOptions> (the base class of `TlumachBlazorOptions`) and `AddTlumachCultures`. It depends neither on Blazor nor on ASP.NET Core, so WebAssembly clients can use it. |
| `Tlumach.AspNetCore` | `AlliedBits.Tlumach.AspNetCore` | `UseTlumachRequestLocalization` and `MapTlumachCultureEndpoint`, which run on the server. It does not depend on `Tlumach.Blazor`. |

The Blazor package brings the Web package, `AlliedBits.Tlumach.Extensions.Localization`, and the core `AlliedBits.Tlumach` package with it. Which packages a project references depends on the hosting model:

| Application | Packages |
|---|---|
| Blazor Server, static SSR | `AlliedBits.Tlumach.Blazor` and `AlliedBits.Tlumach.AspNetCore` |
| Blazor Web App with WebAssembly islands | Server project: `AlliedBits.Tlumach.Blazor` and `AlliedBits.Tlumach.AspNetCore`; client project: `AlliedBits.Tlumach.Blazor` |
| Standalone Blazor WebAssembly | `AlliedBits.Tlumach.Blazor` |
| Blazor Hybrid | `AlliedBits.Tlumach.Blazor`, plus the integration package of the native UI framework if the native parts of the UI are localized, too (see [Packages](referencing.md)) |

The translations are normally kept in a project that is processed by [Generator](generator.md), which the server and the client projects reference. Embedded resources work in WebAssembly without any file system.

## Registration

<xref:Tlumach.Blazor.TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor*> registers the per-user culture state, the cascading <xref:Tlumach.Blazor.TlumachCulture> value, the culture store, and the culture-aware string localizers. In a Blazor Web App, call it both in the server and in the client project, with the same options; a static method that configures the options, kept in the client project and called from both `Program.cs` files, keeps them in sync (the sample does this). A repeated call in the same project only applies its callback to the options of the first call.

```csharp
using System.Globalization;

using Tlumach.Blazor;
using Tlumach.Extensions.Localization;

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager); // for IStringLocalizer
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("uk-UA")];
    options.DefaultCulture = options.SupportedCultures[0];
    options.Persistence = TlumachCulturePersistence.Cookie;
    options.DefaultManager = Strings.TranslationManager;
});
```

### Options

The first three options belong to <xref:Tlumach.Web.TlumachCultureOptions>, the others to <xref:Tlumach.Blazor.TlumachBlazorOptions>:

| Option | Default | Meaning |
|---|---|---|
| `SupportedCultures` | empty | The cultures that the user may choose. Always set it: `UseTlumachRequestLocalization` honors only these cultures, and the selector offers them. With an empty list, any culture is accepted by the culture state, but the request localization middleware honors only the current culture of the server. |
| `DefaultCulture` | `null` | The culture used when the culture of the request or of the browser is not supported. When not set, the first supported culture is used. |
| `CultureEndpoint` | `/tlumach/culture` | The path of the endpoint that stores the culture in a cookie, relative to the base path of the application. |
| `DefaultManager` | `null` | The translation manager, from which `TlumachText` takes texts by key when its `Manager` parameter is not set. |
| `ApplyCultureGlobally` | `null` | Whether a switch (and `LoadTlumachCultureAsync`) also changes the process-wide culture: `CultureInfo.DefaultThreadCurrentCulture`, `CultureInfo.DefaultThreadCurrentUICulture`, and `CurrentCulture` of every translation manager. `null` means `true` in the browser and `false` elsewhere, so Blazor Hybrid applications set it to `true` explicitly. Never set it to `true` on a server. |
| `Persistence` | `null` | Where the chosen culture is stored (see [Persistence](#persistence)). `null` means `LocalStorage` in the browser and `Cookie` elsewhere, so the WebAssembly client of a Blazor Web App and a Blazor Hybrid application set it explicitly. |
| `LocalStorageKey` | `"tlumach.culture"` | The key, under which the culture is kept in the local storage of the browser. |

A requested culture is matched against the supported ones by <xref:Tlumach.Web.TlumachCultureOptions.FindSupportedCulture(System.Globalization.CultureInfo)>: the culture itself, then the closest of its parent cultures ("de-AT" is served by "de"), then a culture of the same language. <xref:Tlumach.Web.TlumachCultureOptions.FindSupportedCulture(System.String)> does the same for a culture name without creating cultures for arbitrary names; the culture endpoint uses it. <xref:Tlumach.Web.TlumachCultureOptions.ResolveInitialCulture(System.Globalization.CultureInfo)> picks the culture to start with: the supported culture that serves the given one, else `DefaultCulture`, else the first supported culture.

### Blazor together with MVC or Razor Pages

An application that needs the cultures but no Blazor services, such as an MVC or Razor Pages application, calls <xref:Tlumach.Web.TlumachWebServiceCollectionExtensions.AddTlumachCultures*> instead of `AddTlumachBlazor` (see [Localization of MVC and Razor Pages](razor-localization.md)). An application that has both can call both methods, in any order, and they share one set of cultures: `AddTlumachBlazor` takes over `SupportedCultures`, `DefaultCulture`, and `CultureEndpoint` from an earlier `AddTlumachCultures` call, and `AddTlumachCultures` called after `AddTlumachBlazor` configures the options that `AddTlumachBlazor` registered. `AddTlumachBlazor` registers its options as `TlumachCultureOptions`, too, so `UseTlumachRequestLocalization`, `MapTlumachCultureEndpoint`, and the `<tlumach-culture-selector>` tag helper of MVC work with either registration.

## The culture of a user

<xref:Tlumach.Blazor.TlumachCultureState> is a scoped service: Blazor Server creates one per circuit (and one per prerendering request), so the users of one server do not share the culture, while Blazor WebAssembly and Blazor Hybrid effectively have one per application. The state starts with the culture of the current execution context (set by the request localization middleware on the server, or by `LoadTlumachCultureAsync` in WebAssembly), matched against the supported cultures with `ResolveInitialCulture`.

| Member | Purpose |
|---|---|
| `Culture` | The culture of the user. |
| `Current` | The immutable <xref:Tlumach.Blazor.TlumachCulture> snapshot that is cascaded to the components. |
| `SupportedCultures` | The cultures that the user may choose. |
| `SetCultureAsync(culture, forceReload)` | Switches the culture (see below). |
| `CultureChanged` | Occurs after a switch, before the components re-render. |
| `LocalizerCulture` | The culture that localizers of this user should use (see below). |

### Switching the language

<xref:Tlumach.Blazor.TlumachCultureState.SetCultureAsync*> takes a culture that must match one of the supported cultures; otherwise, it throws an `ArgumentException`. A switch to the current culture does nothing.

```csharp
await CultureState.SetCultureAsync(CultureInfo.GetCultureInfo("de-DE"));
await CultureState.SetCultureAsync(CultureInfo.GetCultureInfo("de-DE"), forceReload: true);
```

A **live switch** (`forceReload: false`, the default) replaces the cascaded snapshot, sets `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture` for the rest of the call, applies the culture globally when `ApplyCultureGlobally` is in effect, raises `CultureChanged`, re-renders the components that take the cascading `TlumachCulture` value, and finally stores the culture. Everything that Tlumach renders changes at once: `TlumachText`, the `T` methods, and injected `IStringLocalizer`. On Blazor Server, the culture of the circuit itself, which ASP.NET Core uses for formatting that does not go through Tlumach (for example, `@price.ToString("C")` or the validation messages of data annotations), changes only on the next page load.

With `forceReload: true`, the culture is stored, and the page is reloaded entirely, so everything uses the new culture.

If storing the culture fails (JS interop is not available while prerendering, or the circuit is gone), the failure is logged as a warning, and the switch is not undone; the next visit may then start with the previous language. If a handler of `CultureChanged` throws, the components are still notified and the culture is still stored, and then the exception propagates from `SetCultureAsync`.

### Reacting to a switch

Components react through the cascading <xref:Tlumach.Blazor.TlumachCulture> value: a component that declares a cascading parameter of this type is re-rendered after a switch. `TlumachText`, `TlumachCultureSelector`, and components derived from `TlumachComponentBase` declare it. Code that is not a component subscribes to <xref:Tlumach.Blazor.TlumachCultureState.CultureChanged> and unsubscribes when it is disposed:

```csharp
using Tlumach;
using Tlumach.Blazor;

public sealed class PriceFormatter : IDisposable
{
    private readonly TlumachCultureState _cultureState;

    public PriceFormatter(TlumachCultureState cultureState)
    {
        _cultureState = cultureState;
        _cultureState.CultureChanged += OnCultureChanged;
    }

    public void Dispose() => _cultureState.CultureChanged -= OnCultureChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        // e.Culture is the new culture of the user
    }
}
```

Register such a service as scoped, so that it gets the culture state of its user.

### The culture for localizers

<xref:Tlumach.Blazor.TlumachCultureState.LocalizerCulture> is the culture that the localizers of a user use, including those of the [component suite integrations](#component-suites). After a live switch in the scope, it is `Culture`. Before that, it is the culture of the current execution context (the request or circuit), matched against the supported cultures. Inside the user's scope, both are the same until the first switch; a state that is resolved from the root provider and is never switched thus follows each request instead of keeping the culture of the application's start.

## Showing texts

### The TlumachText component

<xref:Tlumach.Blazor.TlumachText> renders the text of a translation unit or of a key in the culture of the user and re-renders when the user switches the language:

```razor
<h1><TlumachText Unit="Strings.Hello" /></h1>
<TlumachText Unit="Strings.Greeting" Args="@(new Dictionary<string, object?> { ["name"] = user })" />
<TlumachText Unit="Strings.Position" Values="@(new object[] { index, total })" />
<TlumachText Key="Hello" />
<TlumachText Key="Hello" Manager="@OtherStrings.TranslationManager" />
```

| Parameter | Meaning |
|---|---|
| `Unit` | The translation unit, usually a member of a class created by Generator. |
| `Key` | The key of the text, used when `Unit` is not set. The key is resolved by `Manager` or, when it is not set, by `TlumachBlazorOptions.DefaultManager`. A missing key renders the key itself as text (encoded also with `AsMarkup`), as the string localizers and the MVC tag helpers do. |
| `Manager` | The translation manager that resolves `Key`. |
| `Args` | The values of named placeholders, keyed by placeholder names. |
| `Values` | The values of indexed placeholders, used when `Args` is not set. In the `Arb` text processing mode, where placeholders are named, `Values` fills them by position, in the order in which they appear in the text. |
| `AsMarkup` | Renders the text as markup; see [Encoding](#encoding). |

Either `Unit` or `Key` must be set, and `Key` needs `Manager` or `DefaultManager`; otherwise, the component throws an `InvalidOperationException`. ICU placeholders (`plural`, `select`, `number`, `date`, ...) are formatted in the culture of the user; see [Templates and Placeholders](placeholders.md).

### Texts in attributes and code

Where a component cannot be placed, for example in attributes or in code, derive the component from <xref:Tlumach.Blazor.TlumachComponentBase> and use its methods, which return plain text for the culture of the user:

```razor
@inherits TlumachComponentBase

<input placeholder="@T(Strings.NamePlaceholder)" />
<p>@T(Strings.Position, index, total)</p>
<p>@TFrom(Strings.Today, new { date = DateTime.Today.ToString("D", Culture.Culture) })</p>
```

* `T(unit)`, `T(unit, params object[] values)`, and `T(unit, IDictionary<string, object?> args)` return the text without values, with indexed values, and with named values.
* `TFrom(unit, args)` takes an object whose public properties supply the values of named placeholders, such as an anonymous object; it is safe in trimmed applications. It has its own name because, as an overload of `T`, it would win over the overloads for arrays and dictionaries.
* `Culture` is the current `TlumachCulture` snapshot, and `CultureState` is the injected `TlumachCultureState`, e.g. for calling `SetCultureAsync`.
* `OnCultureChangedAsync(TlumachCulture)` is called before the component re-renders after a switch; during the call, `Culture` still returns the previous snapshot.

The class subscribes to no events, so it needs no disposal. A component that does not derive from it can declare the cascading parameter itself and call the methods of <xref:Tlumach.Blazor.TlumachCulture> (`Get`, `GetFrom`, and `Markup`):

```razor
<span title="@Culture.Get(Strings.Hello)">...</span>

@code {
    [CascadingParameter] private TlumachCulture Culture { get; set; } = default!;
}
```

Do not use `@Strings.Hello` (the `CurrentValue` of a unit) in a Blazor Server application: it follows the process-wide culture of the translation manager, which all users share.

### IStringLocalizer

`AddTlumachBlazor` registers `IStringLocalizer` and `IStringLocalizer<T>` as **scoped** services (one per user) that retrieve strings in `LocalizerCulture`, also after a live switch, and replaces earlier registrations of these interfaces, including those of `AddLocalization()`. The localizers are created by the localizer factory, which `AddTlumachLocalization` registers (see [Dependency Injection](di.md)); a localizer that does not come from Tlumach is used unchanged.

Inject the localizer into components or into scoped services. A singleton service should not inject `IStringLocalizer<T>`: scope validation rejects that in the Development environment. Where a localizer is resolved outside a user's scope anyway, it follows the culture of the current request or circuit, but not live switches.

## Encoding

Razor encodes text, and so does `TlumachText`. Leave <xref:Tlumach.TranslationManager.WebEncodeValues> off in Blazor; if it is on, `TlumachText` and the `T` methods detect it and do not encode twice.

For translations that contain trusted HTML, use `AsMarkup="true"` or `Culture.Markup(unit)`. The translation text is then trusted, while placeholder values are data:

* `string` values in `Args`, `Values`, or the dictionary passed to `Culture.Markup(unit, args)` are HTML-encoded before they are substituted, so `Welcome, <b>{name}</b>` stays safe when the name comes from a user.
* A `MarkupString` value inserts trusted HTML.
* Other values (numbers, dates, ...) are formatted as usual.
* Values supplied through `OnPlaceholderValueNeeded` or `CachePlaceholderValue` (that is, when no `Args` or `Values` are passed) are not encoded; pass the values of markup texts through `Args` or `Values`.
* When `WebEncodeValues` is on, `AsMarkup` renders the encoded text of a `Unit`, so tags appear literally; for a `Key`, it uses the raw text, because the translation manager does not encode.

Anyone who can change a translation file can inject markup into the pages that render it as markup, so translation files must come from trusted sources.

## The culture selector

<xref:Tlumach.Blazor.TlumachCultureSelector> lets the user choose one of the supported cultures:

```razor
<TlumachCultureSelector />
<TlumachCultureSelector ForceReload="true" class="form-select" />
<TlumachCultureSelector SubmitText="✓" DisplayName="@(c => c.EnglishName)" />
```

* In an interactive component, it is a `select` element that switches the language with `SetCultureAsync` when the selection changes.
* In static SSR, it is a form that sends the choice (`GET`) to the culture endpoint, which stores it in the cookie and redirects back to the current page, so no interactivity and no JavaScript are needed. In a prerendered interactive page, the selector is first rendered as the form and turns into a `select` when the page becomes interactive.
* `ForceReload` passes `forceReload` to `SetCultureAsync`. `DisplayName` returns the label of a culture; the default is its `NativeName`. `SubmitText` is the text of the button of the form; it defaults to "OK" and is not localized, so set it to a neutral symbol or to a translated text. Other attributes (`class`, `id`) are applied to the `select` element.
* The selector switches only to the cultures it offers; other values, for example from a forged request, are ignored. With an empty `SupportedCultures` list, it offers the current culture only.

## Persistence

`Persistence` is a flags value of <xref:Tlumach.Blazor.TlumachCulturePersistence>:

| Value | Store | Typical application |
|---|---|---|
| `Cookie` | <xref:Tlumach.Blazor.CookieCultureStore>: saves by sending `POST {CultureEndpoint}?culture=...` with `fetch` to the culture endpoint, which writes the ASP.NET Core culture cookie; loads from the `lang` attribute of the `html` element, which the server renders from the same cookie | Blazor Web App (server and client) |
| `LocalStorage` | <xref:Tlumach.Blazor.LocalStorageCultureStore>: the local storage of the browser, under `LocalStorageKey` | Standalone Blazor WebAssembly, Blazor Hybrid |
| `Cookie \| LocalStorage` | Both: a switch is saved to both places; when the culture is loaded, the cookie store is asked first, and the local storage is used only when the page has no `lang` attribute | |
| `None` | The culture is not stored | |

With `Cookie`, render the culture into the `lang` attribute in `App.razor` of the server; screen readers need it anyway:

```razor
<html lang="@System.Globalization.CultureInfo.CurrentUICulture.Name">
```

A static `<html lang="en">` in the `index.html` of a standalone WebAssembly application would win over the local storage with `Cookie | LocalStorage`, so standalone applications use `LocalStorage` alone.

To store the culture elsewhere, for example in a user profile, implement <xref:Tlumach.Blazor.ITlumachCultureStore> (`LoadAsync` returns the name of the stored culture or `null`, `SaveAsync` stores a culture) and register it before calling `AddTlumachBlazor`; the built-in stores are then not used.

## The server side

`Tlumach.AspNetCore` connects the stored culture with the request localization of ASP.NET Core:

```csharp
using Tlumach.AspNetCore;

var app = builder.Build();

app.UseTlumachRequestLocalization();   // before the endpoints
app.UseAntiforgery();
app.MapStaticAssets();
app.MapTlumachCultureEndpoint();       // TlumachCultureOptions.CultureEndpoint, "/tlumach/culture" by default
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();
```

<xref:Tlumach.AspNetCore.TlumachAspNetCoreExtensions.UseTlumachRequestLocalization*> adds the request localization middleware configured from the registered `TlumachCultureOptions`: the supported cultures, the default culture (or the first supported one), and the culture cookie moved to the first place among the request culture providers. It requires `AddTlumachBlazor` or `AddTlumachCultures`. The middleware sets the culture of every request and therefore the initial culture of every circuit and of every prerendering.

<xref:Tlumach.AspNetCore.TlumachAspNetCoreExtensions.MapTlumachCultureEndpoint*> maps two endpoints:

* `POST {pattern}?culture=de-DE` sets the cookie and returns 204; `CookieCultureStore` calls it from interactive components.
* `GET {pattern}?culture=de-DE&redirectUri=/page` sets the cookie and redirects; the form of the selector in static SSR uses it.

Details:

* Without a `pattern` argument, `CultureEndpoint` is used. The selector and `CookieCultureStore` always send their requests to `CultureEndpoint`, so to move the endpoint, change `CultureEndpoint` (on the server and the client) rather than passing a pattern.
* The culture is resolved with `FindSupportedCulture(string)`; a culture that is not supported gets the response 400. With an empty `SupportedCultures` list, only the names of predefined cultures are accepted.
* The cookie is the ASP.NET Core culture cookie (`.AspNetCore.Culture`), valid for one year, `HttpOnly`, `SameSite=Lax`, and `Secure` on HTTPS. It is marked as essential (`IsEssential`), because the culture is a functional preference, so it is written even when a cookie-consent policy has not been accepted.
* `redirectUri` must be a local path. Anything else, including values with control characters or characters outside printable ASCII, leads to the root of the application.
* Both endpoints change only the culture cookie and carry no antiforgery token, by design: at worst, a request from another site switches the user to another supported language.

## Blazor WebAssembly

The client project of a Blazor Web App and a standalone WebAssembly application load the stored culture before the application starts:

```csharp
builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("uk-UA")];
    options.Persistence = TlumachCulturePersistence.Cookie;   // in the client of a Blazor Web App; omit it in a standalone application
});

var host = builder.Build();
await host.Services.LoadTlumachCultureAsync();
await host.RunAsync();
```

<xref:Tlumach.Blazor.TlumachBlazorServiceProviderExtensions.LoadTlumachCultureAsync*> loads the stored culture from the culture store, matches it against the supported cultures (falling back to `ResolveInitialCulture`), and makes it the culture of the whole process: `CultureInfo.DefaultThreadCurrentCulture`, `CultureInfo.DefaultThreadCurrentUICulture`, and `CurrentCulture` of every translation manager. It returns the applied culture. Because it changes the process-wide culture, it is meant for the WebAssembly client, where one user owns the process; never call it in a server project. It applies the culture only when `ApplyCultureGlobally` allows it, which it does by default in the browser and, in Blazor Hybrid, when the option is set to `true`. Otherwise, for example when it is called in a server project by mistake, it changes nothing, logs a warning, and only returns the culture, so that the culture of the other users is not switched. If the stored culture cannot be loaded, it logs a warning and uses the initial culture.

In WebAssembly, a live switch also changes the process-wide culture (`ApplyCultureGlobally` is `true` in the browser by default), so all formatting follows the new language.

Add `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` to the client project, so that the runtime has the data of all cultures, and do not enable `InvariantGlobalization`; `LoadTlumachCultureAsync` logs a warning when the invariant mode is on.

## Blazor Hybrid

In Blazor Hybrid (MAUI, WPF, Windows Forms), one user owns the process, but the defaults of the options are those of a non-browser host. Register with `ApplyCultureGlobally = true` and `Persistence = TlumachCulturePersistence.LocalStorage`, which uses the local storage of the web view, and set the culture before the first render, as described in [Getting Started for integration with Blazor](getting-started-blazor.md).

## Render modes and prerendering

* Each interactive island has its own culture state: a Server island lives in the circuit, a WebAssembly island in the browser. A switch inside one island is stored, but the islands of the other kind on the same page show the new language only after the next page load, and statically rendered parts (such as a static layout) after the next navigation. A page with one render mode avoids this; `forceReload` updates everything at once.
* Prerendering and the interactive render read the same cookie, so the prerendered HTML matches the interactive page. The culture is never stored during prerendering.

## Placeholder values on the server

Generated translation units are static and shared by all users. Do not keep per-user values in them with `CachePlaceholderValue` or `OnPlaceholderValueNeeded`; pass the values with `Args`, `Values`, `T(...)`, or `TFrom(...)` instead.

## Trimming and NativeAOT

`Tlumach.Blazor`, `Tlumach.Web`, and `Tlumach.AspNetCore` are marked as trimming- and AOT-compatible. For placeholder values, prefer `TFrom` and `TlumachCulture.GetFrom` (or dictionaries and arrays) over the APIs of translation units that take `object`, which may lose the properties of the object in a trimmed application; see [Templates and Placeholders](placeholders.md).

## Component suites

The built-in texts of component suites can come from Tlumach, too, in the culture of each user (`LocalizerCulture`). The integrations are shipped as separate packages that build on `Tlumach.Blazor`:

* `AlliedBits.Tlumach.MudBlazor`: see [Localization of MudBlazor](component-suites-mudblazor.md).
* `AlliedBits.Tlumach.Syncfusion.Blazor`: see [Localization of Syncfusion Blazor](component-suites-syncfusion.md).

## Sample

The `samples/Tlumach.Sample.Blazor` Web App shows a static SSR page, an Interactive Server page, and an Interactive WebAssembly page with live switching, placeholders, ICU plurals, `IStringLocalizer`, and `ForceReload`.

## See also

* [Getting Started for integration with Blazor](getting-started-blazor.md)
* [Dependency Injection](di.md)
* [Localization of MVC and Razor Pages](razor-localization.md)
* [Templates and Placeholders](placeholders.md)
* [Packages](referencing.md)
