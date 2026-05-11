// <copyright file="TlumachPackage.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
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

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using AlliedBits.Tlumach.Extension.VisualStudio.Navigation;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Tlumach.Generator;

using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Tlumach.NET Generator Visual Studio package.
/// Hosts the MEF-based <see cref="Navigation.GoToTranslationCommandHandler"/> and provides
/// the <see cref="AsyncPackage"/> bridge for DTE-dependent services used by
/// <see cref="GeneratorRunner"/> and <see cref="Navigation.TranslationNavigator"/>.
/// Also registers old-SDK <see cref="OleMenuCommand"/> handlers so the three commands
/// appear in Solution Explorer and editor context menus.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(
    Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionHasMultipleProjects_string,
    PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(
    Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionHasSingleProject_string,
    PackageAutoLoadFlags.BackgroundLoad)]
// Adds the extension's install directory to the CLR assembly probing path so that
// Tlumach.Generator.dll and its dependencies (System.Text.Json, etc.) are resolved
// correctly when the VisualStudio.Extensibility runtime activates commands in-process.
[ProvideBindingPath]
public sealed class TlumachPackage : AsyncPackage
{
    /// <summary>Package GUID — must match source.extension.vsixmanifest.</summary>
    public const string PackageGuidString = "C3A5B7D1-E2F4-4A6B-8C9D-0E1F2A3B4C5D";

    private static readonly Guid CommandSetGuid = new("A1B2C3D4-E5F6-7890-ABCD-EF0123456789");

    private const int RunGeneratorCommandId                    = 0x0100;
    private const int RunAllGeneratorsCommandId                = 0x0101;
    private const int GoToTranslationDefinitionCommandId       = 0x0102;
    private const int RunGeneratorSubMenuId                    = 0x0103;
    private const int GoToTranslationDefinitionSubMenuId       = 0x0104;

    /// <summary>
    /// The single loaded instance of this package, available after <see cref="InitializeAsync"/> completes.
    /// Used by new-model commands to pass an <see cref="AsyncPackage"/> reference into VSSDK service methods.
    /// </summary>
    public static TlumachPackage? Instance { get; private set; }

    /// <inheritdoc />
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        // [ProvideBindingPath] is unreliable for in-process hosting; resolve bundled
        // assemblies directly from the extension directory before any other code runs.
        // Skip Extensibility runtime DLLs — let VS resolve those from its own load context
        // to avoid version conflicts with VS's in-process copies.
        string extensionDir = Path.GetDirectoryName(typeof(TlumachPackage).Assembly.Location)!;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            AssemblyName requested = new AssemblyName(args.Name);
            string asmName = requested.Name!;
            if (asmName.StartsWith("Microsoft.VisualStudio.Extensibility", StringComparison.Ordinal) ||
                asmName.StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal))
                return null;
            string path = Path.Combine(extensionDir, asmName + ".dll");
            if (!File.Exists(path))
            {
                Debug.WriteLine($"Tlumach AssemblyResolve: {asmName} → NOT FOUND");
                return null;
            }
            Debug.WriteLine($"Tlumach AssemblyResolve: {asmName} → FOUND at {path}");
            Assembly resolved = Assembly.LoadFrom(path);
            // Assembly.LoadFrom() matches by simple name (ignoring version) for weak-named
            // assemblies in the LoadFrom context. If Tlumach.Generator was already loaded
            // as a Roslyn analyzer from the NuGet cache at a different version, LoadFrom
            // returns that stale version instead of loading the VSIX-bundled one. Detect
            // this and fall back to loading from raw bytes, which bypasses the cache.
            if (requested.Version != null && resolved.GetName().Version != requested.Version)
            {
                Debug.WriteLine($"Tlumach AssemblyResolve: {asmName} version mismatch " +
                    $"(got {resolved.GetName().Version}, need {requested.Version}) — loading from bytes");
                resolved = Assembly.Load(File.ReadAllBytes(path));
            }
            return resolved;
        };

#pragma warning disable CA1031
        // Initialize the Extensibility runtime. On some VS versions the bundled runtime DLLs
        // may conflict with VS's own in-process copies and cause this call to throw. That is
        // acceptable — the VSSDK OleMenuCommand handlers below still work without it.
        Exception? baseInitError = null;
        try
        {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            baseInitError = ex;
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"base.InitializeAsync failed (VisualStudio.Extensibility runtime may not have initialised; " +
                $"VSSDK commands will still be registered): {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
        }

        Instance = this;

        // Register VSSDK OleMenuCommand handlers. This runs regardless of whether the
        // Extensibility runtime initialised successfully.
        try
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (baseInitError is not null)
                OutputWindowHelper.TryWriteLineOnUIThread(
                    $"Tlumach: VisualStudio.Extensibility runtime failed to initialise " +
                    $"({baseInitError.GetType().Name}: {baseInitError.Message}). " +
                    $"VSSDK commands (context menus) will still work.");

            var commandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true)
                as OleMenuCommandService;

            if (commandService is null)
            {
                const string msg = "Tlumach: OleMenuCommandService is null — context-menu commands not registered.";
                ActivityLog.TryLogError(nameof(TlumachPackage), msg);
                OutputWindowHelper.TryWriteLineOnUIThread(msg);
                return;
            }

            RegisterLegacyCommands(commandService);
        }
        catch (Exception ex)
        {
            string msg = $"Tlumach: VSSDK command registration failed: {ex.GetType().Name}: {ex.Message}";
            ActivityLog.TryLogError(nameof(TlumachPackage), $"{msg}{Environment.NewLine}{ex}");
            OutputWindowHelper.TryWriteLineOnUIThread(msg);
        }
#pragma warning restore CA1031
    }

    private void RegisterLegacyCommands(OleMenuCommandService svc)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            // RunGeneratorCommand — project context menu
            var runGen = new OleMenuCommand(OnRunGenerator, new CommandID(CommandSetGuid, RunGeneratorCommandId));
            runGen.BeforeQueryStatus += OnRunGeneratorQueryStatus;
            svc.AddCommand(runGen);

            // RunAllGeneratorsCommand — solution + project context menus (VSCT CommandPlacements handles dual placement)
            svc.AddCommand(new OleMenuCommand(OnRunAllGenerators, new CommandID(CommandSetGuid, RunAllGeneratorsCommandId)));

            // GoToTranslationDefinitionCommand — editor context menu
            var goTo = new OleMenuCommand(OnGoToTranslationDefinition, new CommandID(CommandSetGuid, GoToTranslationDefinitionCommandId));
            goTo.BeforeQueryStatus += OnGoToTranslationDefinitionQueryStatus;
            svc.AddCommand(goTo);

            // Submenu (Extensions > Tlumach) variants — always visible, same handlers as context-menu versions
            svc.AddCommand(new OleMenuCommand(OnRunGenerator, new CommandID(CommandSetGuid, RunGeneratorSubMenuId)));
            svc.AddCommand(new OleMenuCommand(OnGoToTranslationDefinition, new CommandID(CommandSetGuid, GoToTranslationDefinitionSubMenuId)));
        }
        catch (Exception ex)
        {
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"RegisterLegacyCommands failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
        }
#pragma warning restore CA1031
    }

    private void OnRunGeneratorQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            var cmd = (OleMenuCommand)sender;
            var dte = GetService(typeof(DTE)) as DTE2;
            Project? project = TryGetSelectedProject(dte);
            cmd.Visible = project is not null && ProjectHelper.HasTlumachConfigFiles(project);
        }
        catch (Exception ex)
        {
            ((OleMenuCommand)sender).Visible = false;
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"OnRunGeneratorQueryStatus failed: {ex.GetType().Name}: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    private void OnRunGenerator(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            var dte = GetService(typeof(DTE)) as DTE2;
            Project? project = TryGetSelectedProject(dte);
            if (project is null)
                return;

            JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await GeneratorRunner.RunForProjectAsync(this, project).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // VS is shutting down — ignore
                }
                catch (Exception ex)
                {
                    ActivityLog.TryLogError(
                        nameof(TlumachPackage),
                        $"RunForProjectAsync failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
                }
            });
        }
        catch (Exception ex)
        {
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"OnRunGenerator failed: {ex.GetType().Name}: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    private void OnRunAllGenerators(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            var dte = GetService(typeof(DTE)) as DTE2;
            if (dte is null)
                return;

            JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await GeneratorRunner.RunForAllProjectsAsync(this, dte).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // VS is shutting down — ignore
                }
                catch (Exception ex)
                {
                    ActivityLog.TryLogError(
                        nameof(TlumachPackage),
                        $"RunForAllProjectsAsync failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
                }
            });
        }
        catch (Exception ex)
        {
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"OnRunAllGenerators failed: {ex.GetType().Name}: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    private void OnGoToTranslationDefinitionQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            var cmd = (OleMenuCommand)sender;
            var dte = GetService(typeof(DTE)) as DTE2;
            if (dte is null)
            {
                cmd.Visible = false;
                return;
            }

            var (ns, className, identifier) = SymbolExtractor.ExtractSymbolFromDte(dte);
            cmd.Visible = !string.IsNullOrEmpty(identifier)
                && KeyIndex.IsPopulated
                && KeyIndex.FindDeclaration(ns, className, identifier!) is not null;
        }
        catch (Exception ex)
        {
            ((OleMenuCommand)sender).Visible = false;
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"OnGoToTranslationDefinitionQueryStatus failed: {ex.GetType().Name}: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    private void OnGoToTranslationDefinition(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            var dte = GetService(typeof(DTE)) as DTE2;
            if (dte is null)
                return;

            var (ns, className, identifier) = SymbolExtractor.ExtractSymbolFromDte(dte);
            if (string.IsNullOrEmpty(identifier))
                return;

            if (!KeyIndex.IsPopulated)
                ProjectHelper.RegenerateIndex();

            if (!KeyIndex.IsPopulated)
                return;

            KeyLocation? location = KeyIndex.FindDeclaration(ns, className, identifier!);
            if (location is null)
                return;

            JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await TranslationNavigator.NavigateToAsync(this, location).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // VS is shutting down — ignore
                }
                catch (Exception ex)
                {
                    ActivityLog.TryLogError(
                        nameof(TlumachPackage),
                        $"NavigateToAsync failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
                }
            });
        }
        catch (Exception ex)
        {
            ActivityLog.TryLogError(
                nameof(TlumachPackage),
                $"OnGoToTranslationDefinition failed: {ex.GetType().Name}: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    private static Project? TryGetSelectedProject(DTE2? dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            return dte?.SelectedItems?.Item(1)?.Project;
        }
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }
}
