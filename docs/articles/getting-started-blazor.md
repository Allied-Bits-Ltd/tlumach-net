# Getting Started

## Integration with Blazor

`Tlumach.Blazor` localizes Blazor applications of every hosting model: Blazor Server (Interactive Server), Blazor WebAssembly (Interactive WebAssembly in a Blazor Web App and standalone), static server-side rendering (SSR), and Blazor Hybrid (MAUI, WPF, Windows Forms). The language can be switched while the application runs, and the components show the new language at once. `Tlumach.AspNetCore` adds the server-side pieces: request localization and an endpoint that stores the chosen culture in a cookie.

`Tlumach.Blazor` is in the `AlliedBits.Tlumach.Blazor` package and `Tlumach.Web` in the `AlliedBits.Tlumach.Web` package (.NET 9 and .NET 10); the Blazor package brings the Web package, `AlliedBits.Tlumach.Extensions.Localization`, and the core `AlliedBits.Tlumach` package with it. Neither ships JavaScript or CSS files. To store the chosen culture in a cookie on the server (`MapTlumachCultureEndpoint`), add the `AlliedBits.Tlumach.AspNetCore` package to the server project.

### Why a Blazor-specific integration

In a Blazor Server application, all users share one process, so the process-wide <xref:Tlumach.TranslationManager.CurrentCulture> cannot hold the language of a user. `Tlumach.Blazor` keeps the culture of each user in the scoped <xref:Tlumach.Blazor.TlumachCultureState> service (one per circuit) and always retrieves texts for that culture explicitly. In Blazor WebAssembly, one user owns the process, and the same API also switches the process-wide culture; in Blazor Hybrid, it does so when `ApplyCultureGlobally = true` is set (see section 7).

### 1. Translations

Create a translation project with the generator as described in [Generator](generator.md), e.g. a .NET Standard 2.0 class library with ARB files as embedded resources and a configuration file:

```ini
defaultFile=sample.arb
generatedNamespace=MyApp.Translation
generatedClass=Strings
textProcessingMode=Arb

[translations]
de=sample_de.arb
uk=sample_uk.arb
```

Reference this project from the server and the client projects. Embedded resources work in WebAssembly without any file system.

### 2. Registration

Server project (`Program.cs`):

```csharp
using Tlumach.AspNetCore;
using Tlumach.Blazor;
using Tlumach.Extensions.Localization;

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager); // optional, for IStringLocalizer
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [new("en-US"), new("de-DE"), new("uk-UA")];
    options.DefaultCulture = options.SupportedCultures[0];
    options.Persistence = TlumachCulturePersistence.Cookie;
});

var app = builder.Build();

app.UseTlumachRequestLocalization();   // before the endpoints
app.UseAntiforgery();
app.MapStaticAssets();
app.MapTlumachCultureEndpoint();       // "/tlumach/culture" by default
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();
```

Client project of a Blazor Web App (`Program.cs`):

```csharp
builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [new("en-US"), new("de-DE"), new("uk-UA")];
    options.Persistence = TlumachCulturePersistence.Cookie;
});

var host = builder.Build();
await host.Services.LoadTlumachCultureAsync();
await host.RunAsync();
```

Call `AddTlumachBlazor` once per project (the server and the client once each) and with the same options on both sides; a static class with a `Configure(TlumachBlazorOptions)` method in the client project, used by both `Program.cs` files, keeps them in sync (see the sample).

The options `SupportedCultures`, `DefaultCulture`, and `CultureEndpoint` belong to the base class <xref:Tlumach.Web.TlumachCultureOptions> of `TlumachBlazorOptions`, which is in the `Tlumach.Web` assembly. `Tlumach.Web` does not depend on Blazor or ASP.NET Core, and `Tlumach.AspNetCore` (request localization and the culture endpoint) uses `TlumachCultureOptions` and does not depend on `Tlumach.Blazor`. An application that needs the cultures but no Blazor services (e.g. an MVC or Razor Pages application, see [Getting Started for integration with ASP.NET Core MVC](getting-started-aspnetcore-mvc.md)) calls `AddTlumachCultures` instead of `AddTlumachBlazor`; an application that has both MVC and Blazor can call both methods, in any order, and they share one set of cultures (when `AddTlumachBlazor` finds the options of `AddTlumachCultures`, it takes over their settings).

Always set `SupportedCultures`. `UseTlumachRequestLocalization` builds the request localization from it: the middleware honors only the listed cultures and falls back to `DefaultCulture` (or the first listed culture). With an empty list, the culture endpoint still accepts the names of predefined cultures, but the middleware only honors the current culture of the server, so the chosen language would not reach the requests.

A requested culture is matched against the supported ones by <xref:Tlumach.Web.TlumachCultureOptions.FindSupportedCulture(System.Globalization.CultureInfo)> or, when only the name is known (as in the culture endpoint), by <xref:Tlumach.Web.TlumachCultureOptions.FindSupportedCulture(System.String)>: the exact name first, then the name with the trailing subtags removed ("de-AT" is served by "de"), then a culture of the same language. The string overload creates a culture only when the name is the name of a predefined culture (a bounded set) and then matches it exactly like the `CultureInfo` overload; any other name is matched as a string, and no culture is created for it. With an empty `SupportedCultures` list, it accepts only the names of predefined cultures. On ICU-based runtimes, some legacy region names (for example, zh-TW, zh-CN, sr-RS) are not predefined cultures, so the string overload maps them by language rather than by script; list the cultures you support explicitly (for example, zh-Hant-TW) if that matters.

In `App.razor` of the server, render the culture into the `lang` attribute. Screen readers need it anyway, and with `TlumachCulturePersistence.Cookie` the WebAssembly client reads the culture from it on startup:

```razor
<html lang="@System.Globalization.CultureInfo.CurrentUICulture.Name">
```

`AddTlumachBlazor` registers `IStringLocalizer` and `IStringLocalizer<T>` as **scoped** services (one per user) and replaces earlier registrations of these interfaces, including those of `AddLocalization()`. Inject the localizer into components or into scoped services. A **singleton** service should not inject `IStringLocalizer<T>`: scope validation rejects that in the Development environment. Where a localizer is nevertheless resolved outside a user's scope (a singleton with scope validation off, or a service resolved from the root provider), it follows the culture of the current request or circuit (`CultureInfo.CurrentUICulture`), as localizers did before `AddTlumachBlazor`; it does not see live switches, which belong to the user's scope. When `AddTlumachBlazor` is not used and `AddLocalization()` is called before `AddTlumachLocalization()`, the framework's `StringLocalizer<T>` stays registered, and it still delegates to the Tlumach localizer factory.

### 3. Showing texts

Use the `TlumachText` component for text content:

```razor
<h1><TlumachText Unit="Strings.Hello" /></h1>
<TlumachText Unit="Strings.Greeting" Args="@(new Dictionary<string, object?> { ["name"] = user })" />
<TlumachText Unit="Strings.Position" Values="@(new object[] { index, total })" />
<TlumachText Key="Hello" />   @* by key, through TlumachBlazorOptions.DefaultManager or the Manager parameter *@
```

Here, `Strings.Position` may be the unit "Item {current} of {total}". In the `Arb` text processing mode, placeholders are named (`{name}`); `Values` fills them by position, in the order in which they appear in the text, so it works with named placeholders, too. Use `Args` when you prefer to pass the values by name.

The component re-renders when the language changes. ICU placeholders (`plural`, `select`, `number`, `date`, ...) are formatted with the culture of the user; see [Templates and Placeholders](placeholders.md).

Where a component cannot be placed, e.g. in attributes or in code, derive the component from `TlumachComponentBase` and use its `T` methods:

```razor
@inherits TlumachComponentBase

<input placeholder="@T(Strings.NamePlaceholder)" />
<p>@TFrom(Strings.Today, new { date = DateTime.Today.ToString("D", Culture.Culture) })</p>
```

`TFrom` takes an object whose properties supply the values of named placeholders (anonymous objects work, also in trimmed applications). `T` has overloads for indexed values and for a dictionary. `OnCultureChangedAsync` is called before the component re-renders after a switch.

A component that does not derive from `TlumachComponentBase` can take the culture as a cascading parameter and gets re-rendered just the same:

```razor
@code {
    [CascadingParameter] private TlumachCulture Culture { get; set; } = default!;
}

<span title="@Culture.Get(Strings.Hello)">...</span>
```

Do not use `@Strings.Hello` (the `CurrentValue` of a unit) in a Blazor Server application: it follows the process-wide culture of the translation manager, which is shared by all users.

### 4. Switching the language

Place the selector anywhere:

```razor
<TlumachCultureSelector />
<TlumachCultureSelector ForceReload="true" class="form-select" />
```

In an interactive component, it is a `select` element; a change switches the language at once. In static SSR, it is a small form that sends the choice to the culture endpoint, which stores it in the cookie and reloads the page; no JavaScript is needed. In a prerendered interactive page, the selector is first rendered as the SSR form and turns into a `select` when the page becomes interactive. The selector accepts only the cultures it offers: other values (for example, from a forged request) are ignored. The button of the SSR form shows `SubmitText`, which defaults to "OK" and is not localized; in a multilingual application, set it to a neutral symbol (e.g. `SubmitText="✓"`) or to a translated text.

In code, inject `TlumachCultureState` and call `SetCultureAsync`:

```csharp
await CultureState.SetCultureAsync(CultureInfo.GetCultureInfo("de-DE"));
await CultureState.SetCultureAsync(CultureInfo.GetCultureInfo("de-DE"), forceReload: true);
```

A live switch on Blazor Server changes everything that Tlumach renders: `TlumachText`, `T(...)`, injected `IStringLocalizer`. The culture of the circuit itself, which ASP.NET Core uses for other formatting such as `@price.ToString("C")` and for validation messages of data annotations, changes only on the next page load. When that matters, use `forceReload: true`: the culture is stored, and the page is reloaded entirely in the new language.

Code that is not a component can subscribe to `TlumachCultureState.CultureChanged`; unsubscribe when the subscriber is disposed.

### 5. Persistence

| Application | `Persistence` | How the culture is stored and restored |
|---|---|---|
| Blazor Web App (Server, SSR, WebAssembly islands) | `Cookie` (server and client) | The endpoint writes the `.AspNetCore.Culture` cookie (`fetch` POST in interactive components, a form in SSR); `UseTlumachRequestLocalization` reads it; the WebAssembly client reads `<html lang>`. |
| Standalone Blazor WebAssembly | `LocalStorage` (the default in the browser) | `localStorage` key `tlumach.culture`, read by `LoadTlumachCultureAsync`. |
| Blazor Hybrid | `LocalStorage` | `localStorage` of the web view. |

`Persistence` is a flags value, so `Cookie | LocalStorage` is allowed: a switch is saved to both places. When the culture is loaded, the stores are asked in turn, and the cookie store goes first: its value, the `lang` attribute of the `html` element, wins, and `localStorage` is used only when the page has no `lang` attribute. A static `<html lang="en">` in the `index.html` of a standalone Blazor WebAssembly application therefore also wins over `localStorage`, so standalone WebAssembly applications should use `LocalStorage` alone.

To store the culture elsewhere, e.g. in a user profile, register your own `ITlumachCultureStore` before calling `AddTlumachBlazor`.

#### The culture endpoint

`MapTlumachCultureEndpoint` maps `POST {pattern}?culture=de-DE`, which sets the cookie and returns 204 (used by interactive components), and `GET {pattern}?culture=de-DE&redirectUri=/page`, which sets the cookie and redirects (used by the form of the selector in static SSR).

- The cookie is `HttpOnly`, `SameSite=Lax`, and `Secure` on HTTPS. It is also marked as essential (`IsEssential`): the culture is a functional preference, so the cookie is written even when the application uses a cookie-consent policy that the user has not accepted.
- `redirectUri` must be a local path. Anything else, including values with control characters or characters outside printable ASCII (send the path percent-encoded), falls back to the root of the application.
- A culture that is not supported gets the response 400.
- Both GET and POST change only the culture cookie and carry no antiforgery token, by design: at worst, a request from another site switches the user to another supported language.

### 6. Blazor WebAssembly specifics

- Add `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` to the client project, so that the runtime has the data of all cultures, and do not enable `InvariantGlobalization`. `LoadTlumachCultureAsync` logs a warning when the invariant mode is on.
- `Tlumach.Blazor` is trimming- and AOT-compatible. Prefer `TFrom`/`GetFrom` over APIs that take `object` for placeholder values.
- In WebAssembly, a switch also sets `CultureInfo.DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture` and the culture of every translation manager, so all formatting follows the new language.

### 7. Blazor Hybrid

Register with `ApplyCultureGlobally = true` (one user owns the process) and `Persistence = TlumachCulturePersistence.LocalStorage`, and set the culture before the first render, e.g. from the platform preferences or with `await services.LoadTlumachCultureAsync()` once the web view is ready.

### 8. Render modes and prerendering

- Each interactive island has its own culture state: a Server island lives in the circuit, a WebAssembly island in the browser. A switch inside one island is stored, but the islands of the other kind on the same page show the new language only after the next page load. Statically rendered parts (e.g. a static layout) update after the next navigation. A page with one render mode avoids this; `ForceReload` updates everything at once.
- Prerendering and the interactive render read the same cookie, so the prerendered HTML matches the interactive page. The culture is never stored during prerendering.

### 9. Encoding

Razor encodes text, and so does `TlumachText`. Leave <xref:Tlumach.TranslationManager.WebEncodeValues> off in Blazor; if it is on, `TlumachText` and the `T` methods detect it and do not encode twice. For translations that contain trusted HTML, use `AsMarkup="true"` or `Culture.Markup(unit)`; translators then control the markup. The translation text is trusted, but placeholder values are data: in markup mode, `string` values (in `Args`, `Values`, or the arguments of `Culture.Markup(unit, args)`) are HTML-encoded before they are substituted, so `Welcome, <b>{name}</b>` stays safe when the name comes from a user. To insert trusted HTML through a value, pass a `MarkupString`; other values (numbers, dates, ...) are formatted as usual. When `WebEncodeValues` is on, `AsMarkup` renders the encoded text of a `Unit` (so tags appear literally), while for a `Key` it uses the raw text, because lookups by key go through the translation manager, which does not encode. Values supplied through `OnPlaceholderValueNeeded` or `CachePlaceholderValue` (that is, when no `Args` or `Values` are passed) are not encoded in markup mode; pass values through `Args` or `Values` (which are encoded) instead.

### 10. Placeholders on the server

Generated translation units are static and shared by all users. Do not keep per-user values in them via `CachePlaceholderValue` or `OnPlaceholderValueNeeded`; pass the values with `Args`, `Values`, `T(...)`, or `TFrom(...)` instead.

### Component suites

The built-in texts of MudBlazor components (filters, pagers, pickers, ...) can come from Tlumach in the language of each user through the separate `AlliedBits.Tlumach.MudBlazor` package. See [Getting Started for integration with MudBlazor](getting-started-mudblazor.md) and [Localization of MudBlazor](component-suites-mudblazor.md).

The built-in texts of Syncfusion Blazor components (grid pager and filter menus, date picker, ...) can come from Tlumach in the same way through the separate `AlliedBits.Tlumach.Syncfusion.Blazor` package. See [Getting Started for integration with Syncfusion Blazor](getting-started-syncfusion.md) and [Localization of Syncfusion Blazor](component-suites-syncfusion.md).

### Sample

The `samples/Tlumach.Sample.Blazor` Web App (with `Tlumach.Sample.Blazor.Client` and `Tlumach.Sample.Blazor.Translation`) shows a static SSR page, an Interactive Server page, and an Interactive WebAssembly page with live switching, placeholders, ICU plurals, `IStringLocalizer`, and `ForceReload`.
