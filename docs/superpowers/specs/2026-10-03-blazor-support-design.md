# Blazor Support — Design

Date: 2026-10-03
Branch: `blazor`
Status: approved in brainstorming, pending written-spec review

## Goal

First-class Blazor support for Tlumach.NET that works for Blazor Server (Interactive Server), Blazor WebAssembly (Interactive WebAssembly in a Web App and standalone), static SSR, and Blazor Hybrid (MAUI, WPF, WinForms BlazorWebView), with live language switching and per-user culture on the server.

## Background and constraints

- `TranslationManager.CurrentCulture` is process-wide. On Blazor Server many users share one process, so it cannot carry a per-user culture.
- On Blazor Server the culture of a circuit flows from the SignalR connection request (cookie + RequestLocalization middleware). Setting `CultureInfo.CurrentCulture` inside an event handler only affects that async flow; later events of the same circuit start again from the connection's culture. Therefore a live switch within a circuit can only be achieved for texts that are retrieved with an explicit culture. Other culture-sensitive formatting (`@price.ToString("C")`, DataAnnotations messages) follows the circuit culture until the page is reloaded.
- `TranslationUnit.CurrentValue` caches one value per unit in unsynchronized fields. With `UseContextCulture` and concurrent circuits of different cultures it thrashes and can momentarily return the other circuit's language. Blazor code must call `unit.GetValue(culture, ...)` with an explicit culture and never rely on `CurrentValue` on the server.
- Generated units are static singletons shared by all circuits. `CachePlaceholderValue` and `OnPlaceholderValueNeeded` are process-wide; per-user values must not be stored there on the server.
- No JavaScript file and no static web assets are shipped (packaging static web assets via the hand-written `Tlumach.nuspec` would leak build props to every consumer). Browser access uses only built-in functions through `IJSRuntime` (`fetch`, `localStorage.getItem`, `localStorage.setItem`, `document.documentElement.getAttribute`; Blazor binds a function found through a dotted path to its parent object, so `getAttribute` runs on `<html>`).
- A spike confirmed that a Blazor WebAssembly app publishes (Release, trimmed, relinked) without warnings when the package's `lib/net10.0` contains an assembly that references `Microsoft.AspNetCore.App` types it never uses; the assembly is carried as a small unused `.wasm` file.

## Decisions

| # | Topic | Decision |
|---|---|---|
| 1 | Switch behaviour on Server | Live by default (per-user state, components re-render, culture persisted). Opt-in `forceReload` saves the culture (the cookie is written before the reload starts) and reloads the page so that all .NET formatting switches too. WASM/Hybrid are always live. |
| 2 | JS asset | None. Cookie written by a server endpoint called through `fetch`; in a Web App the WASM client reads the culture from `<html lang>` rendered by the server; localStorage via built-in `IJSRuntime` calls for standalone WASM/Hybrid. Persistence is behind `ITlumachCultureStore`. |
| 3 | Re-render mechanism | Both: a root-level cascading value (`CascadingValueSource<TlumachCulture>`) for components, and a public `CultureChanged` event on the scoped state for non-component consumers. |
| 4 | Component name | `<TlumachText>`. |
| 5 | Server-only helpers | Separate assembly `Tlumach.AspNetCore` in the same `AlliedBits.Tlumach` package, only under `lib/net9.0` and `lib/net10.0`. |

## 1. Assemblies and public surface

### Tlumach.Blazor

Razor class library (`Microsoft.NET.Sdk.Razor`), `net9.0;net10.0`, `IsAotCompatible=true`, nullable enabled, no JS/CSS. References `Tlumach`, `Tlumach.Base`, `Tlumach.Extensions.Localization`, and `Microsoft.AspNetCore.Components.Web` (package reference for the matching major version).

| Type | Purpose |
|---|---|
| `TlumachBlazorOptions` | `SupportedCultures` (`IList<CultureInfo>`), `DefaultCulture`, `DefaultManager` (for `Key=` lookups), `ApplyCultureGlobally` (`bool?`; `null` means `OperatingSystem.IsBrowser()`), `Persistence` (`TlumachCulturePersistence` flags `None`, `Cookie`, `LocalStorage`; `null` means `LocalStorage` in the browser and `Cookie` elsewhere), `CultureEndpoint` (default `/tlumach/culture`), `LocalStorageKey` (default `tlumach.culture`). |
| `TlumachCultureState` (scoped) | Per-user culture: `Culture`, `SupportedCultures`, public `event EventHandler<CultureChangedEventArgs>? CultureChanged` (reuses `Tlumach.CultureChangedEventArgs`, whose constructor becomes public), `Current` (the `TlumachCulture` snapshot), `Task SetCultureAsync(CultureInfo culture, bool forceReload = false)`. Owns the `CascadingValueSource<TlumachCulture>`. |
| `TlumachCulture` | Immutable snapshot cascaded to components. `Culture` plus helpers that always pass the explicit culture: `Get(BaseTranslationUnit unit)`, `Get(unit, params object[] values)`, `Get(unit, IDictionary<string, object?> args)`, `GetFrom<[DynamicallyAccessedMembers(PublicProperties)] TArgs>(unit, TArgs args)`, `Markup(unit, ...)` returning `MarkupString`. |
| `<TlumachText>` | Parameters: `Unit` (`BaseTranslationUnit?`) or `Key` (`string?`) with optional `Manager` (`TranslationManager?`, defaults to `options.DefaultManager`); `Args` (`IDictionary<string, object?>?`), `Values` (`object[]?`), `AsMarkup` (`bool`). Takes `[CascadingParameter] TlumachCulture`. |
| `<TlumachCultureSelector>` | `<select>` of `SupportedCultures` labelled with `NativeName` (overridable via `DisplayName` func), `ForceReload`, `CssClass`/additional attributes. When `RendererInfo.IsInteractive` is `false` it renders a GET `<form>` targeting the culture endpoint with a hidden `redirectUri` and a submit button. |
| `TlumachComponentBase` | Optional base: `[CascadingParameter] TlumachCulture`, `[Inject] TlumachCultureState`, `protected string T(...)` overloads mirroring `TlumachCulture.Get`, `TFrom<TArgs>(...)` mirroring `GetFrom`, `protected virtual Task OnCultureChangedAsync(TlumachCulture culture)` called before the re-render that a culture change causes. The base subscribes to nothing, so it needs no `Dispose`. |
| `ITlumachCultureStore` | `ValueTask<string?> LoadAsync()`, `ValueTask SaveAsync(CultureInfo culture)`. Built-ins: `CookieCultureStore` (POST `fetch` to `CultureEndpoint?culture=..`), `LocalStorageCultureStore`, and a composite used when both flags are set. |
| `TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor(this IServiceCollection, Action<TlumachBlazorOptions>?)` | Registration (section 4). |
| `TlumachBlazorServiceProviderExtensions.LoadTlumachCultureAsync(this IServiceProvider)` | For WASM clients before `RunAsync()`: reads the store, sets `CultureInfo.DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture`. Defined on `IServiceProvider` so that the library does not need a WebAssembly package reference; callers pass `host.Services`. |

### Tlumach.AspNetCore

Class library, `net9.0;net10.0`, `FrameworkReference Microsoft.AspNetCore.App`, `IsAotCompatible=true`. References `Tlumach.Blazor` only for `TlumachBlazorOptions` (supported cultures, endpoint path).

- `app.UseTlumachRequestLocalization()` — configures the stock `RequestLocalizationMiddleware` from `TlumachBlazorOptions.SupportedCultures`/`DefaultCulture` with `CookieRequestCultureProvider` first.
- `app.MapTlumachCultureEndpoint(string pattern = "/tlumach/culture")`:
  - `POST ?culture=de` — sets the `.AspNetCore.Culture` cookie (`CookieRequestCultureProvider.MakeCookieValue`, one year, `SameSite=Lax`, `Path=/`, `HttpOnly`, `IsEssential`, `Secure` on HTTPS), returns 204. Both routes use `RequestDelegate` handlers (no `Delegate` overloads), so they need no Request Delegate Generator and stay AOT-safe.
  - `GET ?culture=de&redirectUri=/x` — sets the cookie and redirects; a non-local `redirectUri` falls back to `/` (open-redirect protection).
  - A culture that is not in `SupportedCultures` (or not a valid culture name) returns 400 and sets nothing.

### Change to Tlumach.Extensions.Localization

`AddTlumachLocalization` registers `IStringLocalizer<>` and `IStringLocalizer` with `TryAdd` instead of `Add`, so that the order of `AddTlumachLocalization` and `AddTlumachBlazor` does not matter.

## 2. Culture lifecycle

### Initial culture

`TlumachCultureState` reads `CultureInfo.CurrentUICulture` once when its scope is created and normalizes it against `SupportedCultures`: exact match, then parent culture, then `DefaultCulture`, then the first supported culture. With no supported cultures configured, the current UI culture is used unchanged.

- Server / static SSR / prerender: RequestLocalization applies the cookie both to page requests and to the `/_blazor` connection request, so the prerender scope and the subsequent circuit start from the same culture.
- WASM (standalone and Web App client): `LoadTlumachCultureAsync()` runs before `RunAsync()`, reads the store and sets the default thread cultures; the state picks them up. In a Web App both sides use `Persistence = Cookie`: the server renders `<html lang="@CultureInfo.CurrentUICulture.Name">` in `App.razor` from the cookie culture, and `CookieCultureStore.LoadAsync` reads that attribute (`document.documentElement.getAttribute("lang")`), so the server prerender, the static SSR form path and the client always agree. Standalone WASM uses `LocalStorage` (the browser default).
- Hybrid: `Persistence = LocalStorage` and `ApplyCultureGlobally = true` set explicitly (not detectable), or the application sets the culture before the first render.

### SetCultureAsync(culture, forceReload)

1. Normalize and validate against `SupportedCultures`; an unsupported culture throws `ArgumentException`. If it equals the current culture, return.
2. `forceReload`: save to the store (the cookie `fetch` completes before the call returns, because JS interop awaits the returned promise), then navigate to the current URI with `forceLoad: true`. Return. The GET route of the endpoint is used only by the non-interactive (static SSR) selector form.
3. Otherwise:
   1. Update `Culture` and the cascaded `TlumachCulture` snapshot.
   2. Set `CultureInfo.CurrentCulture`/`CurrentUICulture` for the current async flow.
   3. If `ApplyCultureGlobally` (WASM default; Hybrid sets it explicitly): also set `CultureInfo.DefaultThreadCurrentCulture`/`DefaultThreadCurrentUICulture` and `CurrentCulture` on every manager in `TranslationManager.TranslationManagers` (except `TranslationManager.Empty`), so plain `@Strings.Hello` and all other .NET formatting follow. Never on the server.
   4. Raise `CultureChanged`, then `await NotifyChangedAsync()` on the cascading source. Each subscribing component re-renders through its own renderer dispatcher. (Notification precedes saving so that the UI does not wait for a network round trip.)
   5. Save through `ITlumachCultureStore`. Failures (e.g. `JSDisconnectedException`, `JSException`, prerender without JS) are logged through `ILogger<TlumachCultureState>` and do not throw; the live switch has already happened.

### Render-mode boundaries

`AddTlumachBlazor()` must be called in both the server and the client `Program.cs`. Server and WASM islands each have their own state (the circuit's and the browser's). A switch inside a WASM island persists to the cookie, but Server islands on the same page pick it up only on the next load, and vice versa. This is documented; a uniform render mode per page avoids it.

### Prerendering

The store is only touched in `SetCultureAsync` and `LoadTlumachCultureAsync`, never during prerender. Both scopes initialize from the same cookie, so prerendered HTML matches the interactive render.

## 3. Encoding, placeholders, trimming

### Encoding

- `<TlumachText>` renders plain text with `AddContent` (Razor encodes it).
- If the unit's manager has `WebEncodeValues = true`, the text is already HTML-encoded; `<TlumachText>` renders it with `AddMarkupContent` to avoid double encoding (safe because it is encoded).
- `AsMarkup="true"` renders the raw text as markup — for trusted HTML translations only (translators then control markup). With `WebEncodeValues` on, tags show literally, which is still safe.
- `T(...)`/`Get(...)` return strings for attributes and code; when `WebEncodeValues` is on they apply `WebUtility.HtmlDecode` so attributes are not double-encoded. `Markup(...)` returns a `MarkupString` with the same rules as `<TlumachText>`.
- Docs recommend leaving `WebEncodeValues` off in Blazor; it stays the tool for Razor Pages/MVC with `@Html.Raw`.

### Placeholders

- Named: `<TlumachText Unit="Strings.Greeting" Args="@(new Dictionary<string, object?> { ["name"] = user })" />`.
- Indexed: `Values="@(new object[] { count })"`.
- Code: `TFrom(Strings.Greeting, new { name = user })` (on `TlumachComponentBase`) and `GetFrom(...)` (on `TlumachCulture`) map to `GetValueFrom<TArgs>` (trim-safe for anonymous types). They are deliberately not overloads of `T`/`Get`: a generic overload would win overload resolution against `params object[]` and `IDictionary` arguments (identity conversion) and silently switch those calls to property-based lookup.
- No `object Args` component parameter (it would require the reflection path marked `RequiresUnreferencedCode`).
- ICU formatting (plural/select/number/date) uses the explicit per-user culture.
- Docs warn that `CachePlaceholderValue`/`OnPlaceholderValueNeeded` are process-wide on the server.

### Key lookup

`<TlumachText Key="Hello" />` uses `Manager ?? options.DefaultManager`, calls `manager.GetValue(key, culture)` and, when the entry contains placeholders and `Args`/`Values` are given, `entry.ProcessTemplatedValue(culture, mode, ...)` where `mode` is `manager.DefaultConfiguration?.TextProcessingMode ?? TextFormat.None`. A missing key renders as empty (consistent with `TranslationUnit`); the manager's `OnTranslationValueNotFound` still fires. With neither `Unit` nor `Key` (or `Key` without any manager) the component throws `InvalidOperationException` from `OnParametersSet`.

### Trimming, AOT and WASM globalization

- Both new assemblies are `IsAotCompatible`; no reflection in the hot path; generic helpers carry `DynamicallyAccessedMembers`.
- Generated units load configuration and translations from embedded resources, which work under WASM.
- Runtime culture switching in WASM requires `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` and no `InvariantGlobalization`. `LoadTlumachCultureAsync` logs a warning when invariant globalization is detected (`AppContext` switch `System.Globalization.Invariant`).
- Verification publishes the sample WASM client trimmed.

## 4. Dependency injection

`AddTlumachBlazor(configure)`:

- `TlumachBlazorOptions` — singleton.
- `TlumachCultureState` — scoped.
- `ITlumachCultureStore` — scoped via `TryAdd` (an application-registered store wins), chosen from `Persistence`.
- Root-level cascading value: `services.AddCascadingValue(sp => sp.GetRequiredService<TlumachCultureState>().CascadingSource)`.
- `IStringLocalizer<T>` and `IStringLocalizer` — scoped wrappers registered with `Replace`. Each wrapper obtains the inner localizer from `IStringLocalizerFactory`; if it is a `TlumachStringLocalizer`, every call goes through `WithCulture(state.Culture)`; otherwise the inner localizer is used unchanged.

Known limitation (documented): DataAnnotations validation messages are formatted under the circuit's ambient culture and on the server switch only after a reload (`ForceReload` covers this).

Server `Program.cs`:

```csharp
builder.Services.AddTlumachLocalization(o => o.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachBlazor(o => o.SupportedCultures = [new("en-US"), new("de-DE"), new("uk-UA")]);
// ...
app.UseTlumachRequestLocalization();
app.MapTlumachCultureEndpoint();
```

WASM client `Program.cs`:

```csharp
builder.Services.AddTlumachBlazor(o =>
{
    o.SupportedCultures = [...];
    o.Persistence = TlumachCulturePersistence.Cookie; // Web App client: reads <html lang> on startup
});
var host = builder.Build();
await host.Services.LoadTlumachCultureAsync();
await host.RunAsync();
```

## 5. Deliverables

### Projects and solutions

- `src/Tlumach.Blazor`, `src/Tlumach.AspNetCore` — added to `src/Tlumach.sln`, `src/Tlumach.Main.sln` (no workloads needed; builds on ubuntu CI) and `tests/Tlumach.Tests.sln`.

### Packaging (`Tlumach.nuspec`)

- `Tlumach.Blazor.dll` + `.xml` in every net9/net10 lib folder (`net9.0`, `net9.0-windows7.0`, `net9.0-windows10.0.19041.0`, `net9.0-windows10.0.26100.0`, `net9.0-android21.0`, `net9.0-ios15.0`, `net9.0-maccatalyst15.0` and the net10.0 equivalents) to cover Hybrid hosts.
- `Tlumach.AspNetCore.dll` + `.xml` only in `lib/net9.0` and `lib/net10.0`.
- No new dependency groups, consistent with how `Tlumach.Extensions.Localization` ships; every Blazor host already references `Microsoft.AspNetCore.Components.Web`.

### Samples

- `samples/Tlumach.Sample.Blazor` — Blazor Web App server project (net10.0): static SSR home page with the form-based selector, an Interactive Server page and an Interactive WebAssembly page, each showing `<TlumachText>`, an ICU plural with placeholders, an attribute bound through `T()`, an injected `IStringLocalizer<Strings>`, the culture selector and a `ForceReload` demo.
- `samples/Tlumach.Sample.Blazor.Client` — the WASM client project (`BlazorWebAssemblyLoadAllGlobalizationData`).
- `samples/Tlumach.Sample.Blazor.Translation` — netstandard2.0 project with the generator and embedded translation files (pattern of `Tlumach.Sample.Avalonia.Translation`).
- No standalone WASM or MAUI Hybrid sample; Hybrid is covered by a docs section.

### Tests

`tests/Tlumach.BlazorTests` (net10.0, xUnit, bUnit, `Microsoft.AspNetCore.TestHost`), added to `tests/Tlumach.Tests.sln` and as a step in `.github/workflows/build-test.yml`:

- `<TlumachText>` renders in the state's culture; re-renders after `SetCultureAsync`.
- Two concurrent scopes (two bUnit contexts) with different cultures stay isolated.
- Disposal: `TlumachComponentBase`-derived and event subscribers leave no `CultureChanged` subscriptions behind; disposed components are not re-rendered.
- Encoding: `<script>` in a translation is escaped; `WebEncodeValues` is not double-encoded; `AsMarkup` renders raw; `T()` decodes under `WebEncodeValues`.
- `Args`, `Values`, `Key` lookup, missing key, missing manager.
- Selector: interactive `<select>` calls `SetCultureAsync`; non-interactive renders the form.
- Stores: expected `fetch`/`localStorage`/`getAttribute` calls through the bUnit JSInterop mock; save failures do not break the switch.
- `ApplyCultureGlobally` updates managers and default thread cultures; off leaves them untouched.
- Localizer wrapper follows the state culture, with both registration orders of `AddTlumachLocalization`/`AddTlumachBlazor`.
- Endpoint: POST sets the cookie (204); GET sets the cookie and redirects locally; unsupported culture returns 400; non-local `redirectUri` falls back to `/`.

### Documentation

- New `docs/articles/getting-started-blazor.md` (setup per hosting model, components, placeholders, encoding, persistence, render-mode boundaries, WASM globalization, Hybrid, server caveats) and its `docs/articles/toc.yml` entry.
- Updates: `docs/articles/index.md`, `strings.md` and `di.md` (WebEncodeValues vs Blazor; localizer in Blazor), `README.nuget.md`, `CHANGELOG.md` (`[NEW] ...` entries), `CLAUDE.md` repository layout.

### Verification

- `dotnet build src/Tlumach.sln`, `dotnet build src/Tlumach.Main.sln`, `dotnet test tests/Tlumach.Tests.sln`.
- Trimmed `dotnet publish` of the sample client.
- Run the sample through the preview tools and switch languages on the Server page, the WebAssembly page and the static SSR page (including `ForceReload`).
- `graphify update .` after code changes.

## Out of scope

- Fixing the `TranslationUnit.CurrentValue` race for concurrent cultures in the core (Blazor code avoids it; can be a follow-up).
- Live switching of DataAnnotations messages on Blazor Server.
- Synchronizing Server and WASM islands on the same page without a reload.
