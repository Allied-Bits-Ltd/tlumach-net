# FluentValidation Support — Design

Date: 2026-10-06
Branch: `fluentvalidation`
Status: approved in brainstorming, pending written-spec review

## Goal

Localize FluentValidation messages through Tlumach. That covers both the built-in messages of FluentValidation (`NotEmptyValidator`, `LengthValidator`, ...) and the messages and property names that an application sets per rule. The result should use the same concepts as `Tlumach.DataAnnotations`, so that users of both validation stacks find the same model. It must work in desktop, console, ASP.NET Core (request localization) and Blazor (`TlumachCultureState`) applications.

## Background and constraints

- **FluentValidation 12.1.1 is the latest stable release** (checked on NuGet and `main` at fa3c160).
  - v12 ships `lib/net8.0` only, with no dependencies. It dropped netstandard2.0/2.1 and net5–7.
  - Our targets, net9.0 and net10.0, consume it as is.
  - The assembly is not declared trimmable or AOT-compatible.
- **`ILanguageManager`** has three members: `bool Enabled`, `CultureInfo Culture`, and `string GetString(string key, CultureInfo culture = null)`.
- **`LanguageManager`** is the stock implementation.
  - Only `GetString` is virtual.
  - It is thread-safe (a `ConcurrentDictionary` cache keyed `"{culture}:{key}"`).
  - It resolves the culture as `culture ?? Culture ?? CultureInfo.CurrentUICulture`, walks the parent cultures, and then falls back to English. An unknown *neutral* culture is the exception: it returns `""`.
  - `Enabled = false` always returns English.
  - The per-culture lookup (`GetTranslation`) is private.
- **Built-in validators** obtain their template lazily, on every failure, through `ResolveErrorMessageUsingErrorCode(errorCode, validatorName)`. That calls `GetString(errorCode)` first when an error code is set (`WithErrorCode`), and `GetString(validatorName)` when that result is empty.
- **`RuleComponent.GetErrorMessage`** passes every template through `MessageFormatter.BuildMessage`. That includes templates returned by `WithMessage(Func<T, string>)` and `WithMessage(Func<T, TProperty, string>)`.
  - The placeholder syntax is `{Name}` or `{Name:format}`, where the format is applied with the current culture.
  - Unknown placeholders are left as they are.
- **`ValidatorOptions.Global.DisplayNameResolver`** (`Func<Type, MemberInfo, LambdaExpression, string>`) is invoked lazily on every validation. `WithName(Func<T, string>)` is evaluated lazily as well.
- **FluentValidation has no DI hook for the language manager.** It is only reachable through the static `ValidatorOptions.Global.LanguageManager`.
- **`TranslationUnit` has an implicit conversion to `string`** (its `CurrentValue`). Without the Tlumach extension methods in scope, `rule.WithMessage(Strings.X)` binds to FluentValidation's `WithMessage(string)` and freezes the text in the culture current when the rule is built.
- **Tlumach core issue found during the design.** With `CacheDefaultTranslations = true` (the default), an entry resolved from the basic-culture or the default translation is copied into the requested culture's own `Translation`. The exact-match pass of `GetValue` then treats the copy as a genuine hit. As a result:
  - `foundForCulture` reports `true` for default-file text after the first lookup.
  - In a multi-language `langIDs` lookup, a cached default under an earlier language beats a real match of a later language, such as `["de", "fr-CA"]` where `fr-FR` has the key.
- **`foundForCulture` is deliberately `false` when the requested culture is the default file's locale.** `Optimizations/Item01` asserts this, and `IStringLocalizer.ResourceNotFound` relies on it. It cannot be repurposed.

## Decisions

| Topic | Decision |
|---|---|
| Packaging | A separate NuGet package `AlliedBits.Tlumach.FluentValidation`, with its own nuspec. It is not part of `AlliedBits.Tlumach`, so that FluentValidation is not forced on every Tlumach user |
| FluentValidation version | `[12.0.0, 13.0.0)` |
| Targets | `net9.0;net10.0`, `IsAotCompatible=true` |
| Fallback | Culture-aware precedence (section 2), backed by a core change that makes the source of an entry reliable |
| Key location | `FluentValidationGroup` option, default `"FluentValidation"`. Keys are looked up as `{group}.{key}`, or as `{key}` when the group is null or empty |
| Culture source | A FluentValidation-specific option, `MessageCultureSource`, default `CurrentUICulture` (FluentValidation's own convention). It is independent of `TlumachValidationDefaults` |
| Display names | `WithName(unit)` and `WithName(manager, key)`, plus an opt-in Tlumach-backed `DisplayNameResolver` |

## 1. Core change (Tlumach, Tlumach.Base)

### Borrowed entries

When `CacheDefaultTranslations` backfills an entry into a `Translation`, the entry stays in the dictionary, so `GetTranslation(c).ContainsKey(key)` and the existing backfill tests keep their behaviour. The `Translation` additionally records the key as *borrowed*, together with the tier it was borrowed from: the basic culture or the default translation. The record is internal and is written under the same lock as the entry.

`GetValue` treats borrowed entries as follows:

- **The exact-match pass** ignores a borrowed entry. A lookup for one requested culture can therefore never short-circuit a later requested culture's exact match.
- **The basic-culture pass** may return an entry borrowed from the basic culture, as a basic-culture hit. That is the purpose of the cache.
- **An entry borrowed from the default translation** is never returned before the default stage. The default stage reads the default translation directly, as today.

As a result, `foundForCulture` is accurate after caching, and the `langIDs` short-circuit is gone. Each of these behaviours gets a regression test, written first and confirmed to fail before the fix.

### TranslationEntrySource

A new public enum in `Tlumach`, `TranslationEntrySource`:

| Value | Meaning |
|---|---|
| `NotFound` | No text was found |
| `Culture` | Exact match for a requested culture, or text supplied by an `OnTranslationValueNeeded` handler |
| `BasicCulture` | Found in the basic culture of a requested culture, e.g. `de-DE` for `de-AT` |
| `DefaultTranslation` | Found in the default translation |

New methods `GetValueWithSource(TranslationConfiguration config, string key, CultureInfo culture, out TranslationEntrySource source)` and `GetValueWithSource(string key, CultureInfo culture, out TranslationEntrySource source)` (on the default configuration). They are not `GetValue` overloads, because `GetValue(config, key, culture, out _)` already exists with `out bool`: an overload that differs only in the type of the `out` parameter would make every existing call that uses a discard or `out var` ambiguous, which is a source-breaking change. The existing `foundForCulture` overloads are reimplemented on top of the new one, with `foundForCulture = source is Culture or BasicCulture`, so their meaning is unchanged. That includes `false` for a request in the default file's locale.

## 2. TlumachLanguageManager

`public class TlumachLanguageManager : LanguageManager` in the namespace `Tlumach.FluentValidation`.

- Constructor: `TlumachLanguageManager(TranslationManager translationManager, TlumachLanguageManagerOptions? options = null)`.
- `TlumachLanguageManagerOptions`:
  - `string? FluentValidationGroup = "FluentValidation"`
  - `MessageCultureSource CultureSource = MessageCultureSource.CurrentUICulture`
- `MessageCultureSource` is a public enum with two values:
  - `CurrentUICulture`: `CultureInfo.CurrentUICulture`.
  - `TranslationManager`: `TranslationManager.CurrentCulture`, which follows `CultureInfo.CurrentCulture` when `UseContextCulture` is set.
- The options are copied in the constructor, so the instance is immutable apart from the `Culture` and `Enabled` properties inherited from FluentValidation.

### Culture

The effective culture is the explicit `culture` argument, then `Culture`, then the configured `CultureSource`. The culture that is resolved is also the one passed to `base.GetString`, so both sources of a single message agree.

### GetString(key, culture), when Enabled

The Tlumach key is `{FluentValidationGroup}.{key}`, or `key` when the group is null or empty. The source of the text is chosen in this order:

1. **Tlumach for the culture.** Call `TranslationManager.GetValueWithSource(lookupKey, culture, out source)`.
   - If `source` is `Culture` or `BasicCulture` and the text is not empty, return the text.
   - If `source` is `DefaultTranslation`, the default file's locale (`DefaultConfiguration.DefaultFileLocale`, when known) shares the neutral language of the requested culture (for example `en` and `en-GB`), and the text is not empty, return the text. The default file *is* that culture's text.
2. **FluentValidation for the culture.** Let `builtIn = base.GetString(key, culture)`. FluentValidation "has" the culture when `builtIn` is not empty and either the culture is English-family (the culture or one of its parents is `en`) or `builtIn` differs from `base.GetString(key, CultureInfo.GetCultureInfo("en"))`. If it has the culture, return `builtIn`.
   - The detection compares against English because FluentValidation's per-culture lookup is private. A built-in translation that is identical to the English text counts as missing (documented edge case).
   - Translations added through `AddTranslation` take part naturally.
3. **Tlumach's default translation.** If step 1 returned text that was not accepted, and it is not empty, return it. This is default-file text in another language, or a text supplied by an `OnTranslationValueNotFound` handler.
4. **FluentValidation's English.** Return `base.GetString(key, CultureInfo.GetCultureInfo("en"))`, which may be `""` for an unknown key, as in FluentValidation.

### GetString(key, culture), when Disabled

When `Enabled` is `false`, return Tlumach's default-translation text for the key (looked up for the default file's locale, falling back to `CultureInfo.InvariantCulture` when that locale is unknown) when it is not empty. Otherwise return FluentValidation's English text (`base.GetString` with `Enabled = false` semantics).

### Template handling and thread safety

- The template is `TranslationEntry.Text`, which is unescaped by the parser and never processed by Tlumach's placeholder engine. Neither `OnPlaceholderValueNeeded` nor ICU processing runs, so `{PropertyName}` and the other FluentValidation placeholders reach FluentValidation's `MessageFormatter` intact.
- The documentation states that a template is returned verbatim, whatever the `textProcessingMode` of the file is.
- Custom error codes (`WithErrorCode("X")`) resolve through the same path, under the same group. An application can translate them without any other code.
- Thread safety: the class holds no mutable state of its own. The base class is thread-safe, and `TranslationManager.GetValue` is used concurrently elsewhere already.

## 3. Rule-level helpers

`public static class TlumachRuleBuilderExtensions` in `Tlumach.FluentValidation`. Every helper defers the read to validation time through FluentValidation's `Func` overloads.

```csharp
IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty, TUnit>(this IRuleBuilderOptions<T, TProperty> rule, TUnit unit) where TUnit : BaseTranslationUnit;
IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, BaseTranslationUnit unit, Action<T, TProperty, MessageFormatter> placeholders);
IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key);
IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key, Action<T, TProperty, MessageFormatter> placeholders);
IRuleBuilderOptions<T, TProperty> WithName<T, TProperty, TUnit>(this IRuleBuilderOptions<T, TProperty> rule, TUnit unit) where TUnit : BaseTranslationUnit;
IRuleBuilderOptions<T, TProperty> WithName<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key);
```

- **Template reading.** The template is read with `unit.GetValueAsTemplate(culture)` or `manager.GetValue(key, culture).Text`, raw and in the same way as section 2. FluentValidation then formats it.
- **Culture.** The culture comes from `ValidatorOptions.Global.LanguageManager`:
  - when it is a `TlumachLanguageManager`, through that manager's culture rules (`Culture`, then its `CultureSource`);
  - otherwise from its `Culture` property, then `CultureInfo.CurrentUICulture`.
  So the rule messages and the built-in messages always agree.
- **Custom placeholders.** The `placeholders` overloads create a fresh `MessageFormatter` and let the callback call `AppendArgument`. They return `formatter.BuildMessage(template)`.
  - Placeholders that were not filled are left for FluentValidation's own pass, which then fills `{PropertyName}` and the rest. Format specifiers such as `{Max:N0}` work.
  - Values are inserted before FluentValidation's pass, so a value that itself contains `{PropertyName}` is substituted again (documented).
- **Overload binding.** `TranslationUnit` and the unit classes of Avalonia, WinUI and UWP convert implicitly to `string`. A non-generic overload that takes `BaseTranslationUnit` would therefore be ambiguous (CS0121) with FluentValidation's `WithMessage(string)` and `WithName(string)` for such a unit. The single-argument overloads are generic in `TUnit : BaseTranslationUnit` instead: a unit argument binds to them by identity conversion, which wins over the user-defined conversion to `string`, and a `string` argument fails the constraint, so it still binds to FluentValidation's overload. This holds when `Tlumach.FluentValidation` is imported; the documentation warns about the silent binding to the string overload when it is not.
- **Missing text.** A unit or key without text for the culture throws `InvalidOperationException` that names the key and the culture, the same as DataAnnotations: configuration that cannot work is reported rather than ignored.
- **Display names.** `WithName` uses the same raw read, so `WebEncodeValues` never HTML-encodes a name that ends up in a plain-text message.

### Display name resolver (opt-in)

`public sealed class TlumachDisplayNameResolver` takes a `TranslationManager` and a `string? DisplayNamesGroup` (default `"DisplayNames"`). It reads names for the same culture as the rule-level messages (the rules of the installed `TlumachLanguageManager`), so a name and its message are always in one language. Its `Resolve(Type type, MemberInfo member, LambdaExpression expression)` method matches FluentValidation's delegate.

- It looks up the type-qualified key `{group}.{type.Name}.{member.Name}` and the member key `{group}.{member.Name}`, once each, with `GetValueWithSource`. `type` is the type of the validated (root) object that FluentValidation passes. When the group is empty, the group part is omitted.
- A name in the language of the message wins over a name in another language. It returns the first non-empty text of: (1) the type-qualified key with source `Culture` or `BasicCulture`; (2) the member key with source `Culture` or `BasicCulture`; (3) the type-qualified key from the default translation (or supplied by an `OnTranslationValueNotFound` handler); (4) the member key likewise. Otherwise it returns `null`, so FluentValidation's default name applies.
- When `member` is null it returns `null`.
- It is installed with `ValidatorOptions.Global.DisplayNameResolver = resolver.Resolve`, or through DI (section 4).
- A rule with `WithName` overrides it, as in FluentValidation.

## 4. Dependency injection

```csharp
services.AddTlumachFluentValidation(options =>
{
    options.TranslationManager = Strings.TranslationManager; // required
    options.FluentValidationGroup = "FluentValidation";      // default
    options.CultureSource = MessageCultureSource.CurrentUICulture; // default
    options.UseDisplayNameResolver = true;                    // default false
    options.DisplayNamesGroup = "DisplayNames";               // default
});
```

- `TlumachFluentValidationOptions` extends the language manager options with `TranslationManager`, `UseDisplayNameResolver` and `DisplayNamesGroup`.
- The method validates the options. A missing `TranslationManager` throws `ArgumentException`.
- It creates the `TlumachLanguageManager` and assigns `ValidatorOptions.Global.LanguageManager`, because FluentValidation offers no other hook. It also assigns `DisplayNameResolver` when that is enabled.
- It registers the language manager as a singleton for `TlumachLanguageManager` and `ILanguageManager`, and returns `services`.
- It composes with `AddTlumachLocalization(...)` and `AddValidatorsFromAssemblyContaining<T>()`; the order does not matter.
- The XML documentation states that the setting is process-wide.
- An application without DI assigns `ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(...)` directly.

## 5. Deliverables

### Projects and solutions

- `src/Tlumach.FluentValidation/Tlumach.FluentValidation.csproj` follows the template of `Tlumach.AspNetCore`:
  - project references to Tlumach.Base and Tlumach
  - package references to `FluentValidation` `[12.0.0, 13.0.0)` and `Microsoft.Extensions.DependencyInjection.Abstractions` `10.*`
  - the StyleCop `AdditionalFiles`
  - `InternalsVisibleTo Tlumach.FluentValidationTests`
- Added to `src/Tlumach.Main.sln` and `src/Tlumach.sln`.

### Packaging

- `Tlumach.FluentValidation.nuspec` at the repository root, modelled on `Tlumach.Writers.nuspec`:
  - id `AlliedBits.Tlumach.FluentValidation`
  - dependency groups for net9.0 and net10.0 on `AlliedBits.Tlumach` (current version), `FluentValidation` `[12.0.0, 13.0.0)` and `Microsoft.Extensions.DependencyInjection.Abstractions`
  - `lib/net9.0` and `lib/net10.0` dll and xml
  - readme `README.fluentvalidation.nuget.md`
- `Tlumach.nuspec` is unchanged.

### Samples

- `samples/Tlumach.Sample.FluentValidation` is a console application with:
  - a Generator-backed translation in two or three languages, including `FluentValidation.*` overrides and a custom key;
  - a `CustomerValidator` that uses built-in messages, `WithMessage(unit)`, `WithMessage(unit, placeholders)` and `WithName(unit)`.
- It validates the same invalid object in each language by switching `CultureInfo.CurrentUICulture`, and prints the messages.

### Tests

- **Core**, in `tests/Tlumach.Tests`: borrowed-entry regression tests and tests of `TranslationEntrySource`, test-first. The existing `foundForCulture` and backfill tests must stay green.
- **`tests/Tlumach.FluentValidationTests`** (xUnit, net10.0, run in CI), with the translation files as embedded resources following the `TestData` conventions:
  - per-culture resolution of built-in keys in each of the four precedence steps, including the default-locale-language rule and `BasicCulture`;
  - fallback to FluentValidation's built-in messages, including a culture that FluentValidation has and Tlumach lacks;
  - `Enabled = false`, and the `Culture` override;
  - placeholders preserved and formatted by FluentValidation, including ICU-mode and DotNet-mode files, and `OnPlaceholderValueNeeded` never firing;
  - error codes resolved through the group;
  - a null or empty `FluentValidationGroup`;
  - `WithMessage`/`WithName` with generated units and with manager + key, the `placeholders` overload with format specifiers, and missing text throwing;
  - the display name resolver;
  - a culture switch between validations;
  - concurrent validation under different cultures (parallel tasks, each with its own `CurrentUICulture`);
  - `AddTlumachFluentValidation`.
  - Tests that mutate `ValidatorOptions.Global` run in one non-parallel xUnit collection and restore the previous values.
- **CI**: add a "Test the FluentValidation integration" step to `.github/workflows/build-test.yml`.

### Documentation

- `docs/articles/fluent-validation.md`, structured like `data-annotations.md`:
  - Overview
  - The Language Manager:
    - The Message Template
    - Culture
    - How the Message Is Found
    - Error Codes
    - Trimming and NativeAOT
  - Rule-Level Messages and Names
  - Display Names
  - Dependency Injection
- `docs/articles/toc.yml` entry, mention in `docs/articles/index.md`, `README.nuget.md` (a pointer to the separate package), `README.fluentvalidation.nuget.md`.
- `CHANGELOG.md`:
  - `[NEW]` for FluentValidation support and for `TranslationEntrySource`
  - `[FIX]` for the cached-default `foundForCulture` and `langIDs` issue
- `CLAUDE.md` repository layout (the new src and tests projects).

### Verification

- `dotnet build src/Tlumach.Main.sln` and `dotnet build src/Tlumach.sln`, both with no new analyzer warnings.
- All test projects pass.
- The sample runs.
- `graphify update .` after the code changes.

## Out of scope

- FluentValidation 11.x / netstandard2.0 support.
- A Tlumach Generator option for FluentValidation. The group is an ordinary Tlumach group, and Generator already creates the nested class for it.
- Per-validator-instance language managers. FluentValidation itself supports only the global one.
- Client-side (`_Simple`) keys. They resolve through the same path if present, but get no special treatment.
