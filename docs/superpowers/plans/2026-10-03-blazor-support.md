# Blazor Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add first-class Blazor support (`Tlumach.Blazor` + `Tlumach.AspNetCore`) with per-user culture, live language switching, components, persistence helpers, packaging, a Web App sample, bUnit tests, and docs.

**Architecture:** A scoped `TlumachCultureState` holds each user's culture (one per circuit on Server, effectively one per app in WASM/Hybrid). It cascades an immutable `TlumachCulture` snapshot through a root-level `CascadingValueSource`, so `<TlumachText>`, `<TlumachCultureSelector>`, and `TlumachComponentBase` re-render on a switch without any event subscriptions; a public `CultureChanged` event serves non-component consumers. Texts are always fetched with the explicit per-user culture (`unit.GetValue(culture, ...)`), never through the process-wide `TranslationManager.CurrentCulture`. Persistence goes through `ITlumachCultureStore` (cookie via a `fetch` POST to an endpoint mapped by `Tlumach.AspNetCore`, `<html lang>` read-back for Web App WASM clients, localStorage for standalone WASM/Hybrid) — no JavaScript file is shipped.

**Tech Stack:** C# (LangVersion latest), Blazor (`Microsoft.AspNetCore.Components.Web` 9.0/10.0), ASP.NET Core (`Microsoft.AspNetCore.App` framework reference), `net9.0;net10.0`, xUnit 2.9, bUnit 2.11.3, `Microsoft.AspNetCore.TestHost` 10.x.

**Spec:** `docs/superpowers/specs/2026-10-03-blazor-support-design.md`

## Global Constraints

- Work on branch `blazor`. **Ask the user before the first commit; never push.** Commit steps below assume the user has approved per-task commits.
- Every commit message ends with the line `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Every new `.cs` file (library, tests, sample) starts with the exact header used across the repo (StyleCop checks it against `src/Shared/stylecop.json`, which says **2025**); `FILE_NAME.cs` is replaced by the file's own name. `.razor` files get no header.

  ```csharp
  // <copyright file="FILE_NAME.cs" company="Allied Bits Ltd.">
  //
  // Copyright 2025 Allied Bits Ltd.
  //
  // Licensed under the Apache License, Version 2.0 (the "License");
  // you may not use this file except in compliance with the License.
  // You may obtain a copy of the License at
  //
  // http://www.apache.org/licenses/LICENSE-2.0
  //
  // Unless required by applicable law or agreed to in writing, software
  // distributed under the License is distributed on an "AS IS" BASIS,
  // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  // See the License for the specific language governing permissions and
  // limitations under the License.
  //
  // </copyright>
  ```

  The code blocks below omit this header for brevity; **add it to every new `.cs` file**.
- 4-space indentation, UTF-8. `.gitattributes` normalizes line endings (`* text=auto`), so LF from editors is fine for new files; keep CRLF when editing existing CRLF files (`Tlumach.nuspec`, `.sln` files).
- New code in `src/Tlumach.Blazor`, `src/Tlumach.AspNetCore`, and `tests/Tlumach.BlazorTests` uses file-scoped namespaces and `ImplicitUsings=enable`. Edits to existing files keep their style (e.g. block namespaces in `tests/Tlumach.Tests`).
- Analyzers (StyleCop, Roslynator, Sonar, Meziantou, NetAnalyzers; `AnalysisMode=AllEnabledByDefault`) run in every build. Fix new warnings in new code. Suppress only with a `#pragma warning disable XXX // reason` pair when the rule is understood and does not apply. Already disabled in `.editorconfig`: CA1510, SA1101, SA1200, SA1309, SA1600, SA1633, S3267.
- `await` policy (CA2007 is on): in components and `TlumachCultureState` use `.ConfigureAwait(true)` (stay on the renderer's synchronization context); in stores, the localizer, `LoadTlumachCultureAsync`, and `Tlumach.AspNetCore` use `.ConfigureAwait(false)`. Test code does not use `ConfigureAwait`.
- Logging uses `LoggerMessage.Define` static delegates (CA1848), not `logger.LogWarning(...)`.
- No reflection in the hot path; both new assemblies are `IsAotCompatible`. Minimal-API endpoints use `RequestDelegate` handlers only (no `Delegate` overloads).
- bUnit: a component that reads `RendererInfo` (only `TlumachCultureSelector` and the sample's `DemoPanel`) needs `ctx.SetRendererInfo(...)` in tests. Tests use `JSRuntimeMode.Loose` unless they verify specific JS calls.
- Public API names (used across tasks):
  - `Tlumach.Blazor`: `TlumachBlazorOptions`, `TlumachCulturePersistence`, `TlumachCulture`, `ITlumachCultureStore`, `CookieCultureStore`, `LocalStorageCultureStore`, `TlumachCultureState`, `TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor`, `TlumachBlazorServiceProviderExtensions.LoadTlumachCultureAsync`, `TlumachText`, `TlumachComponentBase`, `TlumachCultureSelector`; internal: `CompositeCultureStore`, `CultureStoreFactory`, `CultureApplier`, `TlumachCultureStringLocalizer`, `TlumachCultureStringLocalizer<T>`.
  - `Tlumach.AspNetCore`: `TlumachAspNetCoreExtensions.UseTlumachRequestLocalization`, `TlumachAspNetCoreExtensions.MapTlumachCultureEndpoint`; internal `TlumachAspNetCoreExtensions.IsLocalUrl`.
- Run the Blazor tests with: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`.

## File Structure

| File | Responsibility |
|---|---|
| `src/Tlumach/CultureChangedEventArgs.cs` (modify) | Constructor becomes public |
| `src/Tlumach.Extensions.Localization/TlumachLocalizationExtensions.cs` (modify) | `TryAdd` for `IStringLocalizer`/`IStringLocalizer<>` |
| `src/Tlumach.Blazor/Tlumach.Blazor.csproj` | Razor class library, `net9.0;net10.0` |
| `src/Tlumach.Blazor/TlumachCulturePersistence.cs` | `[Flags]` enum: where the culture is stored |
| `src/Tlumach.Blazor/TlumachBlazorOptions.cs` | Options + supported-culture matching |
| `src/Tlumach.Blazor/TlumachCulture.cs` | Immutable snapshot + text helpers with explicit culture |
| `src/Tlumach.Blazor/ITlumachCultureStore.cs` | Persistence abstraction |
| `src/Tlumach.Blazor/CookieCultureStore.cs` | `fetch` POST to the culture endpoint; reads `<html lang>` |
| `src/Tlumach.Blazor/LocalStorageCultureStore.cs` | `localStorage.getItem/setItem` |
| `src/Tlumach.Blazor/CompositeCultureStore.cs` | Several stores as one (internal) |
| `src/Tlumach.Blazor/CultureStoreFactory.cs` | Builds the store from `Persistence` (internal) |
| `src/Tlumach.Blazor/CultureApplier.cs` | Process-wide culture for single-user hosts (internal) |
| `src/Tlumach.Blazor/TlumachCultureState.cs` | Scoped per-user culture, switching, cascading source |
| `src/Tlumach.Blazor/TlumachBlazorServiceCollectionExtensions.cs` | `AddTlumachBlazor` |
| `src/Tlumach.Blazor/TlumachBlazorServiceProviderExtensions.cs` | `LoadTlumachCultureAsync` for WASM startup |
| `src/Tlumach.Blazor/TlumachText.cs` | `<TlumachText>` component |
| `src/Tlumach.Blazor/TlumachComponentBase.cs` | Optional base class with `T(...)` helpers |
| `src/Tlumach.Blazor/TlumachCultureSelector.cs` | `<TlumachCultureSelector>` (interactive select / SSR form) |
| `src/Tlumach.Blazor/TlumachCultureStringLocalizer.cs` | Culture-aware scoped `IStringLocalizer` wrappers (internal) |
| `src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj` | Class library with `Microsoft.AspNetCore.App` |
| `src/Tlumach.AspNetCore/TlumachAspNetCoreExtensions.cs` | Request localization + culture endpoint |
| `tests/Tlumach.BlazorTests/*` | bUnit + TestHost tests |
| `samples/Tlumach.Sample.Blazor.Translation/*` | netstandard2.0 generator project with ARB translations |
| `samples/Tlumach.Sample.Blazor.Client/*` | WASM client (shared demo panel, WASM page) |
| `samples/Tlumach.Sample.Blazor/*` | Web App server (SSR home, Server page, layout) |
| `docs/articles/getting-started-blazor.md` | New guide |

---

### Task 1: Core prerequisites

**Files:**
- Modify: `src/Tlumach/CultureChangedEventArgs.cs`
- Modify: `src/Tlumach.Extensions.Localization/TlumachLocalizationExtensions.cs`
- Create: `tests/Tlumach.Tests/CultureChangedEventArgsTests.cs`
- Modify: `tests/Tlumach.Tests/TlumachStringLocalizerTests.cs`

**Interfaces:**
- Produces: `public CultureChangedEventArgs(CultureInfo culture)`; `AddTlumachLocalization` no longer overrides `IStringLocalizer`/`IStringLocalizer<>` registered earlier.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.Tests/CultureChangedEventArgsTests.cs` (add the header):

```csharp
using System.Globalization;

namespace Tlumach.Tests
{
    public class CultureChangedEventArgsTests
    {
        [Fact]
        public void Constructor_IsPublicAndKeepsCulture()
        {
            CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

            CultureChangedEventArgs args = new(culture);

            Assert.Same(culture, args.Culture);
        }
    }
}
```

In `tests/Tlumach.Tests/TlumachStringLocalizerTests.cs`, add inside the class (before `CreateLocalizer`):

```csharp
        [Fact]
        public void AddTlumachLocalization_KeepsLocalizersRegisteredEarlier()
        {
            TranslationManager manager = new(Path.Combine(TestFilesPath, "Localizer.cfg"))
            {
                LoadFromDisk = true,
                TranslationsDirectory = TestFilesPath,
            };

            ServiceCollection services = new();
            services.AddTransient(typeof(IStringLocalizer<>), typeof(MarkerLocalizer<>));
            services.AddTlumachLocalization(options => options.TranslationManager = manager);

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsType<MarkerLocalizer<TlumachStringLocalizerTests>>(provider.GetRequiredService<IStringLocalizer<TlumachStringLocalizerTests>>());
        }

        private sealed class MarkerLocalizer<T> : IStringLocalizer<T>
        {
            public LocalizedString this[string name] => new(name, name);

            public LocalizedString this[string name, params object[] arguments] => new(name, name);

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj --filter "FullyQualifiedName~CultureChangedEventArgsTests|FullyQualifiedName~AddTlumachLocalization_KeepsLocalizersRegisteredEarlier"`
Expected: build error CS0122 (`CultureChangedEventArgs.CultureChangedEventArgs(CultureInfo)` is inaccessible). After temporarily commenting out that test file, the second test FAILS (`IsType` gets `TlumachStringLocalizer<...>`). Restore the file.

- [ ] **Step 3: Implement**

In `src/Tlumach/CultureChangedEventArgs.cs` replace

```csharp
        internal CultureChangedEventArgs(CultureInfo culture)
        {
```

with

```csharp
        /// <summary>
        /// Initializes a new instance of the <see cref="CultureChangedEventArgs"/> class.
        /// </summary>
        /// <param name="culture">The new culture.</param>
        public CultureChangedEventArgs(CultureInfo culture)
        {
```

In `src/Tlumach.Extensions.Localization/TlumachLocalizationExtensions.cs` add `using Microsoft.Extensions.DependencyInjection.Extensions;` and replace

```csharp
            services.AddTransient(typeof(IStringLocalizer<>), typeof(TlumachStringLocalizer<>));
            services.AddTransient<IStringLocalizer>(sp =>
```

with

```csharp
            // TryAdd keeps localizers registered earlier, e.g. the per-user ones of AddTlumachBlazor, so the order of the calls does not matter.
            services.TryAddTransient(typeof(IStringLocalizer<>), typeof(TlumachStringLocalizer<>));
            services.TryAddTransient<IStringLocalizer>(sp =>
```

Also update the XML `<summary>` of `AddTlumachLocalization` by appending: `<para>The <see cref="IStringLocalizer"/> and <see cref="IStringLocalizer{T}"/> registrations are added only if no other registration exists.</para>`

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj --filter "Category=Localization|FullyQualifiedName~CultureChangedEventArgsTests"`
Expected: PASS (all localization tests, including the existing ones).

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach/CultureChangedEventArgs.cs src/Tlumach.Extensions.Localization/TlumachLocalizationExtensions.cs tests/Tlumach.Tests/CultureChangedEventArgsTests.cs tests/Tlumach.Tests/TlumachStringLocalizerTests.cs
git commit -m "Make CultureChangedEventArgs constructible and keep earlier IStringLocalizer registrations

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Tlumach.Blazor project, options, and the TlumachCulture snapshot

**Files:**
- Create: `src/Tlumach.Blazor/Tlumach.Blazor.csproj`
- Create: `src/Tlumach.Blazor/TlumachCulturePersistence.cs`
- Create: `src/Tlumach.Blazor/TlumachBlazorOptions.cs`
- Create: `src/Tlumach.Blazor/TlumachCulture.cs`
- Create: `tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`
- Create: `tests/Tlumach.BlazorTests/TestAssemblyInfo.cs`
- Create: `tests/Tlumach.BlazorTests/TestTranslations.cs`
- Create: `tests/Tlumach.BlazorTests/TlumachBlazorOptionsTests.cs`
- Create: `tests/Tlumach.BlazorTests/TlumachCultureTests.cs`

**Interfaces:**
- Consumes: `BaseTranslationUnit.GetValue(CultureInfo)`, `GetValue(CultureInfo, params object[])`, `GetValue(CultureInfo, IDictionary<string, object?>)`, `GetValueFrom<T>(CultureInfo, T)`; `TranslationManager.GetValue(string key, CultureInfo culture)`; `TranslationEntry.ProcessTemplatedValue(CultureInfo, TextFormat, IDictionary<string, object?>)` / `(CultureInfo, TextFormat, params object?[])`.
- Produces:
  - `TlumachBlazorOptions` with `IReadOnlyList<CultureInfo> SupportedCultures`, `CultureInfo? DefaultCulture`, `TranslationManager? DefaultManager`, `bool? ApplyCultureGlobally`, `TlumachCulturePersistence? Persistence`, `string CultureEndpoint` (default `"/tlumach/culture"`), `string LocalStorageKey` (default `"tlumach.culture"`), `CultureInfo? FindSupportedCulture(CultureInfo)`, internal `CultureInfo ResolveInitialCulture(CultureInfo)`, internal `bool EffectiveApplyCultureGlobally`, internal `TlumachCulturePersistence EffectivePersistence`.
  - `TlumachCulture(CultureInfo)` with `Culture`, `Get(unit)`, `Get(unit, params object[])`, `Get(unit, IDictionary<string, object?>)`, `GetFrom<TArgs>(unit, TArgs)`, `Markup(unit)`, `Markup(unit, IDictionary<string, object?>)`, internal `GetRaw(unit, IDictionary<string, object?>? args, object[]? values)`, internal `GetByKey(TranslationManager, string key, IDictionary<string, object?>? args, object[]? values)`.
  - Test fixture `TestTranslations` with `Manager`, `Configuration`, units `Hello`, `Greeting` (`{name}`), `Position` (`{0}`/`{1}`), `Items` (ICU plural on `{count}`), `Rich` (`<b>` markup), `Script` (`<script>`); static `En` (`en-US`), `De` (`de-DE`).

- [ ] **Step 1: Create the library project**

`src/Tlumach.Blazor/Tlumach.Blazor.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

    <PropertyGroup>
        <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <LangVersion>latest</LangVersion>
        <IsAotCompatible>true</IsAotCompatible>
    </PropertyGroup>

    <ItemGroup>
        <!-- Enables the platform compatibility analyzer for the browser (Blazor WebAssembly). -->
        <SupportedPlatform Include="browser" />
    </ItemGroup>

    <ItemGroup>
        <AdditionalFiles Include="../Shared/stylecop.json" />
    </ItemGroup>

    <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
        <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.*" />
    </ItemGroup>

    <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
        <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.*" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Tlumach.Base\Tlumach.Base.csproj" />
        <ProjectReference Include="..\Tlumach\Tlumach.csproj" />
        <ProjectReference Include="..\Tlumach.Extensions.Localization\Tlumach.Extensions.Localization.csproj" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="Tlumach.BlazorTests" />
    </ItemGroup>

</Project>
```

`src/Tlumach.Blazor/TlumachCulturePersistence.cs`:

```csharp
namespace Tlumach.Blazor;

/// <summary>
/// Specifies where <see cref="TlumachCultureState"/> stores the culture that the user has chosen.
/// </summary>
[Flags]
#pragma warning disable CA1714 // Flags enums should have plural names - "persistence" names the feature; the values are the places where it stores the culture
public enum TlumachCulturePersistence
#pragma warning restore CA1714
{
    /// <summary>
    /// The culture is not stored.
    /// </summary>
    None = 0,

    /// <summary>
    /// The culture is stored in the ASP.NET Core culture cookie by the endpoint that <c>MapTlumachCultureEndpoint</c> maps,
    /// and a WebAssembly client of a Blazor Web App reads it back from the <c>lang</c> attribute of the <c>html</c> element.
    /// </summary>
    Cookie = 1,

    /// <summary>
    /// The culture is stored in the local storage of the browser.
    /// </summary>
    LocalStorage = 2,
}
```

`src/Tlumach.Blazor/TlumachBlazorOptions.cs`:

```csharp
using System.Globalization;

namespace Tlumach.Blazor;

/// <summary>
/// The options of the Blazor integration of Tlumach, configured in <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/>.
/// </summary>
public sealed class TlumachBlazorOptions
{
    /// <summary>
    /// The default value of <see cref="CultureEndpoint"/>.
    /// </summary>
    public const string DefaultCultureEndpoint = "/tlumach/culture";

    /// <summary>
    /// The default value of <see cref="LocalStorageKey"/>.
    /// </summary>
    public const string DefaultLocalStorageKey = "tlumach.culture";

    /// <summary>
    /// Gets or sets the cultures that the user may choose. When the list is empty, any culture is accepted.
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; set; } = [];

    /// <summary>
    /// Gets or sets the culture used when the culture of the request or of the browser is not supported.
    /// <para>When not set, the first supported culture is used.</para>
    /// </summary>
    public CultureInfo? DefaultCulture { get; set; }

    /// <summary>
    /// Gets or sets the translation manager, from which <see cref="TlumachText"/> takes texts by key when its <c>Manager</c> parameter is not set.
    /// </summary>
    public TranslationManager? DefaultManager { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a culture switch also changes the process-wide culture: <see cref="CultureInfo.DefaultThreadCurrentCulture"/>,
    /// <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>, and <see cref="TranslationManager.CurrentCulture"/> of every translation manager.
    /// <para>This is right only when one user owns the process: Blazor WebAssembly and Blazor Hybrid. When <see langword="null"/>, the value is
    /// <see langword="true"/> in the browser and <see langword="false"/> elsewhere, so Blazor Hybrid applications set it to <see langword="true"/> explicitly.</para>
    /// </summary>
    public bool? ApplyCultureGlobally { get; set; }

    /// <summary>
    /// Gets or sets where the chosen culture is stored. When <see langword="null"/>, it is <see cref="TlumachCulturePersistence.LocalStorage"/> in the browser and
    /// <see cref="TlumachCulturePersistence.Cookie"/> elsewhere. A WebAssembly client of a Blazor Web App and a Blazor Hybrid application set it explicitly.
    /// </summary>
    public TlumachCulturePersistence? Persistence { get; set; }

    /// <summary>
    /// Gets or sets the path of the endpoint that stores the culture in a cookie, relative to the base path of the application.
    /// </summary>
    public string CultureEndpoint { get; set; } = DefaultCultureEndpoint;

    /// <summary>
    /// Gets or sets the key, under which the culture is kept in the local storage of the browser.
    /// </summary>
    public string LocalStorageKey { get; set; } = DefaultLocalStorageKey;

    internal bool EffectiveApplyCultureGlobally => ApplyCultureGlobally ?? OperatingSystem.IsBrowser();

    internal TlumachCulturePersistence EffectivePersistence
        => Persistence ?? (OperatingSystem.IsBrowser() ? TlumachCulturePersistence.LocalStorage : TlumachCulturePersistence.Cookie);

    /// <summary>
    /// Finds the supported culture that serves the given culture: the culture itself, then the closest of its parent cultures, then a culture of the same language.
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    /// <returns>The matching supported culture, the requested culture itself when <see cref="SupportedCultures"/> is empty, or <see langword="null"/> if no supported culture matches.</returns>
    public CultureInfo? FindSupportedCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (SupportedCultures.Count == 0)
            return culture;

        for (CultureInfo? current = culture; current is not null; current = current.Name.Length == 0 ? null : current.Parent)
        {
            foreach (CultureInfo supported in SupportedCultures)
            {
                if (supported.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase))
                    return supported;
            }
        }

        string language = culture.TwoLetterISOLanguageName;
        foreach (CultureInfo supported in SupportedCultures)
        {
            if (supported.TwoLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase))
                return supported;
        }

        return null;
    }

    /// <summary>
    /// Picks the culture to start with: the supported culture that serves <paramref name="current"/>, else <see cref="DefaultCulture"/>, else the first supported culture.
    /// </summary>
    /// <param name="current">The culture of the request or of the browser.</param>
    /// <returns>The culture to use.</returns>
    internal CultureInfo ResolveInitialCulture(CultureInfo current)
    {
        CultureInfo? resolved = FindSupportedCulture(current);
        if (resolved is not null)
            return resolved;

        if (DefaultCulture is not null)
            return FindSupportedCulture(DefaultCulture) ?? DefaultCulture;

        return SupportedCultures.Count > 0 ? SupportedCultures[0] : current;
    }
}
```

`src/Tlumach.Blazor/TlumachCulture.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;

using Microsoft.AspNetCore.Components;

using Tlumach.Base;

namespace Tlumach.Blazor;

/// <summary>
/// An immutable snapshot of the culture of one user, cascaded to components by <see cref="TlumachCultureState"/>.
/// <para>A component that declares a <see cref="CascadingParameterAttribute">cascading parameter</see> of this type is re-rendered when the user switches the language.
/// The methods of this class retrieve texts for <see cref="Culture"/> explicitly, which is what keeps the users of a Blazor Server application apart.</para>
/// </summary>
public sealed class TlumachCulture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachCulture"/> class.
    /// </summary>
    /// <param name="culture">The culture of the user.</param>
    public TlumachCulture(CultureInfo culture)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    /// <summary>
    /// Gets the culture of the user.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> as plain (not HTML-encoded) text, suitable for attributes and code.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The text.</returns>
    public string Get(BaseTranslationUnit unit) => ToPlainText(unit, GetRaw(unit, args: null, values: null));

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with indexed placeholders replaced by <paramref name="values"/>.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="values">The values of the placeholders, in the order of their indexes.</param>
    /// <returns>The text.</returns>
    public string Get(BaseTranslationUnit unit, params object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return ToPlainText(unit, GetRaw(unit, args: null, values));
    }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with named placeholders replaced by the values in <paramref name="args"/>.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the placeholders, keyed by placeholder names.</param>
    /// <returns>The text.</returns>
    public string Get(BaseTranslationUnit unit, IDictionary<string, object?> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return ToPlainText(unit, GetRaw(unit, args, values: null));
    }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with named placeholders replaced by the values of the public properties of <paramref name="args"/>,
    /// e.g., an anonymous object. The trimmer keeps those properties, so this method is safe in trimmed applications.
    /// </summary>
    /// <typeparam name="TArgs">The type that supplies the values through its public properties.</typeparam>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The object that supplies the values.</param>
    /// <returns>The text.</returns>
    public string GetFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(BaseTranslationUnit unit, TArgs args)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return ToPlainText(unit, unit.GetValueFrom(Culture, args));
    }

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> as markup. Use it only for translations that contain trusted HTML.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The markup.</returns>
    public MarkupString Markup(BaseTranslationUnit unit) => new(GetRaw(unit, args: null, values: null));

    /// <summary>
    /// Returns the text of the unit for <see cref="Culture"/> with named placeholders replaced, as markup. Use it only for translations that contain trusted HTML.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the placeholders, keyed by placeholder names.</param>
    /// <returns>The markup.</returns>
    public MarkupString Markup(BaseTranslationUnit unit, IDictionary<string, object?> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new(GetRaw(unit, args, values: null));
    }

    /// <summary>
    /// Returns the text as the unit produces it, i.e. HTML-encoded when <see cref="TranslationManager.WebEncodeValues"/> is set.
    /// </summary>
    internal string GetRaw(BaseTranslationUnit unit, IDictionary<string, object?>? args, object[]? values)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (args is not null)
            return unit.GetValue(Culture, args);

        if (values is not null)
            return unit.GetValue(Culture, values);

        return unit.GetValue(Culture);
    }

    /// <summary>
    /// Returns the text of the entry with the given key, processing placeholders when values are supplied. A missing key yields an empty string.
    /// </summary>
    internal string GetByKey(TranslationManager manager, string key, IDictionary<string, object?>? args, object[]? values)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);

        TranslationEntry entry = manager.GetValue(key, Culture);
        string? text = entry.Text;
        if (text is null)
            return string.Empty;

        if (!entry.ContainsPlaceholders || (args is null && values is null))
            return text;

        TextFormat mode = manager.DefaultConfiguration?.TextProcessingMode ?? TextFormat.None;
        return args is not null
            ? entry.ProcessTemplatedValue(Culture, mode, args)
            : entry.ProcessTemplatedValue(Culture, mode, values!);
    }

    private static string ToPlainText(BaseTranslationUnit unit, string value)
        => unit.TranslationManager.WebEncodeValues ? WebUtility.HtmlDecode(value) : value;
}
```

- [ ] **Step 2: Build the library**

Run: `dotnet build src/Tlumach.Blazor/Tlumach.Blazor.csproj`
Expected: Build succeeded, no new warnings in `Tlumach.Blazor` files (fix any).

- [ ] **Step 3: Create the test project and fixture**

`tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="bunit" Version="2.11.3" />
    <PackageReference Include="coverlet.collector" Version="6.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.TestHost" Version="10.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tlumach.Base\Tlumach.Base.csproj" />
    <ProjectReference Include="..\..\src\Tlumach\Tlumach.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Extensions.Localization\Tlumach.Extensions.Localization.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Blazor\Tlumach.Blazor.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Bunit" />
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

`tests/Tlumach.BlazorTests/TestAssemblyInfo.cs`:

```csharp
// Several tests change process-wide state (CultureInfo.DefaultThreadCurrentCulture, TranslationManager.TranslationManagers),
// so the test classes must not run in parallel. Tests that check isolation start their concurrent work inside one test.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

`tests/Tlumach.BlazorTests/TestTranslations.cs`:

```csharp
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.BlazorTests;

/// <summary>
/// English and German ARB translations written to a temporary directory, with a manager and units for every kind of text the tests need.
/// </summary>
internal sealed class TestTranslations : IDisposable
{
    public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private const string DefaultArb = """
        {
            "@@locale": "en",
            "hello": "Hello",
            "greeting": "Hello, {name}!",
            "position": "Item {0} of {1}",
            "items": "{count, plural, =0{no items} =1{# item} other{# items}}",
            "rich": "Click <b>here</b>",
            "script": "<script>alert(1)</script>"
        }
        """;

    private const string GermanArb = """
        {
            "@@locale": "de",
            "hello": "Hallo",
            "greeting": "Hallo, {name}!",
            "position": "Element {0} von {1}",
            "items": "{count, plural, =0{keine Elemente} =1{# Element} other{# Elemente}}",
            "rich": "Klicken Sie <b>hier</b>",
            "script": "<script>alert(2)</script>"
        }
        """;

    private readonly string _directory;

    public TestTranslations()
    {
        ArbParser.Use();

        _directory = Path.Combine(Path.GetTempPath(), "TlumachBlazorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "Strings.arb"), DefaultArb);
        File.WriteAllText(Path.Combine(_directory, "Strings_de.arb"), GermanArb);

        Configuration = new TranslationConfiguration(assembly: null, "Strings.arb", "en", TextFormat.Arb) { DirectoryHint = _directory };
        Manager = new TranslationManager(Configuration) { LoadFromDisk = true, TranslationsDirectory = _directory };

        Hello = new TranslationUnit(Manager, Configuration, "hello", containsPlaceholders: false);
        Greeting = new TranslationUnit(Manager, Configuration, "greeting", containsPlaceholders: true);
        Position = new TranslationUnit(Manager, Configuration, "position", containsPlaceholders: true);
        Items = new TranslationUnit(Manager, Configuration, "items", containsPlaceholders: true);
        Rich = new TranslationUnit(Manager, Configuration, "rich", containsPlaceholders: false);
        Script = new TranslationUnit(Manager, Configuration, "script", containsPlaceholders: false);
    }

    public TranslationConfiguration Configuration { get; }

    public TranslationManager Manager { get; }

    public TranslationUnit Hello { get; }

    public TranslationUnit Greeting { get; }

    public TranslationUnit Position { get; }

    public TranslationUnit Items { get; }

    public TranslationUnit Rich { get; }

    public TranslationUnit Script { get; }

    public void Dispose()
    {
        Hello.Dispose();
        Greeting.Dispose();
        Position.Dispose();
        Items.Dispose();
        Rich.Dispose();
        Script.Dispose();
        Manager.Dispose();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary directory does not affect the results.
        }
    }
}
```

- [ ] **Step 4: Write the failing tests**

`tests/Tlumach.BlazorTests/TlumachBlazorOptionsTests.cs`:

```csharp
using System.Globalization;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public class TlumachBlazorOptionsTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void FindSupportedCulture_ExactMatch_ReturnsSupportedInstance()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(new CultureInfo("de-DE")));
    }

    [Fact]
    public void FindSupportedCulture_ParentCulture_IsUsed()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de");
        TlumachBlazorOptions options = new() { SupportedCultures = [En, german] };

        Assert.Same(german, options.FindSupportedCulture(CultureInfo.GetCultureInfo("de-AT")));
    }

    [Fact]
    public void FindSupportedCulture_SameLanguage_IsUsed()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(CultureInfo.GetCultureInfo("de-AT")));
    }

    [Fact]
    public void FindSupportedCulture_Unsupported_ReturnsNull()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Null(options.FindSupportedCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void FindSupportedCulture_NoSupportedCultures_ReturnsRequestedCulture()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Same(french, new TlumachBlazorOptions().FindSupportedCulture(french));
    }

    [Fact]
    public void ResolveInitialCulture_Unsupported_UsesDefaultCulture()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De], DefaultCulture = De };

        Assert.Same(De, options.ResolveInitialCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void ResolveInitialCulture_NoDefault_UsesFirstSupported()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(En, options.ResolveInitialCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void Defaults_EndpointAndStorageKey()
    {
        TlumachBlazorOptions options = new();

        Assert.Equal("/tlumach/culture", options.CultureEndpoint);
        Assert.Equal("tlumach.culture", options.LocalStorageKey);
        Assert.Equal(TlumachCulturePersistence.Cookie, options.EffectivePersistence);
        Assert.False(options.EffectiveApplyCultureGlobally);
    }
}
```

`tests/Tlumach.BlazorTests/TlumachCultureTests.cs`:

```csharp
using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public void Get_ReturnsTextOfSnapshotCulture()
    {
        Assert.Equal("Hello", new TlumachCulture(TestTranslations.En).Get(_translations.Hello));
        Assert.Equal("Hallo", new TlumachCulture(TestTranslations.De).Get(_translations.Hello));
    }

    [Fact]
    public void Get_NamedArguments_AreSubstituted()
    {
        string text = new TlumachCulture(TestTranslations.De).Get(_translations.Greeting, new Dictionary<string, object?> { ["name"] = "Anna" });

        Assert.Equal("Hallo, Anna!", text);
    }

    [Fact]
    public void Get_IndexedValues_AreSubstituted()
    {
        Assert.Equal("Element 2 von 5", new TlumachCulture(TestTranslations.De).Get(_translations.Position, 2, 5));
    }

    [Fact]
    public void Get_IcuPlural_UsesCount()
    {
        string text = new TlumachCulture(TestTranslations.En).Get(_translations.Items, new Dictionary<string, object?> { ["count"] = 3 });

        Assert.Equal("3 items", text);
    }

    [Fact]
    public void GetFrom_AnonymousObject_SuppliesNamedValues()
    {
        Assert.Equal("Hello, Anna!", new TlumachCulture(TestTranslations.En).GetFrom(_translations.Greeting, new { name = "Anna" }));
    }

    [Fact]
    public void Get_WebEncodeValues_ReturnsDecodedText()
    {
        _translations.Manager.WebEncodeValues = true;

        Assert.Equal("Click <b>here</b>", new TlumachCulture(TestTranslations.En).Get(_translations.Rich));
    }

    [Fact]
    public void Markup_ReturnsRawText()
    {
        Assert.Equal("Click <b>here</b>", new TlumachCulture(TestTranslations.En).Markup(_translations.Rich).Value);
    }

    [Fact]
    public void GetByKey_MissingKey_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, new TlumachCulture(TestTranslations.En).GetByKey(_translations.Manager, "missing", args: null, values: null));
    }

    [Fact]
    public void GetByKey_NamedArguments_AreSubstituted()
    {
        string text = new TlumachCulture(TestTranslations.De).GetByKey(_translations.Manager, "greeting", new Dictionary<string, object?> { ["name"] = "Anna" }, values: null);

        Assert.Equal("Hallo, Anna!", text);
    }
}
```

- [ ] **Step 5: Run the tests**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`
Expected: all PASS. (The tests were written after the code in this task because the project did not exist; they pin the behaviour for the following tasks.) If `Get_IcuPlural_UsesCount` or `Get_IndexedValues_AreSubstituted` fails, check how `tests/Tlumach.Tests/PlaceholderTests.cs` formats the same constructs in `TextFormat.Arb` and fix the test data, not the library.

- [ ] **Step 6: Commit**

```bash
git add src/Tlumach.Blazor tests/Tlumach.BlazorTests
git commit -m "Add Tlumach.Blazor with options and the TlumachCulture snapshot

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Culture stores

**Files:**
- Create: `src/Tlumach.Blazor/ITlumachCultureStore.cs`
- Create: `src/Tlumach.Blazor/CookieCultureStore.cs`
- Create: `src/Tlumach.Blazor/LocalStorageCultureStore.cs`
- Create: `src/Tlumach.Blazor/CompositeCultureStore.cs`
- Create: `src/Tlumach.Blazor/CultureStoreFactory.cs`
- Test: `tests/Tlumach.BlazorTests/CultureStoreTests.cs`

**Interfaces:**
- Consumes: `TlumachBlazorOptions.CultureEndpoint`, `LocalStorageKey`, `EffectivePersistence`.
- Produces: `ITlumachCultureStore { ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default); ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default); }`; `CookieCultureStore(IJSRuntime, NavigationManager, TlumachBlazorOptions)`; `LocalStorageCultureStore(IJSRuntime, TlumachBlazorOptions)`; internal `CompositeCultureStore(params ITlumachCultureStore[] stores)`; internal `CultureStoreFactory.Create(IServiceProvider services, TlumachBlazorOptions options)`.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.BlazorTests/CultureStoreTests.cs`:

```csharp
using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public class CultureStoreTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public async Task LocalStorage_Save_SetsItem()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.SetupVoid("localStorage.setItem", "tlumach.culture", "de-DE").SetVoidResult();
        LocalStorageCultureStore store = new(ctx.JSInterop.JSRuntime, new TlumachBlazorOptions());

        await store.SaveAsync(De);

        ctx.JSInterop.VerifyInvoke("localStorage.setItem");
    }

    [Fact]
    public async Task LocalStorage_Load_GetsItemWithConfiguredKey()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "my.key").SetResult("de-DE");
        LocalStorageCultureStore store = new(ctx.JSInterop.JSRuntime, new TlumachBlazorOptions { LocalStorageKey = "my.key" });

        Assert.Equal("de-DE", await store.LoadAsync());
    }

    [Fact]
    public async Task Cookie_Save_PostsToEndpoint()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.SetupVoid("fetch", _ => true).SetVoidResult();
        CookieCultureStore store = new(ctx.JSInterop.JSRuntime, ctx.Services.GetRequiredService<NavigationManager>(), new TlumachBlazorOptions());

        await store.SaveAsync(De);

        var invocation = ctx.JSInterop.VerifyInvoke("fetch");
        Assert.Equal("http://localhost/tlumach/culture?culture=de-DE", invocation.Arguments[0]);
        var init = Assert.IsType<Dictionary<string, string>>(invocation.Arguments[1]);
        Assert.Equal("POST", init["method"]);
        Assert.Equal("same-origin", init["credentials"]);
    }

    [Fact]
    public async Task Cookie_Load_ReadsHtmlLang()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Setup<string?>("document.documentElement.getAttribute", "lang").SetResult("de-DE");
        CookieCultureStore store = new(ctx.JSInterop.JSRuntime, ctx.Services.GetRequiredService<NavigationManager>(), new TlumachBlazorOptions());

        Assert.Equal("de-DE", await store.LoadAsync());
    }

    [Fact]
    public async Task Composite_Save_CallsEveryStore_Load_ReturnsFirstValue()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("document.documentElement.getAttribute", "lang").SetResult(null);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult("de-DE");
        TlumachBlazorOptions options = new();
        CompositeCultureStore store = new(
            new CookieCultureStore(ctx.JSInterop.JSRuntime, ctx.Services.GetRequiredService<NavigationManager>(), options),
            new LocalStorageCultureStore(ctx.JSInterop.JSRuntime, options));

        await store.SaveAsync(De);

        ctx.JSInterop.VerifyInvoke("fetch");
        ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        Assert.Equal("de-DE", await store.LoadAsync());
    }

    [Theory]
    [InlineData(TlumachCulturePersistence.Cookie, typeof(CookieCultureStore))]
    [InlineData(TlumachCulturePersistence.LocalStorage, typeof(LocalStorageCultureStore))]
    [InlineData(TlumachCulturePersistence.Cookie | TlumachCulturePersistence.LocalStorage, typeof(CompositeCultureStore))]
    [InlineData(TlumachCulturePersistence.None, typeof(CompositeCultureStore))]
    public async Task Factory_CreatesStoreForPersistence(TlumachCulturePersistence persistence, Type expected)
    {
        await using BunitContext ctx = new();

        ITlumachCultureStore store = CultureStoreFactory.Create(ctx.Services, new TlumachBlazorOptions { Persistence = persistence });

        Assert.IsType(expected, store);
    }

    [Fact]
    public async Task Factory_None_LoadsNothingAndSavesNothing()
    {
        await using BunitContext ctx = new();
        ITlumachCultureStore store = CultureStoreFactory.Create(ctx.Services, new TlumachBlazorOptions { Persistence = TlumachCulturePersistence.None });

        await store.SaveAsync(De);

        Assert.Null(await store.LoadAsync());
        Assert.Empty(ctx.JSInterop.Invocations);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~CultureStoreTests`
Expected: build errors (`LocalStorageCultureStore`, `CookieCultureStore`, ... not found).

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/ITlumachCultureStore.cs`:

```csharp
using System.Globalization;

namespace Tlumach.Blazor;

/// <summary>
/// Stores the culture that the user has chosen so that the next visit or the next circuit starts with it.
/// <para>Register an own implementation before calling <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/> to replace the built-in stores.</para>
/// </summary>
public interface ITlumachCultureStore
{
    /// <summary>
    /// Loads the name of the stored culture.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The culture name, or <see langword="null"/> if nothing is stored.</returns>
    ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the culture.
    /// </summary>
    /// <param name="culture">The culture to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the culture is stored.</returns>
    ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default);
}
```

`src/Tlumach.Blazor/CookieCultureStore.cs`:

```csharp
using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Stores the culture in the ASP.NET Core culture cookie by calling the endpoint mapped by <c>MapTlumachCultureEndpoint</c> through <c>fetch</c>,
/// and loads it from the <c>lang</c> attribute of the <c>html</c> element, which the server renders from the same cookie.
/// <para>The store uses only built-in browser functions, so no JavaScript file is needed.</para>
/// </summary>
public sealed class CookieCultureStore : ITlumachCultureStore
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private readonly TlumachBlazorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieCultureStore"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    /// <param name="navigationManager">The navigation manager, which supplies the base address of the application.</param>
    /// <param name="options">The options, which supply the path of the endpoint.</param>
    public CookieCultureStore(IJSRuntime jsRuntime, NavigationManager navigationManager, TlumachBlazorOptions options)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default)
        => _jsRuntime.InvokeAsync<string?>("document.documentElement.getAttribute", cancellationToken, "lang");

    /// <inheritdoc/>
    public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);

        string url = _navigationManager.BaseUri + _options.CultureEndpoint.TrimStart('/') + "?culture=" + Uri.EscapeDataString(culture.Name);
        Dictionary<string, string> init = new(StringComparer.Ordinal)
        {
            ["method"] = "POST",
            ["credentials"] = "same-origin",
        };

        // JS interop awaits the promise that fetch returns, so the cookie is set when this task completes.
        return _jsRuntime.InvokeVoidAsync("fetch", cancellationToken, url, init);
    }
}
```

`src/Tlumach.Blazor/LocalStorageCultureStore.cs`:

```csharp
using System.Globalization;

using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Stores the culture in the local storage of the browser. Used by standalone Blazor WebAssembly and Blazor Hybrid applications.
/// </summary>
public sealed class LocalStorageCultureStore : ITlumachCultureStore
{
    private readonly IJSRuntime _jsRuntime;
    private readonly TlumachBlazorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageCultureStore"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime.</param>
    /// <param name="options">The options, which supply the storage key.</param>
    public LocalStorageCultureStore(IJSRuntime jsRuntime, TlumachBlazorOptions options)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default)
        => _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, _options.LocalStorageKey);

    /// <inheritdoc/>
    public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken, _options.LocalStorageKey, culture.Name);
    }
}
```

`src/Tlumach.Blazor/CompositeCultureStore.cs`:

```csharp
using System.Globalization;

namespace Tlumach.Blazor;

/// <summary>
/// Combines several stores: saves to all of them and loads from the first one that has a value. With no stores, nothing is stored.
/// </summary>
internal sealed class CompositeCultureStore : ITlumachCultureStore
{
    private readonly ITlumachCultureStore[] _stores;

    public CompositeCultureStore(params ITlumachCultureStore[] stores)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
    }

    public async ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default)
    {
        foreach (ITlumachCultureStore store in _stores)
        {
            string? value = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    public async ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        foreach (ITlumachCultureStore store in _stores)
            await store.SaveAsync(culture, cancellationToken).ConfigureAwait(false);
    }
}
```

`src/Tlumach.Blazor/CultureStoreFactory.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Creates the store that <see cref="TlumachBlazorOptions.Persistence"/> asks for.
/// </summary>
internal static class CultureStoreFactory
{
    internal static ITlumachCultureStore Create(IServiceProvider services, TlumachBlazorOptions options)
    {
        TlumachCulturePersistence persistence = options.EffectivePersistence;
        List<ITlumachCultureStore> stores = [];

        if ((persistence & TlumachCulturePersistence.Cookie) != 0)
            stores.Add(new CookieCultureStore(services.GetRequiredService<IJSRuntime>(), services.GetRequiredService<NavigationManager>(), options));

        if ((persistence & TlumachCulturePersistence.LocalStorage) != 0)
            stores.Add(new LocalStorageCultureStore(services.GetRequiredService<IJSRuntime>(), options));

        return stores.Count == 1 ? stores[0] : new CompositeCultureStore([.. stores]);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~CultureStoreTests`
Expected: PASS. If `ctx.JSInterop.JSRuntime` does not exist in bUnit 2.11, use `ctx.Services.GetRequiredService<IJSRuntime>()` instead.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor tests/Tlumach.BlazorTests
git commit -m "Add cookie and local-storage culture stores to Tlumach.Blazor

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: TlumachCultureState and AddTlumachBlazor

**Files:**
- Create: `src/Tlumach.Blazor/CultureApplier.cs`
- Create: `src/Tlumach.Blazor/TlumachCultureState.cs`
- Create: `src/Tlumach.Blazor/TlumachBlazorServiceCollectionExtensions.cs`
- Create: `tests/Tlumach.BlazorTests/TestContexts.cs`
- Create: `tests/Tlumach.BlazorTests/GlobalCultureScope.cs`
- Test: `tests/Tlumach.BlazorTests/TlumachCultureStateTests.cs`

**Interfaces:**
- Consumes: Task 2 options/snapshot, Task 3 `ITlumachCultureStore`, `CultureStoreFactory.Create`, Task 1 `public CultureChangedEventArgs(CultureInfo)`.
- Produces:
  - `TlumachCultureState(TlumachBlazorOptions, ITlumachCultureStore, NavigationManager, ILogger<TlumachCultureState>)` with `CultureInfo Culture`, `TlumachCulture Current`, `IReadOnlyList<CultureInfo> SupportedCultures`, `event EventHandler<CultureChangedEventArgs>? CultureChanged`, `Task SetCultureAsync(CultureInfo culture, bool forceReload = false)`, internal `CascadingValueSource<TlumachCulture> CascadingSource`.
  - internal `CultureApplier.ApplyGlobally(CultureInfo)`.
  - `IServiceCollection AddTlumachBlazor(this IServiceCollection services, Action<TlumachBlazorOptions>? configure = null)`.
  - Test helpers: `TestContexts.Create(TestTranslations translations, Action<TlumachBlazorOptions>? configure = null, Action<IServiceCollection>? configureServices = null, CultureInfo? initialCulture = null)` returning a `BunitContext` whose state is already created; `GlobalCultureScope : IDisposable` (restores `DefaultThreadCurrent*`).

- [ ] **Step 1: Write the test helpers and the failing tests**

`tests/Tlumach.BlazorTests/TestContexts.cs`:

```csharp
using System.Globalization;

using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

internal static class TestContexts
{
    /// <summary>
    /// Creates a bUnit context with Tlumach.Blazor registered (en-US and de-DE supported, local-storage persistence, loose JS interop)
    /// and creates the culture state with <paramref name="initialCulture"/> (en-US by default) as the culture of the "request".
    /// </summary>
    public static BunitContext Create(
        TestTranslations translations,
        Action<TlumachBlazorOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null,
        CultureInfo? initialCulture = null)
    {
        BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddLogging();
        configureServices?.Invoke(ctx.Services);
        ctx.Services.AddTlumachBlazor(options =>
        {
            options.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            options.DefaultManager = translations.Manager;
            options.Persistence = TlumachCulturePersistence.LocalStorage;
            configure?.Invoke(options);
        });

        // The state reads CultureInfo.CurrentUICulture once, when it is created; create it now so that the result does not depend on the machine.
        CultureInfo.CurrentUICulture = initialCulture ?? TestTranslations.En;
        _ = ctx.Services.GetRequiredService<TlumachCultureState>();
        return ctx;
    }
}
```

`tests/Tlumach.BlazorTests/GlobalCultureScope.cs`:

```csharp
using System.Globalization;

namespace Tlumach.BlazorTests;

/// <summary>
/// Restores the process-wide default cultures that a test changes.
/// </summary>
internal sealed class GlobalCultureScope : IDisposable
{
    private readonly CultureInfo? _culture = CultureInfo.DefaultThreadCurrentCulture;
    private readonly CultureInfo? _uiCulture = CultureInfo.DefaultThreadCurrentUICulture;

    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _uiCulture;
    }
}
```

`tests/Tlumach.BlazorTests/TlumachCultureStateTests.cs`:

```csharp
using System.Globalization;

using Bunit.TestDoubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureStateTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task InitialCulture_FollowsCurrentUICulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        Assert.Same(TestTranslations.De, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
    }

    [Fact]
    public async Task InitialCulture_Unsupported_FallsBackToDefaultCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.DefaultCulture = TestTranslations.De, initialCulture: CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Same(TestTranslations.De, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
    }

    [Fact]
    public async Task SetCultureAsync_ChangesCultureAndSnapshot()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        await state.SetCultureAsync(CultureInfo.GetCultureInfo("de-DE"));

        Assert.Same(TestTranslations.De, state.Culture);
        Assert.Same(TestTranslations.De, state.Current.Culture);
        Assert.Equal("Hallo", state.Current.Get(_translations.Hello));
    }

    [Fact]
    public async Task SetCultureAsync_RaisesCultureChangedOnce_AndNotForSameCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        List<string> raised = [];
        state.CultureChanged += (_, e) => raised.Add(e.Culture.Name);

        await state.SetCultureAsync(TestTranslations.De);
        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(new[] { "de-DE" }, raised);
    }

    [Fact]
    public async Task SetCultureAsync_UnsubscribedHandler_IsNotCalled()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        int calls = 0;
        void Handler(object? sender, CultureChangedEventArgs e) => calls++;
        state.CultureChanged += Handler;
        state.CultureChanged -= Handler;

        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SetCultureAsync_Unsupported_Throws()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        await Assert.ThrowsAsync<ArgumentException>(() => state.SetCultureAsync(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public async Task SetCultureAsync_SavesToStore()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        var invocation = ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        Assert.Equal(new object?[] { "tlumach.culture", "de-DE" }, invocation.Arguments);
    }

    [Fact]
    public async Task SetCultureAsync_StoreFailure_DoesNotThrow()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, configureServices: s => s.AddScoped<ITlumachCultureStore, ThrowingStore>());
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();

        await state.SetCultureAsync(TestTranslations.De);

        Assert.Same(TestTranslations.De, state.Culture);
    }

    [Fact]
    public async Task SetCultureAsync_ForceReload_SavesAndReloadsCurrentPage()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        BunitNavigationManager navigation = ctx.Services.GetRequiredService<BunitNavigationManager>();
        navigation.NavigateTo("/page?x=1");

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De, forceReload: true);

        ctx.JSInterop.VerifyInvoke("localStorage.setItem");
        NavigationHistory last = navigation.History.First();
        Assert.Equal("http://localhost/page?x=1", last.Uri);
        Assert.True(last.Options.ForceLoad);
    }

    [Fact]
    public async Task ApplyCultureGlobally_True_ChangesDefaultsAndManagers()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.ApplyCultureGlobally = true);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        Assert.Same(TestTranslations.De, CultureInfo.DefaultThreadCurrentUICulture);
        Assert.Same(TestTranslations.De, CultureInfo.DefaultThreadCurrentCulture);
        Assert.Equal("de-DE", _translations.Manager.CurrentCulture.Name);
    }

    [Fact]
    public async Task ApplyCultureGlobally_False_LeavesProcessWideCultureAlone()
    {
        using GlobalCultureScope scope = new();
        CultureInfo? before = CultureInfo.DefaultThreadCurrentUICulture;
        string managerCulture = _translations.Manager.CurrentCulture.Name;
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.ApplyCultureGlobally = false);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        Assert.Same(before, CultureInfo.DefaultThreadCurrentUICulture);
        Assert.Equal(managerCulture, _translations.Manager.CurrentCulture.Name);
    }

    [Fact]
    public async Task TwoScopes_SwitchConcurrently_StayIsolated()
    {
        await using BunitContext first = TestContexts.Create(_translations);
        await using BunitContext second = TestContexts.Create(_translations, initialCulture: TestTranslations.De);
        TlumachCultureState firstState = first.Services.GetRequiredService<TlumachCultureState>();
        TlumachCultureState secondState = second.Services.GetRequiredService<TlumachCultureState>();

        await Task.WhenAll(
            Task.Run(() => firstState.SetCultureAsync(TestTranslations.De)),
            Task.Run(() => secondState.SetCultureAsync(TestTranslations.En)));

        Assert.Equal("Hallo", firstState.Current.Get(_translations.Hello));
        Assert.Equal("Hello", secondState.Current.Get(_translations.Hello));
    }

    private sealed class ThrowingStore : ITlumachCultureStore
    {
        public ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default) => throw new JSException("load failed");

        public ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default) => throw new JSException("save failed");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachCultureStateTests`
Expected: build errors (`TlumachCultureState`, `AddTlumachBlazor` not found).

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/CultureApplier.cs`:

```csharp
using System.Globalization;

namespace Tlumach.Blazor;

/// <summary>
/// Applies a culture to the whole process. Only for hosts where one user owns the process (Blazor WebAssembly, Blazor Hybrid).
/// </summary>
internal static class CultureApplier
{
    internal static void ApplyGlobally(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // A copy, because the list may change while it is enumerated when a translation manager is created or disposed.
        foreach (TranslationManager manager in TranslationManager.TranslationManagers.ToArray())
        {
            if (!ReferenceEquals(manager, TranslationManager.Empty))
                manager.CurrentCulture = culture;
        }
    }
}
```

`src/Tlumach.Blazor/TlumachCultureState.cs`:

```csharp
using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Holds the culture of one user and switches it.
/// <para>The service is scoped: Blazor Server creates one per circuit (and one per prerendering request), so the users of one server do not share the culture;
/// Blazor WebAssembly and Blazor Hybrid effectively have one per application.</para>
/// <para>Components re-render after a switch through the cascading <see cref="TlumachCulture"/> value; other code can subscribe to <see cref="CultureChanged"/>.</para>
/// </summary>
public sealed class TlumachCultureState
{
    private static readonly Action<ILogger, string, Exception?> LogSaveFailed = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, "CultureSaveFailed"),
        "Tlumach could not store the culture '{Culture}'. The language was switched, but the next visit may start with the previous language.");

    private readonly TlumachBlazorOptions _options;
    private readonly ITlumachCultureStore _store;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<TlumachCultureState> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachCultureState"/> class with the culture that the request localization middleware (Blazor Server, static SSR)
    /// or <see cref="TlumachBlazorServiceProviderExtensions.LoadTlumachCultureAsync"/> (Blazor WebAssembly) has set.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="store">The store for the chosen culture.</param>
    /// <param name="navigationManager">The navigation manager, used to reload the page.</param>
    /// <param name="logger">The logger.</param>
    public TlumachCultureState(TlumachBlazorOptions options, ITlumachCultureStore store, NavigationManager navigationManager, ILogger<TlumachCultureState> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Current = new TlumachCulture(options.ResolveInitialCulture(CultureInfo.CurrentUICulture));
        CascadingSource = new CascadingValueSource<TlumachCulture>(Current, isFixed: false);
    }

    /// <summary>
    /// Occurs after the culture has been switched, before the components re-render.
    /// </summary>
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <summary>
    /// Gets the culture of the user.
    /// </summary>
    public CultureInfo Culture => Current.Culture;

    /// <summary>
    /// Gets the snapshot of the culture that is cascaded to the components.
    /// </summary>
    public TlumachCulture Current { get; private set; }

    /// <summary>
    /// Gets the cultures that the user may choose.
    /// </summary>
    public IReadOnlyList<CultureInfo> SupportedCultures => _options.SupportedCultures;

    internal CascadingValueSource<TlumachCulture> CascadingSource { get; }

    /// <summary>
    /// Switches the culture of the user.
    /// </summary>
    /// <param name="culture">The new culture. It must match one of <see cref="SupportedCultures"/> (see <see cref="TlumachBlazorOptions.FindSupportedCulture"/>).</param>
    /// <param name="forceReload"><see langword="true"/> to store the culture and reload the page, so that everything, including the formatting that does not go through Tlumach,
    /// uses the new culture; <see langword="false"/> to switch the language live.</param>
    /// <returns>A task that completes when the components have been notified and the culture has been stored.</returns>
    /// <exception cref="ArgumentException">The culture is not supported.</exception>
    public async Task SetCultureAsync(CultureInfo culture, bool forceReload = false)
    {
        ArgumentNullException.ThrowIfNull(culture);

        CultureInfo target = _options.FindSupportedCulture(culture)
            ?? throw new ArgumentException($"The culture '{culture.Name}' is not one of TlumachBlazorOptions.SupportedCultures.", nameof(culture));

        if (target.Name.Equals(Culture.Name, StringComparison.OrdinalIgnoreCase))
            return;

        if (forceReload)
        {
            await SaveAsync(target).ConfigureAwait(true);
            _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
            return;
        }

        Current = new TlumachCulture(target);

        // Affects the rest of this call, including components that render synchronously during the notification.
        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;

        if (_options.EffectiveApplyCultureGlobally)
            CultureApplier.ApplyGlobally(target);

        CultureChanged?.Invoke(this, new CultureChangedEventArgs(target));
        await CascadingSource.NotifyChangedAsync(Current).ConfigureAwait(true);

        // Stored last, so that the page does not wait for a network round trip before it shows the new language.
        await SaveAsync(target).ConfigureAwait(true);
    }

    private async Task SaveAsync(CultureInfo culture)
    {
        try
        {
            await _store.SaveAsync(culture).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or OperationCanceledException)
        {
            // JS interop is unavailable while prerendering and fails when the circuit is gone; neither should break the switch.
            LogSaveFailed(_logger, culture.Name, ex);
        }
    }
}
```

`src/Tlumach.Blazor/TlumachBlazorServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tlumach.Blazor;

/// <summary>
/// Registers the Blazor integration of Tlumach.
/// </summary>
public static class TlumachBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the per-user culture state, the cascading <see cref="TlumachCulture"/> value, and the culture store.
    /// <para>In a Blazor Web App, call this method both in the server and in the client project.</para>
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configure">A callback that configures the options.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddTlumachBlazor(this IServiceCollection services, Action<TlumachBlazorOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TlumachBlazorOptions options = new();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.TryAddScoped(sp => CultureStoreFactory.Create(sp, options));
        services.TryAddScoped<TlumachCultureState>();
        services.AddCascadingValue(sp => sp.GetRequiredService<TlumachCultureState>().CascadingSource);

        return services;
    }
}
```

Note: `TryAddScoped(sp => CultureStoreFactory.Create(sp, options))` infers `TService = ITlumachCultureStore` from the factory's return type.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`
Expected: all PASS. If `BunitNavigationManager.History` entries expose the URI differently in bUnit 2.11 (e.g. a relative URI), assert with `Assert.EndsWith("/page?x=1", last.Uri)`.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor tests/Tlumach.BlazorTests
git commit -m "Add the per-user TlumachCultureState and AddTlumachBlazor

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: The TlumachText component

**Files:**
- Create: `src/Tlumach.Blazor/TlumachText.cs`
- Test: `tests/Tlumach.BlazorTests/TlumachTextTests.cs`

**Interfaces:**
- Consumes: `TlumachCulture.GetRaw`, `TlumachCulture.GetByKey`, `TlumachCultureState.Current`, `TlumachBlazorOptions.DefaultManager`.
- Produces: `<TlumachText Unit=... Key=... Manager=... Args=... Values=... AsMarkup=... />` with parameters `BaseTranslationUnit? Unit`, `string? Key`, `TranslationManager? Manager`, `IDictionary<string, object?>? Args`, `IReadOnlyList<object>? Values`, `bool AsMarkup`.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.BlazorTests/TlumachTextTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachTextTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task Renders_UnitText_InStateCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));

        Assert.Equal("Hallo", cut.Markup);
    }

    [Fact]
    public async Task ReRenders_WhenCultureChanges()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        Assert.Equal("Hello", cut.Markup);

        await cut.InvokeAsync(() => state.SetCultureAsync(TestTranslations.De));

        cut.WaitForAssertion(() => Assert.Equal("Hallo", cut.Markup));
    }

    [Fact]
    public async Task Args_ReplaceNamedPlaceholders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p
            .Add(x => x.Unit, _translations.Greeting)
            .Add(x => x.Args, new Dictionary<string, object?> { ["name"] = "Anna" }));

        Assert.Equal("Hello, Anna!", cut.Markup);
    }

    [Fact]
    public async Task Values_ReplaceIndexedPlaceholders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        var cut = ctx.Render<TlumachText>(p => p
            .Add(x => x.Unit, _translations.Position)
            .Add(x => x.Values, new object[] { 2, 5 }));

        Assert.Equal("Element 2 von 5", cut.Markup);
    }

    [Fact]
    public async Task Text_IsHtmlEncoded()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Script));

        Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt;", cut.Markup);
        Assert.Empty(cut.FindAll("script"));
    }

    [Fact]
    public async Task WebEncodeValues_IsNotEncodedTwice()
    {
        _translations.Manager.WebEncodeValues = true;
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Rich));

        Assert.Equal("Click &lt;b&gt;here&lt;/b&gt;", cut.Markup);
        Assert.DoesNotContain("&amp;", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AsMarkup_RendersTrustedHtml()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Rich).Add(x => x.AsMarkup, true));

        Assert.Equal("here", cut.Find("b").TextContent);
    }

    [Fact]
    public async Task Key_UsesDefaultManager()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, initialCulture: TestTranslations.De);

        var cut = ctx.Render<TlumachText>(p => p
            .Add(x => x.Key, "greeting")
            .Add(x => x.Args, new Dictionary<string, object?> { ["name"] = "Anna" }));

        Assert.Equal("Hallo, Anna!", cut.Markup);
    }

    [Fact]
    public async Task Key_Missing_RendersNothing()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Key, "missing"));

        Assert.Equal(string.Empty, cut.Markup);
    }

    [Fact]
    public async Task NoUnitAndNoKey_Throws()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        Assert.Throws<InvalidOperationException>(() => ctx.Render<TlumachText>());
    }

    [Fact]
    public async Task KeyWithoutManager_Throws()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, o => o.DefaultManager = null);

        Assert.Throws<InvalidOperationException>(() => ctx.Render<TlumachText>(p => p.Add(x => x.Key, "hello")));
    }

    [Fact]
    public async Task TwoContexts_RenderTheirOwnCulture()
    {
        await using BunitContext first = TestContexts.Create(_translations);
        await using BunitContext second = TestContexts.Create(_translations);
        var firstCut = first.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        var secondCut = second.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));

        await Task.WhenAll(
            firstCut.InvokeAsync(() => first.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De)),
            secondCut.InvokeAsync(() => second.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.En)));

        firstCut.WaitForAssertion(() => Assert.Equal("Hallo", firstCut.Markup));
        Assert.Equal("Hello", secondCut.Markup);
    }

    [Fact]
    public async Task DisposedComponent_IsNotRenderedAgain()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = ctx.Render<TlumachText>(p => p.Add(x => x.Unit, _translations.Hello));
        int renders = cut.RenderCount;

        ctx.DisposeComponents();
        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(renders, cut.RenderCount);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachTextTests`
Expected: build errors (`TlumachText` not found).

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/TlumachText.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tlumach.Blazor;

/// <summary>
/// Renders the text of a translation unit or of a key in the culture of the user and re-renders when the user switches the language.
/// <para>The text is HTML-encoded like any other Razor output. If the translation manager encodes values itself (<see cref="TranslationManager.WebEncodeValues"/>),
/// the already encoded text is not encoded again. Set <see cref="AsMarkup"/> only for translations that contain trusted HTML.</para>
/// </summary>
public sealed class TlumachText : ComponentBase
{
    /// <summary>
    /// Gets or sets the translation unit, usually a member of a generated class, e.g. <c>Strings.Hello</c>.
    /// </summary>
    [Parameter]
    public BaseTranslationUnit? Unit { get; set; }

    /// <summary>
    /// Gets or sets the key of the text, used when <see cref="Unit"/> is not set.
    /// </summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the translation manager that resolves <see cref="Key"/>. When not set, <see cref="TlumachBlazorOptions.DefaultManager"/> is used.
    /// </summary>
    [Parameter]
    public TranslationManager? Manager { get; set; }

    /// <summary>
    /// Gets or sets the values of named placeholders, keyed by placeholder names.
    /// </summary>
    [Parameter]
    public IDictionary<string, object?>? Args { get; set; }

    /// <summary>
    /// Gets or sets the values of indexed placeholders, used when <see cref="Args"/> is not set.
    /// </summary>
    [Parameter]
    public IReadOnlyList<object>? Values { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the text is rendered as markup. Use it only for translations that contain trusted HTML.
    /// </summary>
    [Parameter]
    public bool AsMarkup { get; set; }

    [CascadingParameter]
    private TlumachCulture? Culture { get; set; }

    [Inject]
    private TlumachCultureState State { get; set; } = default!;

    [Inject]
    private TlumachBlazorOptions Options { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (Unit is null && string.IsNullOrEmpty(Key))
            throw new InvalidOperationException($"{nameof(TlumachText)} requires either the {nameof(Unit)} or the {nameof(Key)} parameter.");

        if (Unit is null && (Manager ?? Options.DefaultManager) is null)
            throw new InvalidOperationException($"{nameof(TlumachText)} with the {nameof(Key)} parameter requires the {nameof(Manager)} parameter or {nameof(TlumachBlazorOptions)}.{nameof(TlumachBlazorOptions.DefaultManager)}.");
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        TlumachCulture culture = Culture ?? State.Current;
        object[]? values = Values is null ? null : Values as object[] ?? [.. Values];

        string text;
        bool markup;
        if (Unit is not null)
        {
            text = culture.GetRaw(Unit, Args, values);

            // With WebEncodeValues, the unit returns encoded text; adding it as markup keeps it from being encoded twice.
            markup = AsMarkup || Unit.TranslationManager.WebEncodeValues;
        }
        else
        {
            text = culture.GetByKey((Manager ?? Options.DefaultManager)!, Key!, Args, values);
            markup = AsMarkup;
        }

        if (markup)
            builder.AddMarkupContent(0, text);
        else
            builder.AddContent(1, text);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachTextTests`
Expected: PASS. If Sonar/Roslynator report the private `[CascadingParameter]`/`[Inject]` setters as unused, suppress that rule locally with `#pragma warning disable <id> // Set by the Blazor renderer through reflection` around the properties. If `DisposeComponents` is not on `BunitContext` in bUnit 2.11, use `ctx.Renderer.DisposeComponents()`.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor/TlumachText.cs tests/Tlumach.BlazorTests/TlumachTextTests.cs
git commit -m "Add the TlumachText component

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: TlumachComponentBase

**Files:**
- Create: `src/Tlumach.Blazor/TlumachComponentBase.cs`
- Test: `tests/Tlumach.BlazorTests/TlumachComponentBaseTests.cs`

**Interfaces:**
- Consumes: `TlumachCulture.Get/GetFrom`, `TlumachCultureState.Current`.
- Produces: `abstract class TlumachComponentBase : ComponentBase` with `protected TlumachCulture? CascadedCulture` (cascading parameter), `protected TlumachCultureState CultureState` (injected), `protected TlumachCulture Culture`, `protected string T(BaseTranslationUnit)`, `T(BaseTranslationUnit, params object[])`, `T(BaseTranslationUnit, IDictionary<string, object?>)`, `protected string TFrom<TArgs>(BaseTranslationUnit, TArgs)`, `protected virtual Task OnCultureChangedAsync(TlumachCulture culture)`.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.BlazorTests/TlumachComponentBaseTests.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachComponentBaseTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task T_UsesStateCulture_AndReRenders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        var cut = Render(ctx);
        Assert.Equal("Hello", cut.Find("span").TextContent);

        await cut.InvokeAsync(() => ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));

        cut.WaitForAssertion(() => Assert.Equal("Hallo", cut.Find("span").TextContent));
    }

    [Fact]
    public async Task TFrom_AnonymousObject_FillsPlaceholders()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = Render(ctx);

        Assert.Equal("Hello, Anna!", cut.Find("em").TextContent);
    }

    [Fact]
    public async Task T_WithWebEncodeValues_AttributeIsNotEncodedTwice()
    {
        _translations.Manager.WebEncodeValues = true;
        await using BunitContext ctx = TestContexts.Create(_translations);

        var cut = Render(ctx);

        Assert.Equal("Click <b>here</b>", cut.Find("span").GetAttribute("title"));
    }

    [Fact]
    public async Task OnCultureChangedAsync_IsCalledOncePerSwitch()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = Render(ctx);

        await cut.InvokeAsync(() => state.SetCultureAsync(TestTranslations.De));
        cut.WaitForAssertion(() => Assert.Equal(new[] { "de-DE" }, cut.Instance.CultureChanges));
        cut.Render();

        Assert.Equal(new[] { "de-DE" }, cut.Instance.CultureChanges);
    }

    [Fact]
    public async Task DisposedComponent_LeavesNoSubscription()
    {
        await using BunitContext ctx = TestContexts.Create(_translations);
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        var cut = Render(ctx);
        int renders = cut.RenderCount;

        ctx.DisposeComponents();
        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal(renders, cut.RenderCount);
        Assert.Empty(cut.Instance.CultureChanges);
    }

    private IRenderedComponent<ProbeComponent> Render(BunitContext ctx)
        => ctx.Render<ProbeComponent>(p => p
            .Add(x => x.Hello, _translations.Hello)
            .Add(x => x.Greeting, _translations.Greeting)
            .Add(x => x.Rich, _translations.Rich));

    internal sealed class ProbeComponent : TlumachComponentBase
    {
        [Parameter]
        public BaseTranslationUnit Hello { get; set; } = default!;

        [Parameter]
        public BaseTranslationUnit Greeting { get; set; } = default!;

        [Parameter]
        public BaseTranslationUnit Rich { get; set; } = default!;

        public List<string> CultureChanges { get; } = [];

        protected override Task OnCultureChangedAsync(TlumachCulture culture)
        {
            CultureChanges.Add(culture.Culture.Name);
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "title", T(Rich));
            builder.AddContent(2, T(Hello));
            builder.CloseElement();
            builder.OpenElement(3, "em");
            builder.AddContent(4, TFrom(Greeting, new { name = "Anna" }));
            builder.CloseElement();
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachComponentBaseTests`
Expected: build errors (`TlumachComponentBase` not found).

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/TlumachComponentBase.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

namespace Tlumach.Blazor;

/// <summary>
/// An optional base class for components that use translations in code or in attributes, where <see cref="TlumachText"/> cannot be placed.
/// <para>The component re-renders when the user switches the language, because it takes the cascading <see cref="TlumachCulture"/> value.
/// The class subscribes to no events, so it needs no disposal.</para>
/// </summary>
public abstract class TlumachComponentBase : ComponentBase
{
    private string? _lastCultureName;

    /// <summary>
    /// Gets or sets the cascaded culture snapshot.
    /// </summary>
    [CascadingParameter]
    protected TlumachCulture? CascadedCulture { get; set; }

    /// <summary>
    /// Gets or sets the culture state of the user, e.g. for switching the language.
    /// </summary>
    [Inject]
    protected TlumachCultureState CultureState { get; set; } = default!;

    /// <summary>
    /// Gets the culture snapshot used by the <c>T</c> methods.
    /// </summary>
    protected TlumachCulture Culture => CascadedCulture ?? CultureState.Current;

    /// <summary>
    /// Switches to the new culture before the parameters are applied and calls <see cref="OnCultureChangedAsync"/> when the cascaded culture has changed.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>A task that completes when the parameters have been set.</returns>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue(nameof(CascadedCulture), out TlumachCulture? incoming) && incoming is not null)
        {
            bool changed = _lastCultureName is not null && !incoming.Culture.Name.Equals(_lastCultureName, StringComparison.Ordinal);
            _lastCultureName = incoming.Culture.Name;
            if (changed)
                await OnCultureChangedAsync(incoming).ConfigureAwait(true);
        }

        await base.SetParametersAsync(parameters).ConfigureAwait(true);
    }

    /// <summary>
    /// Returns the text of the unit as plain text, suitable for attributes and code.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The text.</returns>
    protected string T(BaseTranslationUnit unit) => Culture.Get(unit);

    /// <summary>
    /// Returns the text of the unit with indexed placeholders replaced.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="values">The values of the placeholders.</param>
    /// <returns>The text.</returns>
    protected string T(BaseTranslationUnit unit, params object[] values) => Culture.Get(unit, values);

    /// <summary>
    /// Returns the text of the unit with named placeholders replaced.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the placeholders, keyed by placeholder names.</param>
    /// <returns>The text.</returns>
    protected string T(BaseTranslationUnit unit, IDictionary<string, object?> args) => Culture.Get(unit, args);

    /// <summary>
    /// Returns the text of the unit with named placeholders replaced by the public properties of <paramref name="args"/>, e.g. an anonymous object.
    /// <para>The method has its own name: as an overload of <c>T</c>, it would win over the other overloads for arrays and dictionaries and silently change their meaning.</para>
    /// </summary>
    /// <typeparam name="TArgs">The type that supplies the values.</typeparam>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The object that supplies the values.</param>
    /// <returns>The text.</returns>
    protected string TFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(BaseTranslationUnit unit, TArgs args)
        => Culture.GetFrom(unit, args);

    /// <summary>
    /// Called before the component re-renders because the user has switched the language.
    /// </summary>
    /// <param name="culture">The new culture snapshot. <see cref="Culture"/> still returns the previous one during the call.</param>
    /// <returns>A task that completes when the component has reacted.</returns>
    protected virtual Task OnCultureChangedAsync(TlumachCulture culture) => Task.CompletedTask;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachComponentBaseTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor/TlumachComponentBase.cs tests/Tlumach.BlazorTests/TlumachComponentBaseTests.cs
git commit -m "Add TlumachComponentBase with translation helpers for code and attributes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: TlumachCultureSelector

**Files:**
- Create: `src/Tlumach.Blazor/TlumachCultureSelector.cs`
- Test: `tests/Tlumach.BlazorTests/TlumachCultureSelectorTests.cs`

**Interfaces:**
- Consumes: `TlumachCultureState.SetCultureAsync/SupportedCultures/Current`, `TlumachBlazorOptions.CultureEndpoint`, `ComponentBase.RendererInfo` (.NET 9+).
- Produces: `<TlumachCultureSelector ForceReload=... DisplayName=... SubmitText=... @attributes />` with `bool ForceReload`, `Func<CultureInfo, string>? DisplayName`, `string SubmitText` (default `"OK"`), `IReadOnlyDictionary<string, object>? AdditionalAttributes` (applied to `<select>`).

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.BlazorTests/TlumachCultureSelectorTests.cs`:

```csharp
using System.Globalization;

using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureSelectorTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task Interactive_RendersSupportedCultures_WithCurrentSelected()
    {
        await using BunitContext ctx = Create(interactive: true);

        var cut = ctx.Render<TlumachCultureSelector>(p => p.AddUnmatched("class", "lang"));

        var options = cut.FindAll("option");
        Assert.Equal(new string?[] { "en-US", "de-DE" }, options.Select(o => o.GetAttribute("value")));
        Assert.True(options[0].HasAttribute("selected"));
        Assert.Equal("lang", cut.Find("select").GetAttribute("class"));
        Assert.Empty(cut.FindAll("form"));
    }

    [Fact]
    public async Task Interactive_Change_SwitchesCulture()
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>();

        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "de-DE" });

        Assert.Same(TestTranslations.De, ctx.Services.GetRequiredService<TlumachCultureState>().Culture);
        cut.WaitForAssertion(() => Assert.True(cut.FindAll("option")[1].HasAttribute("selected")));
    }

    [Fact]
    public async Task Interactive_ForceReload_ReloadsPage()
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>(p => p.Add(x => x.ForceReload, true));

        await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "de-DE" });

        Assert.True(ctx.Services.GetRequiredService<BunitNavigationManager>().History.First().Options.ForceLoad);
    }

    [Fact]
    public async Task DisplayName_IsUsedForOptions()
    {
        await using BunitContext ctx = Create(interactive: true);

        var cut = ctx.Render<TlumachCultureSelector>(p => p.Add(x => x.DisplayName, (CultureInfo c) => c.Name.ToUpperInvariant()));

        Assert.Equal("DE-DE", cut.FindAll("option")[1].TextContent);
    }

    [Fact]
    public async Task Static_RendersGetFormToEndpoint()
    {
        await using BunitContext ctx = Create(interactive: false);
        ctx.Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/page?x=1");

        var cut = ctx.Render<TlumachCultureSelector>(p => p.Add(x => x.SubmitText, "Go"));

        var form = cut.Find("form");
        Assert.Equal("get", form.GetAttribute("method"));
        Assert.Equal("http://localhost/tlumach/culture", form.GetAttribute("action"));
        Assert.Equal("/page?x=1", cut.Find("input[name=redirectUri]").GetAttribute("value"));
        Assert.Equal("culture", cut.Find("select").GetAttribute("name"));
        Assert.Equal("Go", cut.Find("button").TextContent);
    }

    [Fact]
    public async Task ReRenders_WhenCultureIsSwitchedElsewhere()
    {
        await using BunitContext ctx = Create(interactive: true);
        var cut = ctx.Render<TlumachCultureSelector>();

        await cut.InvokeAsync(() => ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De));

        cut.WaitForAssertion(() => Assert.True(cut.FindAll("option")[1].HasAttribute("selected")));
    }

    private BunitContext Create(bool interactive)
    {
        BunitContext ctx = TestContexts.Create(_translations);
        ctx.SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        return ctx;
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachCultureSelectorTests`
Expected: build errors (`TlumachCultureSelector` not found).

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/TlumachCultureSelector.cs`:

```csharp
using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tlumach.Blazor;

/// <summary>
/// Lets the user choose one of <see cref="TlumachBlazorOptions.SupportedCultures"/>.
/// <para>In an interactive component, it is a <c>select</c> element that switches the language when the selection changes. In static server-side rendering,
/// it is a form that sends the choice to the culture endpoint (<c>MapTlumachCultureEndpoint</c>), which stores it in a cookie and reloads the page.</para>
/// </summary>
public sealed class TlumachCultureSelector : ComponentBase
{
    /// <summary>
    /// Gets or sets a value indicating whether the page is reloaded after a switch so that all formatting uses the new culture.
    /// </summary>
    [Parameter]
    public bool ForceReload { get; set; }

    /// <summary>
    /// Gets or sets the function that returns the label of a culture. By default, the native name of the culture is used.
    /// </summary>
    [Parameter]
    public Func<CultureInfo, string>? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the text of the submit button of the form rendered in static server-side rendering.
    /// </summary>
    [Parameter]
    public string SubmitText { get; set; } = "OK";

    /// <summary>
    /// Gets or sets the attributes applied to the <c>select</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private TlumachCulture? Culture { get; set; }

    [Inject]
    private TlumachCultureState State { get; set; } = default!;

    [Inject]
    private TlumachBlazorOptions Options { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        CultureInfo current = (Culture ?? State.Current).Culture;

        if (RendererInfo.IsInteractive)
        {
            builder.OpenRegion(0);
            BuildSelect(builder, current, interactive: true);
            builder.CloseRegion();
            return;
        }

        builder.OpenElement(1, "form");
        builder.AddAttribute(2, "method", "get");
        builder.AddAttribute(3, "action", Navigation.BaseUri + Options.CultureEndpoint.TrimStart('/'));
        builder.OpenElement(4, "input");
        builder.AddAttribute(5, "type", "hidden");
        builder.AddAttribute(6, "name", "redirectUri");
        builder.AddAttribute(7, "value", new Uri(Navigation.Uri).PathAndQuery);
        builder.CloseElement();
        builder.OpenRegion(8);
        BuildSelect(builder, current, interactive: false);
        builder.CloseRegion();
        builder.OpenElement(9, "button");
        builder.AddAttribute(10, "type", "submit");
        builder.AddContent(11, SubmitText);
        builder.CloseElement();
        builder.CloseElement();
    }

    private void BuildSelect(RenderTreeBuilder builder, CultureInfo current, bool interactive)
    {
        IReadOnlyList<CultureInfo> cultures = State.SupportedCultures.Count > 0 ? State.SupportedCultures : [current];

        builder.OpenElement(0, "select");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "name", "culture");
        if (interactive)
            builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, OnChangeAsync));

        foreach (CultureInfo culture in cultures)
        {
            builder.OpenElement(4, "option");
            builder.AddAttribute(5, "value", culture.Name);
            builder.AddAttribute(6, "selected", culture.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase));
            builder.AddContent(7, DisplayName?.Invoke(culture) ?? culture.NativeName);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        if (e.Value is string name && name.Length > 0)
            await State.SetCultureAsync(CultureInfo.GetCultureInfo(name), ForceReload).ConfigureAwait(true);
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachCultureSelectorTests`
Expected: PASS. If bUnit lacks `ChangeAsync`, use `cut.Find("select").Change("de-DE")` followed by `cut.WaitForAssertion(...)`.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor/TlumachCultureSelector.cs tests/Tlumach.BlazorTests/TlumachCultureSelectorTests.cs
git commit -m "Add the TlumachCultureSelector component

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Culture-aware IStringLocalizer

**Files:**
- Create: `src/Tlumach.Blazor/TlumachCultureStringLocalizer.cs`
- Modify: `src/Tlumach.Blazor/TlumachBlazorServiceCollectionExtensions.cs`
- Test: `tests/Tlumach.BlazorTests/TlumachCultureStringLocalizerTests.cs`

**Interfaces:**
- Consumes: `TlumachStringLocalizer.WithCulture(CultureInfo)` (public, `Tlumach.Extensions.Localization`), `IStringLocalizerFactory`, `TlumachCultureState.Culture`.
- Produces: internal `TlumachCultureStringLocalizer(IStringLocalizer inner, TlumachCultureState state) : IStringLocalizer`; internal `TlumachCultureStringLocalizer<T>(IStringLocalizerFactory factory, TlumachCultureState state) : IStringLocalizer<T>`; `AddTlumachBlazor` now replaces `IStringLocalizer<>` and `IStringLocalizer` with scoped culture-aware wrappers.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.BlazorTests/TlumachCultureStringLocalizerTests.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Tlumach.Blazor;
using Tlumach.Extensions.Localization;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureStringLocalizerTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task GenericLocalizer_FollowsStateCulture()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, configureServices: s => s.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager));
        IStringLocalizer<TlumachCultureStringLocalizerTests> localizer = ctx.Services.GetRequiredService<IStringLocalizer<TlumachCultureStringLocalizerTests>>();
        Assert.Equal("Hello", localizer["hello"].Value);

        await ctx.Services.GetRequiredService<TlumachCultureState>().SetCultureAsync(TestTranslations.De);

        Assert.Equal("Hallo", localizer["hello"].Value);
        Assert.Equal("Element 2 von 5", localizer["position", 2, 5].Value);
    }

    [Fact]
    public async Task NonGenericLocalizer_FollowsStateCulture()
    {
        await using BunitContext ctx = TestContexts.Create(
            _translations,
            configureServices: s => s.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager),
            initialCulture: TestTranslations.De);

        Assert.Equal("Hallo", ctx.Services.GetRequiredService<IStringLocalizer>()["hello"].Value);
    }

    [Fact]
    public async Task AddTlumachLocalizationAfterAddTlumachBlazor_StillFollowsStateCulture()
    {
        await using BunitContext ctx = new();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddLogging();
        ctx.Services.AddTlumachBlazor(o => o.SupportedCultures = [TestTranslations.En, TestTranslations.De]);
        ctx.Services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);
        System.Globalization.CultureInfo.CurrentUICulture = TestTranslations.En;
        TlumachCultureState state = ctx.Services.GetRequiredService<TlumachCultureState>();
        IStringLocalizer<TlumachCultureStringLocalizerTests> localizer = ctx.Services.GetRequiredService<IStringLocalizer<TlumachCultureStringLocalizerTests>>();

        await state.SetCultureAsync(TestTranslations.De);

        Assert.Equal("Hallo", localizer["hello"].Value);
    }

    [Fact]
    public async Task OtherLocalizerFactory_IsPassedThrough()
    {
        await using BunitContext ctx = TestContexts.Create(_translations, configureServices: s => s.AddSingleton<IStringLocalizerFactory, FixedFactory>());

        Assert.Equal("fixed", ctx.Services.GetRequiredService<IStringLocalizer<TlumachCultureStringLocalizerTests>>()["anything"].Value);
    }

    private sealed class FixedFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new FixedLocalizer();

        public IStringLocalizer Create(string baseName, string location) => new FixedLocalizer();
    }

    private sealed class FixedLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, "fixed");

        public LocalizedString this[string name, params object[] arguments] => new(name, "fixed");

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~TlumachCultureStringLocalizerTests`
Expected: FAIL — the localizers return English after the switch (they follow `CultureInfo.CurrentCulture`, not the state), and `OtherLocalizerFactory_IsPassedThrough` fails to resolve `IStringLocalizer<T>`.

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/TlumachCultureStringLocalizer.cs`:

```csharp
using System.Globalization;

using Microsoft.Extensions.Localization;

using Tlumach.Extensions.Localization;

namespace Tlumach.Blazor;

/// <summary>
/// A localizer that retrieves strings in the culture of the user (<see cref="TlumachCultureState.Culture"/>) instead of the culture of the thread.
/// A localizer that does not come from Tlumach is used unchanged.
/// </summary>
internal sealed class TlumachCultureStringLocalizer : IStringLocalizer
{
    private readonly IStringLocalizer _inner;
    private readonly TlumachCultureState _state;
    private IStringLocalizer? _cached;
    private string? _cachedCultureName;

    public TlumachCultureStringLocalizer(IStringLocalizer inner, TlumachCultureState state)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public LocalizedString this[string name] => Current[name];

    public LocalizedString this[string name, params object[] arguments] => Current[name, arguments];

    private IStringLocalizer Current
    {
        get
        {
            if (_inner is not TlumachStringLocalizer tlumach)
                return _inner;

            CultureInfo culture = _state.Culture;
            if (_cached is null || !culture.Name.Equals(_cachedCultureName, StringComparison.Ordinal))
            {
                _cached = tlumach.WithCulture(culture);
                _cachedCultureName = culture.Name;
            }

            return _cached;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Current.GetAllStrings(includeParentCultures);
}

/// <summary>
/// The generic counterpart of <see cref="TlumachCultureStringLocalizer"/>, registered for <see cref="IStringLocalizer{T}"/>.
/// </summary>
/// <typeparam name="T">The type whose name selects the strings.</typeparam>
internal sealed class TlumachCultureStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly TlumachCultureStringLocalizer _localizer;

    public TlumachCultureStringLocalizer(IStringLocalizerFactory factory, TlumachCultureState state)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _localizer = new TlumachCultureStringLocalizer(factory.Create(typeof(T)), state);
    }

    public LocalizedString this[string name] => _localizer[name];

    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _localizer.GetAllStrings(includeParentCultures);
}
```

In `src/Tlumach.Blazor/TlumachBlazorServiceCollectionExtensions.cs` add `using Microsoft.Extensions.Localization;`, append to the `<summary>` of `AddTlumachBlazor` the sentence `It also makes the injected <see cref="IStringLocalizer"/> and <see cref="IStringLocalizer{T}"/> follow the culture of the user.`, and add before `return services;`:

```csharp
        // Scoped, so that each user's localizer follows that user's culture; Replace, so that the registrations of AddTlumachLocalization or AddLocalization do not win.
        services.Replace(ServiceDescriptor.Scoped(typeof(IStringLocalizer<>), typeof(TlumachCultureStringLocalizer<>)));
        services.Replace(ServiceDescriptor.Scoped<IStringLocalizer>(sp => new TlumachCultureStringLocalizer(
            sp.GetRequiredService<IStringLocalizerFactory>().Create(string.Empty, string.Empty),
            sp.GetRequiredService<TlumachCultureState>())));
```

- [ ] **Step 4: Run all Blazor tests**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor tests/Tlumach.BlazorTests/TlumachCultureStringLocalizerTests.cs
git commit -m "Make injected IStringLocalizer follow the per-user Blazor culture

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 9: LoadTlumachCultureAsync for WebAssembly startup

**Files:**
- Create: `src/Tlumach.Blazor/TlumachBlazorServiceProviderExtensions.cs`
- Test: `tests/Tlumach.BlazorTests/LoadTlumachCultureTests.cs`

**Interfaces:**
- Consumes: `ITlumachCultureStore.LoadAsync`, `TlumachBlazorOptions.FindSupportedCulture/ResolveInitialCulture`, `CultureApplier.ApplyGlobally`.
- Produces: `static Task<CultureInfo> LoadTlumachCultureAsync(this IServiceProvider services)`.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.BlazorTests/LoadTlumachCultureTests.cs`:

```csharp
using System.Globalization;

using Microsoft.JSInterop;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class LoadTlumachCultureTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task StoredCulture_IsAppliedGlobally()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult("de-DE");

        CultureInfo culture = await ctx.Services.LoadTlumachCultureAsync();

        Assert.Same(TestTranslations.De, culture);
        Assert.Same(TestTranslations.De, CultureInfo.DefaultThreadCurrentUICulture);
        Assert.Equal("de-DE", _translations.Manager.CurrentCulture.Name);
    }

    [Fact]
    public async Task CookiePersistence_ReadsHtmlLang()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.Cookie);
        ctx.JSInterop.Setup<string?>("document.documentElement.getAttribute", "lang").SetResult("de-DE");

        Assert.Same(TestTranslations.De, await ctx.Services.LoadTlumachCultureAsync());
    }

    [Fact]
    public async Task NothingStored_FallsBackToDefaultCulture()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult(null);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Same(TestTranslations.De, await ctx.Services.LoadTlumachCultureAsync());
    }

    [Fact]
    public async Task UnsupportedStoredCulture_IsIgnored()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetResult("fr-FR");
        CultureInfo.CurrentUICulture = TestTranslations.En;

        Assert.Same(TestTranslations.En, await ctx.Services.LoadTlumachCultureAsync());
    }

    [Fact]
    public async Task StoreFailure_FallsBack()
    {
        using GlobalCultureScope scope = new();
        await using BunitContext ctx = CreateContext(TlumachCulturePersistence.LocalStorage);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", "tlumach.culture").SetException(new JSException("unavailable"));
        CultureInfo.CurrentUICulture = TestTranslations.En;

        Assert.Same(TestTranslations.En, await ctx.Services.LoadTlumachCultureAsync());
    }

    private BunitContext CreateContext(TlumachCulturePersistence persistence)
    {
        BunitContext ctx = new();
        ctx.Services.AddLogging();
        ctx.Services.AddTlumachBlazor(o =>
        {
            o.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            o.DefaultCulture = TestTranslations.De;
            o.Persistence = persistence;
        });
        return ctx;
    }
}
```

(`AddLogging` needs `using Microsoft.Extensions.DependencyInjection;` — add it.)

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~LoadTlumachCultureTests`
Expected: build errors (`LoadTlumachCultureAsync` not found).

- [ ] **Step 3: Implement**

`src/Tlumach.Blazor/TlumachBlazorServiceProviderExtensions.cs`:

```csharp
using System.Globalization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Startup helpers for Blazor WebAssembly applications.
/// </summary>
public static class TlumachBlazorServiceProviderExtensions
{
    private static readonly Action<ILogger, Exception?> LogInvariantGlobalization = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(2, "InvariantGlobalization"),
        "Globalization invariant mode is enabled, so cultures cannot be switched. Remove InvariantGlobalization and set BlazorWebAssemblyLoadAllGlobalizationData to true.");

    private static readonly Action<ILogger, Exception?> LogLoadFailed = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(3, "CultureLoadFailed"),
        "Tlumach could not load the stored culture; the default culture is used.");

    /// <summary>
    /// Loads the stored culture and makes it the culture of the application. Call it in a Blazor WebAssembly client before <c>RunAsync</c>:
    /// <c>await host.Services.LoadTlumachCultureAsync();</c>.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The culture that was applied.</returns>
    public static async Task<CultureInfo> LoadTlumachCultureAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TlumachBlazorOptions options = services.GetRequiredService<TlumachBlazorOptions>();
        ILogger? logger = services.GetService<ILoggerFactory>()?.CreateLogger("Tlumach.Blazor");

        if (logger is not null && AppContext.TryGetSwitch("System.Globalization.Invariant", out bool invariant) && invariant)
            LogInvariantGlobalization(logger, null);

        string? name = null;
        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            try
            {
                name = await scope.ServiceProvider.GetRequiredService<ITlumachCultureStore>().LoadAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is JSException or InvalidOperationException or OperationCanceledException)
            {
                if (logger is not null)
                    LogLoadFailed(logger, ex);
            }
        }

        CultureInfo? culture = null;
        if (!string.IsNullOrEmpty(name))
        {
            try
            {
                culture = options.FindSupportedCulture(CultureInfo.GetCultureInfo(name));
            }
            catch (CultureNotFoundException)
            {
                culture = null;
            }
        }

        culture ??= options.ResolveInitialCulture(CultureInfo.CurrentUICulture);
        CultureApplier.ApplyGlobally(culture);
        return culture;
    }
}
```

- [ ] **Step 4: Run all Blazor tests**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tlumach.Blazor/TlumachBlazorServiceProviderExtensions.cs tests/Tlumach.BlazorTests/LoadTlumachCultureTests.cs
git commit -m "Add LoadTlumachCultureAsync for Blazor WebAssembly startup

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 10: Tlumach.AspNetCore — request localization and the culture endpoint

**Files:**
- Create: `src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj`
- Create: `src/Tlumach.AspNetCore/TlumachAspNetCoreExtensions.cs`
- Modify: `tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj` (add project reference)
- Test: `tests/Tlumach.BlazorTests/CultureEndpointTests.cs`

**Interfaces:**
- Consumes: `TlumachBlazorOptions` (registered as a singleton by `AddTlumachBlazor`), `FindSupportedCulture`, `CultureEndpoint`, `SupportedCultures`, `DefaultCulture`.
- Produces: `IApplicationBuilder UseTlumachRequestLocalization(this IApplicationBuilder app)`; `IEndpointConventionBuilder MapTlumachCultureEndpoint(this IEndpointRouteBuilder endpoints, string? pattern = null)`; internal `bool IsLocalUrl(string? url)`.

- [ ] **Step 1: Create the project**

`src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <LangVersion>latest</LangVersion>
        <IsAotCompatible>true</IsAotCompatible>
    </PropertyGroup>

    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>

    <ItemGroup>
        <AdditionalFiles Include="../Shared/stylecop.json" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Tlumach.Blazor\Tlumach.Blazor.csproj" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="Tlumach.BlazorTests" />
    </ItemGroup>

</Project>
```

Add to `tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj` inside the `ProjectReference` item group:

```xml
    <ProjectReference Include="..\..\src\Tlumach.AspNetCore\Tlumach.AspNetCore.csproj" />
```

- [ ] **Step 2: Write the failing tests**

`tests/Tlumach.BlazorTests/CultureEndpointTests.cs`:

```csharp
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;

using Tlumach.AspNetCore;
using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public class CultureEndpointTests
{
    [Fact]
    public async Task Post_SetsCookie_AndReturnsNoContent()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsync(new Uri("/tlumach/culture?culture=de-DE", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith(".AspNetCore.Culture=" + Uri.EscapeDataString("c=de-DE|uic=de-DE"), cookie, StringComparison.Ordinal);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("")]
    [InlineData("not a culture")]
    public async Task Post_UnsupportedOrInvalidCulture_Returns400WithoutCookie(string culture)
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsync(new Uri("/tlumach/culture?culture=" + Uri.EscapeDataString(culture), UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Get_SetsCookie_AndRedirectsToLocalUri()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/tlumach/culture?culture=de-DE&redirectUri=" + Uri.EscapeDataString("/counter?x=1"), UriKind.Relative));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/counter?x=1", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example/")]
    public async Task Get_NonLocalRedirect_FallsBackToRoot(string redirectUri)
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/tlumach/culture?culture=de-DE&redirectUri=" + Uri.EscapeDataString(redirectUri), UriKind.Relative));

        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task RequestLocalization_ReadsCookie()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/culture");
        request.Headers.Add("Cookie", ".AspNetCore.Culture=" + Uri.EscapeDataString("c=de-DE|uic=de-DE"));
        request.Headers.Add("Accept-Language", "en-US");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal("de-DE", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RequestLocalization_WithoutCookie_UsesDefaultCulture()
    {
        await using WebApplication app = await StartAsync();
        using HttpClient client = app.GetTestClient();

        Assert.Equal("en-US", await client.GetStringAsync(new Uri("/culture", UriKind.Relative)));
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/a/b?c=d", true)]
    [InlineData("//evil", false)]
    [InlineData("/\\evil", false)]
    [InlineData("https://evil", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLocalUrl_AcceptsOnlyRootRelativePaths(string? url, bool expected)
    {
        Assert.Equal(expected, TlumachAspNetCoreExtensions.IsLocalUrl(url));
    }

    private static async Task<WebApplication> StartAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddTlumachBlazor(o =>
        {
            o.SupportedCultures = [TestTranslations.En, TestTranslations.De];
            o.DefaultCulture = TestTranslations.En;
        });

        WebApplication app = builder.Build();
        app.UseTlumachRequestLocalization();
        app.MapTlumachCultureEndpoint();
        app.MapGet("/culture", (HttpContext context) => context.Features.Get<IRequestCultureFeature>()!.RequestCulture.UICulture.Name);
        await app.StartAsync();
        return app;
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj --filter FullyQualifiedName~CultureEndpointTests`
Expected: build errors (`UseTlumachRequestLocalization`, `MapTlumachCultureEndpoint` not found).

- [ ] **Step 4: Implement**

`src/Tlumach.AspNetCore/TlumachAspNetCoreExtensions.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Blazor;

namespace Tlumach.AspNetCore;

/// <summary>
/// Connects the culture chosen in Blazor (or any ASP.NET Core application) with the request localization of ASP.NET Core.
/// </summary>
public static class TlumachAspNetCoreExtensions
{
    // The longest culture name that Windows accepts (LOCALE_NAME_MAX_LENGTH); longer values are rejected without parsing.
    private const int MaxCultureNameLength = 85;

    /// <summary>
    /// Adds the request localization middleware configured from <see cref="TlumachBlazorOptions"/>: the supported cultures, the default culture,
    /// and the culture cookie as the first source of the culture. Call <c>AddTlumachBlazor</c> first.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>The value of <paramref name="app"/>.</returns>
    public static IApplicationBuilder UseTlumachRequestLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        TlumachBlazorOptions options = app.ApplicationServices.GetRequiredService<TlumachBlazorOptions>();
        RequestLocalizationOptions localization = new();

        if (options.SupportedCultures.Count > 0)
        {
            List<CultureInfo> cultures = [.. options.SupportedCultures];
            localization.SupportedCultures = cultures;
            localization.SupportedUICultures = cultures;
            localization.DefaultRequestCulture = new RequestCulture(options.DefaultCulture ?? cultures[0]);
        }
        else if (options.DefaultCulture is not null)
        {
            localization.DefaultRequestCulture = new RequestCulture(options.DefaultCulture);
        }

        IRequestCultureProvider? cookieProvider = localization.RequestCultureProviders.OfType<CookieRequestCultureProvider>().FirstOrDefault();
        if (cookieProvider is not null)
        {
            localization.RequestCultureProviders.Remove(cookieProvider);
            localization.RequestCultureProviders.Insert(0, cookieProvider);
        }

        return app.UseRequestLocalization(localization);
    }

    /// <summary>
    /// Maps the endpoint that stores the chosen culture in the ASP.NET Core culture cookie.
    /// <para><c>POST {pattern}?culture=de-DE</c> sets the cookie and returns 204; it is called by <see cref="CookieCultureStore"/>.
    /// <c>GET {pattern}?culture=de-DE&amp;redirectUri=/page</c> sets the cookie and redirects to the local URI; it is used by the form of <see cref="TlumachCultureSelector"/>
    /// in static server-side rendering. An unsupported culture yields 400.</para>
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern. When <see langword="null"/>, <see cref="TlumachBlazorOptions.CultureEndpoint"/> is used.</param>
    /// <returns>A builder for further configuration of both endpoints.</returns>
    public static IEndpointConventionBuilder MapTlumachCultureEndpoint(this IEndpointRouteBuilder endpoints, string? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        pattern ??= endpoints.ServiceProvider.GetRequiredService<TlumachBlazorOptions>().CultureEndpoint;
        RouteGroupBuilder group = endpoints.MapGroup(pattern);

        // RequestDelegate handlers need no request delegate generator, which keeps the library trimming- and AOT-safe.
        group.MapPost(string.Empty, context => SetCultureAsync(context, redirect: false));
        group.MapGet(string.Empty, context => SetCultureAsync(context, redirect: true));
        return group;
    }

    /// <summary>
    /// Checks that the URL is a path within this application ("/page"), not a URL of another site ("//site", "/\site", "https://site").
    /// </summary>
    internal static bool IsLocalUrl([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
            return false;

        return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
    }

    private static Task SetCultureAsync(HttpContext context, bool redirect)
    {
        TlumachBlazorOptions options = context.RequestServices.GetRequiredService<TlumachBlazorOptions>();
        CultureInfo? culture = ParseCulture(context.Request.Query["culture"], options);
        if (culture is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Task.CompletedTask;
        }

        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = true,
                IsEssential = true,
                Secure = context.Request.IsHttps,
            });

        if (!redirect)
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }

        string? redirectUri = context.Request.Query["redirectUri"];
        context.Response.Redirect(IsLocalUrl(redirectUri) ? redirectUri : $"{context.Request.PathBase}/");
        return Task.CompletedTask;
    }

    private static CultureInfo? ParseCulture(string? name, TlumachBlazorOptions options)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxCultureNameLength)
            return null;

        try
        {
            return options.FindSupportedCulture(CultureInfo.GetCultureInfo(name));
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
```

- [ ] **Step 5: Run all Blazor tests**

Run: `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj`
Expected: all PASS. Also run `dotnet build src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj` and confirm no IL2026/IL3050 (trimming/AOT) warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Tlumach.AspNetCore tests/Tlumach.BlazorTests
git commit -m "Add Tlumach.AspNetCore with request localization and the culture endpoint

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 11: Solutions, CI, and packaging

**Files:**
- Modify: `src/Tlumach.sln`, `src/Tlumach.Main.sln`, `tests/Tlumach.Tests.sln`
- Modify: `.github/workflows/build-test.yml`
- Modify: `Tlumach.nuspec`

- [ ] **Step 1: Add the projects to the solutions**

```bash
dotnet sln src/Tlumach.sln add src/Tlumach.Blazor/Tlumach.Blazor.csproj src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj
dotnet sln src/Tlumach.Main.sln add src/Tlumach.Blazor/Tlumach.Blazor.csproj src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj
dotnet sln tests/Tlumach.Tests.sln add src/Tlumach.Blazor/Tlumach.Blazor.csproj src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj
```

Check `git diff --stat` that only those three `.sln` files changed and that their line endings stayed CRLF (`file src/Tlumach.sln`).

- [ ] **Step 2: Add the CI step**

In `.github/workflows/build-test.yml`, after the `Test the generator` step, add (same indentation as the other steps):

```yaml
      - name: Test the Blazor integration
        run: dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj -c Release
```

- [ ] **Step 3: Add the assemblies to the nuspec**

Check the line endings: `file Tlumach.nuspec` (expect CRLF). Insert the Blazor dll and xml next to every `Tlumach.Extensions.Localization.dll` entry (same TFM folder and target) with:

```bash
sed -i -E 's#^([[:space:]]*)<file src="src\\Tlumach\.Extensions\.Localization\\bin\\Release\\(net[0-9.]+)\\Tlumach\.Extensions\.Localization\.dll" target="([^"]+)" />#&\r\n\1<file src="src\\Tlumach.Blazor\\bin\\Release\\\2\\Tlumach.Blazor.dll" target="\3" />\r\n\1<file src="src\\Tlumach.Blazor\\bin\\Release\\\2\\Tlumach.Blazor.xml" target="\3" />#' Tlumach.nuspec
```

(If `file` reported LF endings, drop both `\r` from the replacement.)

Then, with the Edit tool, add the `Tlumach.AspNetCore` entries after the Blazor xml lines whose target is exactly `lib\net9.0` and exactly `lib\net10.0`:

```xml
        <file src="src\Tlumach.AspNetCore\bin\Release\net9.0\Tlumach.AspNetCore.dll" target="lib\net9.0" />
        <file src="src\Tlumach.AspNetCore\bin\Release\net9.0\Tlumach.AspNetCore.xml" target="lib\net9.0" />
```

```xml
        <file src="src\Tlumach.AspNetCore\bin\Release\net10.0\Tlumach.AspNetCore.dll" target="lib\net10.0" />
        <file src="src\Tlumach.AspNetCore\bin\Release\net10.0\Tlumach.AspNetCore.xml" target="lib\net10.0" />
```

(Use the indentation of the surrounding lines.)

- [ ] **Step 4: Verify the packaging entries**

Run:

```bash
grep -c 'Tlumach.Blazor.dll' Tlumach.nuspec
grep -c 'Tlumach.Blazor.xml' Tlumach.nuspec
grep -c 'Tlumach.AspNetCore' Tlumach.nuspec
file Tlumach.nuspec
dotnet build src/Tlumach.Blazor/Tlumach.Blazor.csproj -c Release
dotnet build src/Tlumach.AspNetCore/Tlumach.AspNetCore.csproj -c Release
grep -oE 'src\\Tlumach\.(Blazor|AspNetCore)\\[^"]+' Tlumach.nuspec | sort -u | sed 's#\\#/#g' | while read -r f; do test -f "$f" || echo "MISSING $f"; done
```

Expected: `14`, `14`, `4`, CRLF still reported, both builds succeed, no `MISSING` lines.

- [ ] **Step 5: Build the solutions**

Run: `dotnet build src/Tlumach.Main.sln` and `dotnet test tests/Tlumach.Tests.sln`
Expected: both succeed; all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Tlumach.sln src/Tlumach.Main.sln tests/Tlumach.Tests.sln .github/workflows/build-test.yml Tlumach.nuspec
git commit -m "Add the Blazor projects to the solutions, CI, and the package

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 12: Blazor Web App sample

**Files:**
- Create: `samples/Tlumach.Sample.Blazor.Translation/Tlumach.Sample.Blazor.Translation.csproj`, `Sample.cfg`, `sample.arb`, `sample_de.arb`, `sample_uk.arb`
- Create: `samples/Tlumach.Sample.Blazor.Client/Tlumach.Sample.Blazor.Client.csproj`, `Program.cs`, `SampleCultures.cs`, `_Imports.razor`, `DemoPanel.razor`, `Pages/WebAssemblyPage.razor`
- Create: `samples/Tlumach.Sample.Blazor/Tlumach.Sample.Blazor.csproj`, `Program.cs`, `Properties/launchSettings.json`, `wwwroot/app.css`, `Components/App.razor`, `Components/Routes.razor`, `Components/_Imports.razor`, `Components/Layout/MainLayout.razor`, `Components/Pages/Home.razor`, `Components/Pages/ServerPage.razor`
- Modify: `src/Tlumach.sln` (add the three sample projects in a `samples` solution folder if the solution has one, otherwise at the root)
- Modify (untracked, not committed): `.claude/launch.json`

**Interfaces:**
- Consumes: everything from Tasks 2–10. The generator produces `Tlumach.Sample.Blazor.Translation.Strings` with static units named after the ARB keys (`Strings.Hello`, ...) and `Strings.TranslationManager`.

- [ ] **Step 1: Translation project**

`samples/Tlumach.Sample.Blazor.Translation/Tlumach.Sample.Blazor.Translation.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
        <LangVersion>8</LangVersion>
        <TlumachGeneratorExtraParsers>ArbParser,IniParser,JsonParser,ResxParser,TomlParser,TsvParser,XliffParser</TlumachGeneratorExtraParsers>
    </PropertyGroup>

    <ItemGroup>
        <CompilerVisibleProperty Include="TlumachGeneratorExtraParsers" />
    </ItemGroup>

    <ItemGroup>
        <AdditionalFiles Include="Sample.cfg" />
    </ItemGroup>

    <ItemGroup>
        <EmbeddedResource Include="sample.arb" />
        <EmbeddedResource Include="sample_de.arb" />
        <EmbeddedResource Include="sample_uk.arb" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Tlumach.Base\Tlumach.Base.csproj" />
        <ProjectReference Include="..\..\src\Tlumach.Generator\Tlumach.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\..\src\Tlumach\Tlumach.csproj" />
    </ItemGroup>

</Project>
```

`samples/Tlumach.Sample.Blazor.Translation/Sample.cfg`:

```ini
defaultFile=sample.arb
generatedNamespace=Tlumach.Sample.Blazor.Translation
generatedClass=Strings
textProcessingMode=Arb
delayedUnitsCreation=true

[translations]
de=sample_de.arb
uk=sample_uk.arb
```

`samples/Tlumach.Sample.Blazor.Translation/sample.arb`:

```json
{
    "@@locale": "en",
    "AppTitle": "Tlumach Blazor Sample",
    "NavHome": "Home (static SSR)",
    "NavServer": "Interactive Server",
    "NavWebAssembly": "Interactive WebAssembly",
    "HomeIntro": "This page is rendered on the server without interactivity. The language selector in the header sends a form to the culture endpoint, which stores the choice in a cookie and reloads the page.",
    "Hello": "Hello, world!",
    "RenderedBy": "This panel is rendered by the {mode} renderer.",
    "Language": "Language",
    "ReloadOption": "Reload the page after switching",
    "NamePlaceholder": "Type your name",
    "Greeting": "Hello, {name}!",
    "AddItem": "Add an item",
    "ItemCount": "{count, plural, =0{The basket is empty.} =1{The basket contains # item.} other{The basket contains # items.}}",
    "RichText": "Translations may contain <b>trusted HTML</b> when rendered with AsMarkup.",
    "LocalizerText": "This sentence comes from an injected IStringLocalizer.",
    "Today": "Today is {date}.",
    "BoundaryNote": "The header is rendered statically; it shows the new language after the next navigation."
}
```

`samples/Tlumach.Sample.Blazor.Translation/sample_de.arb`:

```json
{
    "@@locale": "de",
    "AppTitle": "Tlumach-Blazor-Beispiel",
    "NavHome": "Startseite (statisches SSR)",
    "NavServer": "Interaktiver Server",
    "NavWebAssembly": "Interaktives WebAssembly",
    "HomeIntro": "Diese Seite wird ohne Interaktivität auf dem Server gerendert. Die Sprachauswahl in der Kopfzeile sendet ein Formular an den Kultur-Endpunkt, der die Auswahl in einem Cookie speichert und die Seite neu lädt.",
    "Hello": "Hallo, Welt!",
    "RenderedBy": "Dieser Bereich wird vom Renderer {mode} gerendert.",
    "Language": "Sprache",
    "ReloadOption": "Seite nach dem Umschalten neu laden",
    "NamePlaceholder": "Geben Sie Ihren Namen ein",
    "Greeting": "Hallo, {name}!",
    "AddItem": "Artikel hinzufügen",
    "ItemCount": "{count, plural, =0{Der Warenkorb ist leer.} =1{Der Warenkorb enthält # Artikel.} other{Der Warenkorb enthält # Artikel.}}",
    "RichText": "Übersetzungen können <b>vertrauenswürdiges HTML</b> enthalten, wenn sie mit AsMarkup gerendert werden.",
    "LocalizerText": "Dieser Satz stammt aus einem injizierten IStringLocalizer.",
    "Today": "Heute ist {date}.",
    "BoundaryNote": "Die Kopfzeile wird statisch gerendert; sie zeigt die neue Sprache nach der nächsten Navigation."
}
```

`samples/Tlumach.Sample.Blazor.Translation/sample_uk.arb`:

```json
{
    "@@locale": "uk",
    "AppTitle": "Приклад Tlumach для Blazor",
    "NavHome": "Головна (статичний SSR)",
    "NavServer": "Інтерактивний сервер",
    "NavWebAssembly": "Інтерактивний WebAssembly",
    "HomeIntro": "Ця сторінка рендериться на сервері без інтерактивності. Вибір мови в заголовку надсилає форму до кінцевої точки культури, яка зберігає вибір у файлі cookie та перезавантажує сторінку.",
    "Hello": "Привіт, світе!",
    "RenderedBy": "Цю панель рендерить рендерер {mode}.",
    "Language": "Мова",
    "ReloadOption": "Перезавантажити сторінку після перемикання",
    "NamePlaceholder": "Введіть своє ім’я",
    "Greeting": "Привіт, {name}!",
    "AddItem": "Додати товар",
    "ItemCount": "{count, plural, =0{Кошик порожній.} =1{У кошику один товар.} other{Товарів у кошику: #.}}",
    "RichText": "Переклади можуть містити <b>довірений HTML</b>, якщо їх рендерити з AsMarkup.",
    "LocalizerText": "Це речення надходить із впровадженого IStringLocalizer.",
    "Today": "Сьогодні {date}.",
    "BoundaryNote": "Заголовок рендериться статично; нову мову він покаже після наступної навігації."
}
```

(In `Arb` mode a straight apostrophe `'` is an escape character; the Ukrainian text uses the typographic `’`.)

Run: `dotnet build samples/Tlumach.Sample.Blazor.Translation`
Expected: Build succeeded. If the generated class is missing (errors about `Strings` in later steps), set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` temporarily and inspect `obj/Generated`.

- [ ] **Step 2: WebAssembly client project**

`samples/Tlumach.Sample.Blazor.Client/Tlumach.Sample.Blazor.Client.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <NoDefaultLaunchSettingsFile>true</NoDefaultLaunchSettingsFile>
    <StaticWebAssetProjectMode>Default</StaticWebAssetProjectMode>
    <!-- Required for switching to arbitrary cultures at run time; do not enable InvariantGlobalization. -->
    <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tlumach.Base\Tlumach.Base.csproj" />
    <ProjectReference Include="..\..\src\Tlumach\Tlumach.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Blazor\Tlumach.Blazor.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Extensions.Localization\Tlumach.Extensions.Localization.csproj" />
    <ProjectReference Include="..\Tlumach.Sample.Blazor.Translation\Tlumach.Sample.Blazor.Translation.csproj" />
  </ItemGroup>

</Project>
```

`samples/Tlumach.Sample.Blazor.Client/SampleCultures.cs` (add the header):

```csharp
using System.Globalization;

using Tlumach.Blazor;

namespace Tlumach.Sample.Blazor.Client;

/// <summary>
/// The configuration shared by the server and the client, so both sides agree on the cultures and on where the choice is stored.
/// </summary>
public static class SampleCultures
{
    public static IReadOnlyList<CultureInfo> All { get; } =
    [
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.GetCultureInfo("uk-UA"),
    ];

    public static void Configure(TlumachBlazorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SupportedCultures = All;
        options.DefaultCulture = All[0];

        // Web App: the server stores the culture in a cookie and renders <html lang>, from which the WebAssembly client reads it on startup.
        options.Persistence = TlumachCulturePersistence.Cookie;
    }
}
```

`samples/Tlumach.Sample.Blazor.Client/Program.cs` (add the header):

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Tlumach.Blazor;
using Tlumach.Extensions.Localization;
using Tlumach.Sample.Blazor.Client;
using Tlumach.Sample.Blazor.Translation;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachBlazor(SampleCultures.Configure);

WebAssemblyHost host = builder.Build();
await host.Services.LoadTlumachCultureAsync();
await host.RunAsync();
```

`samples/Tlumach.Sample.Blazor.Client/_Imports.razor`:

```razor
@using System.Globalization
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.Extensions.Localization
@using Tlumach.Blazor
@using Tlumach.Sample.Blazor.Client
@using Tlumach.Sample.Blazor.Translation
```

`samples/Tlumach.Sample.Blazor.Client/DemoPanel.razor`:

```razor
@inherits TlumachComponentBase
@inject IStringLocalizer<Strings> Localizer

<section class="demo">
    <p class="note">
        <TlumachText Unit="Strings.RenderedBy" Args="@(new Dictionary<string, object?> { ["mode"] = RendererInfo.Name })" />
    </p>

    <p>
        <label>
            <TlumachText Unit="Strings.Language" />:
            <TlumachCultureSelector ForceReload="forceReload" />
        </label>
        <label>
            <input type="checkbox" @bind="forceReload" />
            <TlumachText Unit="Strings.ReloadOption" />
        </label>
    </p>

    <h2><TlumachText Unit="Strings.Hello" /></h2>

    <p>
        <input placeholder="@T(Strings.NamePlaceholder)" @bind="name" @bind:event="oninput" />
        @if (!string.IsNullOrWhiteSpace(name))
        {
            <TlumachText Unit="Strings.Greeting" Args="@(new Dictionary<string, object?> { ["name"] = name })" />
        }
    </p>

    <p>
        <button type="button" @onclick="() => count++"><TlumachText Unit="Strings.AddItem" /></button>
        <TlumachText Unit="Strings.ItemCount" Args="@(new Dictionary<string, object?> { ["count"] = count })" />
    </p>

    <p><TlumachText Unit="Strings.RichText" AsMarkup="true" /></p>
    <p>@Localizer["LocalizerText"]</p>
    <p>@TFrom(Strings.Today, new { date = DateTime.Today.ToString("D", Culture.Culture) })</p>
    <p class="note"><TlumachText Unit="Strings.BoundaryNote" /></p>
</section>

@code {
    private bool forceReload;
    private string name = string.Empty;
    private int count;
}
```

`samples/Tlumach.Sample.Blazor.Client/Pages/WebAssemblyPage.razor`:

```razor
@page "/webassembly"
@rendermode InteractiveWebAssembly

<h1><TlumachText Unit="Strings.NavWebAssembly" /></h1>
<DemoPanel />
```

- [ ] **Step 3: Server project**

`samples/Tlumach.Sample.Blazor/Tlumach.Sample.Blazor.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="10.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tlumach.AspNetCore\Tlumach.AspNetCore.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Base\Tlumach.Base.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Blazor\Tlumach.Blazor.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.Extensions.Localization\Tlumach.Extensions.Localization.csproj" />
    <ProjectReference Include="..\..\src\Tlumach\Tlumach.csproj" />
    <ProjectReference Include="..\Tlumach.Sample.Blazor.Client\Tlumach.Sample.Blazor.Client.csproj" />
    <ProjectReference Include="..\Tlumach.Sample.Blazor.Translation\Tlumach.Sample.Blazor.Translation.csproj" />
  </ItemGroup>

</Project>
```

`samples/Tlumach.Sample.Blazor/Program.cs` (add the header):

```csharp
using Tlumach.AspNetCore;
using Tlumach.Blazor;
using Tlumach.Extensions.Localization;
using Tlumach.Sample.Blazor.Client;
using Tlumach.Sample.Blazor.Components;
using Tlumach.Sample.Blazor.Translation;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachBlazor(SampleCultures.Configure);

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapTlumachCultureEndpoint();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SampleCultures).Assembly);

await app.RunAsync();
```

`samples/Tlumach.Sample.Blazor/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

`samples/Tlumach.Sample.Blazor/wwwroot/app.css`:

```css
body { font-family: system-ui, sans-serif; margin: 0; }
header { display: flex; gap: 1.5rem; align-items: center; padding: 0.75rem 1.5rem; background: #f0f0f4; flex-wrap: wrap; }
header nav { display: flex; gap: 1rem; }
main { padding: 1rem 1.5rem; }
.demo { border: 1px solid #ccd; border-radius: 6px; padding: 0.5rem 1rem; max-width: 48rem; }
.note { color: #555; font-size: 0.9rem; }
label { margin-right: 1rem; }
```

`samples/Tlumach.Sample.Blazor/Components/_Imports.razor`:

```razor
@using System.Globalization
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.Extensions.Localization
@using Tlumach.Blazor
@using Tlumach.Sample.Blazor.Client
@using Tlumach.Sample.Blazor.Components
@using Tlumach.Sample.Blazor.Components.Layout
@using Tlumach.Sample.Blazor.Translation
```

`samples/Tlumach.Sample.Blazor/Components/App.razor`:

```razor
<!DOCTYPE html>
@* The WebAssembly client reads the culture from the lang attribute on startup (TlumachCulturePersistence.Cookie). *@
<html lang="@CultureInfo.CurrentUICulture.Name">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="@Assets["app.css"]" />
    <ImportMap />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>
</html>
```

`samples/Tlumach.Sample.Blazor/Components/Routes.razor`:

```razor
<Router AppAssembly="typeof(Program).Assembly" AdditionalAssemblies="new[] { typeof(SampleCultures).Assembly }">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
    </Found>
</Router>
```

`samples/Tlumach.Sample.Blazor/Components/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase

<header>
    <strong><TlumachText Unit="Strings.AppTitle" /></strong>
    <nav>
        <a href=""><TlumachText Unit="Strings.NavHome" /></a>
        <a href="server"><TlumachText Unit="Strings.NavServer" /></a>
        <a href="webassembly"><TlumachText Unit="Strings.NavWebAssembly" /></a>
    </nav>
    @* The layout is rendered statically, so the selector renders as a form that goes through the culture endpoint. *@
    <TlumachCultureSelector SubmitText="✓" aria-label="Language" />
</header>

<main>
    @Body
</main>
```

`samples/Tlumach.Sample.Blazor/Components/Pages/Home.razor`:

```razor
@page "/"
@inject IStringLocalizer<Strings> Localizer

<h1><TlumachText Unit="Strings.NavHome" /></h1>
<p><TlumachText Unit="Strings.HomeIntro" /></p>
<h2><TlumachText Unit="Strings.Hello" /></h2>
<p>@Localizer["LocalizerText"]</p>
```

`samples/Tlumach.Sample.Blazor/Components/Pages/ServerPage.razor`:

```razor
@page "/server"
@rendermode InteractiveServer

<h1><TlumachText Unit="Strings.NavServer" /></h1>
<DemoPanel />
```

- [ ] **Step 4: Build and add to the solution**

Run: `dotnet build samples/Tlumach.Sample.Blazor`
Expected: Build succeeded (fix analyzer warnings in sample `.cs` files).

Run: `dotnet sln src/Tlumach.sln add --solution-folder samples samples/Tlumach.Sample.Blazor/Tlumach.Sample.Blazor.csproj samples/Tlumach.Sample.Blazor.Client/Tlumach.Sample.Blazor.Client.csproj samples/Tlumach.Sample.Blazor.Translation/Tlumach.Sample.Blazor.Translation.csproj` (first check with `grep -n samples src/Tlumach.sln` whether other samples are in the solution; if none are, skip this step to follow the existing pattern).

- [ ] **Step 5: Run the sample and verify switching (preview tools)**

Add to `.claude/launch.json` (read it first; keep existing entries) a configuration:

```json
{
  "name": "blazor-sample",
  "runtimeExecutable": "dotnet",
  "runtimeArgs": ["run", "--project", "samples/Tlumach.Sample.Blazor", "--launch-profile", "http"],
  "port": 5180
}
```

Start it with `preview_start {name: "blazor-sample"}` and check, using `read_page`/`get_page_text`, `computer` and `read_console_messages`:

1. `/` (static SSR): English texts; choose `Deutsch` in the header form, submit → page reloads in German; the cookie `.AspNetCore.Culture` exists (`javascript_tool`: cannot read HttpOnly — instead verify the German text after a reload).
2. `/server`: the panel says it is rendered by `Server`; change the panel's selector to `українська` → panel texts switch to Ukrainian without a page reload (the header stays German, as the boundary note explains); type a name → greeting appears; click "add item" → plural changes; reload → the page now starts in Ukrainian (cookie written via `fetch`).
3. `/webassembly`: the panel says `WebAssembly`; it starts in the cookie culture (via `<html lang>`); switching to English updates the panel live; reload → starts in English.
4. Tick "Reload the page after switching" on `/server`, switch → full reload in the new language, header included.
5. No errors in `read_console_messages` and `preview_logs` (level error).

Take a screenshot of the Server page after a live switch as proof.

- [ ] **Step 6: Trimmed publish of the client**

On this machine TEMP/TMP are on `D:` while the repo is on `C:`; emscripten (used by the relink during publish when `wasm-tools` is installed) fails across drives. Run with C:-based temp/cache dirs:

```bash
TEMP='C:\Users\Eugene\.tlumach-tmp' TMP='C:\Users\Eugene\.tlumach-tmp' EM_CACHE='C:\Users\Eugene\.tlumach-emcache' dotnet publish samples/Tlumach.Sample.Blazor -c Release -o "$SCRATCHPAD/blazor-publish" 2>&1 | grep -E "warning IL|error" | sort -u
```

(`$SCRATCHPAD` is the session scratchpad directory; create the `C:\Users\Eugene\.tlumach-*` directories first.)
Expected: no `IL2xxx`/`IL3xxx` warnings that originate in `Tlumach.Blazor` or `Tlumach.AspNetCore`; no errors. Then run the published app (`dotnet "$SCRATCHPAD/blazor-publish/Tlumach.Sample.Blazor.dll" --urls http://localhost:5181`, via a temporary `.claude/launch.json` entry) and repeat check 3 to confirm the trimmed WebAssembly client still translates and switches.

- [ ] **Step 7: Commit**

```bash
git add samples/Tlumach.Sample.Blazor samples/Tlumach.Sample.Blazor.Client samples/Tlumach.Sample.Blazor.Translation src/Tlumach.sln
git commit -m "Add the Blazor Web App sample with Server and WebAssembly render modes

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 13: Documentation

**Files:**
- Create: `docs/articles/getting-started-blazor.md`
- Modify: `docs/articles/index.md`, `docs/articles/strings.md`, `docs/articles/di.md`, `docs/articles/referencing.md`, `README.nuget.md`, `CHANGELOG.md`, `CLAUDE.md`

Note: `docs/articles/toc.yml` lists no getting-started page (they are linked from `index.md`), so the new guide is linked from `index.md` only, like the other getting-started pages.

- [ ] **Step 1: Write the guide**

`docs/articles/getting-started-blazor.md`:

````markdown
# Getting Started

## Integration with Blazor

`Tlumach.Blazor` localizes Blazor applications of every hosting model: Blazor Server (Interactive Server), Blazor WebAssembly (Interactive WebAssembly in a Blazor Web App and standalone), static server-side rendering (SSR), and Blazor Hybrid (MAUI, WPF, Windows Forms). The language can be switched while the application runs, and the components show the new language at once. `Tlumach.AspNetCore` adds the server-side pieces: request localization and an endpoint that stores the chosen culture in a cookie.

Both assemblies are in the `AlliedBits.Tlumach` package (.NET 9 and .NET 10). They ship no JavaScript or CSS files.

### Why a Blazor-specific integration

In a Blazor Server application, all users share one process, so the process-wide <xref:Tlumach.TranslationManager.CurrentCulture> cannot hold the language of a user. `Tlumach.Blazor` keeps the culture of each user in the scoped <xref:Tlumach.Blazor.TlumachCultureState> service (one per circuit) and always retrieves texts for that culture explicitly. In Blazor WebAssembly and Blazor Hybrid, one user owns the process, and the same API also switches the process-wide culture.

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

Call `AddTlumachBlazor` with the same options on both sides; a static class with a `Configure(TlumachBlazorOptions)` method in the client project, used by both `Program.cs` files, keeps them in sync (see the sample).

In `App.razor` of the server, render the culture into the `lang` attribute. Screen readers need it anyway, and with `TlumachCulturePersistence.Cookie` the WebAssembly client reads the culture from it on startup:

```razor
<html lang="@System.Globalization.CultureInfo.CurrentUICulture.Name">
```

### 3. Showing texts

Use the `TlumachText` component for text content:

```razor
<h1><TlumachText Unit="Strings.Hello" /></h1>
<TlumachText Unit="Strings.Greeting" Args="@(new Dictionary<string, object?> { ["name"] = user })" />
<TlumachText Unit="Strings.Position" Values="@(new object[] { index, total })" />
<TlumachText Key="Hello" />   @* by key, through TlumachBlazorOptions.DefaultManager or the Manager parameter *@
```

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

In an interactive component, it is a `select` element; a change switches the language at once. In static SSR, it is a small form that sends the choice to the culture endpoint, which stores it in the cookie and reloads the page; no JavaScript is needed.

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

To store the culture elsewhere, e.g. in a user profile, register your own `ITlumachCultureStore` before calling `AddTlumachBlazor`.

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

Razor encodes text, and so does `TlumachText`. Leave <xref:Tlumach.TranslationManager.WebEncodeValues> off in Blazor; if it is on, `TlumachText` and the `T` methods detect it and do not encode twice. For translations that contain trusted HTML, use `AsMarkup="true"` or `Culture.Markup(unit)`; translators then control the markup.

### 10. Placeholders on the server

Generated translation units are static and shared by all users. Do not keep per-user values in them via `CachePlaceholderValue` or `OnPlaceholderValueNeeded`; pass the values with `Args`, `Values`, `T(...)`, or `TFrom(...)` instead.

### Sample

The `samples/Tlumach.Sample.Blazor` Web App (with `Tlumach.Sample.Blazor.Client` and `Tlumach.Sample.Blazor.Translation`) shows a static SSR page, an Interactive Server page, and an Interactive WebAssembly page with live switching, placeholders, ICU plurals, `IStringLocalizer`, and `ForceReload`.
````

- [ ] **Step 2: Update index.md**

In `docs/articles/index.md`, in "The Ways to Use Tlumach" list, insert after item 2 (Windows Forms) a new item and renumber the following items (3→4, 4→5):

```markdown
3. Blazor applications (Server, WebAssembly, static SSR, Hybrid). The `TlumachText` component and the `TlumachComponentBase` helpers show translations in the language of each user, and the components get updated automatically when the language is switched.
```

In the "Choose the desired way" list, add after the Windows Forms line:

```markdown
- [Getting Started for integration with Blazor](getting-started-blazor.md)
```

- [ ] **Step 3: Update strings.md and di.md**

In `docs/articles/strings.md`, after the paragraph that ends with "use the `@Html.Raw(Identifier)` form instead." add:

```markdown
In Blazor, leave this property off: Razor encodes the text itself, and the `TlumachText` component of `Tlumach.Blazor` renders the text of the user's culture. If the property is on, `TlumachText` and the `T` methods of `TlumachComponentBase` detect it and do not encode the text twice. See [Getting Started for integration with Blazor](getting-started-blazor.md).
```

In `docs/articles/di.md`, after the "Culture" section's paragraph, add:

```markdown
## Blazor

In a Blazor Server application, the culture of the thread is the culture of the circuit, which does not change when the user switches the language in a running application. Call `AddTlumachBlazor` from `Tlumach.Blazor` in addition to `AddTlumachLocalization` (in any order): it registers scoped localizers that retrieve the strings in the culture of each user (<xref:Tlumach.Blazor.TlumachCultureState>), also after a live switch. See [Getting Started for integration with Blazor](getting-started-blazor.md).
```

In the "Web-safe Formatting" section of `di.md`, append: `In Blazor, leave it off; see [Getting Started for integration with Blazor](getting-started-blazor.md).`

- [ ] **Step 4: Update referencing.md**

In `docs/articles/referencing.md`:
- In the first paragraph's assembly list, add `` `Tlumach.Blazor.dll`, `Tlumach.AspNetCore.dll`, `` after `` `Tlumach.UWP.dll`, ``.
- Add table rows after the "Avalonia" row:

```markdown
| Blazor Web App / Blazor Server (server project) | `net9.0` / `net10.0` | `Tlumach.Blazor`, `Tlumach.AspNetCore`, `Tlumach.Extensions.Localization` | `Tlumach.Avalonia`, `Tlumach.WinUI` |
| Blazor WebAssembly (client project) | `net9.0` / `net10.0` | `Tlumach.Blazor`, `Tlumach.Extensions.Localization` | `Tlumach.AspNetCore` (server-only; not usable in the browser), `Tlumach.Avalonia`, `Tlumach.WinUI` |
```

- In the "Console / server / DI-only" row's last column, add `` `Tlumach.Blazor`, `Tlumach.AspNetCore` (unless used), `` at the beginning.

- [ ] **Step 5: Update README.nuget.md, CHANGELOG.md, CLAUDE.md**

`README.nuget.md`: after the Windows Forms bullet (line starting `* Integration with Windows Forms`), add:

```markdown
* Integration with Blazor (Server, WebAssembly, static SSR, Hybrid): the `TlumachText` component, the `TlumachComponentBase` helpers, and the `TlumachCultureSelector` component show translations in the language of each user and switch it live; `Tlumach.AspNetCore` stores the chosen culture in a cookie.
```

`CHANGELOG.md`: `version.json` says `1.13.0-alpha`, so 1.13.0 is not released — add these entries at the end of the bullet list of the `Version: 1.13.0` section:

```markdown
- [NEW] Blazor applications are supported through the new `Tlumach.Blazor` assembly (.NET 9 and .NET 10), which works with Blazor Server, Blazor WebAssembly (in a Blazor Web App and standalone), static server-side rendering, and Blazor Hybrid. The scoped `TlumachCultureState` service keeps the culture of each user, so the users of a Blazor Server application no longer share the process-wide `TranslationManager.CurrentCulture`; `SetCultureAsync` switches the language live or, with `forceReload`, reloads the page in the new language. The `TlumachText` component renders a translation unit or a key, with values for named or indexed placeholders, and re-renders when the language changes; `TlumachComponentBase` adds the `T` and `TFrom` methods for attributes and code; `TlumachCultureSelector` lets the user choose the language and works without interactivity, too. Injected `IStringLocalizer` and `IStringLocalizer<T>` follow the culture of the user. The culture is stored in the ASP.NET Core culture cookie or in the local storage of the browser; no JavaScript file is needed. A new sample, `samples/Tlumach.Sample.Blazor`, shows a Blazor Web App with static, Interactive Server, and Interactive WebAssembly pages. See "Getting Started for integration with Blazor".
- [NEW] The new `Tlumach.AspNetCore` assembly (.NET 9 and .NET 10, in the `net9.0` and `net10.0` folders of the package) provides `UseTlumachRequestLocalization`, which configures the request localization of ASP.NET Core from the Blazor options with the culture cookie first, and `MapTlumachCultureEndpoint`, which stores the chosen culture in the cookie.
- [NEW] The constructor of `CultureChangedEventArgs` is public, so that other code can raise culture change events with it.
- [FIX] `AddTlumachLocalization` replaced `IStringLocalizer` and `IStringLocalizer<T>` registrations that were made before it. It now adds its registrations only when there are none, so the culture-aware localizers of `AddTlumachBlazor` are kept regardless of the order of the calls.
```

`CLAUDE.md`:
- In the `src/` layout block, after the `Tlumach.WinForms/` line add:

```
  Tlumach.Blazor/               # Blazor integration (TlumachText, TlumachCultureState, TlumachCultureSelector)
  Tlumach.AspNetCore/           # ASP.NET Core helpers (UseTlumachRequestLocalization, MapTlumachCultureEndpoint)
```

- In the `tests/` layout block, after the `Tlumach.GeneratorTests/` line add:

```
  Tlumach.BlazorTests/          # bUnit and TestHost tests of Tlumach.Blazor and Tlumach.AspNetCore (run in CI)
```

- In the "Tests" section's first code block, add:

```bash
# Blazor integration tests (bUnit; also run in CI)
dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj -c Release
```

- In the "CI/CD" section, change the steps line to: `- Steps: build `Tlumach.Main.sln`, then run the main, generator, and Blazor tests`.

- [ ] **Step 6: Commit**

```bash
git add docs/articles/getting-started-blazor.md docs/articles/index.md docs/articles/strings.md docs/articles/di.md docs/articles/referencing.md README.nuget.md CHANGELOG.md CLAUDE.md
git commit -m "Document the Blazor integration

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 14: Final verification

- [ ] **Step 1: Full builds and tests**

Run, one by one:

```bash
dotnet build src/Tlumach.sln
dotnet build src/Tlumach.Main.sln -c Release
dotnet test tests/Tlumach.Tests.sln
dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release
dotnet test tests/Tlumach.GeneratorTests/Tlumach.GeneratorTests.csproj -c Release
dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj -c Release
```

Expected: all succeed. `src/Tlumach.sln` may need platform workloads for other projects; if it fails in a non-Blazor project, confirm the same failure exists on `main` (`git stash` is not needed — compare with `git worktree add` of `main` only if unsure) and report it rather than fixing it.

- [ ] **Step 2: Check new warnings**

Run: `dotnet build src/Tlumach.Main.sln -c Release 2>&1 | grep -E "Tlumach\.(Blazor|AspNetCore)" | grep -c warning`
Expected: `0`. Fix or justify each remaining warning.

- [ ] **Step 3: Update the knowledge graph**

Run: `graphify update .`

- [ ] **Step 4: Report**

Summarize for the user: what was built, test counts, the sample verification result with the screenshot, any deviations from the spec, and that nothing was pushed. Use superpowers:finishing-a-development-branch for the integration options.
