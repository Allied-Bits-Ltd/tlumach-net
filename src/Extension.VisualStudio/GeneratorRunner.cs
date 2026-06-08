// <copyright file="GeneratorRunner.cs" company="Allied Bits Ltd.">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using Microsoft.CodeAnalysis;

using Tlumach.Generator;

using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Exposes the protected <c>BaseGenerator.GenerateClass</c> for use outside the Roslyn pipeline.
/// This pattern mirrors the <c>TestGenerator</c> class in <c>Tlumach.GeneratorTests</c>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0048:File name must match type name",
    Justification = "BridgeGenerator is a private implementation detail of GeneratorRunner.")]
internal sealed class BridgeGenerator : BaseGenerator
{
    internal static string? InvokeGenerateClass(
        string configFilePath,
        string projectDir,
        Dictionary<string, string> options)
        => GenerateClass(configFilePath, projectDir, options);
}

/// <summary>
/// Orchestrates finding Tlumach config files in VS projects, running the in-process
/// generator driver (which populates <c>KeyIndex</c> for navigation), and forcing the
/// VS toolchain to re-run its own analyzer copy so the compiler / IntelliSense pick up
/// fresh generator output. No files are written to the project directory; the toolchain
/// holds the only authoritative copy of the generated source.
/// </summary>
internal static class GeneratorRunner
{
    // Option key names expected by BaseGenerator (match constants in Generator.cs).
    private const string OptionUsingNamespace = "UsingNamespace";
    private const string OptionExtraParsers = "ExtraParsers";
    private const string OptionDelayedUnits = "DelayedUnitCreation";

    private const string SolutionFolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

    static GeneratorRunner()
    {
        // Initialize all built-in parsers so FileFormats has them registered
        // before GenerateClass is called. Mirrors Generator.InitializeParsers().
        ArbParser.Use();
        IniParser.Use();
        JsonParser.Use();
        ResxParser.Use();
        CsvParser.Use();
        TomlParser.Use();
        TsvParser.Use();
        XliffParser.Use();
        StringCatParser.Use();
    }

    // -------------------------------------------------------------------------
    // Public entry points
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs the in-process driver for <paramref name="project"/> (populating <c>KeyIndex</c>)
    /// and, when <paramref name="forceToolchainReload"/> is <see langword="true"/>, unloads
    /// and reloads the project so VS Roslyn invalidates its cached source-generator output
    /// and re-runs the toolchain's own copy of the analyzer.
    /// </summary>
    internal static async Task RunForProjectAsync(
        AsyncPackage package,
        Project project,
        bool forceToolchainReload = true,
        CancellationToken cancellationToken = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(package);
        OutputWindowHelper.Activate(pane);

        string projectName = project.Name;
        OutputWindowHelper.WriteLine(pane, $"--- Tlumach Generator: project '{projectName}' ---");
        OutputWindowHelper.WriteLine(pane, $"{DateTime.Now.ToString()}");

        string? projectDir = GetProjectDirectory(project);
        if (string.IsNullOrEmpty(projectDir))
        {
            OutputWindowHelper.WriteLine(pane, $"  ERROR: could not determine project directory for '{projectName}'.");
            return;
        }

        Dictionary<string, string> options = ReadProjectOptions(project, projectDir!);
        List<string> configFiles = CollectAdditionalTlumachFiles(project);

        if (configFiles.Count == 0)
        {
            OutputWindowHelper.WriteLine(pane, $"  No Tlumach configuration files found.");
            return;
        }

        int generated = 0;
        int errors = 0;

        cancellationToken.ThrowIfCancellationRequested();

        GeneratorDriverRunResult driverResult;
#pragma warning disable CA1031
        try
        {
            driverResult = GeneratorDriverRunner.Run(configFiles, options);
        }
        catch (Exception ex)
        {
            OutputWindowHelper.WriteLine(pane, $"  {DateTime.Now.ToString()} ERROR: generator driver failed: {ex.Message}");
            return;
        }
#pragma warning restore CA1031

        foreach (Diagnostic diag in driverResult.Diagnostics)
        {
            if (diag.Severity == DiagnosticSeverity.Error)
            {
                OutputWindowHelper.WriteLine(pane, $"  ERROR: {diag.GetMessage()}");
                errors++;
            }
        }

        foreach (GeneratorRunResult genResult in driverResult.Results)
        {
            if (genResult.Exception is not null)
            {
                OutputWindowHelper.WriteLine(pane,
                    $"  ERROR: generator threw: {genResult.Exception.Message}");
                errors++;
                continue;
            }

            foreach (GeneratedSourceResult source in genResult.GeneratedSources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OutputWindowHelper.WriteLine(pane, $"  Generated (in-memory): {source.HintName}");
                generated++;
            }
        }

        if (forceToolchainReload && errors == 0 && generated > 0)
        {
            await ReloadProjectAsync(project, pane, cancellationToken).ConfigureAwait(true);
        }

        OutputWindowHelper.WriteLine(pane,
            $"--- Done: {generated} unit(s) generated, {errors} error(s). ---");
    }

    internal static async Task RunForAllProjectsAsync(
        AsyncPackage package,
        DTE2 dte,
        bool forceToolchainReload = true,
        CancellationToken cancellationToken = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(package);
        OutputWindowHelper.Activate(pane);
        OutputWindowHelper.WriteLine(pane, "=== Tlumach Generator: all projects ===");

        int count = 0;
        foreach (Project project in EnumerateAllProjects(dte.Solution.Projects))
        {
            cancellationToken.ThrowIfCancellationRequested();
            count++;
            await RunForProjectAsync(package, project, forceToolchainReload, cancellationToken).ConfigureAwait(true);
        }

        if (count == 0)
            OutputWindowHelper.WriteLine(pane, "No projects found in solution.");

        OutputWindowHelper.WriteLine(pane, "=== Tlumach Generator: complete ===");
    }

    // -------------------------------------------------------------------------
    // Toolchain refresh — unload + reload the project so VS Roslyn drops the
    // cached source-generator output and re-runs the analyzer (the package's
    // own copy of Tlumach.Generator.dll) against the real Compilation.
    // -------------------------------------------------------------------------

    private static async Task ReloadProjectAsync(
        Project project,
        IVsOutputWindowPane pane,
        CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
        var solution4 = solution as IVsSolution4;
        if (solution is null || solution4 is null)
        {
            OutputWindowHelper.WriteLine(pane, "  WARNING: IVsSolution4 unavailable; toolchain output not refreshed.");
            return;
        }

#pragma warning disable CA1031
        try
        {
            int hr = solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy? hierarchy);
            if (hr != VSConstants.S_OK || hierarchy is null)
            {
                OutputWindowHelper.WriteLine(pane, "  WARNING: project hierarchy not found; toolchain output not refreshed.");
                return;
            }

            hr = solution.GetGuidOfProject(hierarchy, out Guid projectGuid);
            if (hr != VSConstants.S_OK)
            {
                OutputWindowHelper.WriteLine(pane, "  WARNING: project Guid not available; toolchain output not refreshed.");
                return;
            }

            // Capture the DTE reference before unloading — project.DTE may be
            // inaccessible once the project is zombied by UnloadProject.
            DTE dte = (DTE)project.DTE;
            List<DocumentState> openBefore = SnapshotOpenDocuments(dte);

            // Yield the UI thread so any in-flight background Roslyn analysis tasks
            // (e.g. third-party providers such as DevExpress DeclareProvider) can
            // complete their current work-item before UnloadProject replaces every
            // DocumentId in the project. Those providers hold a workspace snapshot;
            // if they try to remove a document they added to that snapshot after the
            // reload has already replaced all DocumentIds, Roslyn throws
            // InvalidOperationException from TextDocumentStates.GetRequiredState.
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // UnloadProject + ReloadProject does not modify the .csproj on disk; it
            // forces VS to drop the in-memory project model (and Roslyn's cached
            // generator-driver state) and rebuild it from the file. The compiler /
            // IntelliSense then re-runs the toolchain analyzer, picking up any
            // changes to AdditionalFiles since the project was last loaded.
            uint unloadStatus = (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser;
            hr = solution4.UnloadProject(ref projectGuid, unloadStatus);
            if (hr != VSConstants.S_OK)
            {
                OutputWindowHelper.WriteLine(pane, $"  WARNING: UnloadProject failed (hr=0x{hr:X}); toolchain output not refreshed.");
                return;
            }

            hr = solution4.ReloadProject(ref projectGuid);
            if (hr != VSConstants.S_OK)
            {
                OutputWindowHelper.WriteLine(pane, $"  WARNING: ReloadProject failed (hr=0x{hr:X}); project left unloaded.");
                return;
            }

            // Restore the document state that VS altered during unload/reload:
            //   - close any documents VS auto-opened (e.g. the .csproj in the XML editor)
            //   - reopen any documents VS closed (e.g. open translation files)
            RestoreDocumentState(dte, openBefore, pane);

            OutputWindowHelper.WriteLine(pane, "  Toolchain output refreshed (project reloaded).");
        }
        catch (Exception ex)
        {
            OutputWindowHelper.WriteLine(pane, $"  WARNING: toolchain refresh failed: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    // -------------------------------------------------------------------------
    // Document-state snapshot / restore
    // -------------------------------------------------------------------------

    /// <summary>
    /// Captures per-document editor state (cursor, scroll, active flag) for all
    /// documents currently open in the IDE.
    /// </summary>
    private sealed class DocumentState
    {
        internal string Path { get; }
        internal bool IsActive { get; }
        internal int CursorLine { get; }    // 1-based
        internal int CursorColumn { get; }  // 1-based display column
        internal int TopLine { get; }       // 0-based first visible line

        internal DocumentState(string path, bool isActive, int cursorLine, int cursorColumn, int topLine)
        {
            Path = path;
            IsActive = isActive;
            CursorLine = cursorLine;
            CursorColumn = cursorColumn;
            TopLine = topLine;
        }
    }

    /// <summary>
    /// Snapshots the open documents together with their cursor and scroll positions.
    /// </summary>
    private static List<DocumentState> SnapshotOpenDocuments(DTE dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string? activeDocPath = null;
#pragma warning disable CA1031
        try { activeDocPath = dte.ActiveDocument?.FullName; }
        catch (Exception ex) { Debug.WriteLine($"Tlumach: ActiveDocument unavailable: {ex.Message}"); }

        var states = new List<DocumentState>();
        foreach (Document doc in dte.Documents)
        {
            try
            {
                string fullName = doc.FullName;
                if (string.IsNullOrEmpty(fullName))
                    continue;

                bool isActive = string.Equals(fullName, activeDocPath, StringComparison.OrdinalIgnoreCase);
                int cursorLine = 1, cursorCol = 1, topLine = 0;

                try
                {
                    if (doc.Object("TextDocument") is TextDocument textDoc)
                    {
                        TextPoint pt = textDoc.Selection.ActivePoint;
                        cursorLine = pt.Line;
                        cursorCol = pt.DisplayColumn;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Tlumach: cursor snapshot failed for '{fullName}': {ex.Message}");
                }

                try
                {
                    if (TryGetTextView(fullName) is IVsTextView view)
                        view.GetScrollInfo(1 /* SB_VERT */, out _, out _, out _, out topLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Tlumach: scroll snapshot failed for '{fullName}': {ex.Message}");
                }

                states.Add(new DocumentState(fullName, isActive, cursorLine, cursorCol, topLine));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: SnapshotOpenDocuments skipped doc: {ex.Message}");
            }
        }
#pragma warning restore CA1031
        return states;
    }

    /// <summary>
    /// Closes documents VS opened during reload, reopens documents VS closed during unload,
    /// restores cursor and scroll for each reopened document, and reactivates the previously
    /// active document.
    /// </summary>
    private static void RestoreDocumentState(
        DTE dte,
        List<DocumentState> openBefore,
        IVsOutputWindowPane pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // Build a lookup of the expected open set.
        var openBeforePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DocumentState s in openBefore)
            openBeforePaths.Add(s.Path);

        // Snapshot current open documents after reload.
        var openAfter = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase);
#pragma warning disable CA1031
        foreach (Document doc in dte.Documents)
        {
            try
            {
                string fullName = doc.FullName;
                if (!string.IsNullOrEmpty(fullName))
                    openAfter[fullName] = doc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: RestoreDocumentState (after) skipped doc: {ex.Message}");
            }
        }

        // Close documents VS auto-opened during reload (e.g. the .csproj in the XML editor).
        foreach (KeyValuePair<string, Document> kvp in openAfter)
        {
            if (!openBeforePaths.Contains(kvp.Key))
            {
                try
                {
                    kvp.Value.Close(vsSaveChanges.vsSaveChangesNo);
                }
                catch (Exception ex)
                {
                    OutputWindowHelper.WriteLine(pane, $"  WARNING: could not close auto-opened document '{kvp.Key}': {ex.Message}");
                }
            }
        }

        // Reopen documents VS closed during unload and restore their view state.
        DocumentState? activeState = null;
        foreach (DocumentState state in openBefore)
        {
            if (state.IsActive)
                activeState = state;

            if (!openAfter.ContainsKey(state.Path))
            {
                if (!File.Exists(state.Path))
                    continue;

                try
                {
                    dte.ItemOperations.OpenFile(state.Path);
                    RestoreViewState(dte, state);
                }
                catch (Exception ex)
                {
                    OutputWindowHelper.WriteLine(pane, $"  WARNING: could not reopen document '{state.Path}': {ex.Message}");
                }
            }
        }

        // Reactivate the document that had focus before the reload.
        if (activeState is not null)
        {
            try
            {
                foreach (Document doc in dte.Documents)
                {
                    try
                    {
                        if (string.Equals(doc.FullName, activeState.Path, StringComparison.OrdinalIgnoreCase))
                        {
                            doc.Activate();
                            break;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: could not reactivate '{activeState.Path}': {ex.Message}");
            }
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Restores the cursor position and topmost visible line for a document that was just reopened.
    /// </summary>
    private static void RestoreViewState(DTE dte, DocumentState state)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        // Restore cursor.
        try
        {
            foreach (Document doc in dte.Documents)
            {
                try
                {
                    if (!string.Equals(doc.FullName, state.Path, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (doc.Object("TextDocument") is TextDocument textDoc)
                        textDoc.Selection.MoveToLineAndOffset(state.CursorLine, state.CursorColumn, false);

                    break;
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tlumach: cursor restore failed for '{state.Path}': {ex.Message}");
        }

        // Restore scroll (topmost visible line).
        try
        {
            if (TryGetTextView(state.Path) is IVsTextView view)
                view.SetScrollPosition(1 /* SB_VERT */, state.TopLine);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tlumach: scroll restore failed for '{state.Path}': {ex.Message}");
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Returns the <see cref="IVsTextView"/> for the primary pane of an open document,
    /// or <see langword="null"/> if the document is not open or does not have a text view.
    /// </summary>
    private static IVsTextView? TryGetTextView(string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
        if (uiShell is null)
            return null;

        uiShell.GetDocumentWindowEnum(out IEnumWindowFrames? enumFrames);
        if (enumFrames is null)
            return null;

        var frames = new IVsWindowFrame[1];
#pragma warning disable CA1031
        while (enumFrames.Next(1, frames, out uint fetched) == VSConstants.S_OK && fetched > 0)
        {
            try
            {
                frames[0].GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out object? moniker);
                if (!string.Equals(moniker as string, filePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                frames[0].GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out object? docView);
                if (docView is IVsTextView directView)
                    return directView;

                if (docView is IVsCodeWindow codeWindow)
                {
                    codeWindow.GetPrimaryView(out IVsTextView? primaryView);
                    return primaryView;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: TryGetTextView skipped frame: {ex.Message}");
            }
        }
#pragma warning restore CA1031
        return null;
    }

    // -------------------------------------------------------------------------
    // File enumeration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns absolute paths of all items in <paramref name="project"/> that have
    /// <c>ItemType == "AdditionalFiles"</c> and a known Tlumach config extension.
    /// </summary>
    /// <param name="project">The VS project to scan.</param>
    /// <returns>List of absolute file paths.</returns>
    internal static List<string> CollectAdditionalTlumachFiles(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var result = new List<string>();
        CollectItemsRecursive(project.ProjectItems, result);
        return result;
    }

    private static void CollectItemsRecursive(ProjectItems? items, List<string> result)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (items is null)
            return;

        foreach (ProjectItem item in items)
        {
#pragma warning disable CA1031 // COM properties may throw for unloaded or virtual items
            try
            {
                string? itemType = TryGetPropertyValue(item.Properties, "ItemType");
                if (string.Equals(itemType, "AdditionalFiles", StringComparison.Ordinal)
                    && item.FileCount > 0)
                {
                    string? filePath = item.FileNames[1]; // COM collection is 1-based
                    if (!string.IsNullOrEmpty(filePath) && IsTlumachConfigFile(filePath))
                        result.Add(filePath);
                }

                if (item.ProjectItems?.Count > 0)
                    CollectItemsRecursive(item.ProjectItems, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: skipped project item: {ex.Message}");
            }
#pragma warning restore CA1031
        }
    }

    private static bool IsTlumachConfigFile(string filePath) => FileFormats.HasConfigParser(Path.GetExtension(filePath));

    // -------------------------------------------------------------------------
    // MSBuild option reading
    // -------------------------------------------------------------------------

    private static Dictionary<string, string> ReadProjectOptions(Project project, string projectDir)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // BaseGenerator uses projectdir for relative-path calculation
            ["projectdir"] = projectDir,
        };

        IVsHierarchy? hierarchy = GetHierarchy(project);
        if (hierarchy is IVsBuildPropertyStorage bps)
        {
            TryAddMsBuildProperty(bps, "TlumachGeneratorUsingNamespace", OptionUsingNamespace, options);
            TryAddMsBuildProperty(bps, "TlumachGeneratorExtraParsers", OptionExtraParsers, options);
            TryAddMsBuildProperty(bps, "TlumachGeneratorDelayedUnitCreation", OptionDelayedUnits, options);
        }

        return options;
    }

    private static void TryAddMsBuildProperty(
        IVsBuildPropertyStorage bps,
        string msBuildName,
        string optionKey,
        Dictionary<string, string> options)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        int hr = bps.GetPropertyValue(
            msBuildName,
            null,
            (uint)_PersistStorageType.PST_PROJECT_FILE,
            out string? value);

        if (hr == 0 && !string.IsNullOrEmpty(value))
            options[optionKey] = value;
    }

    // -------------------------------------------------------------------------
    // DTE helpers
    // -------------------------------------------------------------------------

    private static string? GetProjectDirectory(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            string? fullName = project.FullName;
            if (!string.IsNullOrEmpty(fullName))
                return Path.GetDirectoryName(fullName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tlumach: FullName failed: {ex.Message}");
        }

        try
        {
            string? fullPath = TryGetPropertyValue(project.Properties, "FullPath");
            if (!string.IsNullOrEmpty(fullPath))
                return fullPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tlumach: FullPath failed: {ex.Message}");
        }
#pragma warning restore CA1031

        return null;
    }

    private static string? TryGetPropertyValue(Properties? props, string name)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (props is null)
            return null;

#pragma warning disable CA1031
        try
        {
            return props.Item(name)?.Value as string;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tlumach: property '{name}' unavailable: {ex.Message}");
            return null;
        }
#pragma warning restore CA1031
    }

    private static IVsHierarchy? GetHierarchy(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
        if (solution is null)
            return null;

        solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy? hierarchy);
        return hierarchy;
    }

    private static IEnumerable<Project> EnumerateAllProjects(Projects projects)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (Project project in projects)
        {
            if (string.Equals(project.Kind, SolutionFolderKind, StringComparison.OrdinalIgnoreCase))
            {
                foreach (Project sub in EnumerateSolutionFolderProjects(project))
                    yield return sub;
            }
            else
            {
                yield return project;
            }
        }
    }

    private static IEnumerable<Project> EnumerateSolutionFolderProjects(Project folder)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (ProjectItem item in folder.ProjectItems)
        {
            Project? sub = item.SubProject;
            if (sub is null)
                continue;

            if (string.Equals(sub.Kind, SolutionFolderKind, StringComparison.OrdinalIgnoreCase))
            {
                foreach (Project nested in EnumerateSolutionFolderProjects(sub))
                    yield return nested;
            }
            else
            {
                yield return sub;
            }
        }
    }
}
