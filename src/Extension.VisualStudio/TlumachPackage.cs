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
        string extensionDir = Path.GetDirectoryName(typeof(TlumachPackage).Assembly.Location)!;
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            string asmName = new AssemblyName(args.Name).Name!;
            string path = Path.Combine(extensionDir, asmName + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };

        await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

        Instance = this;

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var commandService = await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true)
            as OleMenuCommandService;

        if (commandService is not null)
            RegisterLegacyCommands(commandService);
    }

    private void RegisterLegacyCommands(OleMenuCommandService svc)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

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

    private void OnRunGeneratorQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var cmd = (OleMenuCommand)sender;
        var dte = GetService(typeof(DTE)) as DTE2;
        Project? project = TryGetSelectedProject(dte);
        cmd.Visible = project is not null && ProjectHelper.HasTlumachConfigFiles(project);
    }

    private void OnRunGenerator(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var dte = GetService(typeof(DTE)) as DTE2;
        Project? project = TryGetSelectedProject(dte);
        if (project is null)
            return;

        _ = Task.Run(() => GeneratorRunner.RunForProjectAsync(this, project));
    }

    private void OnRunAllGenerators(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var dte = GetService(typeof(DTE)) as DTE2;
        if (dte is null)
            return;

        _ = Task.Run(() => GeneratorRunner.RunForAllProjectsAsync(this, dte));
    }

    private void OnGoToTranslationDefinitionQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

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

    private void OnGoToTranslationDefinition(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

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

        _ = TranslationNavigator.NavigateToAsync(this, location);
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
