# Windows Forms Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Tlumach.WinForms` assembly that localizes Windows Forms UIs (Designer extender component + code-first bindings) with live language switching, plus packaging, a sample, tests, and docs.

**Architecture:** A `TranslationProvider` component (`IExtenderProvider`, `ISupportInitialize`) adds `TranslationKey`/`ToolTipKey` properties to controls, tool strip items, and column headers; it resolves string keys through a `TranslationManager` and re-applies texts on `OnCultureChanged`. A `TranslationBinding` keeps one component property in sync with a generated `Tlumach.TranslationUnit`. Both marshal cross-thread updates through an internal `UiInvoker` that captures the UI thread's `SynchronizationContext`.

**Tech Stack:** C# (LangVersion latest), Windows Forms, `net472;net9.0-windows;net10.0-windows`, xUnit 2.9, Tlumach core (`Tlumach`, `Tlumach.Base`).

**Spec:** `docs/superpowers/specs/2026-10-03-winforms-support-design.md`

## Global Constraints

- Work on branch `feature/winforms-support`. **Ask the user before the first commit; never push.** Commit steps below assume the user has approved per-task commits.
- Every commit message ends with the line `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Every new `.cs` file starts with the exact header used across the repo (StyleCop checks it against `src/Shared/stylecop.json`, which says **2025**):

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

  (`FILE_NAME.cs` is replaced by the file's own name.) Exception: `MainForm.Designer.cs` in the sample (Designer-owned file, no header).
- 4-space indentation. Git normalizes line endings (`.gitattributes`: `* text=auto`), so LF from editors is fine.
- `Tlumach.WinForms` uses `ImplicitUsings=disable` (implicit usings differ on `net472`); write explicit `using` directives.
- Null-argument guards: `if (x is null) throw new ArgumentNullException(nameof(x));` (`ArgumentNullException.ThrowIfNull` does not exist on `net472`; CA1510 is disabled in `.editorconfig`).
- Do not use APIs missing on .NET Framework 4.7.2 in `Tlumach.WinForms` (no `init` accessors, no `[NotNullWhen]`, no `ReferenceEqualityComparer`).
- Do not change `src/Tlumach.Main.sln` or `.github/workflows/build-test.yml` (the ubuntu CI must keep working unchanged).
- Analyzers (StyleCop, Roslynator, Sonar, Meziantou, NetAnalyzers) run in every build. Fix new warnings in new code; only suppress with a `#pragma warning disable XXX // reason` pair when the rule is understood and does not apply, as `src/Tlumach.WPF/TranslateExtension.cs` does.
- Public API names (used across tasks): `Tlumach.WinForms.TranslationProvider`, `Tlumach.WinForms.TranslationBinding`, `Tlumach.WinForms.TranslationBindingExtensions`, internal `Tlumach.WinForms.UiInvoker`.

## File Structure

| File | Responsibility |
|---|---|
| `src/Tlumach.WinForms/Tlumach.WinForms.csproj` | Project (3 TFMs, WinForms) |
| `src/Tlumach.WinForms/UiInvoker.cs` | Runs an action on the creating (UI) thread; posts from other threads |
| `src/Tlumach.WinForms/TranslationProvider.cs` | Designer extender component |
| `src/Tlumach.WinForms/TranslationBinding.cs` | One live binding unit → component property |
| `src/Tlumach.WinForms/TranslationBindingExtensions.cs` | `BindTranslation` extension methods |
| `tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj` | Windows-only xUnit project |
| `tests/Tlumach.WinFormsTests/TestAssemblyInfo.cs` | Disables test parallelization (static `DefaultTranslationManager`, culture changes) |
| `tests/Tlumach.WinFormsTests/WinFormsFixture.cs` | Temp-dir JSON translations + `TranslationManager` |
| `tests/Tlumach.WinFormsTests/QueueSynchronizationContext.cs` | Test UI context that queues posts |
| `tests/Tlumach.WinFormsTests/DesignModeSite.cs` | Fake `ISite` with `DesignMode = true` |
| `tests/Tlumach.WinFormsTests/TranslationProviderTests.cs` | Provider tests |
| `tests/Tlumach.WinFormsTests/TranslationBindingTests.cs` | Binding tests |
| `Tlumach.nuspec`, `docs/docfx.json` | Packaging, API-doc exclusion |
| `samples/Tlumach.Sample.Translation/*` | 8 new keys for the WinForms sample |
| `samples/Tlumach.Sample.WinForms/*` | Sample app + its own `.sln` |
| `docs/articles/getting-started-winforms.md`, `docs/articles/index.md`, `README.md`, `README.nuget.md`, `CHANGELOG.md`, `CLAUDE.md` | Documentation |

---

### Task 1: Tlumach.WinForms project and TranslationProvider

**Files:**
- Create: `src/Tlumach.WinForms/Tlumach.WinForms.csproj`
- Create: `src/Tlumach.WinForms/UiInvoker.cs`
- Create: `src/Tlumach.WinForms/TranslationProvider.cs`
- Modify: `src/Tlumach.sln` (via `dotnet sln add`)
- Create: `tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj`
- Create: `tests/Tlumach.WinFormsTests/TestAssemblyInfo.cs`
- Create: `tests/Tlumach.WinFormsTests/WinFormsFixture.cs`
- Create: `tests/Tlumach.WinFormsTests/QueueSynchronizationContext.cs`
- Create: `tests/Tlumach.WinFormsTests/DesignModeSite.cs`
- Create: `tests/Tlumach.WinFormsTests/TranslationProviderTests.cs`
- Modify: `tests/Tlumach.Tests.sln` (via `dotnet sln add`)

**Interfaces:**
- Consumes: `Tlumach.TranslationManager` (`GetValue(string key)` → `TranslationEntry`, returns the `TranslationEntry.Empty` instance when the key is not found; `CurrentCulture`; `event EventHandler<CultureChangedEventArgs> OnCultureChanged`), `Tlumach.Base.TranslationEntry` (`string? Text`, `static TranslationEntry Empty`).
- Produces:
  - `internal sealed class UiInvoker { public UiInvoker(); public void Invoke(Action action); }`
  - `public sealed class TranslationProvider : Component, IExtenderProvider, ISupportInitialize` with `TranslationProvider()`, `TranslationProvider(IContainer container)`, `static TranslationManager? DefaultTranslationManager { get; set; }`, `TranslationManager? TranslationManager { get; set; }`, `ToolTip? ToolTip { get; set; }`, `bool ApplyRightToLeft { get; set; }`, `ContainerControl? ContainerControl { get; set; }`, `bool CanExtend(object extendee)`, `string GetTranslationKey(Component)`, `void SetTranslationKey(Component, string?)`, `string GetToolTipKey(Component)`, `void SetToolTipKey(Component, string?)`, `void ApplyTranslations()`, `void BeginInit()`, `void EndInit()`.
  - Test helpers `WinFormsFixture` (`Manager`, `Configuration`, `CreateUnit(string key, bool containsPlaceholders = false)`), `QueueSynchronizationContext` (`PendingCount`, `RunPending()`), `DesignModeSite`.

- [ ] **Step 1: Create the library project file**

`src/Tlumach.WinForms/Tlumach.WinForms.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net472;net9.0-windows;net10.0-windows</TargetFrameworks>
        <!-- Implicit usings differ between .NET Framework and .NET; the sources list their usings explicitly. -->
        <ImplicitUsings>disable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <LangVersion>latest</LangVersion>
        <UseWindowsForms>true</UseWindowsForms>
    </PropertyGroup>

    <ItemGroup>
        <AdditionalFiles Include="../Shared/stylecop.json" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Tlumach.Base\Tlumach.Base.csproj" />
        <ProjectReference Include="..\Tlumach\Tlumach.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Create the test project and helpers**

`tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.*" />
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
    <ProjectReference Include="..\..\src\Tlumach.WinForms\Tlumach.WinForms.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

`tests/Tlumach.WinFormsTests/TestAssemblyInfo.cs` (header, then):

```csharp
// The tests change TranslationProvider.DefaultTranslationManager (a static property) and the synchronization context of the test thread,
// so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

`tests/Tlumach.WinFormsTests/WinFormsFixture.cs` (header, then):

```csharp
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// Creates a temporary directory with an English default translation and a German translation, and a translation manager that loads them from the disk.
    /// The manager starts with the invariant culture, which selects the default (English) translation regardless of the culture of the machine.
    /// </summary>
    internal sealed class WinFormsFixture : IDisposable
    {
        public const string DefaultFile = "Strings.json";

        private const string DefaultJson = """
            {
                "greeting": "Hello",
                "farewell": "Goodbye",
                "hint": "Click here",
                "named": "Hello, {name}"
            }
            """;

        private const string GermanJson = """
            {
                "greeting": "Hallo",
                "farewell": "Auf Wiedersehen",
                "hint": "Hier klicken",
                "named": "Hallo, {name}"
            }
            """;

        private static readonly object _registrationLock = new();
        private static bool _registered;

        public WinFormsFixture()
        {
            EnsureParsersRegistered();

            DirectoryPath = Path.Combine(Path.GetTempPath(), "TlumachWinFormsTests", Path.GetRandomFileName());
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, DefaultFile), DefaultJson, Encoding.UTF8);
            File.WriteAllText(Path.Combine(DirectoryPath, "Strings_de.json"), GermanJson, Encoding.UTF8);

            Configuration = new TranslationConfiguration(assembly: null, DefaultFile, "en", TextFormat.DotNet)
            {
                DirectoryHint = DirectoryPath,
            };

            Manager = new TranslationManager(Configuration)
            {
                LoadFromDisk = true,
                TranslationsDirectory = DirectoryPath,
                CurrentCulture = CultureInfo.InvariantCulture,
            };
        }

        public string DirectoryPath { get; }

        public TranslationConfiguration Configuration { get; }

        public TranslationManager Manager { get; }

        public TranslationUnit CreateUnit(string key, bool containsPlaceholders = false)
            => new(Manager, Configuration, key, containsPlaceholders);

        public void Dispose()
        {
            Manager.Dispose();
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary directory does not affect the tests.
            }
        }

        private static void EnsureParsersRegistered()
        {
            lock (_registrationLock)
            {
                if (_registered)
                    return;

                JsonParser.Use();
                _registered = true;
            }
        }
    }
}
```

`tests/Tlumach.WinFormsTests/QueueSynchronizationContext.cs` (header, then):

```csharp
using System.Collections.Concurrent;

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// A synchronization context that stands in for the UI thread: posted callbacks are queued and run only when the test calls <see cref="RunPending"/>.
    /// </summary>
    internal sealed class QueueSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();

        public int PendingCount => _queue.Count;

        public override void Post(SendOrPostCallback d, object? state) => _queue.Enqueue((d, state));

        public void RunPending()
        {
            while (_queue.TryDequeue(out (SendOrPostCallback Callback, object? State) item))
                item.Callback(item.State);
        }
    }
}
```

`tests/Tlumach.WinFormsTests/DesignModeSite.cs` (header, then):

```csharp
using System.ComponentModel;

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// A site that reports design mode, as the Visual Studio Designer does for the components on the designed form.
    /// </summary>
    internal sealed class DesignModeSite : ISite
    {
        public DesignModeSite(IComponent component) => Component = component;

        public IComponent Component { get; }

        public IContainer? Container => null;

        public bool DesignMode => true;

        public string? Name { get; set; } = "translationProvider1";

        public object? GetService(Type serviceType) => null;
    }
}
```

- [ ] **Step 3: Write the failing provider tests**

`tests/Tlumach.WinFormsTests/TranslationProviderTests.cs` (header, then):

```csharp
using System.ComponentModel;
using System.Globalization;

using Tlumach.WinForms;

namespace Tlumach.WinFormsTests
{
    [Trait("Category", "WinForms")]
    public class TranslationProviderTests
    {
        private static readonly CultureInfo German = new("de");

        [Fact]
        public void ShouldApplyTranslationKeyToControlText()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetTranslationKey(label, "greeting");

            Assert.Equal("Hello", label.Text);
            Assert.Equal("greeting", provider.GetTranslationKey(label));
        }

        [Fact]
        public void ShouldApplyTranslationKeyToFormsToolStripItemsAndColumnHeaders()
        {
            using WinFormsFixture fixture = new();
            using Form form = new() { Text = "designed" };
            using ToolStripMenuItem menuItem = new() { Text = "designed" };
            using ColumnHeader column = new() { Text = "designed" };
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetTranslationKey(form, "greeting");
            provider.SetTranslationKey(menuItem, "farewell");
            provider.SetTranslationKey(column, "hint");

            Assert.Equal("Hello", form.Text);
            Assert.Equal("Goodbye", menuItem.Text);
            Assert.Equal("Click here", column.Text);
        }

        [Fact]
        public void ShouldApplyToolTipKeys()
        {
            using WinFormsFixture fixture = new();
            using ToolTip toolTip = new();
            using Button button = new();
            using ToolStripMenuItem menuItem = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager, ToolTip = toolTip };

            provider.SetToolTipKey(button, "hint");
            provider.SetToolTipKey(menuItem, "hint");

            Assert.Equal("Click here", toolTip.GetToolTip(button));
            Assert.Equal("Click here", menuItem.ToolTipText);
            Assert.Equal("hint", provider.GetToolTipKey(button));
        }

        [Fact]
        public void ShouldKeepDesignedTextWhenKeyIsMissing()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetTranslationKey(label, "no.such.key");

            Assert.Equal("designed", label.Text);
        }

        [Fact]
        public void ShouldForgetKeyWhenEmptyKeyIsAssigned()
        {
            using WinFormsFixture fixture = new();
            using Label label = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            provider.SetTranslationKey(label, "greeting");

            provider.SetTranslationKey(label, string.Empty);
            label.Text = "manual";
            fixture.Manager.CurrentCulture = German;

            Assert.Equal(string.Empty, provider.GetTranslationKey(label));
            Assert.Equal("manual", label.Text);
        }

        [Fact]
        public void ShouldDeferApplyingUntilEndInit()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new();

            provider.BeginInit();
            provider.TranslationManager = fixture.Manager;
            provider.SetTranslationKey(label, "greeting");
            Assert.Equal("designed", label.Text);

            provider.EndInit();
            Assert.Equal("Hello", label.Text);
        }

        [Fact]
        public void ShouldUseDefaultTranslationManagerWhenNoneIsAssigned()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            TranslationProvider.DefaultTranslationManager = fixture.Manager;
            try
            {
                using TranslationProvider provider = new();
                provider.BeginInit();
                provider.SetTranslationKey(label, "greeting");
                provider.EndInit();

                Assert.Equal("Hello", label.Text);
            }
            finally
            {
                TranslationProvider.DefaultTranslationManager = null;
            }
        }

        [Fact]
        public void ShouldNotTranslateInDesignMode()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new();
            provider.Site = new DesignModeSite(provider);
            provider.TranslationManager = fixture.Manager;

            provider.SetTranslationKey(label, "greeting");
            fixture.Manager.CurrentCulture = German;

            Assert.Equal("designed", label.Text);
            Assert.Equal("greeting", provider.GetTranslationKey(label));
        }

        [Fact]
        public void ShouldReapplyTranslationsWhenCultureChanges()
        {
            using WinFormsFixture fixture = new();
            using Label label = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            provider.SetTranslationKey(label, "greeting");

            fixture.Manager.CurrentCulture = German;

            Assert.Equal("Hallo", label.Text);
        }

        [Fact]
        public void ShouldMarshalCultureChangesFromOtherThreadsToTheUiThread()
        {
            SynchronizationContext? original = SynchronizationContext.Current;
            QueueSynchronizationContext uiContext = new();
            SynchronizationContext.SetSynchronizationContext(uiContext);
            try
            {
                using WinFormsFixture fixture = new();
                using Label label = new();
                using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
                provider.SetTranslationKey(label, "greeting");

                Thread worker = new(() => fixture.Manager.CurrentCulture = German);
                worker.Start();
                worker.Join();

                Assert.Equal("Hello", label.Text);
                Assert.Equal(1, uiContext.PendingCount);

                uiContext.RunPending();

                Assert.Equal("Hallo", label.Text);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void ShouldStopUpdatingAfterDispose()
        {
            using WinFormsFixture fixture = new();
            using Label label = new();
            TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            provider.SetTranslationKey(label, "greeting");

            provider.Dispose();
            fixture.Manager.CurrentCulture = German;

            Assert.Equal("Hello", label.Text);
        }

        [Fact]
        public void ShouldUnsubscribeFromThePreviousTranslationManager()
        {
            using WinFormsFixture first = new();
            using WinFormsFixture second = new();
            using Label label = new();
            using TranslationProvider provider = new() { TranslationManager = first.Manager };
            provider.SetTranslationKey(label, "greeting");

            provider.TranslationManager = second.Manager;
            label.Text = "manual";
            first.Manager.CurrentCulture = German;
            Assert.Equal("manual", label.Text);

            second.Manager.CurrentCulture = German;
            Assert.Equal("Hallo", label.Text);
        }

        [Fact]
        public void ShouldForgetDisposedComponents()
        {
            using WinFormsFixture fixture = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            Label label = new();
            provider.SetTranslationKey(label, "greeting");

            label.Dispose();

            Assert.Equal(string.Empty, provider.GetTranslationKey(label));
        }

        [Fact]
        public void ShouldApplyRightToLeftToTheContainerControl()
        {
            using WinFormsFixture fixture = new();
            using Form form = new();
            using TranslationProvider provider = new() { ContainerControl = form, ApplyRightToLeft = true, TranslationManager = fixture.Manager };

            fixture.Manager.CurrentCulture = new CultureInfo("ar");
            Assert.Equal(RightToLeft.Yes, form.RightToLeft);
            Assert.True(form.RightToLeftLayout);

            fixture.Manager.CurrentCulture = German;
            Assert.Equal(RightToLeft.No, form.RightToLeft);
            Assert.False(form.RightToLeftLayout);
        }

        [Fact]
        public void ShouldNotChangeRightToLeftWhenDisabled()
        {
            using WinFormsFixture fixture = new();
            using Form form = new();
            using TranslationProvider provider = new() { ContainerControl = form, TranslationManager = fixture.Manager };

            fixture.Manager.CurrentCulture = new CultureInfo("ar");

            Assert.Equal(RightToLeft.No, form.RightToLeft);
            Assert.False(form.RightToLeftLayout);
        }

        [Fact]
        public void ShouldExtendOnlySupportedComponents()
        {
            using TranslationProvider provider = new();
            using Label label = new();
            using ToolStripMenuItem menuItem = new();
            using ColumnHeader column = new();
            using System.Windows.Forms.Timer timer = new();

            Assert.True(provider.CanExtend(label));
            Assert.True(provider.CanExtend(menuItem));
            Assert.True(provider.CanExtend(column));
            Assert.False(provider.CanExtend(timer));
            Assert.False(provider.CanExtend(provider));
        }

        [Fact]
        public void ShouldExposeExtenderPropertiesToTheDesigner()
        {
            using Container container = new();
            TranslationProvider provider = new();
            Label label = new();
            container.Add(provider);
            container.Add(label);

            PropertyDescriptor? property = TypeDescriptor.GetProperties(label)["TranslationKey"];

            Assert.NotNull(property);
            DefaultValueAttribute? defaultValue = property.Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
            Assert.Equal(string.Empty, defaultValue?.Value);

            property.SetValue(label, "greeting");

            Assert.Equal("greeting", provider.GetTranslationKey(label));
            Assert.Equal("greeting", property.GetValue(label));
        }
    }
}
```

- [ ] **Step 4: Add the projects to the solutions and confirm the tests fail to compile**

```bash
dotnet sln src/Tlumach.sln add src/Tlumach.WinForms/Tlumach.WinForms.csproj
dotnet sln tests/Tlumach.Tests.sln add tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj
dotnet build tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj
```

Expected: build FAILS with `CS0246: The type or namespace name 'TranslationProvider' could not be found`.

- [ ] **Step 5: Implement `UiInvoker`**

`src/Tlumach.WinForms/UiInvoker.cs` (header, then):

```csharp
using System;
using System.Threading;

namespace Tlumach.WinForms
{
    /// <summary>
    /// Runs actions on the thread that created the invoker, which, for Windows Forms components, is the UI thread.
    /// <para>The invoker captures the synchronization context of that thread (<c>WindowsFormsSynchronizationContext</c> in Windows Forms applications).
    /// An action requested on the same thread runs immediately; an action requested on another thread is posted to the captured context.
    /// When the creating thread has no synchronization context, there is nothing to marshal to, and actions run immediately on the calling thread.</para>
    /// </summary>
    internal sealed class UiInvoker
    {
        private readonly SynchronizationContext? _context;
        private readonly int _threadId;

        public UiInvoker()
        {
            _context = SynchronizationContext.Current;
            _threadId = Environment.CurrentManagedThreadId;
        }

        public void Invoke(Action action)
        {
            // The thread ID, rather than the identity of SynchronizationContext.Current, tells whether we are on the UI thread:
            // code may install a different context on the UI thread, and that must not make us post to ourselves.
            if (_context is null || Environment.CurrentManagedThreadId == _threadId)
                action();
            else
                _context.Post(static state => ((Action)state!).Invoke(), action);
        }
    }
}
```

- [ ] **Step 6: Implement `TranslationProvider`**

`src/Tlumach.WinForms/TranslationProvider.cs` (header, then):

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;

using Tlumach.Base;

namespace Tlumach.WinForms
{
    /// <summary>
    /// A component that sets the texts of controls, tool strip items, and list view column headers from translations and updates them when the language changes.
    /// <para>Place the component onto a form in the Visual Studio Designer and assign translation keys to components using the "TranslationKey on ..." and "ToolTipKey on ..." properties.
    /// At run time, the component resolves the keys using <see cref="TranslationManager"/> or, if that is not set, <see cref="DefaultTranslationManager"/>,
    /// and re-applies the texts every time the culture of the translation manager changes.</para>
    /// <para>When a key is not found, the text set in the Designer is kept. In design mode, the component never changes any text, so translated texts never get into the form's code.</para>
    /// </summary>
    [ProvideProperty("TranslationKey", typeof(Component))]
    [ProvideProperty("ToolTipKey", typeof(Component))]
    [ToolboxItemFilter("System.Windows.Forms")]
    [Description("Sets the texts of the components on a form from Tlumach translations and updates them when the language changes.")]
    public sealed class TranslationProvider : Component, IExtenderProvider, ISupportInitialize
    {
        private readonly Dictionary<Component, TargetKeys> _targets = new();

        private UiInvoker _invoker = new();
        private TranslationManager? _translationManager;
        private TranslationManager? _subscribedManager;
        private ToolTip? _toolTip;
        private ContainerControl? _containerControl;
        private bool _applyRightToLeft;
        private bool _initializing;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationProvider"/> class.
        /// </summary>
        public TranslationProvider()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationProvider"/> class and adds it to the container. The Designer uses this constructor.
        /// </summary>
        /// <param name="container">The container of the form's components.</param>
        public TranslationProvider(IContainer container)
            : this()
        {
            if (container is null)
                throw new ArgumentNullException(nameof(container));

            container.Add(this);
        }

        /// <summary>
        /// Gets or sets the translation manager used by the providers whose <see cref="TranslationManager"/> property is not set.
        /// <para>Set it once at application startup (e.g., in <c>Program.Main</c>) to the <c>TranslationManager</c> property of the generated class.
        /// A provider picks up the current value of this property every time it applies the translations.</para>
        /// </summary>
        public static TranslationManager? DefaultTranslationManager { get; set; }

        /// <summary>
        /// Gets or sets the translation manager that resolves the keys. When <see langword="null"/>, <see cref="DefaultTranslationManager"/> is used.
        /// <para>The property is not available in the Designer; set it in code, e.g., after the call to <c>InitializeComponent</c>. Setting it applies the translations.</para>
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TranslationManager? TranslationManager
        {
            get => _translationManager;
            set
            {
                if (ReferenceEquals(_translationManager, value))
                    return;

                _translationManager = value;
                ApplyTranslations();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Forms.ToolTip"/> component that shows the tooltips assigned to controls using the "ToolTipKey" property.
        /// <para>Tool strip items show their own tooltips and do not need this component.</para>
        /// </summary>
        [Category("Localization")]
        [DefaultValue(null)]
        [Description("The ToolTip component that shows the tooltips assigned to controls using the ToolTipKey property.")]
        public ToolTip? ToolTip
        {
            get => _toolTip;
            set
            {
                if (ReferenceEquals(_toolTip, value))
                    return;

                _toolTip = value;
                ApplyTranslations();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the provider sets the <see cref="Control.RightToLeft"/> property of <see cref="ContainerControl"/>
        /// (and <see cref="Form.RightToLeftLayout"/> when it is a form) according to the writing direction of the current culture.
        /// </summary>
        [Category("Localization")]
        [DefaultValue(false)]
        [Description("Indicates whether the RightToLeft property of ContainerControl (and RightToLeftLayout of a form) follows the writing direction of the current culture.")]
        public bool ApplyRightToLeft
        {
            get => _applyRightToLeft;
            set
            {
                if (_applyRightToLeft == value)
                    return;

                _applyRightToLeft = value;
                ApplyTranslations();
            }
        }

        /// <summary>
        /// Gets or sets the form or user control, the writing direction of which is updated when <see cref="ApplyRightToLeft"/> is enabled.
        /// <para>In the Designer, this property is set automatically to the form or user control being designed.</para>
        /// </summary>
        [Category("Localization")]
        [DefaultValue(null)]
        [Description("The form or user control whose writing direction is updated when ApplyRightToLeft is enabled.")]
        public ContainerControl? ContainerControl
        {
            get => _containerControl;
            set
            {
                if (ReferenceEquals(_containerControl, value))
                    return;

                _containerControl = value;
                ApplyTranslations();
            }
        }

        /// <inheritdoc/>
        public override ISite? Site
        {
            get => base.Site;
            set
            {
                base.Site = value;

                // Like ErrorProvider, pick the root component (the form or user control being designed) as the container control,
                // so that the Designer serializes "translationProvider1.ContainerControl = this;".
                if (value?.GetService(typeof(IDesignerHost)) is IDesignerHost host && host.RootComponent is ContainerControl root)
                    _containerControl = root;
            }
        }

        private bool CanApply => !_initializing && !_disposed && !DesignMode;

        /// <summary>
        /// Tells the Designer which components get the "TranslationKey" and "ToolTipKey" properties: controls (including forms), tool strip items, and list view column headers.
        /// </summary>
        /// <param name="extendee">The component to check.</param>
        /// <returns><see langword="true"/> if the component can have translation keys.</returns>
        public bool CanExtend(object extendee) => extendee is Control or ToolStripItem or ColumnHeader;

        /// <summary>
        /// Gets the key of the translation assigned to the <c>Text</c> property of the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The key, or an empty string if no key is assigned.</returns>
        [DefaultValue("")]
        [Category("Localization")]
        [Description("The key of the translation assigned to the Text property.")]
        [Localizable(false)]
        public string GetTranslationKey(Component component)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            return _targets.TryGetValue(component, out TargetKeys? keys) ? keys.TranslationKey ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Assigns the key of the translation to the <c>Text</c> property of the component and, at run time, applies the translation.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="key">The key. <see langword="null"/> or an empty string removes the assignment; the current text is left as is.</param>
        public void SetTranslationKey(Component component, string? key)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            SetKey(component, key, isToolTip: false);
        }

        /// <summary>
        /// Gets the key of the translation assigned to the tooltip of the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The key, or an empty string if no key is assigned.</returns>
        [DefaultValue("")]
        [Category("Localization")]
        [Description("The key of the translation assigned to the tooltip. Controls need the ToolTip property of the provider to be set; tool strip items use their ToolTipText property.")]
        [Localizable(false)]
        public string GetToolTipKey(Component component)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            return _targets.TryGetValue(component, out TargetKeys? keys) ? keys.ToolTipKey ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Assigns the key of the translation to the tooltip of the component and, at run time, applies the translation.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="key">The key. <see langword="null"/> or an empty string removes the assignment; the current tooltip is left as is.</param>
        public void SetToolTipKey(Component component, string? key)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            SetKey(component, key, isToolTip: true);
        }

        /// <summary>
        /// Applies the translations for the current culture of the translation manager to all components that have keys assigned.
        /// <para>The provider calls this method itself when it is initialized, when the translation manager is set, and when the culture changes.
        /// Call it if the translations change in some other way (e.g., after reloading translation files).</para>
        /// </summary>
        public void ApplyTranslations()
        {
            if (!CanApply)
                return;

            TranslationManager? manager = PrepareManager();
            if (manager is null)
                return;

            // A copy is iterated because setting a text may raise events whose handlers add or remove keys.
            foreach (KeyValuePair<Component, TargetKeys> pair in _targets.ToArray())
                ApplyTo(manager, pair.Key, pair.Value);

            ApplyRightToLeftSetting(manager);
        }

        /// <summary>
        /// Signals the beginning of initialization. The Designer calls this method at the beginning of <c>InitializeComponent</c>; translations are not applied until <see cref="EndInit"/>.
        /// </summary>
        public void BeginInit() => _initializing = true;

        /// <summary>
        /// Signals the end of initialization and applies the translations if a translation manager is available.
        /// </summary>
        public void EndInit()
        {
            _initializing = false;

            // InitializeComponent runs on the UI thread, which makes this the right moment to capture its synchronization context.
            _invoker = new UiInvoker();
            ApplyTranslations();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (_subscribedManager is not null)
                {
                    _subscribedManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
                    _subscribedManager = null;
                }

                foreach (Component component in _targets.Keys.ToArray())
                    component.Disposed -= Target_Disposed;

                _targets.Clear();
            }

            base.Dispose(disposing);
        }

        private static string? Translate(TranslationManager manager, string key)
        {
            TranslationEntry entry = manager.GetValue(key);

            // TranslationManager returns the TranslationEntry.Empty instance when the key is not found; the designed text is kept then.
            return ReferenceEquals(entry, TranslationEntry.Empty) ? null : entry.Text;
        }

        private void SetKey(Component component, string? key, bool isToolTip)
        {
            string? normalizedKey = string.IsNullOrEmpty(key) ? null : key;

            if (!_targets.TryGetValue(component, out TargetKeys? keys))
            {
                if (normalizedKey is null)
                    return;

                keys = new TargetKeys();
                _targets.Add(component, keys);
                component.Disposed += Target_Disposed;
            }

            if (isToolTip)
                keys.ToolTipKey = normalizedKey;
            else
                keys.TranslationKey = normalizedKey;

            if (keys.TranslationKey is null && keys.ToolTipKey is null)
            {
                RemoveTarget(component);
                return;
            }

            if (CanApply)
            {
                TranslationManager? manager = PrepareManager();
                if (manager is not null)
                    ApplyTo(manager, component, keys);
            }
        }

        /// <summary>
        /// Determines the effective translation manager and makes sure that the provider listens to the culture changes of that manager (and only of that manager).
        /// </summary>
        private TranslationManager? PrepareManager()
        {
            TranslationManager? manager = _translationManager ?? DefaultTranslationManager;

            if (!ReferenceEquals(manager, _subscribedManager))
            {
                if (_subscribedManager is not null)
                    _subscribedManager.OnCultureChanged -= TranslationManager_OnCultureChanged;

                _subscribedManager = manager;

                if (manager is not null)
                    manager.OnCultureChanged += TranslationManager_OnCultureChanged;
            }

            return manager;
        }

        private void ApplyTo(TranslationManager manager, Component component, TargetKeys keys)
        {
            if (component is Control { IsDisposed: true })
                return;

            string? text = keys.TranslationKey is null ? null : Translate(manager, keys.TranslationKey);
            string? toolTip = keys.ToolTipKey is null ? null : Translate(manager, keys.ToolTipKey);

            if (component is Control control)
            {
                if (text is not null)
                    control.Text = text;

                if (toolTip is not null)
                    _toolTip?.SetToolTip(control, toolTip);
            }
            else if (component is ToolStripItem item)
            {
                if (text is not null)
                    item.Text = text;

                if (toolTip is not null)
                    item.ToolTipText = toolTip;
            }
            else if (component is ColumnHeader column && text is not null)
            {
                column.Text = text;
            }
        }

        private void ApplyRightToLeftSetting(TranslationManager manager)
        {
            if (!_applyRightToLeft || _containerControl is null || _containerControl.IsDisposed)
                return;

            bool isRightToLeft = manager.CurrentCulture.TextInfo.IsRightToLeft;
            _containerControl.RightToLeft = isRightToLeft ? RightToLeft.Yes : RightToLeft.No;

            if (_containerControl is Form form)
                form.RightToLeftLayout = isRightToLeft;
        }

        private void RemoveTarget(Component component)
        {
            component.Disposed -= Target_Disposed;
            _targets.Remove(component);
        }

        private void Target_Disposed(object? sender, EventArgs e)
        {
            if (sender is Component component)
                RemoveTarget(component);
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs e)
        {
            // The culture may be changed on any thread, while the controls may only be updated on the UI thread.
            _invoker.Invoke(ApplyTranslations);
        }

        private sealed class TargetKeys
        {
            public string? TranslationKey { get; set; }

            public string? ToolTipKey { get; set; }
        }
    }
}
```

- [ ] **Step 7: Build all three TFMs**

```bash
dotnet build src/Tlumach.WinForms/Tlumach.WinForms.csproj
```

Expected: `Build succeeded` for `net472`, `net9.0-windows`, `net10.0-windows`.
If `net472` fails with `CS0234: The type or namespace name 'Forms' does not exist in the namespace 'System.Windows'`, add to the csproj:

```xml
    <ItemGroup Condition="'$(TargetFramework)' == 'net472'">
        <Reference Include="System.Windows.Forms" />
    </ItemGroup>
```

- [ ] **Step 8: Run the provider tests**

```bash
dotnet test tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj --filter "FullyQualifiedName~TranslationProviderTests"
```

Expected: all 17 tests PASS.
If only `ShouldExposeExtenderPropertiesToTheDesigner` fails with a null `property`, look the descriptor up by display name instead (`TypeDescriptor.GetProperties(label).Cast<PropertyDescriptor>().Single(p => p.DisplayName.StartsWith("TranslationKey", StringComparison.Ordinal))`); that only changes the test lookup, not the provider.

- [ ] **Step 9: Check analyzer warnings in the new project**

```bash
dotnet build src/Tlumach.WinForms/Tlumach.WinForms.csproj --no-incremental 2>&1 | grep -E "warning" | grep "Tlumach.WinForms" | sed -E 's/ \[.*\]$//' | sort -u
```

Expected: no output. For each warning, fix the code; suppress only with a commented `#pragma` pair if the rule does not apply (see Global Constraints).

- [ ] **Step 10: Commit**

```bash
git add src/Tlumach.WinForms src/Tlumach.sln tests/Tlumach.WinFormsTests tests/Tlumach.Tests.sln
git commit -m "Add Tlumach.WinForms with the TranslationProvider extender component

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Code-first TranslationBinding

**Files:**
- Create: `src/Tlumach.WinForms/TranslationBinding.cs`
- Create: `src/Tlumach.WinForms/TranslationBindingExtensions.cs`
- Create: `tests/Tlumach.WinFormsTests/TranslationBindingTests.cs`

**Interfaces:**
- Consumes: `UiInvoker` (Task 1); `Tlumach.TranslationUnit` (`string CurrentValue`, `TranslationManager TranslationManager`, `event EventHandler<EventArgs> OnChange` raised by `NotifyPlaceholdersUpdated()`, `CachePlaceholderValue(string, object?)`); `TranslationManager.Empty`; test helpers `WinFormsFixture`, `QueueSynchronizationContext` (Task 1).
- Produces:
  - `public sealed class TranslationBinding : IDisposable` with `TranslationUnit Unit { get; }`, `bool IsDisposed { get; }`, `void Update()`, `void Dispose()`; internal constructor `TranslationBinding(Component target, TranslationUnit unit, Action<string> apply)`.
  - `public static class TranslationBindingExtensions` with `BindTranslation(this Control, TranslationUnit)`, `BindTranslation(this ToolStripItem, TranslationUnit)`, `BindTranslation(this ColumnHeader, TranslationUnit)`, `BindTranslation(this ToolTip, Control, TranslationUnit)`, `BindTranslation<T>(this T, TranslationUnit, Action<T, string>) where T : Component`, each returning `TranslationBinding`.

- [ ] **Step 1: Write the failing tests**

`tests/Tlumach.WinFormsTests/TranslationBindingTests.cs` (header, then):

```csharp
using System.Globalization;

using Tlumach.WinForms;

namespace Tlumach.WinFormsTests
{
    [Trait("Category", "WinForms")]
    public class TranslationBindingTests
    {
        private static readonly CultureInfo German = new("de");

        [Fact]
        public void ShouldApplyTextImmediatelyAndWhenCultureChanges()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using Label label = new();

            using TranslationBinding binding = label.BindTranslation(unit);
            Assert.Equal("Hello", label.Text);
            Assert.Same(unit, binding.Unit);

            fixture.Manager.CurrentCulture = German;
            Assert.Equal("Hallo", label.Text);
        }

        [Fact]
        public void ShouldBindToolStripItemsColumnHeadersAndToolTips()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit greeting = fixture.CreateUnit("greeting");
            using TranslationUnit hint = fixture.CreateUnit("hint");
            using ToolStripMenuItem menuItem = new();
            using ColumnHeader column = new();
            using ToolTip toolTip = new();
            using Button button = new();

            using TranslationBinding menuBinding = menuItem.BindTranslation(greeting);
            using TranslationBinding columnBinding = column.BindTranslation(greeting);
            using TranslationBinding toolTipBinding = toolTip.BindTranslation(button, hint);
            fixture.Manager.CurrentCulture = German;

            Assert.Equal("Hallo", menuItem.Text);
            Assert.Equal("Hallo", column.Text);
            Assert.Equal("Hier klicken", toolTip.GetToolTip(button));
        }

        [Fact]
        public void ShouldBindAnyPropertyUsingASetter()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("hint");
            using Button button = new();

            using TranslationBinding binding = button.BindTranslation(unit, static (b, text) => b.AccessibleName = text);

            Assert.Equal("Click here", button.AccessibleName);
        }

        [Fact]
        public void ShouldUpdateWhenPlaceholdersAreUpdated()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("named", containsPlaceholders: true);
            using Label label = new();
            unit.CachePlaceholderValue("name", "Ann");

            using TranslationBinding binding = label.BindTranslation(unit);
            Assert.Equal("Hello, Ann", label.Text);

            unit.CachePlaceholderValue("name", "Bob");
            unit.NotifyPlaceholdersUpdated();
            Assert.Equal("Hello, Bob", label.Text);
        }

        [Fact]
        public void ShouldStopUpdatingAfterDispose()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using Label label = new();
            TranslationBinding binding = label.BindTranslation(unit);

            binding.Dispose();
            binding.Dispose();
            fixture.Manager.CurrentCulture = German;

            Assert.True(binding.IsDisposed);
            Assert.Equal("Hello", label.Text);
        }

        [Fact]
        public void ShouldDisposeTogetherWithTheTarget()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            Label label = new();
            TranslationBinding binding = label.BindTranslation(unit);

            label.Dispose();

            Assert.True(binding.IsDisposed);
        }

        [Fact]
        public void ShouldMarshalCultureChangesFromOtherThreadsToTheUiThread()
        {
            SynchronizationContext? original = SynchronizationContext.Current;
            QueueSynchronizationContext uiContext = new();
            SynchronizationContext.SetSynchronizationContext(uiContext);
            try
            {
                using WinFormsFixture fixture = new();
                using TranslationUnit unit = fixture.CreateUnit("greeting");
                using Label label = new();
                using TranslationBinding binding = label.BindTranslation(unit);

                Thread worker = new(() => fixture.Manager.CurrentCulture = German);
                worker.Start();
                worker.Join();

                Assert.Equal("Hello", label.Text);

                uiContext.RunPending();

                Assert.Equal("Hallo", label.Text);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void ShouldValidateArguments()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using Label label = new();

            Assert.Throws<ArgumentNullException>(() => ((Label)null!).BindTranslation(unit));
            Assert.Throws<ArgumentNullException>(() => label.BindTranslation(null!));
            Assert.Throws<ArgumentNullException>(() => label.BindTranslation<Label>(unit, null!));
            Assert.Throws<ArgumentNullException>(() => ((ToolTip)null!).BindTranslation(label, unit));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

```bash
dotnet build tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj
```

Expected: FAIL with `CS1061: 'Label' does not contain a definition for 'BindTranslation'` and `CS0246: ... 'TranslationBinding' could not be found`.

- [ ] **Step 3: Implement `TranslationBinding`**

`src/Tlumach.WinForms/TranslationBinding.cs` (header, then):

```csharp
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Tlumach.WinForms
{
    /// <summary>
    /// Keeps a property of a Windows Forms component equal to the text of a translation unit.
    /// <para>The binding sets the property when it is created and every time the culture of the unit's translation manager changes
    /// or the unit reports updated placeholder values (see <see cref="BaseTranslationUnit.NotifyPlaceholdersUpdated"/>).
    /// Updates requested on other threads are performed on the thread that created the binding (the UI thread).</para>
    /// <para>Create bindings using the <c>BindTranslation</c> methods of <see cref="TranslationBindingExtensions"/>.
    /// A binding is disposed of automatically when its component is disposed of; dispose of it earlier to stop the updates.</para>
    /// </summary>
    public sealed class TranslationBinding : IDisposable
    {
        private readonly Component _target;
        private readonly Action<string> _apply;
        private readonly UiInvoker _invoker = new();
        private readonly TranslationManager? _translationManager;

        internal TranslationBinding(Component target, TranslationUnit unit, Action<string> apply)
        {
            _target = target;
            _apply = apply;
            Unit = unit;

            // TranslationManager.Empty never changes its culture, so there is nothing to listen to.
            if (unit.TranslationManager != TranslationManager.Empty)
            {
                _translationManager = unit.TranslationManager;
                _translationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
            }

            unit.OnChange += Unit_OnChange;
            target.Disposed += Target_Disposed;

            Update();
        }

        /// <summary>
        /// Gets the translation unit, the text of which the binding applies.
        /// </summary>
        public TranslationUnit Unit { get; }

        /// <summary>
        /// Gets a value indicating whether the binding has been disposed of and no longer updates the component.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Applies the current text of the unit to the component. Must be called on the UI thread.
        /// </summary>
        public void Update()
        {
            if (IsDisposed || _target is Control { IsDisposed: true })
                return;

            _apply(Unit.CurrentValue);
        }

        /// <summary>
        /// Stops the updates and releases the event subscriptions. Calling the method more than once has no effect.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            if (_translationManager is not null)
                _translationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;

            Unit.OnChange -= Unit_OnChange;
            _target.Disposed -= Target_Disposed;
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs e) => _invoker.Invoke(Update);

        private void Unit_OnChange(object? sender, EventArgs e) => _invoker.Invoke(Update);

        private void Target_Disposed(object? sender, EventArgs e) => Dispose();
    }
}
```

- [ ] **Step 4: Implement `TranslationBindingExtensions`**

`src/Tlumach.WinForms/TranslationBindingExtensions.cs` (header, then):

```csharp
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Tlumach.WinForms
{
    /// <summary>
    /// Methods that bind properties of Windows Forms components to translation units, so that the properties are updated when the language changes.
    /// </summary>
    public static class TranslationBindingExtensions
    {
        /// <summary>
        /// Binds the <see cref="Control.Text"/> property of the control to the translation unit.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the control.</returns>
        public static TranslationBinding BindTranslation(this Control control, TranslationUnit unit)
            => BindTranslation(control, unit, static (c, text) => c.Text = text);

        /// <summary>
        /// Binds the <see cref="ToolStripItem.Text"/> property of the tool strip item (menu item, toolbar button, status label, etc.) to the translation unit.
        /// </summary>
        /// <param name="item">The tool strip item.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the item.</returns>
        public static TranslationBinding BindTranslation(this ToolStripItem item, TranslationUnit unit)
            => BindTranslation(item, unit, static (i, text) => i.Text = text);

        /// <summary>
        /// Binds the <see cref="ColumnHeader.Text"/> property of the list view column header to the translation unit.
        /// </summary>
        /// <param name="column">The column header.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the column header.</returns>
        public static TranslationBinding BindTranslation(this ColumnHeader column, TranslationUnit unit)
            => BindTranslation(column, unit, static (c, text) => c.Text = text);

        /// <summary>
        /// Binds the tooltip that the <see cref="ToolTip"/> component shows for the control to the translation unit.
        /// </summary>
        /// <param name="toolTip">The ToolTip component.</param>
        /// <param name="control">The control, for which the tooltip is shown.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the control.</returns>
        public static TranslationBinding BindTranslation(this ToolTip toolTip, Control control, TranslationUnit unit)
        {
            if (toolTip is null)
                throw new ArgumentNullException(nameof(toolTip));

            return BindTranslation(control, unit, (c, text) => toolTip.SetToolTip(c, text));
        }

        /// <summary>
        /// Binds an arbitrary property of the component to the translation unit, using the provided method to set the property.
        /// </summary>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <param name="component">The component.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <param name="apply">The method that sets the text, e.g., <c>(textBox, text) =&gt; textBox.PlaceholderText = text</c>.</param>
        /// <returns>The binding. It is disposed of automatically together with the component.</returns>
        public static TranslationBinding BindTranslation<T>(this T component, TranslationUnit unit, Action<T, string> apply)
            where T : Component
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            if (unit is null)
                throw new ArgumentNullException(nameof(unit));

            if (apply is null)
                throw new ArgumentNullException(nameof(apply));

            return new TranslationBinding(component, unit, text => apply(component, text));
        }
    }
}
```

- [ ] **Step 5: Run all WinForms tests**

```bash
dotnet test tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj
```

Expected: 25 tests PASS (17 provider + 8 binding).

- [ ] **Step 6: Check analyzer warnings**

```bash
dotnet build src/Tlumach.WinForms/Tlumach.WinForms.csproj --no-incremental 2>&1 | grep -E "warning" | grep "Tlumach.WinForms" | sed -E 's/ \[.*\]$//' | sort -u
```

Expected: no output (handle any warning as in Task 1, Step 9).

- [ ] **Step 7: Commit**

```bash
git add src/Tlumach.WinForms tests/Tlumach.WinFormsTests
git commit -m "Add BindTranslation code-first bindings to Tlumach.WinForms

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: NuGet packaging and API-doc exclusion

**Files:**
- Modify: `Tlumach.nuspec`
- Modify: `docs/docfx.json` (metadata `exclude` list)
- Modify: `docs/superpowers/specs/2026-10-03-winforms-support-design.md` (§1 nuspec, §7 toc)

**Interfaces:**
- Consumes: Release outputs `src/Tlumach.WinForms/bin/Release/{net472,net9.0-windows,net10.0-windows}/Tlumach.WinForms.dll` (Task 1).
- Produces: package layout with `lib\net472`, `lib\net9.0-windows7.0`, `lib\net10.0-windows7.0`, and WinForms added to the four `windows10.0.*` folders.

Reason for `windows7.0` folders: a WinForms or WPF app targeting `net9.0-windows` has platform version 7.0 and cannot use the `net9.0-windows10.0.19041.0` folder, so it currently falls back to `lib\net9.0`. The new folder must be a **superset of `lib\net9.0`** (otherwise Avalonia/Uno apps targeting `net9.0-windows`, which currently get `lib\net9.0`, would lose assemblies) plus `Tlumach.WPF` and `Tlumach.WinForms`.

- [ ] **Step 1: Add the dependency groups**

In `Tlumach.nuspec`, directly after the closing `</group>` of `<group targetFramework=".NETStandard2.0">`, insert:

```xml
            <group targetFramework=".NETFramework4.7.2">
                <dependency id="System.Memory" version="4.6.3" exclude="Build,Analyzers" />
                <dependency id="System.Text.Json" version="10.0.0" exclude="Build,Analyzers" />
            </group>
```

Directly after `<group targetFramework="net9.0">` `</group>`, insert:

```xml
            <group targetFramework="net9.0-windows7.0">
            </group>
```

Directly after `<group targetFramework="net10.0">` `</group>`, insert:

```xml
            <group targetFramework="net10.0-windows7.0">
            </group>
```

- [ ] **Step 2: Add the `net472` folder**

After the two `lib\netstandard2.0` file lines, insert:

```xml

        <!-- .NET Framework 4.7.2 (WinForms) -->
        <file src="src\Tlumach.Base\bin\Release\netstandard2.0\Tlumach.Base.dll" target="lib\net472" />
        <file src="src\Tlumach\bin\Release\netstandard2.0\Tlumach.dll" target="lib\net472" />
        <file src="src\Tlumach.WinForms\bin\Release\net472\Tlumach.WinForms.dll" target="lib\net472" />
```

- [ ] **Step 3: Add the `net9.0-windows7.0` and `net10.0-windows7.0` folders**

Immediately before the line `<!-- .NET 9.0 / Windows (WPF, WinUI, MAUI) -->`, insert:

```xml
        <!-- .NET 9.0 / Windows without a Windows SDK version (the default for WinForms and WPF applications); a superset of lib\net9.0 -->
        <file src="src\Tlumach.Base\bin\Release\net9.0\Tlumach.Base.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach\bin\Release\net9.0\Tlumach.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach.Extensions.Localization\bin\Release\net9.0\Tlumach.Extensions.Localization.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach.DataAnnotations\bin\Release\net9.0\Tlumach.DataAnnotations.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach.WinUI\bin\Release\net9.0\Tlumach.WinUI.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach.Avalonia\bin\Release\net9.0\Tlumach.Avalonia.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach.WPF\bin\Release\net9.0-windows\Tlumach.WPF.dll" target="lib\net9.0-windows7.0" />
        <file src="src\Tlumach.WinForms\bin\Release\net9.0-windows\Tlumach.WinForms.dll" target="lib\net9.0-windows7.0" />

```

Immediately before the line `<!-- .NET 10.0 / Windows (WPF, WinUI, MAUI) -->`, insert the same block with every `9.0` replaced by `10.0`:

```xml
        <!-- .NET 10.0 / Windows without a Windows SDK version (the default for WinForms and WPF applications); a superset of lib\net10.0 -->
        <file src="src\Tlumach.Base\bin\Release\net10.0\Tlumach.Base.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach\bin\Release\net10.0\Tlumach.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach.Extensions.Localization\bin\Release\net10.0\Tlumach.Extensions.Localization.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach.DataAnnotations\bin\Release\net10.0\Tlumach.DataAnnotations.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach.WinUI\bin\Release\net10.0\Tlumach.WinUI.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach.Avalonia\bin\Release\net10.0\Tlumach.Avalonia.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach.WPF\bin\Release\net10.0-windows\Tlumach.WPF.dll" target="lib\net10.0-windows7.0" />
        <file src="src\Tlumach.WinForms\bin\Release\net10.0-windows\Tlumach.WinForms.dll" target="lib\net10.0-windows7.0" />

```

Before inserting, compare with the existing `lib\net9.0` / `lib\net10.0` blocks; if they list an assembly not in the blocks above, add it here too.

- [ ] **Step 4: Add WinForms to the four existing Windows folders**

After each of these four existing lines, add the matching WinForms line:

| After the line ending with | Add |
|---|---|
| `Tlumach.WPF.dll" target="lib\net9.0-windows10.0.19041.0" />` | `<file src="src\Tlumach.WinForms\bin\Release\net9.0-windows\Tlumach.WinForms.dll" target="lib\net9.0-windows10.0.19041.0" />` |
| `Tlumach.WPF.dll" target="lib\net9.0-windows10.0.26100.0" />` | `<file src="src\Tlumach.WinForms\bin\Release\net9.0-windows\Tlumach.WinForms.dll" target="lib\net9.0-windows10.0.26100.0" />` |
| `Tlumach.WPF.dll" target="lib\net10.0-windows10.0.19041.0" />` | `<file src="src\Tlumach.WinForms\bin\Release\net10.0-windows\Tlumach.WinForms.dll" target="lib\net10.0-windows10.0.19041.0" />` |
| `Tlumach.WPF.dll" target="lib\net10.0-windows10.0.26100.0" />` | `<file src="src\Tlumach.WinForms\bin\Release\net10.0-windows\Tlumach.WinForms.dll" target="lib\net10.0-windows10.0.26100.0" />` |

(Indent with 8 spaces like the neighboring lines.)

- [ ] **Step 5: Validate the nuspec**

```bash
powershell -NoProfile -Command "[xml](Get-Content -Raw Tlumach.nuspec) | Out-Null; 'xml ok'"
dotnet build src/Tlumach.WinForms/Tlumach.WinForms.csproj -c Release
for f in $(grep -o 'src="src\\Tlumach\.WinForms[^"]*"' Tlumach.nuspec | sed -e 's/src="//' -e 's/"$//' -e 's#\\#/#g' | sort -u); do test -f "$f" && echo "OK $f" || echo "MISSING $f"; done
grep -c 'Tlumach.WinForms.dll' Tlumach.nuspec
```

Expected: `xml ok`; three `OK ...` lines (net472, net9.0-windows, net10.0-windows) and no `MISSING`; count `7` (`net472` + two `windows7.0` folders + four `windows10.0.*` folders).

- [ ] **Step 6: Exclude the WinForms project from DocFX API metadata**

DocFX builds API metadata with `TargetFramework=net10.0`, which a Windows-only project cannot build (the other UI integrations are excluded the same way). In `docs/docfx.json`, change:

```json
            "**/Tlumach.WinUI/**",
            "**/Tlumach.Avalonia/**"          ]
```

to:

```json
            "**/Tlumach.WinUI/**",
            "**/Tlumach.WinForms/**",
            "**/Tlumach.Avalonia/**"          ]
```

- [ ] **Step 7: Amend the spec**

In `docs/superpowers/specs/2026-10-03-winforms-support-design.md`, replace item 2 of the `Tlumach.nuspec` list with:

```markdown
2. Add new folders `lib\net9.0-windows7.0` and `lib\net10.0-windows7.0` containing everything that `lib\net9.0` /
   `lib\net10.0` contain (`Tlumach.Base`, `Tlumach`, `Tlumach.Extensions.Localization`, `Tlumach.DataAnnotations`,
   `Tlumach.WinUI`, `Tlumach.Avalonia`) plus `Tlumach.WPF` and `Tlumach.WinForms`, with matching empty
   `<group targetFramework="net9.0-windows7.0">` / `net10.0-windows7.0` dependency groups. Reason: a typical WinForms or
   WPF app targets `net9.0-windows` (platform version 7.0), which is not compatible with the `windows10.0.19041.0`
   folders and falls back to `lib\net9.0`; the new folder must stay a superset of `lib\net9.0` so that apps which
   currently get `lib\net9.0` (e.g., Avalonia apps targeting `net9.0-windows`) lose nothing.
```

and in §7 replace the line starting with ``- `docs/articles/toc.yml` `` with:

```markdown
- `docs/articles/toc.yml`: no change (the getting-started articles are linked from `index.md`, not from the TOC).
- `docs/docfx.json`: exclude `**/Tlumach.WinForms/**` from API metadata, like the other UI integrations.
```

- [ ] **Step 8: Commit**

```bash
git add Tlumach.nuspec docs/docfx.json docs/superpowers/specs/2026-10-03-winforms-support-design.md
git commit -m "Package Tlumach.WinForms and add windows7.0 and net472 package folders

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: WinForms sample

**Files:**
- Modify: `samples/Tlumach.Sample.Translation/sample.arb`, `sample_de.toml`, `sample.pl.resx.resx`, `sample_sk.ini`, `sample_hr.json`, `sample_uk.tsv` (new keys; `sample_de-AT.toml` intentionally unchanged, it falls back to `de`)
- Create: `samples/Tlumach.Sample.WinForms/Tlumach.Sample.WinForms.csproj`
- Create: `samples/Tlumach.Sample.WinForms/Program.cs`
- Create: `samples/Tlumach.Sample.WinForms/MainForm.cs`
- Create: `samples/Tlumach.Sample.WinForms/MainForm.Designer.cs`
- Create: `samples/Tlumach.Sample.WinForms/Tlumach.Sample.WinForms.sln`

**Interfaces:**
- Consumes: `TranslationProvider` (Task 1), `BindTranslation` (Task 2); generated class `Tlumach.Sample.Strings` from `samples/Tlumach.Sample.Translation` (`Strings.TranslationManager`, unit properties named after keys, e.g. `Strings.HelloName`; `delayedUnitsCreation=true`, `textProcessingMode=DotNet`).
- Produces: a runnable sample; no API for other tasks.

- [ ] **Step 1: Add the new keys to the sample translations**

New keys and texts:

| Key | en (`sample.arb`) | de | pl | sk | hr | uk |
|---|---|---|---|---|---|---|
| MenuFile | File | Datei | Plik | Súbor | Datoteka | Файл |
| MenuExit | Exit | Beenden | Zakończ | Koniec | Izlaz | Вихід |
| MenuExitHint | Close the application | Anwendung schließen | Zamknij aplikację | Zatvoriť aplikáciu | Zatvori aplikaciju | Закрити програму |
| Language | Language: | Sprache: | Język: | Jazyk: | Jezik: | Мова: |
| LanguageHint | Select the language of the user interface | Wählen Sie die Sprache der Benutzeroberfläche | Wybierz język interfejsu użytkownika | Vyberte jazyk používateľského rozhrania | Odaberite jezik korisničkog sučelja | Виберіть мову інтерфейсу користувача |
| YourName | Your name: | Ihr Name: | Twoje imię: | Vaše meno: | Vaše ime: | Ваше ім'я: |
| ColumnKey | Key | Schlüssel | Klucz | Kľúč | Ključ | Ключ |
| ColumnText | Text | Text | Tekst | Text | Tekst | Текст |

`sample.arb` becomes:

```json
{
    "Hello": "Hello world",
    "HelloName": "Hello {name}",
    "Welcome": "Welcome",
    "Copyright": "Copyright (c) {year}, Allied Bits Ltd.",
    "CultureInfo": "Current UI culture is '{currentCulture}', System culture is '{systemCulture}'",
    "MenuFile": "File",
    "MenuExit": "Exit",
    "MenuExitHint": "Close the application",
    "Language": "Language:",
    "LanguageHint": "Select the language of the user interface",
    "YourName": "Your name:",
    "ColumnKey": "Key",
    "ColumnText": "Text"
}
```

Append to `sample_de.toml`:

```toml
MenuFile="Datei"
MenuExit="Beenden"
MenuExitHint="Anwendung schließen"
Language="Sprache:"
LanguageHint="Wählen Sie die Sprache der Benutzeroberfläche"
YourName="Ihr Name:"
ColumnKey="Schlüssel"
ColumnText="Text"
```

Append to `sample_sk.ini`:

```ini
MenuFile=Súbor
MenuExit=Koniec
MenuExitHint=Zatvoriť aplikáciu
Language=Jazyk:
LanguageHint=Vyberte jazyk používateľského rozhrania
YourName=Vaše meno:
ColumnKey=Kľúč
ColumnText=Text
```

`sample_hr.json` becomes:

```json
{
    "Hello": "Bok",
    "HelloName": "Bok, {name}",
    "Welcome": "Dobrodošli",
    "CultureInfo": "Trenutna kultura korisničkog sučelja je '{currentCulture}', kultura sustava je '{systemCulture}'",
    "MenuFile": "Datoteka",
    "MenuExit": "Izlaz",
    "MenuExitHint": "Zatvori aplikaciju",
    "Language": "Jezik:",
    "LanguageHint": "Odaberite jezik korisničkog sučelja",
    "YourName": "Vaše ime:",
    "ColumnKey": "Ključ",
    "ColumnText": "Tekst"
}
```

Append to `sample_uk.tsv` (a TAB between key and text):

```text
MenuFile	Файл
MenuExit	Вихід
MenuExitHint	Закрити програму
Language	Мова:
LanguageHint	Виберіть мову інтерфейсу користувача
YourName	Ваше ім'я:
ColumnKey	Ключ
ColumnText	Текст
```

In `sample.pl.resx.resx`, insert before `</root>`:

```xml
  <data name="MenuFile" xml:space="preserve">
    <value>Plik</value>
  </data>
  <data name="MenuExit" xml:space="preserve">
    <value>Zakończ</value>
  </data>
  <data name="MenuExitHint" xml:space="preserve">
    <value>Zamknij aplikację</value>
  </data>
  <data name="Language" xml:space="preserve">
    <value>Język:</value>
  </data>
  <data name="LanguageHint" xml:space="preserve">
    <value>Wybierz język interfejsu użytkownika</value>
  </data>
  <data name="YourName" xml:space="preserve">
    <value>Twoje imię:</value>
  </data>
  <data name="ColumnKey" xml:space="preserve">
    <value>Klucz</value>
  </data>
  <data name="ColumnText" xml:space="preserve">
    <value>Tekst</value>
  </data>
```

Make sure each file still ends with a newline and the TSV uses real TAB characters (check with `grep -P '\t' samples/Tlumach.Sample.Translation/sample_uk.tsv | tail -8`).

- [ ] **Step 2: Build the translation project**

```bash
dotnet build samples/Tlumach.Sample.Translation/Tlumach.Sample.Translation.csproj
```

Expected: `Build succeeded`; the generator now produces `Strings.MenuFile` … `Strings.ColumnText`.

- [ ] **Step 3: Create the sample project**

`samples/Tlumach.Sample.WinForms/Tlumach.Sample.WinForms.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tlumach.Base\Tlumach.Base.csproj" />
    <ProjectReference Include="..\..\src\Tlumach\Tlumach.csproj" />
    <ProjectReference Include="..\..\src\Tlumach.WinForms\Tlumach.WinForms.csproj" />
    <ProjectReference Include="..\Tlumach.Sample.Translation\Tlumach.Sample.Translation.csproj" />
  </ItemGroup>

</Project>
```

`samples/Tlumach.Sample.WinForms/Program.cs` (header, then):

```csharp
using Tlumach.WinForms;

namespace Tlumach.Sample.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Every TranslationProvider that has no TranslationManager of its own uses this one.
            // It must be set before the first form is created, because the provider applies the translations at the end of InitializeComponent.
            TranslationProvider.DefaultTranslationManager = Strings.TranslationManager;

            Application.Run(new MainForm());
        }
    }
}
```

- [ ] **Step 4: Create the Designer file**

`samples/Tlumach.Sample.WinForms/MainForm.Designer.cs` (no header; this is the Designer's file):

```csharp
namespace Tlumach.Sample.WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            translationProvider1 = new Tlumach.WinForms.TranslationProvider(components);
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            welcomeLabel = new System.Windows.Forms.Label();
            helloLabel = new System.Windows.Forms.Label();
            languageLabel = new System.Windows.Forms.Label();
            languageComboBox = new System.Windows.Forms.ComboBox();
            nameLabel = new System.Windows.Forms.Label();
            nameTextBox = new System.Windows.Forms.TextBox();
            helloNameLabel = new System.Windows.Forms.Label();
            translationsListView = new System.Windows.Forms.ListView();
            keyColumnHeader = new System.Windows.Forms.ColumnHeader();
            textColumnHeader = new System.Windows.Forms.ColumnHeader();
            copyrightLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)translationProvider1).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // translationProvider1
            // 
            translationProvider1.ApplyRightToLeft = true;
            translationProvider1.ContainerControl = this;
            translationProvider1.ToolTip = toolTip1;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(584, 24);
            menuStrip1.TabIndex = 0;
            // 
            // fileMenuItem
            // 
            fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { exitMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new System.Drawing.Size(37, 20);
            fileMenuItem.Text = "File";
            translationProvider1.SetTranslationKey(fileMenuItem, "MenuFile");
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new System.Drawing.Size(180, 22);
            exitMenuItem.Text = "Exit";
            translationProvider1.SetToolTipKey(exitMenuItem, "MenuExitHint");
            translationProvider1.SetTranslationKey(exitMenuItem, "MenuExit");
            exitMenuItem.Click += ExitMenuItem_Click;
            // 
            // welcomeLabel
            // 
            welcomeLabel.AutoSize = true;
            welcomeLabel.Font = new System.Drawing.Font("Segoe UI", 14F);
            welcomeLabel.Location = new System.Drawing.Point(12, 36);
            welcomeLabel.Name = "welcomeLabel";
            welcomeLabel.Size = new System.Drawing.Size(86, 25);
            welcomeLabel.TabIndex = 1;
            welcomeLabel.Text = "Welcome";
            translationProvider1.SetTranslationKey(welcomeLabel, "Welcome");
            // 
            // helloLabel
            // 
            helloLabel.AutoSize = true;
            helloLabel.Location = new System.Drawing.Point(12, 70);
            helloLabel.Name = "helloLabel";
            helloLabel.Size = new System.Drawing.Size(71, 15);
            helloLabel.TabIndex = 2;
            helloLabel.Text = "Hello world";
            translationProvider1.SetTranslationKey(helloLabel, "Hello");
            // 
            // languageLabel
            // 
            languageLabel.AutoSize = true;
            languageLabel.Location = new System.Drawing.Point(12, 104);
            languageLabel.Name = "languageLabel";
            languageLabel.Size = new System.Drawing.Size(62, 15);
            languageLabel.TabIndex = 3;
            languageLabel.Text = "Language:";
            translationProvider1.SetTranslationKey(languageLabel, "Language");
            // 
            // languageComboBox
            // 
            languageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            languageComboBox.FormattingEnabled = true;
            languageComboBox.Location = new System.Drawing.Point(140, 100);
            languageComboBox.Name = "languageComboBox";
            languageComboBox.Size = new System.Drawing.Size(300, 23);
            languageComboBox.TabIndex = 4;
            translationProvider1.SetToolTipKey(languageComboBox, "LanguageHint");
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(12, 138);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(67, 15);
            nameLabel.TabIndex = 5;
            nameLabel.Text = "Your name:";
            translationProvider1.SetTranslationKey(nameLabel, "YourName");
            // 
            // nameTextBox
            // 
            nameTextBox.Location = new System.Drawing.Point(140, 134);
            nameTextBox.Name = "nameTextBox";
            nameTextBox.Size = new System.Drawing.Size(300, 23);
            nameTextBox.TabIndex = 6;
            nameTextBox.Text = "World";
            nameTextBox.TextChanged += NameTextBox_TextChanged;
            // 
            // helloNameLabel
            // 
            helloNameLabel.AutoSize = true;
            helloNameLabel.Location = new System.Drawing.Point(12, 172);
            helloNameLabel.Name = "helloNameLabel";
            helloNameLabel.Size = new System.Drawing.Size(0, 15);
            helloNameLabel.TabIndex = 7;
            // 
            // translationsListView
            // 
            translationsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { keyColumnHeader, textColumnHeader });
            translationsListView.FullRowSelect = true;
            translationsListView.Location = new System.Drawing.Point(12, 200);
            translationsListView.Name = "translationsListView";
            translationsListView.Size = new System.Drawing.Size(560, 110);
            translationsListView.TabIndex = 8;
            translationsListView.UseCompatibleStateImageBehavior = false;
            translationsListView.View = System.Windows.Forms.View.Details;
            // 
            // keyColumnHeader
            // 
            keyColumnHeader.Text = "Key";
            keyColumnHeader.Width = 150;
            translationProvider1.SetTranslationKey(keyColumnHeader, "ColumnKey");
            // 
            // textColumnHeader
            // 
            textColumnHeader.Text = "Text";
            textColumnHeader.Width = 380;
            translationProvider1.SetTranslationKey(textColumnHeader, "ColumnText");
            // 
            // copyrightLabel
            // 
            copyrightLabel.AutoSize = true;
            copyrightLabel.Location = new System.Drawing.Point(12, 328);
            copyrightLabel.Name = "copyrightLabel";
            copyrightLabel.Size = new System.Drawing.Size(0, 15);
            copyrightLabel.TabIndex = 9;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(584, 361);
            Controls.Add(copyrightLabel);
            Controls.Add(translationsListView);
            Controls.Add(helloNameLabel);
            Controls.Add(nameTextBox);
            Controls.Add(nameLabel);
            Controls.Add(languageComboBox);
            Controls.Add(languageLabel);
            Controls.Add(helloLabel);
            Controls.Add(welcomeLabel);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Tlumach Windows Forms Sample";
            translationProvider1.SetTranslationKey(this, "Welcome");
            ((System.ComponentModel.ISupportInitialize)translationProvider1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Tlumach.WinForms.TranslationProvider translationProvider1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.Label welcomeLabel;
        private System.Windows.Forms.Label helloLabel;
        private System.Windows.Forms.Label languageLabel;
        private System.Windows.Forms.ComboBox languageComboBox;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label helloNameLabel;
        private System.Windows.Forms.ListView translationsListView;
        private System.Windows.Forms.ColumnHeader keyColumnHeader;
        private System.Windows.Forms.ColumnHeader textColumnHeader;
        private System.Windows.Forms.Label copyrightLabel;
    }
}
```

- [ ] **Step 5: Create the form code**

`samples/Tlumach.Sample.WinForms/MainForm.cs` (header, then):

```csharp
using System.Globalization;

using Tlumach.WinForms;

namespace Tlumach.Sample.WinForms
{
    /// <summary>
    /// The main form of the sample.
    /// <para>Most texts are assigned in the Designer: the translationProvider1 component adds the "TranslationKey on translationProvider1"
    /// and "ToolTipKey on translationProvider1" properties to the controls, menu items, and list view columns.
    /// Texts with placeholders are bound in code using the BindTranslation extension methods.</para>
    /// </summary>
    public partial class MainForm : Form
    {
        public MainForm()
        {
            // The texts assigned in the Designer are applied at the end of InitializeComponent,
            // because Program.Main has set TranslationProvider.DefaultTranslationManager.
            InitializeComponent();

            // Code-first bindings. The values of placeholders are cached in the units; the bindings update the labels when the language changes.
            // The bindings are disposed of automatically together with the labels.
            Strings.HelloName.CachePlaceholderValue("name", nameTextBox.Text);
            helloNameLabel.BindTranslation(Strings.HelloName);

            Strings.Copyright.CachePlaceholderValue("year", DateTime.Now.Year);
            copyrightLabel.BindTranslation(Strings.Copyright);

            FillLanguages();
        }

        private void FillLanguages()
        {
            // Neither ListCulturesInConfiguration nor ListTranslationFiles include the default translation, so it is added explicitly,
            // together with "system" (the culture of the operating system) and English (the language of the default translation).
            languageComboBox.Items.Add(new LanguageItem(CultureInfo.InvariantCulture));
            languageComboBox.Items.Add(new LanguageItem(null));
            languageComboBox.Items.Add(new LanguageItem(new CultureInfo("en")));

            foreach (string locale in Strings.TranslationManager.ListCulturesInConfiguration())
            {
                try
                {
                    languageComboBox.Items.Add(new LanguageItem(new CultureInfo(locale)));
                }
                catch (CultureNotFoundException)
                {
                    // A locale unknown to this system is skipped.
                }
            }

            // Selecting an item raises SelectedIndexChanged, which sets the culture and fills the list.
            languageComboBox.SelectedIndex = 0;
        }

        private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (languageComboBox.SelectedItem is LanguageItem item)
            {
                // The translation manager raises OnCultureChanged; translationProvider1 and the bindings update the controls.
                Strings.TranslationManager.CurrentCulture = item.Culture ?? CultureInfo.CurrentCulture;
                RefreshTranslationsList();
            }
        }

        private void NameTextBox_TextChanged(object? sender, EventArgs e)
        {
            Strings.HelloName.CachePlaceholderValue("name", nameTextBox.Text);

            // The binding of helloNameLabel listens to this notification and re-reads the text.
            Strings.HelloName.NotifyPlaceholdersUpdated();
        }

        private void RefreshTranslationsList()
        {
            // List view items are not components, so the form updates their texts itself.
            translationsListView.BeginUpdate();
            translationsListView.Items.Clear();
            translationsListView.Items.Add(new ListViewItem(new[] { nameof(Strings.Hello), Strings.Hello.CurrentValue }));
            translationsListView.Items.Add(new ListViewItem(new[] { nameof(Strings.Welcome), Strings.Welcome.CurrentValue }));
            translationsListView.EndUpdate();
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e) => Close();

        // An item of the language selection box.
        private sealed class LanguageItem
        {
            public LanguageItem(CultureInfo? culture) => Culture = culture;

            public CultureInfo? Culture { get; }

            public override string ToString()
            {
                if (Culture is null)
                    return "(system)";

                if (Culture.Equals(CultureInfo.InvariantCulture))
                    return "(default)";

                return $"{Culture.EnglishName} ({Culture.NativeName})";
            }
        }
    }
}
```

- [ ] **Step 6: Create the sample solution**

```bash
cd samples/Tlumach.Sample.WinForms
dotnet new sln --format sln -n Tlumach.Sample.WinForms
dotnet sln Tlumach.Sample.WinForms.sln add Tlumach.Sample.WinForms.csproj ../Tlumach.Sample.Translation/Tlumach.Sample.Translation.csproj ../../src/Tlumach.Base/Tlumach.Base.csproj ../../src/Tlumach/Tlumach.csproj ../../src/Tlumach.Generator/Tlumach.Generator.csproj ../../src/Tlumach.WinForms/Tlumach.WinForms.csproj
cd ../..
```

Expected: six `Project ... added to the solution` lines. (The WPF sample's `.sln` holds the same set with `Tlumach.WPF` instead of `Tlumach.WinForms`.)

- [ ] **Step 7: Build the sample and the WPF sample (shared translation project)**

```bash
dotnet build samples/Tlumach.Sample.WinForms/Tlumach.Sample.WinForms.sln
dotnet build samples/Tlumach.Sample.WPF/Tlumach.Sample.WPF.csproj
```

Expected: both `Build succeeded`. Analyzer warnings from `MainForm.Designer.cs` should not appear (Roslyn treats `*.Designer.cs` as generated code); fix any warning reported for `MainForm.cs` or `Program.cs`.

- [ ] **Step 8: Run the sample and check it by hand**

```bash
dotnet run --project samples/Tlumach.Sample.WinForms/Tlumach.Sample.WinForms.csproj
```

Check: the window title and big label read "Welcome", menu "File > Exit" with the tooltip "Close the application", the list shows the "Key"/"Text" columns, "Hello World" appears below the name box and changes as you type, the copyright line shows the current year. Select "German (Deutsch)": every text switches to German live (menu, labels, tooltip of the combo box, column headers, list rows, "Hallo, World"). Select "(default)" to return to English. Close via File > Exit.

- [ ] **Step 9: Commit**

```bash
git add samples/Tlumach.Sample.Translation samples/Tlumach.Sample.WinForms
git commit -m "Add the Windows Forms sample

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Documentation

**Files:**
- Create: `docs/articles/getting-started-winforms.md`
- Modify: `docs/articles/index.md:25-26` (ways to use), `docs/articles/index.md:36-41` (getting-started list)
- Modify: `README.md:25`, `README.nuget.md:14` (feature bullets)
- Modify: `CHANGELOG.md` (Version 1.13.0 section; 1.13.0 is unreleased, the latest tag is v1.12.0)
- Modify: `CLAUDE.md` (repository layout)

**Interfaces:**
- Consumes: public API names from Tasks 1–2; package layout from Task 3; sample from Task 4.
- Produces: docs only.

- [ ] **Step 1: Write the getting-started article**

`docs/articles/getting-started-winforms.md`:

````markdown
# Getting Started

## Integration with Windows Forms

Tlumach supports Windows Forms applications on .NET Framework 4.7.2 and later, .NET 9, and .NET 10 through the _Tlumach.WinForms_ assembly. It offers two ways to localize the UI, which can be combined:

* the **TranslationProvider** component, which you place on a form in the Visual Studio Designer and which adds the translation key properties to controls, menu and toolbar items, and list view column headers;
* the **BindTranslation** extension methods, which bind properties of controls to [generated translation units](glossary.md#GeneratedUnit) in code.

In both cases, the texts are updated when the current language is switched, even if the language is switched in a background thread.

**1. Add Tlumach to your project**:

a) via NuGet

Add a package reference to "Tlumach" to your project

* via NuGet package manager GUI in Visual Studio

* via the command line:

```cmd
dotnet add package Tlumach
```

* using the text editor - add the following reference to your project:
```xml
<ItemGroup>
    <PackageReference Include="Tlumach" Version="1.*" />
</ItemGroup>
```

b) with Source Code

- Check out Tlumach from the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net)
- Add _Tlumach.Base_, _Tlumach_, and _Tlumach.WinForms_ projects to your solution and reference them from your project(s).

**2. Create a configuration file, a default translation file, and a translation project**

These steps are the same as for other application types. Follow steps 2 to 7 of [Getting Started for integration with WPF](getting-started-wpf.md): they create "strings.cfg" with `generatedClass=Strings` and `generatedNamespace=Tlumach.Sample`, a "strings.toml" file with the `hello` key, and a translation project that you reference from your Windows Forms project.

After the translation project is built, the generated class `Tlumach.Sample.Strings` contains the `TranslationManager` property and a [translation unit](glossary.md#GeneratedUnit) for every key.

**3. Tell Tlumach.WinForms which translation manager to use**

Set the default translation manager once, before the first form is created:

```c#
using Tlumach.WinForms;
using Tlumach.Sample;

[STAThread]
static void Main()
{
    ApplicationConfiguration.Initialize();

    TranslationProvider.DefaultTranslationManager = Strings.TranslationManager;

    Application.Run(new MainForm());
}
```

If different forms use different generated classes, set the `TranslationManager` property of the provider on a form instead (e.g., right after the call to `InitializeComponent()` in the constructor of the form). This property takes priority over `DefaultTranslationManager`.

**4. Localize forms in the Designer**

1. Build the solution, so that Visual Studio picks up the `TranslationProvider` component and shows it in the Toolbox.
2. Drop a `TranslationProvider` onto the form. It appears in the component tray as "translationProvider1".
3. Select a control, a menu item, or a list view column. In the Properties window, the "Localization" category now contains **TranslationKey on translationProvider1**. Enter the key of the string (e.g., `hello`). Use the full key with dots for keys in groups (e.g., `menu.file`).
4. To translate tooltips, drop a `ToolTip` component onto the form and select it in the **ToolTip** property of the provider. Then enter keys in **ToolTipKey on translationProvider1** of the controls. Menu and toolbar items do not need a `ToolTip` component: the provider sets their `ToolTipText` property.

The Designer keeps showing the texts that you typed. At run time, the provider replaces them with the translations when the form is initialized and every time the language changes. If a key is not found in the translation, the text from the Designer is kept.

For the form's caption, assign a key to the form itself.

The provider resolves keys as strings, so placeholders in such strings are not processed. For strings with placeholders, use the binding in code (see below).

**5. Localize forms in code**

The `BindTranslation` extension methods bind a property to a generated translation unit and keep it updated:

```c#
using Tlumach.WinForms;
using Tlumach.Sample;

helloLabel.BindTranslation(Strings.hello);                           // Control.Text
fileMenuItem.BindTranslation(Strings.menu.file);                     // ToolStripItem.Text
nameColumn.BindTranslation(Strings.columns.name);                    // ColumnHeader.Text
toolTip1.BindTranslation(saveButton, Strings.hints.save);            // the tooltip of a control
searchBox.BindTranslation(Strings.hints.search, (box, text) => box.PlaceholderText = text); // any property
```

A binding is disposed of together with its control. To stop the updates earlier, dispose of the `TranslationBinding` object returned by the method.

Bindings work with units that contain placeholders. Provide the placeholder values via the `OnPlaceholderValueNeeded` event of the unit or cache them, and call `NotifyPlaceholdersUpdated()` when the values change:

```c#
Strings.helloName.CachePlaceholderValue("name", nameTextBox.Text);
helloNameLabel.BindTranslation(Strings.helloName);

nameTextBox.TextChanged += (sender, e) =>
{
    Strings.helloName.CachePlaceholderValue("name", nameTextBox.Text);
    Strings.helloName.NotifyPlaceholdersUpdated();
};
```

**Switching languages**

To switch the current language, assign a new value to <xref:Tlumach.TranslationManager.CurrentCulture>:

```c#
using Tlumach.Sample;
...
CultureInfo deCulture = new CultureInfo("de-DE");
Strings.TranslationManager.CurrentCulture = deCulture;
```

The providers and the bindings update the controls on the UI thread, so the culture may be changed in any thread.

Remember that you need [locale-specific files](glossary.md#LocaleSpecificFile) for other languages. For this, read about [Translation Files and Formats](files-formats.md).

**Right-to-left languages**

Set the **ApplyRightToLeft** property of the provider to `true` to switch the form to the right-to-left layout when the current culture is written from right to left (e.g., Arabic or Hebrew). The provider then sets the `RightToLeft` property of the form (its **ContainerControl** property, which the Designer sets automatically) and its `RightToLeftLayout` property.

**Sample**

The [Windows Forms sample](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples/Tlumach.Sample.WinForms) shows both ways of localization and a language selector.
````

- [ ] **Step 2: Update `docs/articles/index.md`**

Replace lines 25–26 (items 1 and 2 of "The Ways to Use Tlumach"):

```markdown
1. XAML-based desktop and mobile .NET applications, use in XAML UIs. You can bind XAML attributes to translation units as shown below. This way, when the language is switched in the translation manager, UI elements get updated automatically.
2. Websites and Web, console, and server applications which output the text from code or web files, as well as WinForms applications. There, you can access generated <xref:Tlumach.TranslationUnit> instances in code to pick the text for current or specific locale. The use of generated <xref:Tlumach.TranslationUnit> objects ensures that there is no mistake made when referencing the text.
```

with:

```markdown
1. XAML-based desktop and mobile .NET applications, use in XAML UIs. You can bind XAML attributes to translation units as shown below. This way, when the language is switched in the translation manager, UI elements get updated automatically.
2. Windows Forms applications. You can assign translation keys to controls in the Visual Studio Designer using the `TranslationProvider` component or bind controls to translation units in code. In both cases, the controls get updated automatically when the language is switched.
3. Websites and Web, console, and server applications which output the text from code or web files. There, you can access generated <xref:Tlumach.TranslationUnit> instances in code to pick the text for current or specific locale. The use of generated <xref:Tlumach.TranslationUnit> objects ensures that there is no mistake made when referencing the text.
```

and renumber the following item from `3.` to `4.`.

In the getting-started list, after the line `- [Getting Started for integration with Avalonia](getting-started-avalonia.md)`, add:

```markdown
- [Getting Started for integration with Windows Forms](getting-started-winforms.md)
```

and replace

```markdown
- [Getting Started for work with generated translation units](getting-started-manual.md) (recommended for web, server, and console applications, as well as WinForms applications)
```

with

```markdown
- [Getting Started for work with generated translation units](getting-started-manual.md) (recommended for web, server, and console applications)
```

- [ ] **Step 3: Update `README.md` and `README.nuget.md`**

In both files, after the bullet that starts with `* Integration with XAML (in WPF, UWP, WinUI, Uno Platform, MAUI, and Avalonia projects)`, add:

```markdown
* Integration with Windows Forms: the `TranslationProvider` component adds translation keys to controls, menu items, and list view columns in the Visual Studio Designer, and the `BindTranslation` methods bind controls to translation units in code. The controls are updated when the language changes.
```

- [ ] **Step 4: Update `CHANGELOG.md`**

Insert as the first bullet of the `Version: 1.13.0` section (right after the `Date:` line and its blank line):

```markdown
- [NEW] Windows Forms applications are supported through the new `Tlumach.WinForms` assembly, built for .NET Framework 4.7.2, .NET 9, and .NET 10. The `TranslationProvider` component, placed on a form in the Visual Studio Designer, adds the **TranslationKey** and **ToolTipKey** properties to controls, tool strip items, and list view column headers and updates their texts when `TranslationManager.CurrentCulture` changes, also when it changes in a background thread; optionally, it switches the form to the right-to-left layout for right-to-left languages. The `BindTranslation` extension methods bind properties of components to translation units in code, including units with placeholders. The package now also has the `net472`, `net9.0-windows7.0`, and `net10.0-windows7.0` folders; the latter two are used by applications that target `net9.0-windows` or `net10.0-windows` without a Windows SDK version (the default for Windows Forms and WPF applications), which previously got the plain `net9.0`/`net10.0` assemblies and, as a result, no `Tlumach.WPF` assembly. A new sample, `samples/Tlumach.Sample.WinForms`, shows both ways of localization. See "Getting Started for integration with Windows Forms".
```

- [ ] **Step 5: Update `CLAUDE.md`**

In the "Repository Layout" block, after the line `  Tlumach.UWP/                  # UWP-specific integration`, add:

```text
  Tlumach.WinForms/             # Windows Forms integration (TranslationProvider, BindTranslation)
```

and after the line `  Tlumach.GeneratorTests/       # Generator-specific tests`, add:

```text
  Tlumach.WinFormsTests/        # Windows-only tests of Tlumach.WinForms (not run in CI)
```

In the "Tests" section, after the `dotnet test tests/Tlumach.Tests.sln` code block, add:

````markdown
```bash
# Windows Forms integration tests (Windows only; not part of the ubuntu CI)
dotnet test tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj
```
````

- [ ] **Step 6: Proofread**

```bash
grep -n "WinForms" docs/articles/index.md README.md README.nuget.md CLAUDE.md | head -20
grep -c "getting-started-winforms.md" docs/articles/index.md
```

Expected: the new wording appears in every file; count `1`. No remaining text in `index.md` says WinForms should use generated units from code only.

- [ ] **Step 7: Commit**

```bash
git add docs/articles/getting-started-winforms.md docs/articles/index.md README.md README.nuget.md CHANGELOG.md CLAUDE.md
git commit -m "Document Windows Forms support

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Final verification

**Files:** none created; fixes go to the files of the task that introduced the problem.

**Interfaces:** consumes everything above.

- [ ] **Step 1: Build the full solution**

```bash
dotnet build src/Tlumach.sln
```

Expected: `Build succeeded`. The full solution needs the platform SDKs/workloads of the other XAML integrations (MAUI, UWP, WinUI). If a project other than `Tlumach.WinForms` fails because of a missing workload or SDK, that failure is pre-existing: confirm that `dotnet build src/Tlumach.WinForms/Tlumach.WinForms.csproj` succeeds, report the unrelated failure to the user with its output, and continue.

- [ ] **Step 2: Build what the CI builds and run the CI tests**

```bash
dotnet build src/Tlumach.Main.sln
dotnet test tests/Tlumach.Tests/Tlumach.Tests.csproj -c Release
dotnet test tests/Tlumach.GeneratorTests/Tlumach.GeneratorTests.csproj -c Release
```

Expected: build succeeds; all tests PASS (same counts as on `main`).

- [ ] **Step 3: Run the WinForms tests in Release**

```bash
dotnet test tests/Tlumach.WinFormsTests/Tlumach.WinFormsTests.csproj -c Release
```

Expected: 25 tests PASS.

- [ ] **Step 4: Confirm CI files are untouched**

```bash
git diff main --stat -- src/Tlumach.Main.sln .github/workflows/build-test.yml
```

Expected: no output.

- [ ] **Step 5: Update the knowledge graph**

```bash
graphify update .
```

Expected: completes without errors (AST-only update).

- [ ] **Step 6: Report**

Summarize for the user: test counts, build results, anything skipped or failing (with output), and remind them that nothing has been pushed.
