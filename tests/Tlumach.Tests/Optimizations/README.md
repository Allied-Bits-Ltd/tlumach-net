# Optimization characterization tests

These tests exist to make the performance work described in the optimization review safe to carry out.
They are **characterization tests**: they pin down what the library observably does today, so that a
change made for performance reasons cannot quietly change behaviour as well.

One file per optimization item, matching the numbering in the review and in `tests/Benchmarks`.

| Item | File | What it protects |
|---|---|---|
| 1 | `Item01_KeyNormalizationTests.cs` | Case-insensitive key and culture-name lookup; the basic-culture fallback; `foundForCulture` semantics; back-filling. |
| 2 | `Item02_ConcurrentLoadingTests.cs` | One translation instance per culture under a cold-load race; correct values; reload after `DropAllTranslations`. |
| 3 | `Item03_WarmLookupConcurrencyTests.cs` | Warm reads agree across threads and cultures; reads racing a drop neither throw nor cross over. |
| 4 | `Item04_UnitGetValueTests.cs` | The resolver observes live unit state: cache mutations, late event subscribers, precedence, disposal. |
| 6 | `Item06_ValueToTextTests.cs` | Value-to-text conversion for every type that reaches it, under two cultures. |
| 7 | `Item07_DotNetFormatSpecifierTests.cs` | `.NET` placeholder behaviour — **includes documented defects**, see below. |
| 8 | `Item08_PropertyBagTests.cs` | Property resolution order: exact match beats case-insensitive; public instance only; indexers and inheritance. |
| 9 | `Item09_ArbPlaceholderLookupTests.cs` | Declared vs undeclared placeholders, declared-type coercion, case-insensitive declaration matching. |
| 10 | `Item10_ArbNestedClosureTests.cs` | Placeholder index advancement and ICU branch selection. |
| 15 | `Item15_TemplateBuilderTests.cs` | Nesting, long inputs, escaping, concurrent evaluation — **includes a documented defect**. |
| 16 | `Item16_PlaceholderValueCacheTests.cs` | The placeholder value cache is case-**sensitive**. |
| 17 | `Item17_LeadingNumberTests.cs` | The full `GetLeadingNonNegativeNumber` contract, including overflow. |
| 18 | `Item18_AppleFormatTests.cs` | Every Apple specifier, positional arguments, tokens, and the mismatched-type fallback. |
| 20 | `Item20_ConfigTranslationsTests.cs` | `config.Translations` is case-**sensitive**; the exact / two-letter / "other" resolution order. |

Run just these:

```bash
dotnet test tests/Tlumach.Tests --filter Category=Optimization
```

## Formerly `KnownDefect`

Two groups of tests used to assert behaviour that was **wrong**, so that a performance change would not
be blamed for a pre-existing defect. Both defects have since been fixed, and those tests now assert the
correct output and stand as the regression guard for it.

- **`Item07`** — a `.NET` format-specifier tail kept its leading colon, so `{0:N2}` became the composite
  format `{0::N2}`: the value was discarded and the literal `":N2"` emitted. Alignment (`{0,10}`) was
  swallowed by the same code. Fixed by appending the tail directly after the index, since the tail
  already carries its own `,` or `:` separator. The expectations are computed with `string.Format`
  rather than hard-coded, because that is precisely what a `.NET` placeholder is meant to mean.
- **`Item15`** — after decoding a `\uXXXX` escape the read pointer advanced by four instead of five, so
  the final hex digit was emitted twice (`A` yielded `"A1"`). Fixed. `UnicodeEscape_MatchesUtilsUnescapeString`
  now cross-checks the template path against `Utils.UnescapeString`, which was always correct.

Two further quirks are recorded as ordinary tests rather than defects, because they may well be
intentional:

- `Item10.PluralWithoutFormat_IgnoresTheIcuExpression` — a numeric placeholder with no declared `format`
  never reaches `FormatArbNumber`, so its ICU expression is ignored entirely and only the raw number is
  emitted. Any ICU work must account for this.
- `Item06.LoneReferenceTypedArgument_BindsToThePropertyBagOverload` — a single reference-typed argument
  binds to `ProcessTemplatedValue(…, object)`, not to the positional-array overload, so it is treated as
  a property bag. Scalars are recognised and used directly; other objects are not.

## Conventions

- All classes are in the `Optimizations` xUnit collection, so they run sequentially with respect to each
  other. Several of them mutate process-global state (`TranslationUnit.CacheValues`).
- Tests never rely on a parser's static `TextProcessingMode`; the mode is always passed explicitly, so
  another test class mutating that global cannot disturb them.
- Culture-sensitive expectations are **computed** with the equivalent BCL call rather than hard-coded,
  because the exact output depends on the ICU version shipped with the runtime.
- Translation files are written to a fresh temporary directory per test via `OptimizationFixtures`, so
  the tests neither depend on nor disturb `TestData/`.
