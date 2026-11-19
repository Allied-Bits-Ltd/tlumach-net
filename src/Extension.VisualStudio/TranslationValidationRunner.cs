// <copyright file="TranslationValidationRunner.cs" company="Allied Bits Ltd.">
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tlumach.Validation;

using EnvDTE;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Validates the translation files of projects: lists the files of each project through DTE, runs <see cref="TranslationValidator"/> on a background thread, and reports the result in the output pane and in the Error List.
/// </summary>
internal static class TranslationValidationRunner
{
    private static int _running;

    /// <summary>
    /// Validates the translations of the given projects.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="projects">The projects to validate.</param>
    /// <param name="scope">A description of the projects for the output, such as "all projects".</param>
    /// <param name="cancellationToken">A token that cancels the validation.</param>
    /// <returns>A task.</returns>
    internal static async Task ValidateProjectsAsync(AsyncPackage package, IReadOnlyList<Project> projects, string? scope, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(package);
        OutputWindowHelper.Activate(pane);

        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            OutputWindowHelper.WriteLine(pane, "Tlumach: a validation of translations is already running.");
            return;
        }

        try
        {
            if (scope is not null)
                OutputWindowHelper.WriteLine(pane, $"=== Tlumach: validating the translations of {scope} ===");

            if (projects.Count == 0)
            {
                OutputWindowHelper.WriteLine(pane, "No projects found.");
                return;
            }

            List<ProjectValidationReport> reports = [];
            foreach (Project project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ProjectValidationReport? report = await ValidateProjectAsync(package, pane, project, cancellationToken).ConfigureAwait(true);
                if (report is not null)
                    reports.Add(report);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (reports.Count > 1)
            {
                using StringWriter totals = new();
                ValidationReportWriter.WriteTotals(totals, reports);
                WriteText(pane, totals.ToString());
            }

            if (reports.Exists(r => r.ErrorCount > 0 || r.WarningCount > 0))
                ValidationErrorList.Show();
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private static async Task<ProjectValidationReport?> ValidateProjectAsync(AsyncPackage package, IVsOutputWindowPane pane, Project project, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        string projectName = project.Name;
        OutputWindowHelper.WriteLine(pane, $"--- Tlumach: validating the translations of project '{projectName}' ---");

        string? projectPath = project.FullName;
        string? projectDirectory = GeneratorRunner.GetProjectDirectory(project);
        if (string.IsNullOrEmpty(projectPath) || string.IsNullOrEmpty(projectDirectory))
        {
            OutputWindowHelper.WriteLine(pane, $"  ERROR: could not determine the project file of '{projectName}'.");
            return null;
        }

        IVsHierarchy? hierarchy = GeneratorRunner.GetHierarchy(project);
        List<ProjectFile> items = CollectProjectItems(project, projectDirectory!, hierarchy);

        // Parsing can take a while; keep the UI responsive
        await TaskScheduler.Default;

        ProjectValidationReport report;
        string text;
#pragma warning disable CA1031 // A failure is reported in the output pane; the other projects are still validated
        try
        {
            report = TranslationValidator.ValidateProject(projectPath, items, cancellationToken);

            using StringWriter writer = new();
            ValidationReportWriter.WriteText(writer, report, note: null, message => message.ToString());
            text = writer.ToString();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            OutputWindowHelper.WriteLine(pane, $"  ERROR: the validation failed: {ex.GetType().Name}: {ex.Message}");
            ActivityLog.TryLogError(nameof(TranslationValidationRunner), $"ValidateProject failed for '{projectPath}': {ex}");
            return null;
        }
#pragma warning restore CA1031

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        WriteText(pane, text);
        ValidationErrorList.Replace(package, projectPath, hierarchy, report.Messages);
        return report;
    }

    /// <summary>
    /// Lists the files of a project with their item types. A file shown in another folder than the one where it is (a linked file) gets the path where it is shown.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="projectDirectory">The directory of the project.</param>
    /// <param name="hierarchy">The hierarchy of the project, used to read the metadata of embedded ResX files; may be <see langword="null"/>.</param>
    /// <returns>The files.</returns>
    internal static List<ProjectFile> CollectProjectItems(Project project, string projectDirectory, IVsHierarchy? hierarchy)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        List<ProjectFile> result = [];
        CollectItems(project.ProjectItems, string.Empty, projectDirectory, hierarchy, result);
        return result;
    }

    /// <summary>
    /// Reads the metadata that decides how MSBuild embeds a ResX file. Returns <see langword="null"/> when the project system does not provide them, which skips the check of the embedding.
    /// </summary>
    private static Dictionary<string, string>? ReadResxMetadata(IVsHierarchy? hierarchy, string path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (hierarchy is not IVsBuildPropertyStorage storage
            || hierarchy.ParseCanonicalName(path, out uint itemId) != Microsoft.VisualStudio.VSConstants.S_OK)
        {
            return null;
        }

        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
        bool supported = false;
        foreach (string name in new[] { "Type", "LogicalName", "WithCulture" })
        {
            if (storage.GetItemAttribute(itemId, name, out string? value) != Microsoft.VisualStudio.VSConstants.S_OK)
                continue;

            // A project system answers S_OK, with an empty value, for metadata that the item does not have
            supported = true;
            if (!string.IsNullOrEmpty(value))
                metadata[name] = value;
        }

        // When no attribute could be read, the metadata are unknown rather than empty
        return supported ? metadata : null;
    }

    private static void CollectItems(ProjectItems? items, string logicalFolder, string projectDirectory, IVsHierarchy? hierarchy, List<ProjectFile> result)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (items is null)
            return;

        foreach (EnvDTE.ProjectItem item in items)
        {
#pragma warning disable CA1031 // COM properties may throw for unloaded or virtual items
            try
            {
                string kind = item.Kind ?? string.Empty;
                string name = item.Name ?? string.Empty;

                if (string.Equals(kind, EnvDTE.Constants.vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    if (item.FileCount > 0)
                    {
                        string? path = item.FileNames[1]; // COM collection is 1-based
                        if (!string.IsNullOrEmpty(path))
                        {
                            string itemType = GeneratorRunner.TryGetPropertyValue(item.Properties, "ItemType") ?? string.Empty;
                            string logicalPath = Path.Combine(logicalFolder, name);
                            string shownAt = Path.GetFullPath(Path.Combine(projectDirectory, logicalPath));
                            string? link = string.Equals(shownAt, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase) ? null : logicalPath;
                            Dictionary<string, string>? metadata = null;
                            if (string.Equals(itemType, ProjectFile.EmbeddedResourceItemType, StringComparison.OrdinalIgnoreCase)
                                && path!.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                            {
                                metadata = ReadResxMetadata(hierarchy, path);
                            }

                            result.Add(new ProjectFile(path, itemType, link, metadata));
                        }
                    }

                    // Dependent files (e.g., a designer file under its parent) are in the same folder
                    CollectItems(item.ProjectItems, logicalFolder, projectDirectory, hierarchy, result);
                }
                else if (string.Equals(kind, EnvDTE.Constants.vsProjectItemKindPhysicalFolder, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kind, EnvDTE.Constants.vsProjectItemKindVirtualFolder, StringComparison.OrdinalIgnoreCase))
                {
                    CollectItems(item.ProjectItems, Path.Combine(logicalFolder, name), projectDirectory, hierarchy, result);
                }
                else
                {
                    CollectItems(item.ProjectItems, logicalFolder, projectDirectory, hierarchy, result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: skipped project item: {ex.Message}");
            }
#pragma warning restore CA1031
        }
    }

    /// <summary>
    /// Determines whether a project includes a file with the extension of a Tlumach configuration, whatever its item type. Used to decide whether the command is shown in the context menu of the project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns><see langword="true"/> when the project has such a file.</returns>
    internal static bool HasConfigurationFiles(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        // Runs whenever the context menu opens: only the names are read, which avoids the costly COM properties of the items
        return ContainsConfigurationFile(project.ProjectItems);
    }

    private static bool ContainsConfigurationFile(ProjectItems? items)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (items is null)
            return false;

        foreach (EnvDTE.ProjectItem item in items)
        {
#pragma warning disable CA1031 // COM properties may throw for unloaded or virtual items
            try
            {
                if (TranslationValidator.IsConfigurationFile(item.Name ?? string.Empty)
                    && string.Equals(item.Kind, EnvDTE.Constants.vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (ContainsConfigurationFile(item.ProjectItems))
                    return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Tlumach: skipped project item: {ex.Message}");
            }
#pragma warning restore CA1031
        }

        return false;
    }

    private static void WriteText(IVsOutputWindowPane pane, string text)
    {
        foreach (string line in text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'))
            OutputWindowHelper.WriteLine(pane, line);
    }
}
