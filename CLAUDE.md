# Tlumach.NET — CLAUDE.md

## Project Overview

**Tlumach.NET** (`AlliedBits.Tlumach` on NuGet) is a translation and localization library for .NET applications. It supports multiple file formats, runtime language switching, XAML framework integrations, and Roslyn-based compile-time code generation for type-safe translation access.

**Target platforms:** Desktop (WPF, WinUI, WinForms, UWP), Mobile (MAUI, Avalonia), Web (Razor/Blazor), Console, and Server.

---

## Repository Layout

```
src/
  Tlumach.Main.sln              # Core packages only — use this for most work
  Tlumach.sln                   # Full solution including XAML framework integrations
  Tlumach/                      # Core library (TranslationManager, public API, Templating/TemplateTranslator for template engines)
  Tlumach.Base/                 # Parsers, TranslationEntry, TranslationConfiguration
  Tlumach.Generator/            # Roslyn incremental code generator
  Tlumach.Extensions.Localization/  # Microsoft.Extensions.Localization adapter
  Tlumach.WPF/                  # WPF-specific integration
  Tlumach.WinUI/                # WinUI-specific integration
  Tlumach.MAUI/                 # MAUI-specific integration
  Tlumach.Avalonia/             # Avalonia-specific integration
  Tlumach.UWP/                  # UWP-specific integration
  Tlumach.WinForms/             # Windows Forms integration (TranslationProvider, BindTranslation)
  Tlumach.Web/                  # Shared web core: TlumachCultureOptions, AddTlumachCultures; no ASP.NET dependency
  Tlumach.Blazor/               # Blazor integration (TlumachText, TlumachCultureState, TlumachCultureSelector)
  Tlumach.AspNetCore/           # ASP.NET Core hosting helpers (UseTlumachRequestLocalization, MapTlumachCultureEndpoint); no Blazor dependency
  Tlumach.AspNetCore.Mvc/       # MVC and Razor Pages: IHtmlLocalizer, IViewLocalizer, tag helpers, Html.Tlumach, culture selector, model binding messages, display names
  Tlumach.FluentValidation/     # FluentValidation integration (TlumachLanguageManager, WithMessage/WithName, AddTlumachFluentValidation); separate package
  Tlumach.Scriban/              # Scriban integration (ImportTlumach: t/t_html functions); separate package
  Tlumach.Fluid/                # Fluid (Liquid) integration (AddTlumach: t/t_html filters); separate package
  Tlumach.HandlebarsNet/        # Handlebars.Net integration (RegisterTlumach: t/t_html helpers); separate package
  Tlumach.MudBlazor/            # MudBlazor integration (TlumachMudLocalizer, AddTlumachMudBlazor; per-user culture of Tlumach.Blazor); separate package
  Tlumach.Syncfusion.Blazor/    # Syncfusion Blazor integration (TlumachSyncfusionLocalizer, AddTlumachSyncfusionBlazor; optional official .resx fallback); separate package
  Shared/                       # Shared MSBuild props and StyleCop config
tests/
  Tlumach.Tests.sln
  Tlumach.WinFormsTests.sln     # Windows-only solution for the WinForms tests
  Tlumach.Tests/                # Main xUnit test suite
  Tlumach.GeneratorTests/       # Generator-specific tests
  Tlumach.BlazorTests/          # bUnit tests of Tlumach.Blazor (run in CI)
  Tlumach.MvcTests/             # Tests of Tlumach.AspNetCore.Mvc in an MVC host, and of Tlumach.Web and Tlumach.AspNetCore (run in CI)
  Tlumach.RazorPagesTests/      # Tests of Tlumach.AspNetCore.Mvc in a pure Razor Pages host (run in CI)
  Tlumach.FluentValidationTests/ # Tests of Tlumach.FluentValidation (run in CI)
  Tlumach.TemplateEngineTests/  # Shared scenario tests of the Scriban, Fluid, and Handlebars.Net integrations (run in CI)
  Tlumach.MudBlazorTests/       # bUnit tests of Tlumach.MudBlazor with real MudBlazor components (run in CI)
  Tlumach.SyncfusionTests/      # bUnit tests of Tlumach.Syncfusion.Blazor with a real SfGrid (run in CI)
  Tlumach.WinFormsTests/        # Windows-only tests of Tlumach.WinForms (not run in CI)
samples/                        # One sample project per supported platform/scenario
docs/                           # DocFX documentation source
```

---

## Build

```bash
# Core packages only (preferred for day-to-day work)
dotnet build src/Tlumach.Main.sln

# Full solution including XAML frameworks (requires platform SDKs)
dotnet build src/Tlumach.sln

# Release build
dotnet build src/Tlumach.Main.sln -c Release
```

Build configurations: `Debug`, `Release`, `SignedRelease`.
The `Release` configuration enables deterministic output (`ContinuousIntegrationBuild=true`) and embeds source files.

---

## Tests

```bash
# Run main test suite (as the CI pipeline does)
dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release

# Whole test solution (also works)
dotnet test tests/Tlumach.Tests.sln

# Blazor integration tests (bUnit; also run in CI)
dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj -c Release

# MVC integration tests (also run in CI)
dotnet test tests/Tlumach.MvcTests/Tlumach.MvcTests.csproj -c Release

# Razor Pages integration tests (also run in CI)
dotnet test tests/Tlumach.RazorPagesTests/Tlumach.RazorPagesTests.csproj -c Release

# FluentValidation integration tests (also run in CI)
dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj -c Release

# Template engine integration tests (also run in CI)
dotnet test tests/Tlumach.TemplateEngineTests/Tlumach.TemplateEngineTests.csproj -c Release

# MudBlazor integration tests (also run in CI)
dotnet test tests/Tlumach.MudBlazorTests/Tlumach.MudBlazorTests.csproj -c Release

# Syncfusion Blazor integration tests (also run in CI; no Syncfusion license key needed)
dotnet test tests/Tlumach.SyncfusionTests/Tlumach.SyncfusionTests.csproj -c Release
```

```bash
# Windows Forms integration tests (Windows only; not part of the ubuntu CI and not in Tlumach.Tests.sln)
dotnet test tests/Tlumach.WinFormsTests.sln
```

Test files are in `tests/Tlumach.Tests/`. Each file format has its own `*ParserTests.cs`. Test data (sample translation files) are embedded resources under `tests/Tlumach.Tests/TestData/`.

---

## CI/CD

GitHub Actions workflow: `.github/workflows/build-test.yml`

- Trigger: push/PR to `main` or `release/*`
- Runner: `ubuntu-latest`, .NET 10.0.x
- Steps: build `Tlumach.Main.sln`, then run the main, generator, Blazor, MVC, Razor Pages, FluentValidation, and template engine tests, and build the Blazor, MVC, Razor Pages, FluentValidation, Scriban, Fluid, and Handlebars.Net samples
- MudBlazor: run the MudBlazor tests and build the MudBlazor sample
- Syncfusion Blazor: run the Syncfusion tests and build the Syncfusion sample

---

## Key Architecture

### Core Components

| Component | Location | Purpose |
|---|---|---|
| `TranslationManager` | `src/Tlumach/TranslationManager.cs` | Central service; manages cultures, caching, events |
| `TranslationEntry` | `src/Tlumach.Base/TranslationEntry.cs` | Immutable translation unit (key, text, metadata) |
| `TranslationConfiguration` | `src/Tlumach.Base/TranslationConfiguration.cs` | Declarative `.cfg` file model |
| Parser base classes | `src/Tlumach.Base/` | `BaseParser`, `BaseJsonParser`, `BaseKeyValueParser`, `BaseTableParser`, `BaseXMLParser` |
| Concrete parsers | `src/Tlumach.Base/` | `JsonParser`, `ArbParser`, `IniParser`, `TomlParser`, `CsvParser`, `TsvParser`, `ResxParser` |
| Writer base classes | `src/Tlumach.Writers/` | `BaseWriter`, `BaseJsonWriter`, `BaseKeyValueWriter`, `BaseTableWriter`, `BaseXmlWriter` |
| Concrete writers | `src/Tlumach.Writers/` | `JsonWriter`, `IniWriter`, `TomlWriter`, `CsvWriter`, `TsvWriter`, `ResxWriter` |
| Code generator | `src/Tlumach.Generator/Generator.cs` | Roslyn incremental generator producing typed translation classes |
| ICU/placeholder engine | `src/Tlumach.Base/IcuFragment.cs` | Handles `{name}`, `{0}`, `plural`, `select`, `date`, etc. |
| `TemplateTranslator` | `src/Tlumach/Templating/TemplateTranslator.cs` | Engine-neutral core of the Scriban, Fluid, and Handlebars.Net integrations (culture, lookup, placeholder values, encoding, missing keys) |

### Patterns

- **Strategy** — Parsers are registered with `Use()` and selected per format.
- **Observer** — `TranslationManager` exposes events: `CultureChanged`, `OnTranslationFileNotFound`, `OnReferenceNotResolved`, `OnPlaceholderValueNeeded`.
- **Incremental generation** — `Tlumach.Generator` uses Roslyn's incremental generator API; it targets `Microsoft.CodeAnalysis.CSharp` v5.0.0 for broad SDK compatibility.

### Supported File Formats

JSON, ARB, INI, TOML, CSV, TSV, ResX.

---

## Versioning

Versions are set by hand; Nerdbank.GitVersioning is not referenced by the projects in `src/`.
Current version: `2.0.0` (unreleased; the latest release tag is `v1.12.0`).

| Where | What |
|---|---|
| `Directory.Build.props` | `Version`, `FileVersion`, `AssemblyVersion` of all assemblies |
| `src/Extension.VSCode/package.json`, `src/Extension.VisualStudio/source.extension.vsixmanifest` | Versions of the IDE extensions, kept equal to the library version |
| `version.json` | Next version with `-alpha`; bumped right after a release with `nbgv prepare-release` ("Set version to 'X.Y.Z-alpha'") |
| `CHANGELOG.md` | `Version:` heading of the top section |

The nuspecs contain no versions or commit SHAs: `<version>$version$</version>`, `<repository commit="$commit$">`, and `version="[$version$]"` (an exact range) for every dependency on another `AlliedBits.Tlumach*` package. `build-tlumach-net.cmd` (outside the repository, in `C:\Projects\Tlumach`) fills them in with `nuget pack -Properties "version=...;commit=..."` from `Directory.Build.props` and `git rev-parse HEAD`.

Release steps: bump the versions above in the last code commit and tag it `v{version}`, run `build-tlumach-net.cmd` on that commit (it refuses a working tree with uncommitted changes unless `/dirty` is given), then `publish-tlumach-net.cmd`, then bump `version.json` to the next `-alpha` version. Up to v1.12.0, a separate commit "Updated the nuget specs." set the commit SHAs in the nuspecs; this is no longer needed.
Release branches follow the pattern `release/v{version}`.

---

## Code Style

- Indentation: 4 spaces (enforced by `.editorconfig`)
- Line endings: CRLF (LF for shell scripts)
- Encoding: UTF-8
- Analyzers active in all builds: Roslynator, StyleCop, SonarAnalyzer, Meziantou, NetAnalyzers
- Analysis mode: `AllEnabledByDefault` — expect warnings to be treated as guidance, many suppressed selectively via `.editorconfig`
- StyleCop file headers use the company name defined in `Shared/stylecop.json`

Do not suppress or silence analyzer warnings without understanding the rule. Check `.editorconfig` for any existing rule severities before adding new suppressions.

---

## Adding a New Parser

1. Add a class in `src/Tlumach.Base/` extending the appropriate base (`BaseParser`, `BaseJsonParser`, etc.).
2. Implement `Parse()` returning `IEnumerable<TranslationEntry>`.
3. Add test data files under `tests/Tlumach.Tests/TestData/`.
4. Add a corresponding `*ParserTests.cs` in `tests/Tlumach.Tests/`.
5. If the parser should be available in the generator, expose it via `TlumachGeneratorExtraParsers` or register it in the generator project.

---

## Adding a New Writer

Writers allow saving translations to various file formats. The `Tlumach.Writers` project (`src/Tlumach.Writers/`) contains all writer implementations.

1. **For simple formats** (key-value, CSV/TSV):
   - Extend `BaseKeyValueWriter` or `BaseTableWriter`
   - Implement abstract methods for format-specific output
   - Example: `IniWriter`, `TomlWriter`, `CsvWriter`

2. **For complex formats** (JSON, XML):
   - Extend `BaseJsonWriter` or `BaseXmlWriter`
   - Implement `InternalWriteTranslations()` using the appropriate DOM library
   - Example: `JsonWriter`, `ResxWriter`

3. **Add tests**:
   - Create `*WriterTests.cs` in `tests/Tlumach.WriterTests/`
   - Test round-trip (load → write → load) when possible
   - Test cross-format conversion (e.g., JSON→RESX)
   - Use the shared `TranslationComparer.AssertTranslationsEqual()` for assertions
   - Example: `ResxWriterTests`

### Writer Architecture

**Base classes** (in inheritance order):
- `BaseWriter` — Abstract base defining all writer contracts
- Format-specific bases — `BaseJsonWriter`, `BaseKeyValueWriter`, `BaseTableWriter`, `BaseXmlWriter`
- Concrete writers — Override `FormatName`, `ConfigExtension`, `TranslationExtension`, and implement format-specific logic

**Key properties each writer must implement:**
- `FormatName` — Display name (e.g., "RESX", "JSON")
- `ConfigExtension` — File extension for config files (e.g., ".resxcfg")
- `TranslationExtension` — File extension for translation files (e.g., ".resx")

**Pattern**: Writers mirror parsers. Each parser has a corresponding writer handling the same format.

---

## Documentation

- Source: `docs/` (DocFX)
- Build: `docs/build.cmd` → outputs to `docs/_site/`
- NuGet readme: `README.nuget.md`
- Changelog: `CHANGELOG.md`
- FAQ: `FAQ.md`

---

## NuGet Package

Every `*.nuspec` in the repository root produces one package; `tools/Validate-Packages.ps1` holds the expected layout of all of them (package IDs, lib folders, assemblies, dependencies on each other) and fails on any difference, so update it together with the nuspecs.

| Package | Nuspec | Assemblies |
|---|---|---|
| `AlliedBits.Tlumach` (core) | `Tlumach.nuspec` | `Tlumach.Base`, `Tlumach`, `Tlumach.DataAnnotations` (lib), `Tlumach.Generator` (`analyzers/dotnet`) |
| `AlliedBits.Tlumach.<X>` for X = `WPF`, `WinForms`, `WinUI`, `UWP`, `MAUI`, `Avalonia`, `Blazor`, `Web`, `Extensions.Localization`, `Writers`, `FluentValidation`, `Scriban`, `Fluid`, `HandlebarsNet`, `MudBlazor`, `Syncfusion.Blazor` | `Tlumach.<X>.nuspec` | `Tlumach.<X>` |
| `AlliedBits.Tlumach.AspNetCore` | `Tlumach.AspNetCore.nuspec` | `Tlumach.AspNetCore`, `Tlumach.AspNetCore.Mvc` |

- Each assembly is in exactly one package; every package depends on `AlliedBits.Tlumach`, Blazor and AspNetCore also on Web and Extensions.Localization, MudBlazor and Syncfusion.Blazor also on Blazor.
- The integration packages declare their framework dependencies (Avalonia `[11.0.0, 12.0.0)`, `Microsoft.Maui.Controls` and `Microsoft.AspNetCore.Components.Web` 9.0.0 / 10.0.0 per TFM, the `Microsoft.AspNetCore.App` framework reference); WPF, WinForms, WinUI, and UWP declare none.
- The ten integration packages (all except Writers, FluentValidation, Scriban, Fluid, HandlebarsNet, MudBlazor, Syncfusion.Blazor) reference `AlliedBits.Tlumach` with `exclude="Build"` only, so the generator reaches applications that reference only an integration package.
- Each package has its own `README.<x>.nuget.md`.
- Packages are created by `C:\Projects\Tlumach\build-tlumach-net.cmd` into `C:\Projects\Tlumach\Redist\nuget\<version>\` and published to nuget.org by `C:\Projects\Tlumach\publish-tlumach-net.cmd` (API key from `NUGET_API_KEY` or from `nuget setapikey`).

## graphify

This project has a graphify knowledge graph at graphify-out/.

Rules:
- Before answering architecture or codebase questions, read graphify-out/GRAPH_REPORT.md for god nodes and community structure
- If graphify-out/wiki/index.md exists, navigate it instead of reading raw files
- For cross-module "how does X relate to Y" questions, prefer `graphify query "<question>"`, `graphify path "<A>" "<B>"`, or `graphify explain "<concept>"` over grep — these traverse the graph's EXTRACTED + INFERRED edges instead of scanning files
- After modifying code files in this session, run `graphify update .` to keep the graph current (AST-only, no API cost)
