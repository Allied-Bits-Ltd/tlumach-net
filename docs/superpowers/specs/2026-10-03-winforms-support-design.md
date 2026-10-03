# Windows Forms Support — Design

Date: 2026-10-03
Branch: `feature/winforms-support`
Status: Approved (design), pending spec review

## Goal

Give Windows Forms applications first-class Tlumach support: localize forms with live runtime language
switching, from the Visual Studio Designer and from code, without leaking controls or breaking on
cross-thread culture changes.

Today WinForms has no integration; `docs/articles/index.md` points WinForms users to raw generated
`TranslationUnit` access from code (`getting-started-manual.md`).

## Non-goals

- Design-time preview of translated text in the Designer (the Designer always shows and serializes the designed text).
- A key-picker `UITypeEditor` (hard to support in the out-of-process .NET WinForms designer).
- Placeholder processing for Designer-assigned keys (the code-first API covers placeholders).
- A WinForms-specific `TranslationUnit` class or generator changes.

## Decisions

| Topic | Decision |
|---|---|
| Key resolution in Designer | Plain string keys, resolved through a `TranslationManager` set in code or a static default |
| Target frameworks | `net472;net9.0-windows;net10.0-windows` |
| RTL | Opt-in `ApplyRightToLeft` property on the provider (default `false`) |
| Tests | New `tests/Tlumach.WinFormsTests` project (Windows-only), not run by the ubuntu CI |
| Unit class | Core `Tlumach.TranslationUnit` (generated code unchanged) |

## 1. Project and packaging

### `src/Tlumach.WinForms/Tlumach.WinForms.csproj`

- `TargetFrameworks`: `net472;net9.0-windows;net10.0-windows`; `UseWindowsForms=true`; `Nullable`, `ImplicitUsings`,
  `LangVersion latest` as in `Tlumach.WPF`; `AdditionalFiles ../Shared/stylecop.json` as in `Tlumach`.
- References `Tlumach.Base` and `Tlumach` (their `netstandard2.0` builds are used for `net472`).
- Added to `src/Tlumach.sln` only. `src/Tlumach.Main.sln` contains no UI integrations and stays unchanged, so the
  ubuntu CI build is unaffected.
- Namespace: `Tlumach.WinForms`. Standard Allied Bits Apache-2.0 file headers.

### `Tlumach.nuspec`

1. Add `src\Tlumach.WinForms\bin\Release\net9.0-windows\Tlumach.WinForms.dll` to `lib\net9.0-windows10.0.19041.0`
   and `lib\net9.0-windows10.0.26100.0`; the `net10.0-windows` build to the two `net10.0-windows10.0.*` folders.
2. Add new folders `lib\net9.0-windows7.0` and `lib\net10.0-windows7.0` containing `Tlumach.Base`, `Tlumach`,
   `Tlumach.Extensions.Localization`, `Tlumach.DataAnnotations` (the `net9.0` / `net10.0` builds) and
   `Tlumach.WinForms`, with matching empty `<group targetFramework="net9.0-windows7.0">` /
   `net10.0-windows7.0` dependency groups. Reason: a typical WinForms app targets `net9.0-windows`
   (platform version 7.0), which is not compatible with the `windows10.0.19041.0` folders and would otherwise fall
   back to `lib\net9.0` without the WinForms assembly.
3. Add `lib\net472` with the `netstandard2.0` builds of `Tlumach.Base` and `Tlumach` plus the `net472` build of
   `Tlumach.WinForms`, and a `.NETFramework4.7.2` dependency group mirroring the `.NETStandard2.0` group
   (`System.Memory`, `System.Text.Json`). `NETStandard.Library` is not needed for net472.

## 2. Designer path: `TranslationProvider`

```csharp
[ProvideProperty("TranslationKey", typeof(Component))]
[ProvideProperty("ToolTipKey", typeof(Component))]
public sealed class TranslationProvider : Component, IExtenderProvider, ISupportInitialize
```

### Extended components (`CanExtend`)

`Control` (including `Form`, whose `Text` is the caption), `ToolStripItem`, `ColumnHeader`. Everything else
(including the provider itself) returns `false`.

### Extender properties

- `string GetTranslationKey(Component)` / `void SetTranslationKey(Component, string?)`
- `string GetToolTipKey(Component)` / `void SetToolTipKey(Component, string?)`
- Attributes on the getters: `[DefaultValue("")]`, `[Category("Localization")]`, `[Description(...)]`,
  `[Localizable(false)]`. A `null` or empty value removes the assignment. With `DefaultValue("")`, the Designer emits
  `SetTranslationKey(...)` only for assigned components, keeping `InitializeComponent` stable and compilable:

  ```csharp
  this.translationProvider1.SetTranslationKey(this.label1, "greeting");
  this.translationProvider1.SetToolTipKey(this.button1, "buttons.ok.hint");
  ```

### Applying texts

| Target | TranslationKey sets | ToolTipKey sets |
|---|---|---|
| `Control` | `Text` | `ToolTip.SetToolTip(control, text)` via the provider's `ToolTip` property; ignored when `ToolTip` is `null` |
| `ToolStripItem` | `Text` | `ToolTipText` |
| `ColumnHeader` | `Text` | ignored |

- Lookup: `manager.GetValue(key)`. If the result is `TranslationEntry.Empty` (key not found) or its `Text` is
  `null`, the target is left unchanged, so the designed text serves as the fallback.
- `CanExtend`, `SetTranslationKey` and `SetToolTipKey` accept any component, but the apply step simply skips types it
  does not know.

### Other provider members

- `TranslationManager? TranslationManager { get; set; }`: `[Browsable(false)]`,
  `[DesignerSerializationVisibility(Hidden)]`. Setting it unsubscribes from the old manager's `OnCultureChanged`,
  subscribes to the new one, and applies all translations.
- `static TranslationManager? DefaultTranslationManager { get; set; }`: used when `TranslationManager` is `null`.
  Typical use: one line in `Program.Main`: `TranslationProvider.DefaultTranslationManager = Strings.TranslationManager;`.
  The provider subscribes to the effective manager the first time it applies anything (normally in `EndInit`; for a
  provider created in code without `BeginInit`/`EndInit`, on the first `SetTranslationKey`/`SetToolTipKey`) and does
  not track later changes of the static property.
- `ToolTip? ToolTip { get; set; }`: reference to a `ToolTip` component on the form (Designer-serializable).
- `bool ApplyRightToLeft { get; set; }` (default `false`): when `true`, on every apply sets
  `RightToLeft = Yes/No` and, for a `Form`, `RightToLeftLayout = true/false` on `ContainerControl`, according to
  `manager.CurrentCulture.TextInfo.IsRightToLeft`.
- `ContainerControl? ContainerControl { get; set; }`: the host form. Captured automatically at design time via
  `Site` (the `ErrorProvider` pattern: the setter override of `Site` reads `IDesignerHost.RootComponent`), so the
  Designer serializes `this.translationProvider1.ContainerControl = this;`. Settable from code.
- `void ApplyTranslations()`: re-applies everything now.
- `BeginInit()` / `EndInit()`: the Designer wraps `InitializeComponent` with these. Applying is suppressed between them;
  `EndInit` subscribes to the effective manager (if any) and applies.

### When translations are applied

- `EndInit()`, if an effective manager exists.
- `TranslationManager` assignment.
- `SetTranslationKey` / `SetToolTipKey` at runtime outside initialization (that component only).
- The manager's `OnCultureChanged` event.
- `ApplyTranslations()`.

### Design mode

When `DesignMode` (via `Site?.DesignMode`) is `true`, the provider never changes any text, tooltip, or RTL setting
and never subscribes to managers, so translated text cannot leak into the serialized form.

## 3. Code-first path: `TranslationBinding`

```csharp
public sealed class TranslationBinding : IDisposable
```

Created through extension methods in `TranslationBindingExtensions`:

```csharp
TranslationBinding BindTranslation(this Control control, TranslationUnit unit);                  // Text
TranslationBinding BindTranslation(this ToolStripItem item, TranslationUnit unit);               // Text
TranslationBinding BindTranslation(this ColumnHeader column, TranslationUnit unit);              // Text
TranslationBinding BindTranslation(this ToolTip toolTip, Control control, TranslationUnit unit); // tooltip
TranslationBinding BindTranslation<T>(this T component, TranslationUnit unit, Action<T, string> apply)
    where T : Component;                                                                          // any property
```

Behavior:

- Applies the value immediately (`unit.CurrentValue`).
- Re-applies on `unit.TranslationManager.OnCultureChanged` (skipped for `TranslationManager.Empty`) and on
  `unit.OnChange` (raised by `NotifyPlaceholdersUpdated`), so placeholder units work.
- Disposes itself when the target component raises `Disposed`. `Dispose()` unsubscribes from everything and is
  idempotent.
- Arguments are validated (`ArgumentNullException`).

## 4. Threading and lifetime

An internal `UiInvoker` captures `SynchronizationContext.Current` when created, which for WinForms is the
`WindowsFormsSynchronizationContext` of the UI thread. `Invoke(Action)` runs inline if there is no captured context
or when already on it; otherwise it `Post`s. This mirrors how `Tlumach.WinUI.TranslationUnit` marshals updates.
The provider creates its invoker in the constructor (called from `InitializeComponent` on the UI thread) and refreshes
it in `EndInit`; each binding creates its own invoker.

The value is read on the UI thread inside the posted callback, so it reflects the culture at the time of application.

Leak prevention:

- The provider unsubscribes from its manager in `Dispose(bool)` and when the manager is reassigned.
- The provider subscribes to each tracked component's `Disposed` event and removes the component from its tables
  when it fires; removing the last key for a component also unsubscribes.
- Bindings dispose themselves with their target (see §3).

## 5. Sample

`samples/Tlumach.Sample.WinForms` (`net9.0-windows`, `WinExe`, `UseWindowsForms`), referencing `Tlumach.Base`,
`Tlumach`, `Tlumach.WinForms` and the existing `samples/Tlumach.Sample.Translation` (as the WPF sample does).

- `MainForm.Designer.cs` (hand-written in Designer-compatible form) with a `TranslationProvider`, a `ToolTip`, a
  `MenuStrip`, labels, a button with a tooltip key, a `ListView` with `ColumnHeader`s and a language `ComboBox`.
- `MainForm.cs`: sets the provider's manager, binds one label in code via `BindTranslation` to a unit with
  placeholders, and fills the combo box with the configured cultures. Selecting a culture sets
  `Strings.TranslationManager.CurrentCulture`.
- Keys come from the existing sample translation files; any additions are made to every sample translation file.
- Gets its own `Tlumach.Sample.WinForms.sln`, like `samples/Tlumach.Sample.WPF` (no samples are in `src/Tlumach.sln`).

## 6. Tests

`tests/Tlumach.WinFormsTests` (`net10.0-windows`, `UseWindowsForms`, xUnit, same test packages as
`Tlumach.Tests`), added to `tests/Tlumach.Tests.sln`. The CI workflow is not changed, so ubuntu CI does not build it.
Test translations are created in-memory or as embedded resources inside the project.

Covered:

- `TranslationKey` applies text to a `Control`, `Form`, `ToolStripItem` and `ColumnHeader`; `ToolTipKey` applies to a
  `ToolTip` and to `ToolStripItem.ToolTipText`.
- A missing key keeps the designed text.
- Culture change re-applies; a culture change from a background thread is marshaled through the captured context.
- `BeginInit`/`EndInit` defers applying; `DefaultTranslationManager` fallback works.
- Design mode (a mocked `ISite` with `DesignMode = true`) changes nothing.
- `Dispose` unsubscribes from `OnCultureChanged`; a disposed control is dropped.
- `ApplyRightToLeft` sets `RightToLeft` / `RightToLeftLayout` for an RTL culture and resets them for an LTR culture.
- Extender metadata: `TypeDescriptor.GetProperties(label)` exposes `TranslationKey` with `DefaultValue("")`.
- `BindTranslation`: immediate apply, culture change, `NotifyPlaceholdersUpdated`, auto-dispose with the target, and
  explicit `Dispose`.

## 7. Documentation

- New `docs/articles/getting-started-winforms.md`, structured like `getting-started-wpf.md`: setup and translation
  project, `DefaultTranslationManager`, the Designer workflow, code-first `BindTranslation`, switching languages, RTL.
- `docs/articles/toc.yml`: entry next to the other getting-started articles.
- `docs/articles/index.md`: replace the "WinForms ... access generated units in code" wording with a pointer to the
  new article; add it to the getting-started list.
- `README.nuget.md`, `README.md`: mention Tlumach.WinForms where the platform integrations are listed.
- `CHANGELOG.md`: a `[NEW]` entry (`docs/articles/changelog.md` includes this file, so it needs no edit).
- `CLAUDE.md`: add `Tlumach.WinForms/` to the repository layout.

## 8. Verification

- `dotnet build src/Tlumach.sln` succeeds (all three WinForms TFMs) with no new analyzer warnings left unaddressed.
- `dotnet build src/Tlumach.Main.sln` and `dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release` still pass.
- `dotnet test tests/Tlumach.WinFormsTests` passes on Windows.
- The sample builds; launching it shows translated text and switches languages live.
- `graphify update .` after code changes.
