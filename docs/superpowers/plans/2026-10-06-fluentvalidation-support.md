# FluentValidation Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Localize FluentValidation's built-in messages and per-rule messages and names through Tlumach. This ships as the separate package `AlliedBits.Tlumach.FluentValidation`, together with a Tlumach core fix that makes the source of a looked-up entry reliable.

**Architecture:**
- **Core.** Tlumach records which cached entries were borrowed from another translation and exposes the source of a lookup through `TranslationManager.GetValueWithSource`.
- **Language manager.** The new `Tlumach.FluentValidation` assembly subclasses FluentValidation's `LanguageManager`. It chooses between Tlumach text and FluentValidation's built-in text in a culture-aware four-step order.
- **Rule helpers and DI.** Lazy rule extensions read raw templates, so FluentValidation still formats them. A DI method installs the language manager and an optional display-name resolver.

**Tech Stack:** C#, .NET 9/10, FluentValidation 12.1.x, xUnit 2.9, Microsoft.Extensions.DependencyInjection.

**Spec:** `docs/superpowers/specs/2026-10-06-fluentvalidation-design.md`

## Global Constraints

**FluentValidation version, packaging and targets**
- FluentValidation version range: `[12.0.0, 13.0.0)`. The latest is 12.1.1, which ships `lib/net8.0` only.
- New library targets: `net9.0;net10.0`, `IsAotCompatible=true`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`.
- The package is the separate `AlliedBits.Tlumach.FluentValidation`, from `Tlumach.FluentValidation.nuspec`. `Tlumach.nuspec` is not changed.

**Option names and defaults**
- `FluentValidationGroup` defaults to `"FluentValidation"`. A null or empty value means root keys.
- `DisplayNamesGroup` defaults to `"DisplayNames"`.
- `MessageCultureSource` defaults to `CurrentUICulture`.

**Code style**
- 4 spaces, CRLF line endings, UTF-8.
- File-scoped namespaces in new projects.
- `using` directives go outside the namespace.
- **Never** write a qualified `FluentValidation.X` name inside `namespace Tlumach.FluentValidation`, because it binds to `Tlumach.FluentValidation`. Use `using` directives only.
- Every new `.cs` file starts with this header, with `<FileName>` replaced by the file name:

```csharp
// <copyright file="<FileName>" company="Allied Bits Ltd.">
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

**Analyzers**
- Analyzers run in `AllEnabledByDefault` mode. A build must add no new warnings.
- Fix a warning; do not suppress it unless the rule has been read and understood. If you do suppress one, add a justification, and check `.editorconfig` first.

**Commits**
- Commit after each task with the message given. End every commit message with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Only commit after the user has allowed commits for this execution.
- Never commit to `main`. The branch is `fluentvalidation`.

**Line endings**
- New files must be CRLF. After writing a file with a tool that writes LF, convert it with `sed -i 's/$/\r/' <file>` (Git Bash). Check with `file <file>`.

---

## File Structure

**Core (modify)**
- `src/Tlumach/TranslationEntrySource.cs` (new): the public enum.
- `src/Tlumach/TranslationManager.cs`: `GetValueWithSource`, the private `GetValueCore`, and borrowed-entry tracking.
- `tests/Tlumach.Tests/TranslationEntrySourceTests.cs` (new) and `tests/Tlumach.Tests/TranslationManagerBorrowedEntryTests.cs` (new).

**`src/Tlumach.FluentValidation/` (new)**

| File | Contents |
|---|---|
| `Tlumach.FluentValidation.csproj` | The project |
| `MessageCultureSource.cs` | The enum |
| `TlumachLanguageManagerOptions.cs` | The options of the language manager |
| `CultureMatching.cs` | Internal helpers: neutral language, English check, default-file culture |
| `TlumachLanguageManager.cs` | The `LanguageManager` subclass |
| `ValidationTemplates.cs` | Internal: rule-level culture resolution and raw template reads |
| `TlumachRuleBuilderExtensions.cs` | `WithMessage` / `WithName` |
| `TlumachDisplayNameResolver.cs` | The opt-in `DisplayNameResolver` |
| `TlumachFluentValidationOptions.cs` | DI options |
| `TlumachFluentValidationServiceCollectionExtensions.cs` | `AddTlumachFluentValidation` |

**`tests/Tlumach.FluentValidationTests/` (new)**
- The project, `TestAssemblyInfo.cs`, `TestTranslations.cs`, `ValidatorOptionsScope.cs` and `Customer.cs`.
- Test data: `TestData/Json/Strings.jsoncfg`, `TestData/Json/Strings.json`, `TestData/Json/Strings_de.json`, `TestData/Arb/Strings.arbcfg` and `TestData/Arb/Strings.arb`.
- Test classes: `TlumachLanguageManagerTests.cs`, `TlumachRuleBuilderExtensionsTests.cs`, `TlumachDisplayNameResolverTests.cs`, `ConcurrencyTests.cs` and `ServiceCollectionExtensionsTests.cs`.

**Sample (new):** `samples/Tlumach.Sample.FluentValidation/`.

**Packaging and docs**
- `Tlumach.FluentValidation.nuspec` and `README.fluentvalidation.nuget.md` (new).
- `docs/articles/fluent-validation.md` (new).
- `docs/articles/toc.yml`, `docs/articles/index.md`, `README.nuget.md`, `CHANGELOG.md`, `CLAUDE.md`, `.github/workflows/build-test.yml` (modify).
- `src/Tlumach.Main.sln`, `src/Tlumach.sln` and `tests/Tlumach.Tests.sln` (modify).

---

### Task 1: `TranslationEntrySource` and `GetValueWithSource`

**Files:**
- Create: `src/Tlumach/TranslationEntrySource.cs`
- Modify: `src/Tlumach/TranslationManager.cs`. The `out bool` overloads are at about lines 531-596, and the main lookup method starts at about line 596.
- Test: `tests/Tlumach.Tests/TranslationEntrySourceTests.cs`

**Interfaces:**
- Produces:
  - `public enum TranslationEntrySource { NotFound = 0, Culture = 1, BasicCulture = 2, DefaultTranslation = 3 }` in namespace `Tlumach`.
  - `public TranslationEntry GetValueWithSource(TranslationConfiguration config, string key, CultureInfo culture, out TranslationEntrySource source)`
  - `public TranslationEntry GetValueWithSource(string key, CultureInfo culture, out TranslationEntrySource source)`
  - `public TranslationEntry GetValueWithSource(TranslationConfiguration config, string key, string[] langIDs, out TranslationEntrySource source)`
  - `private TranslationEntry GetValueCore(TranslationConfiguration config, string key, CultureInfo? culture, string[]? langIDs, out TranslationEntrySource source)`
- Consumes: `LangIDsFixtures` from `tests/Tlumach.Tests/LangIDsFixtures.cs`:
  - `CreateDirectory()`, `CreateConfiguration(dir)` and `CreateManager(config, dir, cacheDefaultTranslations)`.
  - The files are `LangStrings.arb` (en: `hello`, `welcome`, `onlyInDefault`), `_de`, `_de-DE`, `_de-AT` (no `welcome`), `_fr`, `_fr-FR` and `_fr-CA` (no `welcome`).

**Why not overload `GetValue`:** `GetValue(config, key, culture, out _)` already exists with `out bool`. An overload that differs only in the `out` type would make calls with `out _` or `out var` ambiguous (CS0121), which breaks existing user code.

- [ ] **Step 1: Write the failing tests**

Create `tests/Tlumach.Tests/TranslationEntrySourceTests.cs` (header, then):

```csharp
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of <see cref="TranslationManager.GetValueWithSource(TranslationConfiguration, string, CultureInfo, out TranslationEntrySource)"/> and its siblings.
    /// Each test performs a single lookup per key, so that the cache of default translations plays no role here; the cache is covered by
    /// <see cref="TranslationManagerBorrowedEntryTests"/>.
    /// </summary>
    [Trait("Category", "TranslationManager")]
    public class TranslationEntrySourceTests
    {
        private static TranslationManager CreateManager(out TranslationConfiguration config)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            config = LangIDsFixtures.CreateConfiguration(dir);
            return LangIDsFixtures.CreateManager(config, dir);
        }

        [Fact]
        public void ExactMatch_ReportsCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "hello", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.Equal("Salut (fr)", entry.Text);
            Assert.Equal(TranslationEntrySource.Culture, source);
        }

        [Fact]
        public void BasicCultureMatch_ReportsBasicCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "welcome", new CultureInfo("de-AT"), out TranslationEntrySource source);

            Assert.Equal("Willkommen (de-DE)", entry.Text);
            Assert.Equal(TranslationEntrySource.BasicCulture, source);
        }

        [Fact]
        public void KeyOnlyInDefaultFile_ReportsDefaultTranslation()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "onlyInDefault", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.Equal("OnlyInDefault", entry.Text);
            Assert.Equal(TranslationEntrySource.DefaultTranslation, source);
        }

        [Fact]
        public void RequestInDefaultLocale_ReportsDefaultTranslation()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "hello", new CultureInfo(LangIDsFixtures.DefaultLocale), out TranslationEntrySource source);

            Assert.Equal("Hello", entry.Text);
            Assert.Equal(TranslationEntrySource.DefaultTranslation, source);
        }

        [Fact]
        public void MissingKey_ReportsNotFound()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "noSuchKey", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.True(string.IsNullOrEmpty(entry.Text));
            Assert.Equal(TranslationEntrySource.NotFound, source);
        }

        [Fact]
        public void TextFromValueNeededHandler_ReportsCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            manager.OnTranslationValueNeeded += (_, args) =>
            {
                if (args.Key == "hello")
                    args.Text = "From the handler";
            };

            TranslationEntry entry = manager.GetValueWithSource(config, "hello", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.Equal("From the handler", entry.Text);
            Assert.Equal(TranslationEntrySource.Culture, source);
        }

        [Fact]
        public void EntryFromValueNeededHandler_ReportsCultureAndFoundForCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            manager.OnTranslationValueNeeded += (_, args) =>
            {
                if (args.Key == "hello")
                    args.Entry = new TranslationEntry("hello", "Entry from the handler");
            };

            manager.GetValueWithSource(config, "hello", new CultureInfo("fr"), out TranslationEntrySource source);
            manager.GetValue(config, "hello", new CultureInfo("fr"), out bool foundForCulture);

            Assert.Equal(TranslationEntrySource.Culture, source);
            Assert.True(foundForCulture);
        }

        [Fact]
        public void DefaultConfigurationOverload_AgreesWithConfigurationOverload()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry viaDefault = manager.GetValueWithSource("hello", new CultureInfo("fr"), out TranslationEntrySource sourceViaDefault);

            Assert.Equal("Salut (fr)", viaDefault.Text);
            Assert.Equal(TranslationEntrySource.Culture, sourceViaDefault);
        }

        [Fact]
        public void LangIDsOverload_ReportsSourceOfTheLanguageThatAnswered()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "welcome", ["es", "fr-CA"], out TranslationEntrySource source);

            Assert.Equal("Bienvenue (fr-FR)", entry.Text);
            Assert.Equal(TranslationEntrySource.BasicCulture, source);
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run: `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj --filter "FullyQualifiedName~TranslationEntrySourceTests"`
Expected: build error CS0103/CS1061 (`TranslationEntrySource` / `GetValueWithSource` not defined).

- [ ] **Step 3: Add the enum**

Create `src/Tlumach/TranslationEntrySource.cs` (header, then):

```csharp
namespace Tlumach;

/// <summary>
/// Tells where <see cref="TranslationManager.GetValueWithSource(Tlumach.Base.TranslationConfiguration, string, System.Globalization.CultureInfo, out TranslationEntrySource)"/> found the returned entry.
/// </summary>
public enum TranslationEntrySource
{
    /// <summary>
    /// No translation contains the key. The returned entry is empty unless a handler of <see cref="TranslationManager.OnTranslationValueNotFound"/> supplied a value.
    /// </summary>
    NotFound = 0,

    /// <summary>
    /// The translation of a requested culture contains the key, or a handler of <see cref="TranslationManager.OnTranslationValueNeeded"/> supplied the value.
    /// </summary>
    Culture = 1,

    /// <summary>
    /// The translation of the basic culture of a requested culture contains the key, for example the translation for "de-DE" when "de-AT" was requested.
    /// </summary>
    BasicCulture = 2,

    /// <summary>
    /// The value comes from the default translation. This is also the source when the requested culture is the locale of the default file.
    /// </summary>
    DefaultTranslation = 3,
}
```

Check how `src/Tlumach/CultureChangedEventArgs.cs` declares its namespace. If it uses a block-scoped `namespace Tlumach { ... }`, use the block form here too to match the project.

- [ ] **Step 4: Route the lookup through `GetValueCore` and add the public methods**

In `src/Tlumach/TranslationManager.cs`:

**(a) Rename the main method.** Rename `public TranslationEntry GetValue(TranslationConfiguration config, string key, CultureInfo? culture, string[]? langIDs, out bool foundForCulture)` (the one with `#pragma warning disable MA0051` above it) to:

```csharp
private TranslationEntry GetValueCore(TranslationConfiguration config, string key, CultureInfo? culture, string[]? langIDs, out TranslationEntrySource source)
```

Keep its XML doc. Change the `<param name="foundForCulture">` to `<param name="source">Upon return, tells where the entry was found.</param>`.

**(b) Change its body** as follows:
- `foundForCulture = false;` becomes `source = TranslationEntrySource.NotFound;`
- In the first pass, the `OnTranslationValueNeeded` branch becomes:

```csharp
                if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, singleCulture, key, originalAssembly: null, originalFile: null, config.DirectoryHint))
                {
                    source = TranslationEntrySource.Culture;
                    return args.Entry;
                }

                if (args.Text is not null || args.EscapedText is not null)
                {
                    source = TranslationEntrySource.Culture;
                    return EntryFromEventArgs(args, textProcessingMode);
                }
```

- In the first pass, the `foundForCulture = true;` before the culture-local `return FireTranslationValueFound(...)` becomes `source = TranslationEntrySource.Culture;`.
- In the second pass, the `foundForCulture = true;` before `return FireTranslationValueFound(singleCulture, ...)` becomes `source = TranslationEntrySource.BasicCulture;`.
- In the default stage, add `source = TranslationEntrySource.DefaultTranslation;` immediately before `return FireTranslationValueFound(reportCulture, key, result, defaultTranslation.OriginalAssembly, ...)`.
- The final `return FireTranslationValueNotFound(...)` keeps `source == NotFound`.

**(c) Re-add the public `out bool` overload** in the place of the renamed method, delegating to the core:

```csharp
    /// <summary>
    /// Retrieves the value based on the default configuration and culture.
    /// </summary>
    /// <param name="config">The configuration that specifies from where to load translations.</param>
    /// <param name="key">The key of the translation entry to retrieve.</param>
    /// <param name="culture">An optional culture, for which the entry is needed.</param>
    /// <param name="langIDs">An optional, ordered list of language IDs, for which the entry is needed. Exact matches for every requested language are tried first, in array order; only if none of them matches are the corresponding basic/parent cultures tried, again in array order; only then is the default translation used.</param>
    /// <param name="foundForCulture">Upon return, indicates if the requested entry was found for the specified culture (or one of the requested languages) or a base culture thereof. <see langword="false" /> will be returned if a value from the default translation was used.</param>
    /// <returns>The translation entry or an empty entry if nothing was found.</returns>
    public TranslationEntry GetValue(TranslationConfiguration config, string key, CultureInfo? culture, string[]? langIDs, out bool foundForCulture)
    {
        TranslationEntry result = GetValueCore(config, key, culture, langIDs, out TranslationEntrySource source);
        foundForCulture = source is TranslationEntrySource.Culture or TranslationEntrySource.BasicCulture;
        return result;
    }
```

**(d) Add the new public methods** directly after it:

```csharp
    /// <summary>
    /// Retrieves the value for the given culture and tells where it was found.
    /// <para>Use this method when the caller must distinguish the text of the requested culture from a fallback, for example to prefer another source of
    /// localized text over the default translation of Tlumach.</para>
    /// </summary>
    /// <param name="config">The configuration that specifies from where to load translations.</param>
    /// <param name="key">The key of the translation entry to retrieve.</param>
    /// <param name="culture">The culture, for which the entry is needed.</param>
    /// <param name="source">Upon return, tells where the entry was found.</param>
    /// <returns>The translation entry or an empty entry if nothing was found.</returns>
    public TranslationEntry GetValueWithSource(TranslationConfiguration config, string key, CultureInfo culture, out TranslationEntrySource source)
    {
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));

        return GetValueCore(config, key, culture, null, out source);
    }

    /// <summary>
    /// Retrieves the value for the given language IDs and tells where it was found.
    /// </summary>
    /// <param name="config">The configuration that specifies from where to load translations.</param>
    /// <param name="key">The key of the translation entry to retrieve.</param>
    /// <param name="langIDs">The ordered list of language IDs, for which the entry is needed.</param>
    /// <param name="source">Upon return, tells where the entry was found.</param>
    /// <returns>The translation entry or an empty entry if nothing was found.</returns>
    public TranslationEntry GetValueWithSource(TranslationConfiguration config, string key, string[] langIDs, out TranslationEntrySource source)
        => GetValueCore(config, key, null, langIDs, out source);

    /// <summary>
    /// Retrieves the value for the given culture from the default configuration and tells where it was found.
    /// </summary>
    /// <param name="key">The key of the translation entry to retrieve.</param>
    /// <param name="culture">The culture, for which the entry is needed.</param>
    /// <param name="source">Upon return, tells where the entry was found. <see cref="TranslationEntrySource.NotFound"/> when the manager has no default configuration.</param>
    /// <returns>The translation entry or an empty entry if nothing was found.</returns>
    public TranslationEntry GetValueWithSource(string key, CultureInfo culture, out TranslationEntrySource source)
    {
        if (_defaultConfig is null)
        {
            source = TranslationEntrySource.NotFound;
            return TranslationEntry.Empty;
        }

        return GetValueWithSource(_defaultConfig, key, culture, out source);
    }
```

Note on behaviour: an entry supplied through `args.Entry` of `OnTranslationValueNeeded` used to report `foundForCulture == false`. It now reports `true`, consistently with `args.Text`. This is deliberate and gets a CHANGELOG `[FIX]` entry in Task 10.

- [ ] **Step 5: Run the new tests and the whole core suite**

Run: `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release`
Expected: all tests pass, including `TranslationEntrySourceTests`, `TranslationManagerLangIDsTests` and `Item01_KeyNormalizationTests.GetValue_ForDefaultLocale_ReportsNotFoundForCulture`.

- [ ] **Step 6: Build the core solution with no new warnings**

Run: `dotnet build src/Tlumach.Main.sln 2>&1 | grep -E "warning|error" | grep -E "TranslationManager|TranslationEntrySource" | sort -u`
Expected: no output.

- [ ] **Step 7: Commit**

```bash
git add src/Tlumach/TranslationEntrySource.cs src/Tlumach/TranslationManager.cs tests/Tlumach.Tests/TranslationEntrySourceTests.cs
git commit -m "Report where TranslationManager found a value through GetValueWithSource"
```

---

### Task 2: Borrowed-entry tracking (cache fix)

**Files:**
- Modify: `src/Tlumach/TranslationManager.cs`, specifically `CacheEntry` (about line 788), `TryGetEntryFromCulture` (about line 802), and the second pass and default stage of `GetValueCore`.
- Test: `tests/Tlumach.Tests/TranslationManagerBorrowedEntryTests.cs`

**Interfaces:**
- Consumes: `GetValueWithSource` and `TranslationEntrySource` (Task 1), and `LangIDsFixtures`.
- Produces: no public API change. `foundForCulture` and `TranslationEntrySource` become accurate when `CacheDefaultTranslations` is `true`.

**The bug:**
- `CacheEntry` copies a fallback entry into the culture-local `Translation`.
- On the next lookup, the exact-match pass of `GetValueCore` finds the copy and reports `Culture`.
- In a `langIDs` lookup, the copy under an earlier language also hides a later language's own match.

- [ ] **Step 1: Write the failing regression tests**

Create `tests/Tlumach.Tests/TranslationManagerBorrowedEntryTests.cs` (header, then):

```csharp
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Regression tests of the cache of default translations (<see cref="TranslationManager.CacheDefaultTranslations"/>): an entry that a lookup copies
    /// into the translation of a culture from the basic culture or from the default translation must not be mistaken for the culture's own text later.
    /// </summary>
    [Trait("Category", "TranslationManager")]
    public class TranslationManagerBorrowedEntryTests
    {
        private static TranslationManager CreateManager(out TranslationConfiguration config)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            config = LangIDsFixtures.CreateConfiguration(dir);
            return LangIDsFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);
        }

        [Fact]
        public void CachedDefault_IsNotReportedAsFoundForCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            CultureInfo fr = new("fr");

            manager.GetValue(config, "onlyInDefault", fr, out bool firstFound);
            TranslationEntry second = manager.GetValue(config, "onlyInDefault", fr, out bool secondFound);

            Assert.False(firstFound);
            Assert.False(secondFound);
            Assert.Equal("OnlyInDefault", second.Text);
        }

        [Fact]
        public void CachedDefault_ReportsDefaultTranslationSource()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            CultureInfo fr = new("fr");

            manager.GetValueWithSource(config, "onlyInDefault", fr, out _);
            manager.GetValueWithSource(config, "onlyInDefault", fr, out TranslationEntrySource source);

            Assert.Equal(TranslationEntrySource.DefaultTranslation, source);
        }

        [Fact]
        public void CachedDefault_DoesNotHideLaterLanguagesBasicCultureMatch()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            // "es" has no file, so this caches the default "Welcome" into the translation of "es".
            manager.GetValue(config, "welcome", ["es"], out _);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["es", "fr-CA"], out bool found);

            Assert.Equal("Bienvenue (fr-FR)", entry.Text);
            Assert.True(found);
        }

        [Fact]
        public void CachedDefault_DoesNotHideLaterLanguagesExactMatch()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            manager.GetValue(config, "welcome", ["es"], out _);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["es", "fr"], out bool found);

            Assert.Equal("Bienvenue (fr)", entry.Text);
            Assert.True(found);
        }

        [Fact]
        public void CachedBasicCultureEntry_IsReportedAsBasicCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            CultureInfo deAt = new("de-AT");

            manager.GetValueWithSource(config, "welcome", deAt, out _);
            TranslationEntry second = manager.GetValueWithSource(config, "welcome", deAt, out TranslationEntrySource source);

            Assert.Equal("Willkommen (de-DE)", second.Text);
            Assert.Equal(TranslationEntrySource.BasicCulture, source);
        }

        [Fact]
        public void CachedEntries_StayInTheTranslation()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            manager.GetValue(config, "onlyInDefault", new CultureInfo("fr"), out _);

            Translation? fr = manager.GetTranslation(new CultureInfo("fr"));
            Assert.NotNull(fr);
            Assert.True(fr.ContainsKey("onlyInDefault"));
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm the regression tests fail**

Run: `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj --filter "FullyQualifiedName~TranslationManagerBorrowedEntryTests"`
Expected:
- `CachedDefault_IsNotReportedAsFoundForCulture`, `CachedDefault_ReportsDefaultTranslationSource`, `CachedDefault_DoesNotHideLaterLanguagesBasicCultureMatch` (gets "Welcome"), `CachedDefault_DoesNotHideLaterLanguagesExactMatch` (gets "Welcome") and `CachedBasicCultureEntry_IsReportedAsBasicCulture` (gets `Culture`) FAIL.
- `CachedEntries_StayInTheTranslation` passes.

If one of the five does not fail, stop and investigate before changing code (superpowers:systematic-debugging).

- [ ] **Step 3: Track borrowed entries**

In `src/Tlumach/TranslationManager.cs`, add `using System.Runtime.CompilerServices;` to the usings. Then add this field next to the other private fields:

```csharp
    /// <summary>
    /// The origin of every entry that <see cref="CacheEntry"/> copied into a translation from another translation, per translation.
    /// <para>The copy stays in the translation, so <see cref="GetTranslation(CultureInfo)"/> keeps showing it, but a lookup must know that it is not the text of that culture:
    /// otherwise it reports the wrong source, and in a lookup for several languages it hides a match of a later language. A weak table is used so that a translation that is
    /// replaced by a reload takes its record with it. A record is read and written only while the lock of its translation is held.</para>
    /// </summary>
    private readonly ConditionalWeakTable<Translation, Dictionary<string, TranslationEntrySource>> _borrowedEntries = new();
```

Replace the static `CacheEntry` method with:

```csharp
    /// <summary>
    /// Copies a resolved entry into a translation so that later lookups find it directly, and records where it came from.
    /// </summary>
    /// <param name="translation">The translation to write into. Ignored when <see langword="null"/>.</param>
    /// <param name="key">The key to store the entry under.</param>
    /// <param name="entry">The entry to store.</param>
    /// <param name="origin">Where the entry was found: <see cref="TranslationEntrySource.BasicCulture"/> or <see cref="TranslationEntrySource.DefaultTranslation"/>.</param>
    private void CacheEntry(Translation? translation, string key, TranslationEntry entry, TranslationEntrySource origin)
    {
        if (translation is null)
            return;

#pragma warning disable CA2002 // Do not lock on objects with weak identity
        lock (translation)
        {
            if (!translation.ContainsKey(key))
            {
                translation.Add(key, entry);

                // Keys of a translation are compared case-insensitively, so the record must be, too.
                _borrowedEntries.GetValue(translation, static _ => new Dictionary<string, TranslationEntrySource>(StringComparer.OrdinalIgnoreCase))[key] = origin;
            }
        }
#pragma warning restore CA2002 // Do not lock on objects with weak identity
    }

    /// <summary>
    /// Tells whether the entry with the given key was copied into the translation from another translation. Must be called while the lock of the translation is held.
    /// </summary>
    private bool IsBorrowed(Translation translation, string key)
        => _borrowedEntries.TryGetValue(translation, out Dictionary<string, TranslationEntrySource>? borrowed) && borrowed.ContainsKey(key);

    /// <summary>
    /// Returns the origin of an entry that was copied into the translation from another translation, together with the entry.
    /// </summary>
    /// <param name="translation">The translation to look into. May be <see langword="null"/>.</param>
    /// <param name="key">The key of the entry.</param>
    /// <param name="entry">Upon return, the copied entry, or <see langword="null"/>.</param>
    /// <returns>The origin, or <see langword="null"/> when the translation holds no copied entry for the key.</returns>
    private TranslationEntrySource? GetBorrowedEntry(Translation? translation, string key, out TranslationEntry? entry)
    {
        entry = null;
        if (translation is null)
            return null;

#pragma warning disable CA2002 // Do not lock on objects with weak identity
        lock (translation)
        {
            if (_borrowedEntries.TryGetValue(translation, out Dictionary<string, TranslationEntrySource>? borrowed)
                && borrowed.TryGetValue(key, out TranslationEntrySource origin)
                && translation.TryGetValue(key, out entry))
            {
                return origin;
            }
        }
#pragma warning restore CA2002 // Do not lock on objects with weak identity

        entry = null;
        return null;
    }
```

- [ ] **Step 4: Ignore borrowed entries in `TryGetEntryFromCulture`**

Replace the locked probe in `TryGetEntryFromCulture`:

```csharp
#pragma warning disable CA2002 // Do not lock on objects with weak identity
        lock (translation)
        {
            translation.TryGetValue(key, out result);
        }
#pragma warning restore CA2002 // Do not lock on objects with weak identity
```

with:

```csharp
#pragma warning disable CA2002 // Do not lock on objects with weak identity
        lock (translation)
        {
            // An entry that the cache copied here from another translation is not the own text of this culture. The second pass of GetValueCore reuses
            // a copy from the basic culture deliberately; everything else must reach its real source.
            if (translation.TryGetValue(key, out result) && IsBorrowed(translation, key))
                result = null;
        }
#pragma warning restore CA2002 // Do not lock on objects with weak identity
```

- [ ] **Step 5: Use the borrowed record in the second pass and pass origins to `CacheEntry`**

In `GetValueCore`, at the top of the second-pass loop body, directly after:

```csharp
            Translation? cultureLocalTranslation = cultureLocalTranslations[idx];
            Translation? translation = null;
```

insert:

```csharp
            // A copy from the basic culture is that basic culture's text, so it is returned without looking at the basic culture again. A copy from the default
            // translation shows that the basic culture has no text for the key (translations do not change after they are loaded), so the basic culture is skipped;
            // the default translation itself is consulted after every requested culture.
            TranslationEntrySource? borrowedFrom = GetBorrowedEntry(cultureLocalTranslation, key, out TranslationEntry? borrowed);
            if (borrowedFrom == TranslationEntrySource.BasicCulture)
            {
                source = TranslationEntrySource.BasicCulture;
                return FireTranslationValueFound(singleCulture, key, borrowed!, cultureLocalTranslation!.OriginalAssembly, cultureLocalTranslation.OriginalFile, textProcessingMode);
            }

            if (borrowedFrom == TranslationEntrySource.DefaultTranslation)
                continue;
```

Then change the three `CacheEntry` calls:
- In the second pass: `CacheEntry(cultureLocalTranslation, key, result);` becomes `CacheEntry(cultureLocalTranslation, key, result, TranslationEntrySource.BasicCulture);`
- In the default stage, the two calls become:

```csharp
                        CacheEntry(cultureLocalTranslations[idx], key, result, TranslationEntrySource.DefaultTranslation);
                        CacheEntry(basicCultureLocalTranslations[idx], key, result, TranslationEntrySource.DefaultTranslation);
```

- [ ] **Step 6: Run the core suite**

Run: `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release`
Expected: all pass, including `TranslationManagerBorrowedEntryTests`, `TranslationEntrySourceTests` and the existing `TranslationManagerLangIDsTests` backfill tests (`GetValue_BasicCultureHit_BackfillsOnlyItsOwnRequestedLanguage`, `GetValue_DefaultFallback_BackfillsEveryRequestedLanguage`, `GetValue_DoesNotCorruptAnotherRequestedLanguagesCache`).

- [ ] **Step 7: Run the other suites that depend on the lookup**

Run: `dotnet test tests/Tlumach.GeneratorTests/Tlumach.GeneratorTests.csproj -c Release` and then `dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj -c Release`
Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add src/Tlumach/TranslationManager.cs tests/Tlumach.Tests/TranslationManagerBorrowedEntryTests.cs
git commit -m "Do not treat entries cached from a fallback as the text of a culture"
```

---

### Task 3: The Tlumach.FluentValidation project, options and test project

**Files:**
- Create: `src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj`, `MessageCultureSource.cs`, `TlumachLanguageManagerOptions.cs` and `CultureMatching.cs`.
- Create the test project files: `tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj`, `TestAssemblyInfo.cs`, `TestTranslations.cs`, `ValidatorOptionsScope.cs` and `Customer.cs`.
- Create the test data: `TestData/Json/Strings.jsoncfg`, `TestData/Json/Strings.json`, `TestData/Json/Strings_de.json`, `TestData/Arb/Strings.arbcfg` and `TestData/Arb/Strings.arb`.
- Create the tests: `CultureMatchingTests.cs` and `TestDataTests.cs`.
- Modify: `src/Tlumach.Main.sln`, `src/Tlumach.sln`, `tests/Tlumach.Tests.sln` and `.github/workflows/build-test.yml`.

**Interfaces:**
- Produces:
  - `public enum MessageCultureSource { CurrentUICulture = 0, TranslationManager = 1 }`
  - `public class TlumachLanguageManagerOptions { public string? FluentValidationGroup { get; set; } = "FluentValidation"; public MessageCultureSource CultureSource { get; set; } = MessageCultureSource.CurrentUICulture; }`
  - `internal static class CultureMatching` with `string NeutralName(CultureInfo)`, `bool IsEnglish(CultureInfo)`, `CultureInfo? DefaultFileCulture(TranslationManager)` and `bool DefaultFileSharesLanguage(TranslationManager, CultureInfo)`.
  - Test helpers:
    - `TestTranslations.CreateJsonManager()`, `TestTranslations.CreateArbManager()` and `TestTranslations.Unit(TranslationManager, string key)`.
    - `ValidatorOptionsScope : IDisposable`.
    - `Customer { string? Name; string? Email; string? Nickname; decimal Discount; }`.

- [ ] **Step 1: Create the library project**

`src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj`:

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
        <AdditionalFiles Include="../Shared/stylecop.json" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="FluentValidation" Version="[12.0.0, 13.0.0)" />
        <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.*" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Tlumach.Base\Tlumach.Base.csproj" />
        <ProjectReference Include="..\Tlumach\Tlumach.csproj" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="Tlumach.FluentValidationTests" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Add the enum and the options**

`src/Tlumach.FluentValidation/MessageCultureSource.cs` (header, then):

```csharp
using System.Globalization;

namespace Tlumach.FluentValidation;

/// <summary>
/// Selects the culture, for which a validation message is read when neither the caller nor <c>LanguageManager.Culture</c> specifies one.
/// </summary>
public enum MessageCultureSource
{
    /// <summary>
    /// Use <see cref="CultureInfo.CurrentUICulture"/>, which is what the built-in language manager of FluentValidation uses and what the request localization of ASP.NET Core and
    /// <c>TlumachCultureState</c> of Blazor set.
    /// </summary>
    CurrentUICulture = 0,

    /// <summary>
    /// Use <see cref="Tlumach.TranslationManager.CurrentCulture"/> of the translation manager, which a desktop application switches to change the language. It returns
    /// <see cref="CultureInfo.CurrentCulture"/> when <see cref="Tlumach.TranslationManager.UseContextCulture"/> is set.
    /// </summary>
    TranslationManager = 1,
}
```

`src/Tlumach.FluentValidation/TlumachLanguageManagerOptions.cs` (header, then):

```csharp
namespace Tlumach.FluentValidation;

/// <summary>
/// The options of <see cref="TlumachLanguageManager"/>. The language manager copies the values when it is created.
/// </summary>
public class TlumachLanguageManagerOptions
{
    /// <summary>
    /// Gets or sets the group of the translation, in which the keys of FluentValidation are stored, for example <c>NotEmptyValidator</c> and the error codes set with
    /// <c>WithErrorCode</c>. A key is looked up as <c>{FluentValidationGroup}.{key}</c>, or as <c>{key}</c> when this property is <see langword="null"/> or empty.
    /// <para>The default value is <c>"FluentValidation"</c>. In a JSON translation file, that is an object named <c>FluentValidation</c> at the root, and Generator creates the
    /// nested class <c>Strings.FluentValidation</c> for it.</para>
    /// </summary>
    public string? FluentValidationGroup { get; set; } = "FluentValidation";

    /// <summary>
    /// Gets or sets the culture source used when neither the caller nor <c>LanguageManager.Culture</c> specifies a culture.
    /// The default value is <see cref="MessageCultureSource.CurrentUICulture"/>.
    /// </summary>
    public MessageCultureSource CultureSource { get; set; } = MessageCultureSource.CurrentUICulture;
}
```

- [ ] **Step 3: Add `CultureMatching`**

`src/Tlumach.FluentValidation/CultureMatching.cs` (header, then):

```csharp
using System.Globalization;

namespace Tlumach.FluentValidation;

/// <summary>
/// Compares cultures by their language for the fallback rules of <see cref="TlumachLanguageManager"/>.
/// </summary>
internal static class CultureMatching
{
    /// <summary>
    /// Returns the name of the topmost neutral culture of the given culture: "en" for "en-GB", "zh-Hant" for "zh-Hant-TW", and an empty string for the invariant culture.
    /// </summary>
    public static string NeutralName(CultureInfo culture)
    {
        CultureInfo current = culture;
        while (!current.IsNeutralCulture && current.Parent.Name.Length != 0)
            current = current.Parent;

        return current.Name;
    }

    /// <summary>
    /// Tells whether the culture is English or a culture of English, for which the built-in messages of FluentValidation are the English ones.
    /// </summary>
    public static bool IsEnglish(CultureInfo culture)
        => NeutralName(culture).Equals("en", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the culture of the default file of the translation manager, or <see langword="null"/> when the locale of the default file is unknown or not a valid culture name.
    /// </summary>
    public static CultureInfo? DefaultFileCulture(TranslationManager manager)
    {
        string? locale = manager.DefaultConfiguration?.DefaultFileLocale;
        if (string.IsNullOrEmpty(locale))
            return null;

        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Tells whether the default file of the translation manager is written in the language of the given culture, so that its text is the text of that culture.
    /// </summary>
    public static bool DefaultFileSharesLanguage(TranslationManager manager, CultureInfo culture)
    {
        CultureInfo? defaultCulture = DefaultFileCulture(manager);
        if (defaultCulture is null)
            return false;

        string language = NeutralName(culture);
        return language.Length != 0 && language.Equals(NeutralName(defaultCulture), StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 4: Create the test project**

`tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="TestData\**" />
    <EmbeddedResource Include="TestData\Json\Strings.jsoncfg" />
    <EmbeddedResource Include="TestData\Json\Strings.json" />
    <EmbeddedResource Include="TestData\Json\Strings_de.json" />
    <EmbeddedResource Include="TestData\Arb\Strings.arbcfg" />
    <EmbeddedResource Include="TestData\Arb\Strings.arb" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
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
    <ProjectReference Include="..\..\src\Tlumach.FluentValidation\Tlumach.FluentValidation.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

`TestAssemblyInfo.cs` (header, then):

```csharp
// The tests change ValidatorOptions.Global, CultureInfo.CurrentUICulture and the parsers registered with Tlumach, which are process-wide, so the test
// classes must not run in parallel. The concurrency tests start their parallel work inside one test.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

`ValidatorOptionsScope.cs` (header, then):

```csharp
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using FluentValidation;
using FluentValidation.Resources;

namespace Tlumach.FluentValidationTests;

/// <summary>
/// Restores the global options of FluentValidation and the cultures of the current thread that a test changes.
/// </summary>
internal sealed class ValidatorOptionsScope : IDisposable
{
    private readonly ILanguageManager _languageManager = ValidatorOptions.Global.LanguageManager;
    private readonly Func<Type, MemberInfo, LambdaExpression, string> _displayNameResolver = ValidatorOptions.Global.DisplayNameResolver;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public void Dispose()
    {
        ValidatorOptions.Global.LanguageManager = _languageManager;
        ValidatorOptions.Global.DisplayNameResolver = _displayNameResolver;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
```

`Customer.cs` (header, then):

```csharp
namespace Tlumach.FluentValidationTests;

public sealed class Customer
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Nickname { get; set; }

    public decimal Discount { get; set; }
}
```

`TestTranslations.cs` (header, then):

```csharp
using Tlumach.Base;

namespace Tlumach.FluentValidationTests;

/// <summary>
/// Creates translation managers over the embedded test data.
/// <list type="bullet">
/// <item>The JSON data (DotNet text processing mode) has an English default file and a German file.</item>
/// <item>The ARB data (ICU text processing mode) has an English default file only, with keys at the root.</item>
/// </list>
/// </summary>
internal static class TestTranslations
{
    public static TranslationManager CreateJsonManager()
    {
        JsonParser.Use();
        return new TranslationManager(typeof(TestTranslations).Assembly, "TestData/Json/Strings.jsoncfg") { LoadFromDisk = false };
    }

    public static TranslationManager CreateArbManager()
    {
        ArbParser.Use();
        return new TranslationManager(typeof(TestTranslations).Assembly, "TestData/Arb/Strings.arbcfg") { LoadFromDisk = false };
    }

    /// <summary>
    /// Creates the unit that Generator would create for the key, for example <c>Strings.Messages.NameTooLong</c> for <c>Messages.NameTooLong</c>.
    /// </summary>
    public static TranslationUnit Unit(TranslationManager manager, string key)
        => new(manager, manager.DefaultConfiguration!, key, containsPlaceholders: true);
}
```

- [ ] **Step 5: Add the test data**

The FluentValidation built-in texts referred to in the comments are verified against FluentValidation 12.1.1. Tests compare with FluentValidation's own `LanguageManager` rather than with literal built-in texts.

`TestData/Json/Strings.jsoncfg`:

```json
{
    "defaultFile": "Strings.json",
    "defaultFileLocale": "en",
    "translations": {
        "de": "Strings_de.json"
    }
}
```

`TestData/Json/Strings.json`:

```json
{
    "FluentValidation": {
        "NotEmptyValidator": "'{PropertyName}' is required.",
        "OnlyInDefault": "Only in the default file.",
        "CustomerNameCode": "The customer name '{PropertyValue}' is not allowed."
    },
    "DisplayNames": {
        "Customer": {
            "Name": "Customer name"
        },
        "Email": "E-mail address"
    },
    "Messages": {
        "NameTooLong": "'{PropertyName}' must be at most {MaxLength} characters long. You entered {TotalLength}.",
        "DiscountLimit": "'{PropertyName}' must not exceed {Limit:N0}. You entered {PropertyValue}.",
        "Nickname": "Nickname"
    },
    "NotEmptyValidator": "'{PropertyName}' is required (root)."
}
```

`TestData/Json/Strings_de.json`:

```json
{
    "FluentValidation": {
        "NotEmptyValidator": "'{PropertyName}' wird benötigt.",
        "CustomerNameCode": "Der Kundenname '{PropertyValue}' ist nicht erlaubt."
    },
    "DisplayNames": {
        "Customer": {
            "Name": "Kundenname"
        },
        "Email": "E-Mail-Adresse"
    },
    "Messages": {
        "NameTooLong": "'{PropertyName}' darf höchstens {MaxLength} Zeichen lang sein. Sie haben {TotalLength} eingegeben.",
        "DiscountLimit": "'{PropertyName}' darf {Limit:N0} nicht überschreiten. Sie haben {PropertyValue} eingegeben.",
        "Nickname": "Spitzname"
    },
    "NotEmptyValidator": "'{PropertyName}' wird benötigt (Wurzel)."
}
```

What the data deliberately leaves out of the German file:
- `FluentValidation.EmailValidator`, so FluentValidation's German built-in text is used.
- `FluentValidation.OnlyInDefault`, a key that FluentValidation does not know, so Tlumach's default text is used.

`TestData/Arb/Strings.arbcfg`:

```json
{
    "defaultFile": "Strings.arb",
    "textProcessingMode": "Arb"
}
```

`TestData/Arb/Strings.arb`:

```json
{
    "@@locale": "en",
    "NotEmptyValidator": "'{PropertyName}' must not be blank (ICU).",
    "Greeting": "Hello, '{PropertyName}'!"
}
```

- [ ] **Step 6: Write the scaffolding tests**

`tests/Tlumach.FluentValidationTests/TestDataTests.cs` (header, then):

```csharp
using System.Globalization;

namespace Tlumach.FluentValidationTests;

/// <summary>
/// Verifies that the embedded test data loads, so that a failure of a later test is not caused by the data.
/// </summary>
public class TestDataTests
{
    [Fact]
    public void JsonData_LoadsGroupedKeysForDefaultAndGerman()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Equal("'{PropertyName}' is required.", manager.GetValue("FluentValidation.NotEmptyValidator", CultureInfo.GetCultureInfo("en")).Text);
        Assert.Equal("'{PropertyName}' wird benötigt.", manager.GetValue("FluentValidation.NotEmptyValidator", CultureInfo.GetCultureInfo("de")).Text);
        Assert.Equal("Kundenname", manager.GetValue("DisplayNames.Customer.Name", CultureInfo.GetCultureInfo("de")).Text);
        Assert.Equal("en", manager.DefaultConfiguration!.DefaultFileLocale);
    }

    [Fact]
    public void ArbData_KeepsApostrophesInText()
    {
        using TranslationManager manager = TestTranslations.CreateArbManager();

        Assert.Equal("'{PropertyName}' must not be blank (ICU).", manager.GetValue("NotEmptyValidator", CultureInfo.GetCultureInfo("en")).Text);
    }
}
```

`tests/Tlumach.FluentValidationTests/CultureMatchingTests.cs` (header, then):

```csharp
using System.Globalization;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class CultureMatchingTests
{
    [Theory]
    [InlineData("en-GB", "en")]
    [InlineData("en", "en")]
    [InlineData("de-AT", "de")]
    [InlineData("zh-Hant-TW", "zh-Hant")]
    [InlineData("", "")]
    public void NeutralName_ReturnsTopmostNeutralCulture(string cultureName, string expected)
        => Assert.Equal(expected, CultureMatching.NeutralName(CultureInfo.GetCultureInfo(cultureName)));

    [Theory]
    [InlineData("en", true)]
    [InlineData("en-GB", true)]
    [InlineData("de", false)]
    [InlineData("", false)]
    public void IsEnglish_RecognizesEnglishCultures(string cultureName, bool expected)
        => Assert.Equal(expected, CultureMatching.IsEnglish(CultureInfo.GetCultureInfo(cultureName)));

    [Theory]
    [InlineData("en-GB", true)]
    [InlineData("en", true)]
    [InlineData("de", false)]
    public void DefaultFileSharesLanguage_ComparesWithDefaultFileLocale(string cultureName, bool expected)
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Equal(expected, CultureMatching.DefaultFileSharesLanguage(manager, CultureInfo.GetCultureInfo(cultureName)));
    }
}
```

- [ ] **Step 7: Add the projects to the solutions**

Run:

```bash
dotnet sln src/Tlumach.Main.sln add src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj
dotnet sln src/Tlumach.sln add src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj
dotnet sln tests/Tlumach.Tests.sln add src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj
```

If `dotnet sln` places the projects in a solution folder that the other projects do not use, move them to match the structure of the file. Make sure the `.sln` files keep CRLF line endings.

- [ ] **Step 8: Add the CI step**

In `.github/workflows/build-test.yml`, after the "Test the Blazor integration" step, add:

```yaml
      - name: Test the FluentValidation integration
        run: dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj -c Release
```

- [ ] **Step 9: Run the scaffolding tests**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj`
Expected: all of `TestDataTests` and `CultureMatchingTests` pass.

If `TestDataTests` fails because the resources are not found, print `typeof(TestTranslations).Assembly.GetManifestResourceNames()` in a scratch test. The names must be `Tlumach.FluentValidationTests.TestData.Json.Strings.json`, and so on.

If `""` fails in `CultureMatchingTests` (the invariant culture), check `CultureInfo.GetCultureInfo("")`; it must return the invariant culture.

- [ ] **Step 10: Build `Tlumach.Main.sln` with no new warnings**

Run: `dotnet build src/Tlumach.Main.sln 2>&1 | grep -E "warning|error" | grep "Tlumach.FluentValidation" | sort -u`
Expected: no output.

- [ ] **Step 11: Commit**

```bash
git add src/Tlumach.FluentValidation tests/Tlumach.FluentValidationTests src/Tlumach.Main.sln src/Tlumach.sln tests/Tlumach.Tests.sln .github/workflows/build-test.yml
git commit -m "Add the Tlumach.FluentValidation project with its options and test project"
```

---

### Task 4: `TlumachLanguageManager`

**Files:**
- Create: `src/Tlumach.FluentValidation/TlumachLanguageManager.cs`
- Test: `tests/Tlumach.FluentValidationTests/TlumachLanguageManagerTests.cs`

**Interfaces:**
- Consumes:
  - `TranslationManager.GetValueWithSource(string, CultureInfo, out TranslationEntrySource)` (Task 1).
  - `CultureMatching` and `TlumachLanguageManagerOptions` (Task 3).
  - FluentValidation's `LanguageManager`, whose only virtual member is `public virtual string GetString(string key, CultureInfo culture = null)`, plus `bool Enabled` and `CultureInfo Culture`.
- Produces:
  - `public class TlumachLanguageManager : LanguageManager`
  - `public TlumachLanguageManager(TranslationManager translationManager, TlumachLanguageManagerOptions? options = null)`
  - `public TranslationManager TranslationManager { get; }`
  - `public string? FluentValidationGroup { get; }`
  - `public MessageCultureSource CultureSource { get; }`
  - `public CultureInfo ResolveCulture(CultureInfo? culture = null)`
  - `public override string GetString(string key, CultureInfo? culture = null)`

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.FluentValidationTests/TlumachLanguageManagerTests.cs` (header, then):

```csharp
using System.Globalization;

using FluentValidation;
using FluentValidation.Resources;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class TlumachLanguageManagerTests
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo EnGb = CultureInfo.GetCultureInfo("en-GB");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");
    private static readonly CultureInfo DeAt = CultureInfo.GetCultureInfo("de-AT");
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk");
    private static readonly CultureInfo GaIe = CultureInfo.GetCultureInfo("ga-IE"); // a culture that FluentValidation 12 has no translation for

    private static readonly LanguageManager Stock = new();

    [Fact]
    public void Step1_TlumachTextForCulture_Wins()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator", De));
        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator", DeAt));
    }

    [Fact]
    public void Step1_DefaultFileInSameLanguage_CountsAsCultureText()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", En));
        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", EnGb));
    }

    [Fact]
    public void Step2_BuiltInTextForCulture_WinsOverTlumachDefault()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        // German has no EmailValidator in Tlumach, FluentValidation has a German text.
        Assert.Equal(Stock.GetString("EmailValidator", De), languageManager.GetString("EmailValidator", De));

        // Ukrainian has no Tlumach file at all; FluentValidation's Ukrainian beats Tlumach's English default text.
        Assert.Equal(Stock.GetString("NotEmptyValidator", Uk), languageManager.GetString("NotEmptyValidator", Uk));
    }

    [Fact]
    public void Step2_EnglishCulture_UsesBuiltInEnglishForKeysMissingInTlumach()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal(Stock.GetString("LengthValidator", EnGb), languageManager.GetString("LengthValidator", EnGb));
    }

    [Fact]
    public void Step3_TlumachDefault_WhenNeitherHasTheCulture()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", GaIe));

        // A key that FluentValidation does not know: German falls back to the default text of Tlumach.
        Assert.Equal("Only in the default file.", languageManager.GetString("OnlyInDefault", De));
    }

    [Fact]
    public void Step4_BuiltInEnglish_WhenTlumachHasNothing()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal(Stock.GetString("LengthValidator", En), languageManager.GetString("LengthValidator", GaIe));
    }

    [Fact]
    public void UnknownKey_ReturnsEmptyString()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        Assert.Equal(string.Empty, languageManager.GetString("NoSuchValidator", De));
    }

    [Fact]
    public void NullGroup_LooksUpRootKeys()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { FluentValidationGroup = null });

        Assert.Equal("'{PropertyName}' wird benötigt (Wurzel).", languageManager.GetString("NotEmptyValidator", De));
    }

    [Fact]
    public void EmptyGroup_LooksUpRootKeys()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { FluentValidationGroup = string.Empty });

        Assert.Equal("'{PropertyName}' is required (root).", languageManager.GetString("NotEmptyValidator", En));
    }

    [Fact]
    public void CultureProperty_OverridesCultureSource()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager) { Culture = De };
        CultureInfo.CurrentUICulture = En;

        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator"));
    }

    [Fact]
    public void ExplicitCulture_OverridesCultureProperty()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager) { Culture = De };

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", En));
    }

    [Fact]
    public void CurrentUICultureSource_FollowsCurrentUICulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager);

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator"));

        CultureInfo.CurrentUICulture = En;
        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator"));
    }

    [Fact]
    public void TranslationManagerSource_FollowsManagerCulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { CultureSource = MessageCultureSource.TranslationManager });
        CultureInfo.CurrentUICulture = En;

        manager.CurrentCulture = De;

        Assert.Equal("'{PropertyName}' wird benötigt.", languageManager.GetString("NotEmptyValidator"));
    }

    [Fact]
    public void Disabled_UsesTlumachDefaultThenBuiltInEnglish()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var languageManager = new TlumachLanguageManager(manager) { Enabled = false };

        Assert.Equal("'{PropertyName}' is required.", languageManager.GetString("NotEmptyValidator", De));
        Assert.Equal(Stock.GetString("EmailValidator", En), languageManager.GetString("EmailValidator", De));
    }

    [Fact]
    public void Constructor_RejectsNullManagerAndUndefinedCultureSource()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Throws<ArgumentNullException>(() => new TlumachLanguageManager(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { CultureSource = (MessageCultureSource)42 }));
    }

    [Fact]
    public void Validation_FormatsTlumachTemplateWithFluentValidationPlaceholders()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();

        Assert.Equal("'Name' wird benötigt.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Validation_IcuFileKeepsApostrophesAndFluentValidationFillsPlaceholders()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateArbManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager, new TlumachLanguageManagerOptions { FluentValidationGroup = null });
        CultureInfo.CurrentUICulture = En;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();

        Assert.Equal("'Name' must not be blank (ICU).", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Validation_ErrorCodeIsLookedUpInTheGroup()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEqual("Bob").WithErrorCode("CustomerNameCode");

        Assert.Equal("Der Kundenname 'Bob' ist nicht erlaubt.", Assert.Single(validator.Validate(new Customer { Name = "Bob" }).Errors).ErrorMessage);
    }

    [Fact]
    public void Validation_UnknownErrorCodeFallsBackToValidatorName()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithErrorCode("NoSuchCode");

        Assert.Equal("'Name' wird benötigt.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj --filter "FullyQualifiedName~TlumachLanguageManagerTests"`
Expected: CS0246 `TlumachLanguageManager` not found.

- [ ] **Step 3: Implement**

`src/Tlumach.FluentValidation/TlumachLanguageManager.cs` (header, then):

```csharp
using System.Globalization;

using FluentValidation.Resources;

using Tlumach.Base;

namespace Tlumach.FluentValidation;

/// <summary>
/// The language manager of FluentValidation that takes the messages from Tlumach translations and falls back to the messages built into FluentValidation.
/// <para>Assign an instance to <c>ValidatorOptions.Global.LanguageManager</c>, or call <c>AddTlumachFluentValidation</c>. The setting is process-wide, as FluentValidation
/// offers no other way to supply a language manager.</para>
/// </summary>
/// <remarks>
/// <para>A message is chosen in this order:</para>
/// <list type="number">
/// <item>The Tlumach text for the culture: text found for the culture or its basic culture, or the text of the default file when that file is written in the language of the culture.</item>
/// <item>The message built into FluentValidation for the culture.</item>
/// <item>The text of the default file of Tlumach.</item>
/// <item>The English message of FluentValidation.</item>
/// </list>
/// <para>The text is returned as it appears in the translation. The placeholder engine of Tlumach is not involved, so the placeholders of FluentValidation, such as
/// <c>{PropertyName}</c>, reach the message formatter of FluentValidation intact.</para>
/// <para>The class holds no mutable state of its own apart from the inherited <c>Culture</c> and <c>Enabled</c> properties, and it can be used from many threads at once.</para>
/// </remarks>
public class TlumachLanguageManager : LanguageManager
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachLanguageManager"/> class.
    /// </summary>
    /// <param name="translationManager">The translation manager that provides the messages, typically <c>Strings.TranslationManager</c> of a class created by Generator.</param>
    /// <param name="options">The options. The values are copied, so a later change of the options object has no effect. When <see langword="null"/>, the defaults apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="translationManager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="TlumachLanguageManagerOptions.CultureSource"/> is not a defined value.</exception>
    public TlumachLanguageManager(TranslationManager translationManager, TlumachLanguageManagerOptions? options = null)
    {
        TranslationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));

        options ??= new TlumachLanguageManagerOptions();

        if (options.CultureSource is not (MessageCultureSource.CurrentUICulture or MessageCultureSource.TranslationManager))
            throw new ArgumentOutOfRangeException(nameof(options), options.CultureSource, "The culture source must be either CurrentUICulture or TranslationManager.");

        FluentValidationGroup = options.FluentValidationGroup;
        CultureSource = options.CultureSource;
    }

    /// <summary>
    /// Gets the translation manager that provides the messages.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the group, in which the keys of FluentValidation are looked up. See <see cref="TlumachLanguageManagerOptions.FluentValidationGroup"/>.
    /// </summary>
    public string? FluentValidationGroup { get; }

    /// <summary>
    /// Gets the culture source used when neither the caller nor <c>Culture</c> specifies a culture.
    /// </summary>
    public MessageCultureSource CultureSource { get; }

    /// <summary>
    /// Returns the culture, for which a message is read: <paramref name="culture"/> when it is given, otherwise <c>Culture</c> when it is set, otherwise the culture that
    /// <see cref="CultureSource"/> selects.
    /// </summary>
    /// <param name="culture">An optional explicit culture.</param>
    /// <returns>The effective culture.</returns>
    public CultureInfo ResolveCulture(CultureInfo? culture = null)
    {
        if (culture is not null)
            return culture;

        if (Culture is not null)
            return Culture;

        return CultureSource == MessageCultureSource.TranslationManager ? TranslationManager.CurrentCulture : CultureInfo.CurrentUICulture;
    }

    /// <summary>
    /// Returns the message template for the key, chosen as described in the remarks of the class.
    /// </summary>
    /// <param name="key">The key: the name of a validator, such as <c>NotEmptyValidator</c>, or an error code set with <c>WithErrorCode</c>.</param>
    /// <param name="culture">An optional culture. See <see cref="ResolveCulture(CultureInfo?)"/>.</param>
    /// <returns>The template, or an empty string when neither Tlumach nor FluentValidation knows the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <see langword="null"/>.</exception>
    public override string GetString(string key, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        string lookupKey = string.IsNullOrEmpty(FluentValidationGroup) ? key : FluentValidationGroup + "." + key;

        if (!Enabled)
            return GetStringWhenDisabled(key, lookupKey);

        CultureInfo effectiveCulture = ResolveCulture(culture);

        TranslationEntry entry = TranslationManager.GetValueWithSource(lookupKey, effectiveCulture, out TranslationEntrySource source);
        string? tlumachText = entry.Text;
        bool hasTlumachText = !string.IsNullOrEmpty(tlumachText);

        // 1. The Tlumach text for the culture.
        if (hasTlumachText
            && (source is TranslationEntrySource.Culture or TranslationEntrySource.BasicCulture
                || (source == TranslationEntrySource.DefaultTranslation && CultureMatching.DefaultFileSharesLanguage(TranslationManager, effectiveCulture))))
        {
            return tlumachText!;
        }

        // 2. The built-in message for the culture. FluentValidation does not tell whether it has a translation for a culture, and it falls back to English silently, so a
        //    result that equals the English message is treated as missing unless the culture itself is English.
        string builtIn = base.GetString(key, effectiveCulture);
        if (builtIn.Length != 0
            && (CultureMatching.IsEnglish(effectiveCulture) || !string.Equals(builtIn, base.GetString(key, English), StringComparison.Ordinal)))
        {
            return builtIn;
        }

        // 3. The text of the default file of Tlumach, or a text supplied by an OnTranslationValueNotFound handler.
        if (hasTlumachText)
            return tlumachText!;

        // 4. The English message of FluentValidation.
        return base.GetString(key, English);
    }

    private string GetStringWhenDisabled(string key, string lookupKey)
    {
        CultureInfo defaultCulture = CultureMatching.DefaultFileCulture(TranslationManager) ?? CultureInfo.InvariantCulture;
        string? text = TranslationManager.GetValueWithSource(lookupKey, defaultCulture, out _).Text;

        // With Enabled set to false, the base class returns the English message regardless of the culture.
        return string.IsNullOrEmpty(text) ? base.GetString(key) : text!;
    }
}
```

**Analyzer note.** FluentValidation 12 is not annotated for nullable reference types, so overriding with `CultureInfo? culture = null` produces no nullability warning. If CS8765 or a similar warning appears, keep the nullable signature, because FluentValidation passes `null`, and look at the exact rule first.

- [ ] **Step 4: Run the tests**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj`
Expected: all pass.

If a test fails, check these first:
- `Step2_BuiltInTextForCulture_WinsOverTlumachDefault` with `Uk`: FluentValidation's Ukrainian `NotEmptyValidator` must differ from the English text. Print both values to confirm.
- `Step3_TlumachDefault_WhenNeitherHasTheCulture`: `ga-IE` must not be among FluentValidation's cultures. The list is in the `LanguageManager` source (`ar az be bg bn bs ca cs cy da de el en es et fa fi fr he hi hr hu id is it ja ka kk km ko lt lv mk nb nl nn pl pt pt-BR rm ro ru sk sl sq sr sr-Latn sv ta te tg th tr uk uz uz-Cyrl-UZ vi zh-Hans zh-Hant`), and `ga` is not in it.

- [ ] **Step 5: Build with no new warnings**

Run: `dotnet build src/Tlumach.Main.sln 2>&1 | grep -E "warning|error" | grep "Tlumach.FluentValidation" | sort -u`
Expected: no output.

- [ ] **Step 6: Commit**

```bash
git add src/Tlumach.FluentValidation/TlumachLanguageManager.cs tests/Tlumach.FluentValidationTests/TlumachLanguageManagerTests.cs
git commit -m "Add TlumachLanguageManager with the culture-aware fallback to built-in messages"
```

---

### Task 5: Rule-level helpers and concurrency

**Files:**
- Create: `src/Tlumach.FluentValidation/ValidationTemplates.cs` and `src/Tlumach.FluentValidation/TlumachRuleBuilderExtensions.cs`
- Test: `tests/Tlumach.FluentValidationTests/TlumachRuleBuilderExtensionsTests.cs` and `tests/Tlumach.FluentValidationTests/ConcurrencyTests.cs`

**Interfaces:**
- Consumes:
  - `TlumachLanguageManager.ResolveCulture()` (Task 4).
  - `BaseTranslationUnit.GetValueAsTemplate(CultureInfo)` and `BaseTranslationUnit.Key`.
  - `TranslationManager.GetValue(string, CultureInfo)`.
  - FluentValidation's `DefaultValidatorOptions.WithMessage(rule, Func<T,string>)`, `WithMessage(rule, Func<T,TProperty,string>)` and `WithName(rule, Func<T,string>)`.
  - `MessageFormatter.AppendArgument(string, object)` and `MessageFormatter.BuildMessage(string)`, both in `FluentValidation.Internal`.
- Produces:
  - `internal static class ValidationTemplates { CultureInfo CurrentCulture(); string Read(BaseTranslationUnit unit); string Read(TranslationManager manager, string key); }`
  - `public static class TlumachRuleBuilderExtensions` with the six methods listed in Step 3.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.FluentValidationTests/TlumachRuleBuilderExtensionsTests.cs` (header, then):

```csharp
using System.Globalization;

using FluentValidation;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class TlumachRuleBuilderExtensionsTests
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");

    private static string SingleMessage(IValidator<Customer> validator, Customer customer)
        => Assert.Single(validator.Validate(customer).Errors).ErrorMessage;

    [Fact]
    public void WithMessageUnit_IsReadAtValidationTimeAndFormattedByFluentValidation()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");

        CultureInfo.CurrentUICulture = En; // the rule is built in English ...
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(unit);

        CultureInfo.CurrentUICulture = De; // ... and validated in German
        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));

        CultureInfo.CurrentUICulture = En;
        Assert.Equal("'Name' must be at most 3 characters long. You entered 5.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void WithMessageManagerAndKey_IsReadAtValidationTime()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(manager, "Messages.NameTooLong");

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void WithMessagePlaceholders_FillsCustomValuesWithFormatAndLeavesTheRestToFluentValidation()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        TranslationUnit unit = TestTranslations.Unit(manager, "Messages.DiscountLimit");

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Discount).LessThanOrEqualTo(1000m).WithMessage(unit, (customer, value, formatter) => formatter.AppendArgument("Limit", 1000m));

        CultureInfo.CurrentUICulture = En;
        CultureInfo.CurrentCulture = En;
        Assert.Equal("'Discount' must not exceed 1,000. You entered 1500.", SingleMessage(validator, new Customer { Discount = 1500m }));
    }

    [Fact]
    public void WithMessagePlaceholders_ManagerAndKeyOverload()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Discount).LessThanOrEqualTo(1000m).WithMessage(manager, "Messages.DiscountLimit", (customer, value, formatter) => formatter.AppendArgument("Limit", 1000m));

        CultureInfo.CurrentUICulture = De;
        CultureInfo.CurrentCulture = De;
        Assert.Equal("'Discount' darf 1.000 nicht überschreiten. Sie haben 1500 eingegeben.", SingleMessage(validator, new Customer { Discount = 1500m }));
    }

    [Fact]
    public void WithNameUnit_LocalizesTheDisplayNameAtValidationTime()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        TranslationUnit name = TestTranslations.Unit(manager, "Messages.Nickname");

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Nickname).NotEmpty().WithName(name);

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Spitzname' wird benötigt.", SingleMessage(validator, new Customer()));

        CultureInfo.CurrentUICulture = En;
        Assert.Equal("'Nickname' is required.", SingleMessage(validator, new Customer()));
    }

    [Fact]
    public void WithNameManagerAndKey_LocalizesTheDisplayName()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithName(manager, "DisplayNames.Customer.Name");

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Kundenname' wird benötigt.", SingleMessage(validator, new Customer()));
    }

    [Fact]
    public void Helpers_FollowTheCulturePropertyOfTheLanguageManager()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager) { Culture = De };
        CultureInfo.CurrentUICulture = En;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(TestTranslations.Unit(manager, "Messages.NameTooLong"));

        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void Helpers_WithStockLanguageManager_FollowCurrentUICulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        // ValidatorOptions.Global.LanguageManager stays the stock one.
        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(manager, "Messages.NameTooLong");

        CultureInfo.CurrentUICulture = De;
        Assert.Equal("'Name' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.", SingleMessage(validator, new Customer { Name = "Alice" }));
    }

    [Fact]
    public void MissingText_ThrowsInvalidOperationExceptionNamingKeyAndCulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        CultureInfo.CurrentUICulture = De;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithMessage(TestTranslations.Unit(manager, "Messages.Missing"));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(new Customer()));
        Assert.Contains("Messages.Missing", ex.Message, StringComparison.Ordinal);
        Assert.Contains("'de'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TemplateRead_DoesNotFirePlaceholderValueNeeded()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        TranslationUnit unit = TestTranslations.Unit(manager, "Messages.NameTooLong");
        bool fired = false;
        unit.OnPlaceholderValueNeeded += (_, _) => fired = true;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).MaximumLength(3).WithMessage(unit);
        validator.Validate(new Customer { Name = "Alice" });

        Assert.False(fired);
    }

    [Fact]
    public void Helpers_RejectNullArguments()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var validator = new InlineValidator<Customer>();

        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage((BaseTranslationUnit)null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithMessage(manager, (string)null!));
        Assert.Throws<ArgumentNullException>(() => validator.RuleFor(c => c.Name).NotEmpty().WithName((BaseTranslationUnit)null!));
    }
}
```

`tests/Tlumach.FluentValidationTests/ConcurrencyTests.cs` (header, then):

```csharp
using System.Globalization;

using FluentValidation;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class ConcurrencyTests
{
    [Fact]
    public async Task ParallelValidationInDifferentCultures_GetsTheMessagesOfTheirOwnCulture()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();
        validator.RuleFor(c => c.Email).EmailAddress();
        validator.RuleFor(c => c.Nickname).MaximumLength(3).WithMessage(TestTranslations.Unit(manager, "Messages.NameTooLong"));

        var customer = new Customer { Email = "not-an-email", Nickname = "Alice" };
        var stock = new global::FluentValidation.Resources.LanguageManager();

        string[] expectedEn =
        [
            "'Name' is required.",
            stock.GetString("EmailValidator", CultureInfo.GetCultureInfo("en")).Replace("{PropertyName}", "Email", StringComparison.Ordinal),
            "'Nickname' must be at most 3 characters long. You entered 5.",
        ];
        string[] expectedDe =
        [
            "'Name' wird benötigt.",
            stock.GetString("EmailValidator", CultureInfo.GetCultureInfo("de")).Replace("{PropertyName}", "Email", StringComparison.Ordinal),
            "'Nickname' darf höchstens 3 Zeichen lang sein. Sie haben 5 eingegeben.",
        ];

        Task<bool>[] tasks = Enumerable.Range(0, 200).Select(i => Task.Run(() =>
        {
            bool german = i % 2 == 0;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(german ? "de" : "en");
            string[] actual = validator.Validate(customer).Errors.Select(e => e.ErrorMessage).ToArray();
            return actual.SequenceEqual(german ? expectedDe : expectedEn);
        })).ToArray();

        bool[] results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.True);
    }
}
```

`global::FluentValidation` is needed in this test file only if the namespace `Tlumach.FluentValidationTests` makes `FluentValidation` ambiguous. If it does not, `using FluentValidation.Resources;` plus `new LanguageManager()` is fine too.

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj --filter "FullyQualifiedName~TlumachRuleBuilderExtensionsTests|FullyQualifiedName~ConcurrencyTests"`
Expected: compile errors, because the `WithMessage(BaseTranslationUnit)` overloads do not exist. The existing `WithMessage(string)` overload of FluentValidation may even bind through the implicit conversion. That is exactly the trap that our overloads fix.

- [ ] **Step 3: Implement**

`src/Tlumach.FluentValidation/ValidationTemplates.cs` (header, then):

```csharp
using System.Globalization;

using FluentValidation;
using FluentValidation.Resources;

namespace Tlumach.FluentValidation;

/// <summary>
/// Reads the raw templates of rule-level messages and names at validation time.
/// </summary>
internal static class ValidationTemplates
{
    private const string NoTextFormat = "The Tlumach translation key '{0}' provides no text for the culture '{1}'.";

    /// <summary>
    /// Returns the culture for a rule-level message. It is the same culture that the language manager uses for the built-in messages, so that the messages of one validation
    /// agree: the rules of <see cref="TlumachLanguageManager"/> when it is installed, otherwise <c>LanguageManager.Culture</c> or <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public static CultureInfo CurrentCulture()
    {
        ILanguageManager languageManager = ValidatorOptions.Global.LanguageManager;

        return languageManager is TlumachLanguageManager tlumach
            ? tlumach.ResolveCulture()
            : languageManager.Culture ?? CultureInfo.CurrentUICulture;
    }

    public static string Read(BaseTranslationUnit unit)
    {
        CultureInfo culture = CurrentCulture();
        string template = unit.GetValueAsTemplate(culture);

        return string.IsNullOrEmpty(template) ? throw NoText(unit.Key, culture) : template;
    }

    public static string Read(TranslationManager manager, string key)
    {
        CultureInfo culture = CurrentCulture();
        string? template = manager.GetValue(key, culture).Text;

        return string.IsNullOrEmpty(template) ? throw NoText(key, culture) : template!;
    }

    private static InvalidOperationException NoText(string key, CultureInfo culture)
        => new(string.Format(CultureInfo.CurrentCulture, NoTextFormat, key, culture.Name));
}
```

`src/Tlumach.FluentValidation/TlumachRuleBuilderExtensions.cs` (header, then):

```csharp
using FluentValidation;
using FluentValidation.Internal;

namespace Tlumach.FluentValidation;

/// <summary>
/// Sets the message and the display name of a rule from a Tlumach translation. The text is read when the rule is validated, so it follows the culture of that moment.
/// </summary>
/// <remarks>
/// <para>Import the namespace <c>Tlumach.FluentValidation</c> wherever these methods are used. <see cref="TranslationUnit"/> converts implicitly to <see cref="string"/>, so without
/// the import, <c>WithMessage(Strings.X)</c> compiles against <c>WithMessage(string)</c> of FluentValidation and keeps the text of the culture that was current when the
/// validator was created.</para>
/// <para>The text is read as a template, without the placeholder engine of Tlumach, and FluentValidation then replaces its placeholders, such as <c>{PropertyName}</c>.
/// A key without text for the culture raises an <see cref="InvalidOperationException"/> during validation.</para>
/// </remarks>
public static class TlumachRuleBuilderExtensions
{
    /// <summary>
    /// Takes the message of the rule from a translation unit, typically one created by Generator.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="unit">The translation unit with the message template.</param>
    /// <returns>The rule.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, BaseTranslationUnit unit)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(unit);

        return DefaultValidatorOptions.WithMessage(rule, (T _) => ValidationTemplates.Read(unit));
    }

    /// <summary>
    /// Takes the message of the rule from a translation unit and fills placeholders of its own before FluentValidation fills the standard ones.
    /// <para>Values are inserted before FluentValidation formats the message, so a value that itself contains a placeholder of FluentValidation, such as
    /// <c>{PropertyName}</c>, has that placeholder replaced too.</para>
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="unit">The translation unit with the message template.</param>
    /// <param name="placeholders">A callback that adds values with <see cref="MessageFormatter.AppendArgument(string, object)"/>. Format specifiers, as in <c>{Limit:N0}</c>,
    /// are applied with the current culture.</param>
    /// <returns>The rule.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, BaseTranslationUnit unit, Action<T, TProperty, MessageFormatter> placeholders)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(placeholders);

        return DefaultValidatorOptions.WithMessage(rule, (T instance, TProperty value) => Fill(ValidationTemplates.Read(unit), instance, value, placeholders));
    }

    /// <summary>
    /// Takes the message of the rule from a translation manager by the key of the translation entry, which is the route for a class generated with <c>onlyDeclareKeys</c>.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="manager">The translation manager.</param>
    /// <param name="key">The full key, including its groups, for example <c>Messages.NameTooLong</c>.</param>
    /// <returns>The rule.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);

        return DefaultValidatorOptions.WithMessage(rule, (T _) => ValidationTemplates.Read(manager, key));
    }

    /// <summary>
    /// Takes the message of the rule from a translation manager by key and fills placeholders of its own before FluentValidation fills the standard ones.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="manager">The translation manager.</param>
    /// <param name="key">The full key, including its groups.</param>
    /// <param name="placeholders">A callback that adds values with <see cref="MessageFormatter.AppendArgument(string, object)"/>.</param>
    /// <returns>The rule.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key, Action<T, TProperty, MessageFormatter> placeholders)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(placeholders);

        return DefaultValidatorOptions.WithMessage(rule, (T instance, TProperty value) => Fill(ValidationTemplates.Read(manager, key), instance, value, placeholders));
    }

    /// <summary>
    /// Takes the display name of the property, which becomes <c>{PropertyName}</c> in the message, from a translation unit.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="unit">The translation unit with the display name.</param>
    /// <returns>The rule.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithName<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, BaseTranslationUnit unit)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(unit);

        return DefaultValidatorOptions.WithName(rule, (T _) => ValidationTemplates.Read(unit));
    }

    /// <summary>
    /// Takes the display name of the property from a translation manager by the key of the translation entry.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="manager">The translation manager.</param>
    /// <param name="key">The full key, including its groups.</param>
    /// <returns>The rule.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithName<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);

        return DefaultValidatorOptions.WithName(rule, (T _) => ValidationTemplates.Read(manager, key));
    }

    private static string Fill<T, TProperty>(string template, T instance, TProperty value, Action<T, TProperty, MessageFormatter> placeholders)
    {
        // A fresh formatter knows only the values that the callback adds. BuildMessage leaves every other placeholder as it is, and FluentValidation fills those afterwards.
        var formatter = new MessageFormatter();
        placeholders(instance, value, formatter);
        return formatter.BuildMessage(template);
    }
}
```

`BaseTranslationUnit` lives in the namespace `Tlumach`. Inside `namespace Tlumach.FluentValidation` it resolves without a `using`. The same is true of `TranslationManager` and `TranslationUnit`.

- [ ] **Step 4: Run all FluentValidation tests**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj`
Expected: all pass.

If the `de` formatting of `1.000` differs on the machine (ICU vs NLS), check `1000m.ToString("N0", CultureInfo.GetCultureInfo("de"))`. Both ICU and NLS use `.` as the German group separator.

- [ ] **Step 5: Build with no new warnings, then commit**

Run: `dotnet build src/Tlumach.Main.sln 2>&1 | grep -E "warning|error" | grep "Tlumach.FluentValidation" | sort -u`
Expected: no output.

```bash
git add src/Tlumach.FluentValidation/ValidationTemplates.cs src/Tlumach.FluentValidation/TlumachRuleBuilderExtensions.cs tests/Tlumach.FluentValidationTests/TlumachRuleBuilderExtensionsTests.cs tests/Tlumach.FluentValidationTests/ConcurrencyTests.cs
git commit -m "Add WithMessage and WithName helpers that read Tlumach translations at validation time"
```

---

### Task 6: `TlumachDisplayNameResolver`

**Files:**
- Create: `src/Tlumach.FluentValidation/TlumachDisplayNameResolver.cs`
- Test: `tests/Tlumach.FluentValidationTests/TlumachDisplayNameResolverTests.cs`

**Interfaces:**
- Consumes: `ValidatorOptions.Global.DisplayNameResolver` of type `Func<Type, MemberInfo, LambdaExpression, string>`, and `ValidationTemplates.CurrentCulture()` (Task 5).
- Produces:
  - `public sealed class TlumachDisplayNameResolver`
  - `public TlumachDisplayNameResolver(TranslationManager translationManager, string? displayNamesGroup = "DisplayNames")`
  - `public TranslationManager TranslationManager { get; }`
  - `public string? DisplayNamesGroup { get; }`
  - `public string? Resolve(Type type, MemberInfo member, LambdaExpression expression)`

**Culture:** the resolver uses `ValidationTemplates.CurrentCulture()`, the same culture as the messages. This is simpler than the spec's per-resolver `MessageCultureSource` and keeps the name and the message of one failure in the same language. The DI options therefore pass no culture source to the resolver.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.FluentValidationTests/TlumachDisplayNameResolverTests.cs` (header, then):

```csharp
using System.Globalization;

using FluentValidation;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class TlumachDisplayNameResolverTests
{
    [Fact]
    public void Resolve_PrefersTypeQualifiedKeyThenMemberKey()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty();
        validator.RuleFor(c => c.Email).NotEmpty();

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
        string[] messages = validator.Validate(new Customer()).Errors.Select(e => e.ErrorMessage).ToArray();

        Assert.Equal(["'Kundenname' wird benötigt.", "'E-Mail-Adresse' wird benötigt."], messages);
    }

    [Fact]
    public void Resolve_ReturnsNullForUnknownMember_SoFluentValidationDefaultApplies()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Nickname).NotEmpty();

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        Assert.Equal("'Nickname' is required.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void WithName_OverridesTheResolver()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(manager);
        ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(manager).Resolve;

        var validator = new InlineValidator<Customer>();
        validator.RuleFor(c => c.Name).NotEmpty().WithName("Override");

        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        Assert.Equal("'Override' is required.", Assert.Single(validator.Validate(new Customer()).Errors).ErrorMessage);
    }

    [Fact]
    public void Resolve_WithEmptyGroup_LooksUpRootKeys()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var resolver = new TlumachDisplayNameResolver(manager, displayNamesGroup: null);

        Assert.Null(resolver.Resolve(typeof(Customer), typeof(Customer).GetProperty(nameof(Customer.Name))!, null!));
    }

    [Fact]
    public void Resolve_NullMember_ReturnsNull()
    {
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        Assert.Null(new TlumachDisplayNameResolver(manager).Resolve(typeof(Customer), null!, null!));
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj --filter "FullyQualifiedName~TlumachDisplayNameResolverTests"`
Expected: CS0246 `TlumachDisplayNameResolver` not found.

- [ ] **Step 3: Implement**

`src/Tlumach.FluentValidation/TlumachDisplayNameResolver.cs` (header, then):

```csharp
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Tlumach.FluentValidation;

/// <summary>
/// Provides the display names of properties from a Tlumach translation, for <c>ValidatorOptions.Global.DisplayNameResolver</c>.
/// <para>For the property <c>Name</c> of the class <c>Customer</c>, the resolver looks up <c>{DisplayNamesGroup}.Customer.Name</c> and then <c>{DisplayNamesGroup}.Name</c>. When
/// neither has text, it returns <see langword="null"/>, and FluentValidation uses its default name. A rule that calls <c>WithName</c> is not affected.</para>
/// <para>The name is read when the rule is validated, for the same culture as the messages of the language manager.</para>
/// </summary>
public sealed class TlumachDisplayNameResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachDisplayNameResolver"/> class.
    /// </summary>
    /// <param name="translationManager">The translation manager that provides the names.</param>
    /// <param name="displayNamesGroup">The group, in which the names are stored. <see langword="null"/> or empty for root keys.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="translationManager"/> is <see langword="null"/>.</exception>
    public TlumachDisplayNameResolver(TranslationManager translationManager, string? displayNamesGroup = "DisplayNames")
    {
        TranslationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));
        DisplayNamesGroup = displayNamesGroup;
    }

    /// <summary>
    /// Gets the translation manager that provides the names.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the group, in which the names are stored.
    /// </summary>
    public string? DisplayNamesGroup { get; }

    /// <summary>
    /// Returns the display name of the member. The signature matches <c>ValidatorOptions.Global.DisplayNameResolver</c>.
    /// </summary>
    /// <param name="type">The type of the validated object.</param>
    /// <param name="member">The member being validated. May be <see langword="null"/> for rules that do not target a member.</param>
    /// <param name="expression">The expression of the rule. Not used.</param>
    /// <returns>The display name, or <see langword="null"/> when the translation has none.</returns>
    public string? Resolve(Type type, MemberInfo member, LambdaExpression expression)
    {
        if (member is null)
            return null;

        CultureInfo culture = ValidationTemplates.CurrentCulture();
        string prefix = string.IsNullOrEmpty(DisplayNamesGroup) ? string.Empty : DisplayNamesGroup + ".";

        return (type is null ? null : Lookup(prefix + type.Name + "." + member.Name, culture))
            ?? Lookup(prefix + member.Name, culture);
    }

    private string? Lookup(string key, CultureInfo culture)
    {
        string? text = TranslationManager.GetValue(key, culture).Text;
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
```

`Resolve_WithEmptyGroup_LooksUpRootKeys` expects `null`, because the test data has no root `Customer.Name` or `Name` key. That is enough to show that the group prefix is dropped rather than doubled.

- [ ] **Step 4: Run all FluentValidation tests, build with no new warnings, then commit**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj` (expected: all pass) and `dotnet build src/Tlumach.Main.sln 2>&1 | grep -E "warning|error" | grep "Tlumach.FluentValidation" | sort -u` (expected: no output).

```bash
git add src/Tlumach.FluentValidation/TlumachDisplayNameResolver.cs tests/Tlumach.FluentValidationTests/TlumachDisplayNameResolverTests.cs
git commit -m "Add TlumachDisplayNameResolver for display names stored in a translation group"
```

---

### Task 7: `AddTlumachFluentValidation`

**Files:**
- Create: `src/Tlumach.FluentValidation/TlumachFluentValidationOptions.cs` and `src/Tlumach.FluentValidation/TlumachFluentValidationServiceCollectionExtensions.cs`
- Test: `tests/Tlumach.FluentValidationTests/ServiceCollectionExtensionsTests.cs`

**Interfaces:**
- Consumes: `TlumachLanguageManager` (Task 4), `TlumachDisplayNameResolver` (Task 6) and `TlumachLanguageManagerOptions` (Task 3).
- Produces:
  - `public sealed class TlumachFluentValidationOptions : TlumachLanguageManagerOptions { TranslationManager? TranslationManager; bool UseDisplayNameResolver; string? DisplayNamesGroup = "DisplayNames"; }`
  - `public static IServiceCollection AddTlumachFluentValidation(this IServiceCollection services, Action<TlumachFluentValidationOptions> configure)`

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.FluentValidationTests/ServiceCollectionExtensionsTests.cs` (header, then):

```csharp
using FluentValidation;
using FluentValidation.Resources;

using Microsoft.Extensions.DependencyInjection;

using Tlumach.FluentValidation;

namespace Tlumach.FluentValidationTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTlumachFluentValidation_InstallsAndRegistersTheLanguageManager()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();

        using ServiceProvider provider = new ServiceCollection()
            .AddTlumachFluentValidation(options =>
            {
                options.TranslationManager = manager;
                options.FluentValidationGroup = null;
                options.CultureSource = MessageCultureSource.TranslationManager;
            })
            .BuildServiceProvider();

        TlumachLanguageManager languageManager = Assert.IsType<TlumachLanguageManager>(ValidatorOptions.Global.LanguageManager);
        Assert.Same(languageManager, provider.GetRequiredService<TlumachLanguageManager>());
        Assert.Same(languageManager, provider.GetRequiredService<ILanguageManager>());
        Assert.Same(manager, languageManager.TranslationManager);
        Assert.Null(languageManager.FluentValidationGroup);
        Assert.Equal(MessageCultureSource.TranslationManager, languageManager.CultureSource);
    }

    [Fact]
    public void AddTlumachFluentValidation_InstallsTheDisplayNameResolverOnlyWhenAskedTo()
    {
        using var scope = new ValidatorOptionsScope();
        using TranslationManager manager = TestTranslations.CreateJsonManager();
        var defaultResolver = ValidatorOptions.Global.DisplayNameResolver;

        new ServiceCollection().AddTlumachFluentValidation(options => options.TranslationManager = manager);
        Assert.Same(defaultResolver, ValidatorOptions.Global.DisplayNameResolver);

        using ServiceProvider provider = new ServiceCollection()
            .AddTlumachFluentValidation(options =>
            {
                options.TranslationManager = manager;
                options.UseDisplayNameResolver = true;
                options.DisplayNamesGroup = "Names";
            })
            .BuildServiceProvider();

        Assert.NotSame(defaultResolver, ValidatorOptions.Global.DisplayNameResolver);
        Assert.Equal("Names", provider.GetRequiredService<TlumachDisplayNameResolver>().DisplayNamesGroup);
    }

    [Fact]
    public void AddTlumachFluentValidation_RequiresTranslationManager()
    {
        using var scope = new ValidatorOptionsScope();

        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddTlumachFluentValidation(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTlumachFluentValidation(null!));
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail to compile**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj --filter "FullyQualifiedName~ServiceCollectionExtensionsTests"`
Expected: CS1061 `AddTlumachFluentValidation` not found.

- [ ] **Step 3: Implement**

`src/Tlumach.FluentValidation/TlumachFluentValidationOptions.cs` (header, then):

```csharp
namespace Tlumach.FluentValidation;

/// <summary>
/// The options of <see cref="TlumachFluentValidationServiceCollectionExtensions.AddTlumachFluentValidation"/>.
/// </summary>
public sealed class TlumachFluentValidationOptions : TlumachLanguageManagerOptions
{
    /// <summary>
    /// Gets or sets the translation manager that provides the messages, typically <c>Strings.TranslationManager</c> of a class created by Generator. Required.
    /// </summary>
    public TranslationManager? TranslationManager { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="TlumachDisplayNameResolver"/> is installed as <c>ValidatorOptions.Global.DisplayNameResolver</c>. The default is <see langword="false"/>.
    /// </summary>
    public bool UseDisplayNameResolver { get; set; }

    /// <summary>
    /// Gets or sets the group, in which the display names are stored, when <see cref="UseDisplayNameResolver"/> is set. The default is <c>"DisplayNames"</c>.
    /// </summary>
    public string? DisplayNamesGroup { get; set; } = "DisplayNames";
}
```

`src/Tlumach.FluentValidation/TlumachFluentValidationServiceCollectionExtensions.cs` (header, then):

```csharp
using FluentValidation;
using FluentValidation.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tlumach.FluentValidation;

/// <summary>
/// Registers the Tlumach integration of FluentValidation.
/// </summary>
public static class TlumachFluentValidationServiceCollectionExtensions
{
    /// <summary>
    /// Creates a <see cref="TlumachLanguageManager"/>, installs it as <c>ValidatorOptions.Global.LanguageManager</c>, and registers it as a singleton for
    /// <see cref="TlumachLanguageManager"/> and <see cref="ILanguageManager"/>. With <see cref="TlumachFluentValidationOptions.UseDisplayNameResolver"/>, it does the same for a
    /// <see cref="TlumachDisplayNameResolver"/> and <c>ValidatorOptions.Global.DisplayNameResolver</c>.
    /// <para>FluentValidation reads its language manager only from the process-wide <c>ValidatorOptions.Global</c>, so this method changes a global setting at the moment it is
    /// called. It can be combined with <c>AddTlumachLocalization</c> and <c>AddValidatorsFromAssemblyContaining</c> in any order.</para>
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configure">Sets the options. <see cref="TlumachFluentValidationOptions.TranslationManager"/> is required.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the callback leaves <see cref="TlumachFluentValidationOptions.TranslationManager"/> unset.</exception>
    public static IServiceCollection AddTlumachFluentValidation(this IServiceCollection services, Action<TlumachFluentValidationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new TlumachFluentValidationOptions();
        configure(options);

        if (options.TranslationManager is null)
            throw new ArgumentException("The TranslationManager property of the options must be set.", nameof(configure));

        var languageManager = new TlumachLanguageManager(options.TranslationManager, options);
        ValidatorOptions.Global.LanguageManager = languageManager;
        services.AddSingleton(languageManager);
        services.AddSingleton<ILanguageManager>(languageManager);

        if (options.UseDisplayNameResolver)
        {
            var resolver = new TlumachDisplayNameResolver(options.TranslationManager, options.DisplayNamesGroup);
            ValidatorOptions.Global.DisplayNameResolver = resolver.Resolve;
            services.TryAddSingleton(resolver);
        }

        return services;
    }
}
```

- [ ] **Step 4: Run all FluentValidation tests, build with no new warnings, then commit**

Run: `dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj` (expected: all pass) and `dotnet build src/Tlumach.Main.sln 2>&1 | grep -E "warning|error" | grep "Tlumach.FluentValidation" | sort -u` (expected: no output).

```bash
git add src/Tlumach.FluentValidation/TlumachFluentValidationOptions.cs src/Tlumach.FluentValidation/TlumachFluentValidationServiceCollectionExtensions.cs tests/Tlumach.FluentValidationTests/ServiceCollectionExtensionsTests.cs
git commit -m "Add AddTlumachFluentValidation"
```

---

### Task 8: Packaging

**Files:**
- Create: `Tlumach.FluentValidation.nuspec` and `README.fluentvalidation.nuget.md`
- Reference: `Tlumach.Writers.nuspec`, the template for a separate package.

**Interfaces:**
- Consumes: the Release build output `src/Tlumach.FluentValidation/bin/Release/{net9.0,net10.0}/Tlumach.FluentValidation.{dll,xml}`.
- Produces: the package `AlliedBits.Tlumach.FluentValidation` 1.13.0.

- [ ] **Step 1: Read `Tlumach.Writers.nuspec` in full**

Copy its metadata conventions: authors, license, icon, repository, and the `exclude="Build,Analyzers"` dependency attributes.

- [ ] **Step 2: Create `Tlumach.FluentValidation.nuspec`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
    <metadata>
        <id>AlliedBits.Tlumach.FluentValidation</id>
        <version>1.13.0</version>
        <authors>Allied Bits Ltd., Eugene Mayevski</authors>
        <copyright>Allied Bits Ltd.</copyright>
        <license type="expression">Apache-2.0</license>
        <licenseUrl>https://licenses.nuget.org/Apache-2.0</licenseUrl>
        <icon>Tlumach.png</icon>
        <title>FluentValidation integration for Tlumach, the translation and localization package for .NET</title>
        <tags>tlumach translation localization fluentvalidation validation</tags>
        <repository type="git" url="https://github.com/Allied-Bits-Ltd/tlumach-net" />
        <projectUrl>https://alliedbits.com/tlumach</projectUrl>
        <readme>README.fluentvalidation.nuget.md</readme>
        <description>
            This package localizes FluentValidation through Tlumach.NET: the built-in messages of FluentValidation, the messages and the display names of rules, and the error codes come from Tlumach translations, with a fallback to the messages built into FluentValidation.
        </description>
        <dependencies>
            <group targetFramework="net9.0">
                <dependency id="AlliedBits.Tlumach" version="1.13.0" exclude="Build,Analyzers" />
                <dependency id="FluentValidation" version="[12.0.0, 13.0.0)" exclude="Build,Analyzers" />
                <dependency id="Microsoft.Extensions.DependencyInjection.Abstractions" version="10.0.0" exclude="Build,Analyzers" />
            </group>
            <group targetFramework="net10.0">
                <dependency id="AlliedBits.Tlumach" version="1.13.0" exclude="Build,Analyzers" />
                <dependency id="FluentValidation" version="[12.0.0, 13.0.0)" exclude="Build,Analyzers" />
                <dependency id="Microsoft.Extensions.DependencyInjection.Abstractions" version="10.0.0" exclude="Build,Analyzers" />
            </group>
        </dependencies>
    </metadata>
    <files>
        <file src="Tlumach.png" target="" />
        <file src="README.fluentvalidation.nuget.md" target="" />
        <file src="src\Tlumach.FluentValidation\bin\Release\net9.0\Tlumach.FluentValidation.dll" target="lib\net9.0" />
        <file src="src\Tlumach.FluentValidation\bin\Release\net9.0\Tlumach.FluentValidation.xml" target="lib\net9.0" />
        <file src="src\Tlumach.FluentValidation\bin\Release\net10.0\Tlumach.FluentValidation.dll" target="lib\net10.0" />
        <file src="src\Tlumach.FluentValidation\bin\Release\net10.0\Tlumach.FluentValidation.xml" target="lib\net10.0" />
    </files>
</package>
```

Before committing, compare it with `Tlumach.Writers.nuspec` and `Tlumach.nuspec`:
- The icon file name and path in `<files>` must match how they include `Tlumach.png`.
- If `Tlumach.nuspec` uses a commit attribute on `<repository>`, mirror its form.
- Keep the UTF-8 BOM and CRLF line endings that the other nuspec files have.

- [ ] **Step 3: Create `README.fluentvalidation.nuget.md`**

```markdown
AlliedBits.Tlumach.FluentValidation localizes [FluentValidation](https://www.nuget.org/packages/FluentValidation) through [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach).

* `TlumachLanguageManager` takes the built-in messages of FluentValidation (`NotEmptyValidator`, `LengthValidator`, ...) and the messages of error codes from a group of a Tlumach translation, `FluentValidation` by default. For a culture without a Tlumach text, the message built into FluentValidation is used.
* `WithMessage(Strings.SomeUnit)` and `WithName(Strings.SomeUnit)` take the message and the display name of a rule from a translation unit created by Tlumach Generator. The text is read when the rule is validated, so it follows the current language.
* `TlumachDisplayNameResolver` provides the display names of properties from a translation group.
* `AddTlumachFluentValidation` sets everything up in an application that uses dependency injection.

The placeholders of FluentValidation, such as `{PropertyName}`, are kept as they are in the translations and filled by FluentValidation.

Requires FluentValidation 12 and .NET 9 or later. See the "Localization of FluentValidation" topic in the [documentation](https://alliedbits.com/tlumach).
```

- [ ] **Step 4: Verify the package builds**

Run:

```bash
dotnet build src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj -c Release
nuget pack Tlumach.FluentValidation.nuspec -OutputDirectory D:/Temp/claude/nupkg-check
```

If `nuget` is not installed, use `dotnet pack` with `NuspecFile` instead: `dotnet pack src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj -c Release -p:NuspecFile=../../Tlumach.FluentValidation.nuspec -p:NuspecBasePath=../.. -o D:/Temp/claude/nupkg-check`.

Expected: a `AlliedBits.Tlumach.FluentValidation.1.13.0.nupkg` that contains `lib/net9.0` and `lib/net10.0`. List its contents with `unzip -l`. Delete the output directory afterwards.

- [ ] **Step 5: Commit**

```bash
git add Tlumach.FluentValidation.nuspec README.fluentvalidation.nuget.md
git commit -m "Add the AlliedBits.Tlumach.FluentValidation package specification"
```

---

### Task 9: Sample

**Files:**
- Create: `samples/Tlumach.Sample.FluentValidation/Tlumach.Sample.FluentValidation.csproj`, `Sample.cfg`, `Strings.json`, `Strings_de.json`, `Strings_uk.json`, `Customer.cs`, `CustomerValidator.cs` and `Program.cs`.
- Reference: `samples/Tlumach.Sample.Blazor.Translation/Tlumach.Sample.Blazor.Translation.csproj` for wiring up Generator.

**Interfaces:**
- Consumes: everything from Tasks 4-7, and the Generator output `Tlumach.Sample.Validation.Strings` with the nested classes `Messages` and `DisplayNames`.

- [ ] **Step 1: Create the project**

`Tlumach.Sample.FluentValidation.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <RootNamespace>Tlumach.Sample.Validation</RootNamespace>
        <TlumachGeneratorExtraParsers>JsonParser</TlumachGeneratorExtraParsers>
    </PropertyGroup>

    <ItemGroup>
        <CompilerVisibleProperty Include="TlumachGeneratorExtraParsers" />
    </ItemGroup>

    <ItemGroup>
        <AdditionalFiles Include="Sample.cfg" />
        <EmbeddedResource Include="Strings.json" />
        <EmbeddedResource Include="Strings_de.json" />
        <EmbeddedResource Include="Strings_uk.json" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\Tlumach.Base\Tlumach.Base.csproj" />
        <ProjectReference Include="..\..\src\Tlumach.Generator\Tlumach.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
        <ProjectReference Include="..\..\src\Tlumach\Tlumach.csproj" />
        <ProjectReference Include="..\..\src\Tlumach.FluentValidation\Tlumach.FluentValidation.csproj" />
    </ItemGroup>

</Project>
```

The namespace is `Tlumach.Sample.Validation`, deliberately not `...FluentValidation`, so that `FluentValidation` in the sample code always means the library.

`Sample.cfg`:

```ini
defaultFile=Strings.json
defaultFileLocale=en
generatedNamespace=Tlumach.Sample.Validation
generatedClass=Strings
textProcessingMode=None

[translations]
de=Strings_de.json
uk=Strings_uk.json
```

`textProcessingMode=None` keeps Generator from treating `{PropertyName}` as a Tlumach placeholder. The texts are FluentValidation templates.

- [ ] **Step 2: Add the translations**

`Strings.json`:

```json
{
    "FluentValidation": {
        "NotEmptyValidator": "Please enter '{PropertyName}'."
    },
    "DisplayNames": {
        "Customer": {
            "Name": "Name",
            "Email": "E-mail address"
        }
    },
    "Messages": {
        "NicknameTooLong": "'{PropertyName}' can have at most {MaxLength} characters; you entered {TotalLength}.",
        "DiscountLimit": "'{PropertyName}' must not exceed {Limit:N0}."
    },
    "Names": {
        "Nickname": "Nickname",
        "Discount": "Discount"
    }
}
```

`Strings_de.json`:

```json
{
    "FluentValidation": {
        "NotEmptyValidator": "Bitte geben Sie '{PropertyName}' ein."
    },
    "DisplayNames": {
        "Customer": {
            "Name": "Name",
            "Email": "E-Mail-Adresse"
        }
    },
    "Messages": {
        "NicknameTooLong": "'{PropertyName}' darf höchstens {MaxLength} Zeichen haben; Sie haben {TotalLength} eingegeben.",
        "DiscountLimit": "'{PropertyName}' darf {Limit:N0} nicht überschreiten."
    },
    "Names": {
        "Nickname": "Spitzname",
        "Discount": "Rabatt"
    }
}
```

`Strings_uk.json`: this file has no `FluentValidation` group, so the Ukrainian built-in messages of FluentValidation are used.

```json
{
    "DisplayNames": {
        "Customer": {
            "Name": "Ім'я",
            "Email": "Адреса електронної пошти"
        }
    },
    "Messages": {
        "NicknameTooLong": "'{PropertyName}' може містити не більше {MaxLength} символів; ви ввели {TotalLength}.",
        "DiscountLimit": "'{PropertyName}' не може перевищувати {Limit:N0}."
    },
    "Names": {
        "Nickname": "Псевдонім",
        "Discount": "Знижка"
    }
}
```

- [ ] **Step 3: Add the code**

`Customer.cs` (header, then):

```csharp
namespace Tlumach.Sample.Validation;

public sealed class Customer
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Nickname { get; set; }

    public decimal Discount { get; set; }
}
```

`CustomerValidator.cs` (header, then):

```csharp
using FluentValidation;

using Tlumach.FluentValidation;

namespace Tlumach.Sample.Validation;

public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public const decimal MaxDiscount = 1000m;

    public CustomerValidator()
    {
        // The message comes from FluentValidation.NotEmptyValidator in the translation, and the name from DisplayNames.Customer.Name.
        RuleFor(c => c.Name).NotEmpty();

        // No Tlumach text: the message built into FluentValidation is used, in the current language.
        RuleFor(c => c.Email).EmailAddress();

        // The message and the name of the rule come from translation units created by Generator.
        RuleFor(c => c.Nickname).MaximumLength(10).WithMessage(Strings.Messages.NicknameTooLong).WithName(Strings.Names.Nickname);

        // A placeholder of our own, {Limit:N0}, is filled here; FluentValidation fills {PropertyName}.
        RuleFor(c => c.Discount).LessThanOrEqualTo(MaxDiscount)
            .WithMessage(Strings.Messages.DiscountLimit, (customer, discount, formatter) => formatter.AppendArgument("Limit", MaxDiscount))
            .WithName(Strings.Names.Discount);
    }
}
```

`Program.cs` (header, then):

```csharp
using System.Globalization;
using System.Text;

using FluentValidation;

using Tlumach.FluentValidation;
using Tlumach.Sample.Validation;

Console.OutputEncoding = Encoding.UTF8;

ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(Strings.TranslationManager);
ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(Strings.TranslationManager).Resolve;

var customer = new Customer { Name = string.Empty, Email = "not-an-email", Nickname = "AVeryLongNickname", Discount = 1500m };
var validator = new CustomerValidator();

foreach (string cultureName in new[] { "en", "de", "uk" })
{
    // FluentValidation and the language manager follow CurrentUICulture, which is what ASP.NET Core request localization sets per request.
    CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
    CultureInfo.CurrentUICulture = culture;
    CultureInfo.CurrentCulture = culture;

    Console.WriteLine($"--- {culture.DisplayName} ---");
    foreach (var error in validator.Validate(customer).Errors)
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");

    Console.WriteLine();
}
```

If Generator emits the units as properties (`delayedUnitsCreation`) or names a group class differently, read the generated source in `obj/` (set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` temporarily) and adjust the member paths. Do not change the translation layout.

Also add a `Tlumach.Sample.FluentValidation.slnx` next to the project, following the other samples (for example `samples/Tlumach.Sample.Console/Tlumach.Sample.Console.slnx`).

- [ ] **Step 4: Build and run**

Run: `dotnet run --project samples/Tlumach.Sample.FluentValidation/Tlumach.Sample.FluentValidation.csproj`
Expected: three blocks.
- In English and German, the `Name` message is the Tlumach text: "Please enter 'Name'." and "Bitte geben Sie 'Name' ein."
- The `Email` message is FluentValidation's text in each language.
- The `Nickname` and `Discount` messages are the Tlumach texts with localized names. The discount limit prints as `1,000` in en, `1.000` in de, and with a space or non-breaking space as the group separator in uk.
- In Ukrainian, the `Name` message is FluentValidation's Ukrainian `NotEmptyValidator` text with "Ім'я".

- [ ] **Step 5: Add a CI build step and commit**

In `.github/workflows/build-test.yml`, after "Build the Blazor sample", add:

```yaml
      - name: Build the FluentValidation sample
        run: dotnet build samples/Tlumach.Sample.FluentValidation/Tlumach.Sample.FluentValidation.csproj -c Release
```

```bash
git add samples/Tlumach.Sample.FluentValidation .github/workflows/build-test.yml
git commit -m "Add a console sample that localizes FluentValidation messages"
```

---

### Task 10: Documentation

**Files:**
- Create: `docs/articles/fluent-validation.md`
- Modify: `docs/articles/toc.yml`, `docs/articles/index.md`, `README.nuget.md`, `CHANGELOG.md` and `CLAUDE.md`

**Interfaces:**
- Consumes: the public API of Tasks 1-7 exactly as implemented. Read the code before writing, so every name in the docs matches.

- [ ] **Step 1: Write `docs/articles/fluent-validation.md`**

Mirror the voice and structure of `docs/articles/data-annotations.md`: plain declarative sentences, `xref` links to API members, and tables where that page uses them. It needs these sections, with this content:

1. **`# Localization of FluentValidation`**, then **`## Overview`**:
   - What is localized: the built-in messages, error codes, rule messages and display names.
   - The package `AlliedBits.Tlumach.FluentValidation`, and that it requires FluentValidation 12 and .NET 9+.
   - A table of the three routes: `TlumachLanguageManager` for built-in messages and error codes; `WithMessage`/`WithName` for rule-level texts; `TlumachDisplayNameResolver` for display names by convention.
2. **`## The Language Manager`**: how to install it, both `ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(Strings.TranslationManager);` and `AddTlumachFluentValidation`. Also a JSON example of the `FluentValidation` group with `NotEmptyValidator`.
   - **`### The Message Template`**:
     - FluentValidation placeholders (`{PropertyName}`, `{PropertyValue}`, `{MinLength}`, `{ComparisonValue}`, `{Name:format}`) stay in the text, and the placeholder engine of Tlumach is bypassed. Named Tlumach placeholders are not resolved; `OnPlaceholderValueNeeded` does not fire.
     - The text is returned as the parser loaded it. Backslash escapes are processed in the `DotNet` and `BackslashEscaping` modes, while ICU apostrophe quoting is **not** applied, so `'{PropertyName}'` keeps its apostrophes in an ARB file.
     - Recommend `textProcessingMode=None` for translation files that hold only validation texts, so that Generator does not treat FluentValidation placeholders as its own.
   - **`### Culture`**:
     - The order: an explicit argument, then `Culture`, then `CultureSource`. `CurrentUICulture` is the default, as in FluentValidation.
     - `MessageCultureSource.TranslationManager` is for desktop applications that switch the language through `TranslationManager.CurrentCulture`.
     - ASP.NET Core request localization and `TlumachCultureState` set `CurrentUICulture`. On Blazor Server, the same limitation as for DataAnnotations messages applies: they follow the culture of the circuit. Link to `getting-started-blazor.md`.
     - Numbers in `{Name:format}` are formatted with `CurrentCulture` by FluentValidation.
   - **`### How the Message Is Found`**:
     - The 4-step list from the spec, section 2.
     - The default-file language rule, with the `en`/`en-GB` example. Advise setting `defaultFileLocale`, because without it the default file never counts as the text of a culture.
     - The detection by comparison with English, and its edge case.
     - `Enabled = false`.
   - **`### Error Codes`**: `WithErrorCode("X")` looks up `FluentValidation.X` first. When that is empty, the validator name is used. Include an example.
   - **`### Trimming and NativeAOT`**:
     - Tlumach.FluentValidation uses no reflection, because units are passed directly.
     - FluentValidation 12 itself is not marked trimmable or AOT-compatible, so test a trimmed application.
3. **`## Rule-Level Messages and Names`**:
   - The six methods, and an example validator (from the sample).
   - The `placeholders` overload with `AppendArgument`, the format specifier, and the double-substitution note.
   - A highlighted warning about the implicit `string` conversion and the required `using Tlumach.FluentValidation;`.
   - Missing text throws `InvalidOperationException` during validation.
4. **`## Display Names`**: `TlumachDisplayNameResolver`, the lookup order `{group}.{Type}.{Member}` then `{group}.{Member}`, a `null` result falls back to FluentValidation, `WithName` wins, and an example.
5. **`## Dependency Injection`**:
   - `AddTlumachFluentValidation`, with all options listed and their defaults.
   - It changes the global `ValidatorOptions` at the moment it is called.
   - A combined example with `AddTlumachLocalization` and `AddValidatorsFromAssemblyContaining<CustomerValidator>()`. The latter needs the `FluentValidation.DependencyInjectionExtensions` package.

- [ ] **Step 2: Update the table of contents and the index**

In `docs/articles/toc.yml`, directly after the `data-annotations.md` entry, add:

```yaml
- name: Localization of FluentValidation
  href: fluent-validation.md
```

In `docs/articles/index.md`, after the line `* [Localization of Data Annotations](data-annotations.md): ...`, add:

```markdown
* [Localization of FluentValidation](fluent-validation.md): Localizing the built-in messages of FluentValidation, the messages and the names of rules, and error codes
```

- [ ] **Step 3: Update `README.nuget.md`**

After the Blazor bullet (line 16), add:

```markdown
* Localization of FluentValidation messages, rule messages, display names, and error codes through the separate `AlliedBits.Tlumach.FluentValidation` package.
```

- [ ] **Step 4: Update `CHANGELOG.md`**

Add these entries to the top of the existing `Version: 1.13.0` list:

```markdown
- [NEW] FluentValidation is supported through the new `AlliedBits.Tlumach.FluentValidation` package (.NET 9 and .NET 10, FluentValidation 12). `TlumachLanguageManager`, assigned to `ValidatorOptions.Global.LanguageManager` or installed with `AddTlumachFluentValidation`, takes the built-in messages of FluentValidation and the messages of error codes from a group of a Tlumach translation (`FluentValidation` by default, set by `FluentValidationGroup`), and falls back to the messages built into FluentValidation: the Tlumach text for the culture comes first, then the built-in message for the culture, then the default file of Tlumach, then the built-in English message. The placeholders of FluentValidation are kept as they are. The `WithMessage` and `WithName` extension methods take the message and the display name of a rule from a translation unit or a key and read it when the rule is validated, and `TlumachDisplayNameResolver` provides display names from a translation group. The culture follows `CurrentUICulture`, as in FluentValidation, or `TranslationManager.CurrentCulture`. A new sample, `samples/Tlumach.Sample.FluentValidation`, shows the messages in three languages. See "Localization of FluentValidation".
- [NEW] `TranslationManager.GetValueWithSource` returns a translation entry together with its source (`TranslationEntrySource`): the requested culture, its basic culture, the default translation, or none.
- [FIX] With `CacheDefaultTranslations` enabled (the default), a value that a lookup had taken from the basic culture or from the default translation and cached in the translation of the requested culture was treated as the culture's own text by later lookups. `foundForCulture` was then `true` for a value from the default translation, and a lookup for several language IDs could return the cached default value of an earlier language instead of the own text of a later one. Cached values are now recognized as such.
- [FIX] An entry supplied through the `Entry` property in an `OnTranslationValueNeeded` handler is now reported as found for the culture (`foundForCulture` is `true`), as a text supplied through the `Text` property already was.
```

- [ ] **Step 5: Update `CLAUDE.md`**

In the repository layout, after `Tlumach.AspNetCore/ ...`, add:

```
  Tlumach.FluentValidation/     # FluentValidation integration (TlumachLanguageManager, WithMessage/WithName, AddTlumachFluentValidation); separate package
```

After `Tlumach.BlazorTests/ ...`, add:

```
  Tlumach.FluentValidationTests/ # Tests of Tlumach.FluentValidation (run in CI)
```

In the Tests section, after the Blazor test command, add:

```bash
# FluentValidation integration tests (also run in CI)
dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj -c Release
```

In the CI/CD section, change "then run the main, generator, and Blazor tests" to "then run the main, generator, Blazor, and FluentValidation tests, and build the Blazor and FluentValidation samples".

- [ ] **Step 6: Build the docs, if DocFX is available**

Run: `docs/build.cmd`, if DocFX is installed. Expected: no broken `xref` warnings for the new page.

If DocFX is not installed, check every `xref` UID by hand against the code, for example `Tlumach.FluentValidation.TlumachLanguageManager`, and say in the final report that DocFX was not run.

- [ ] **Step 7: Commit**

```bash
git add docs/articles/fluent-validation.md docs/articles/toc.yml docs/articles/index.md README.nuget.md CHANGELOG.md CLAUDE.md
git commit -m "Document the FluentValidation integration"
```

---

### Task 11: Full verification

**Files:** none new.

- [ ] **Step 1: Build both solutions**

Run: `dotnet build src/Tlumach.Main.sln -c Release`. Expected: success, with no warnings from `Tlumach.FluentValidation`, `TranslationManager.cs` or `TranslationEntrySource.cs`.

Run: `dotnet build src/Tlumach.sln`. Expected: success. It needs the platform SDKs; if a platform project fails for a reason unrelated to this branch, record the exact error and confirm that it also fails on `main`.

- [ ] **Step 2: Run every test project**

Run each of the following; all must pass:

```bash
dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release
dotnet test tests/Tlumach.GeneratorTests/Tlumach.GeneratorTests.csproj -c Release
dotnet test tests/Tlumach.BlazorTests/Tlumach.BlazorTests.csproj -c Release
dotnet test tests/Tlumach.FluentValidationTests/Tlumach.FluentValidationTests.csproj -c Release
dotnet test tests/Tlumach.WriterTests/Tlumach.WriterTests.csproj -c Release
```

- [ ] **Step 3: Run the sample**

Run: `dotnet run --project samples/Tlumach.Sample.FluentValidation/Tlumach.Sample.FluentValidation.csproj`
Expected: the output described in Task 9, Step 4.

- [ ] **Step 4: Update the knowledge graph**

Run: `graphify update .`

- [ ] **Step 5: Check line endings of the new files**

Run: `git diff --name-only main... | xargs file | grep -v CRLF`
Expected: no `.cs`, `.csproj`, `.md`, `.json`, `.cfg`, `.nuspec` or `.sln` files listed. Shell scripts may be LF. Convert any offenders with `sed -i 's/$/\r/'` and commit with `git commit -m "Normalize line endings"`.
