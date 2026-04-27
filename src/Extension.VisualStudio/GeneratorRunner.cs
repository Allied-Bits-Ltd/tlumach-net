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
using System.Text;
using System.Threading.Tasks;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
/// Orchestrates finding Tlumach config files in VS projects and invoking the generator.
/// </summary>
internal static class GeneratorRunner
{
    // Option key names expected by BaseGenerator (match constants in Generator.cs).
    private const string OptionUsingNamespace = "UsingNamespace";
    private const string OptionExtraParsers = "ExtraParsers";
    private const string OptionDelayedUnits = "DelayedUnitCreation";

    // Output path convention: matches dotnet build EmitCompilerGeneratedFiles path.
    private const string GeneratedFilesSubPath =
        @"obj\GeneratedFiles\AlliedBits.Tlumach.Generator\Generator";

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
    }

    // -------------------------------------------------------------------------
    // Public entry points
    // -------------------------------------------------------------------------

    internal static async Task RunForProjectAsync(AsyncPackage package, Project project, CancellationToken cancellationToken = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(package);
        OutputWindowHelper.Activate(pane);

        string projectName = project.Name;
        OutputWindowHelper.WriteLine(pane, $"--- Tlumach Generator: project '{projectName}' ---");

        string? projectDir = GetProjectDirectory(project);
        if (string.IsNullOrEmpty(projectDir))
        {
            OutputWindowHelper.WriteLine(pane, $"ERROR: could not determine project directory for '{projectName}'.");
            return;
        }

        Dictionary<string, string> options = ReadProjectOptions(project, projectDir!);
        List<string> configFiles = CollectAdditionalTlumachFiles(project);

        if (configFiles.Count == 0)
        {
            OutputWindowHelper.WriteLine(pane, "  No Tlumach configuration files found.");
            return;
        }

        int success = 0;
        int errors = 0;

        foreach (string configPath in configFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fileName = Path.GetFileName(configPath);
            try
            {
                string? generatedCode = BridgeGenerator.InvokeGenerateClass(configPath, projectDir!, options);

                if (generatedCode is null)
                {
                    OutputWindowHelper.WriteLine(pane, $"  SKIP: {fileName} (no output produced)");
                    continue;
                }

                string outputPath = GetOutputPath(projectDir!, configPath);
                WriteGeneratedFile(outputPath, generatedCode);
                OutputWindowHelper.WriteLine(pane, $"  OK:   {fileName}  ->  {outputPath}");
                success++;
            }
#pragma warning disable CA1031 // generator and COM may throw any exception type
            catch (Exception ex)
#pragma warning restore CA1031
            {
                OutputWindowHelper.WriteLine(pane, $"  ERROR: {fileName}: {ex.Message}");
                errors++;
            }
        }

        OutputWindowHelper.WriteLine(pane,
            $"--- Done: {success} file(s) generated, {errors} error(s). ---");
    }

    internal static async Task RunForAllProjectsAsync(AsyncPackage package, DTE2 dte, CancellationToken cancellationToken = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(package);
        OutputWindowHelper.Activate(pane);
        OutputWindowHelper.WriteLine(pane, "=== Tlumach Generator: all projects ===");

        int count = 0;
        foreach (Project project in EnumerateAllProjects(dte.Solution.Projects))
        {
            cancellationToken.ThrowIfCancellationRequested();
            count++;
            await RunForProjectAsync(package, project, cancellationToken).ConfigureAwait(true);
        }

        if (count == 0)
            OutputWindowHelper.WriteLine(pane, "No projects found in solution.");

        OutputWindowHelper.WriteLine(pane, "=== Tlumach Generator: complete ===");
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

    private static bool IsTlumachConfigFile(string filePath) => FileFormats.GetConfigParser(Path.GetExtension(filePath)) is not null;

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
    // Output file writing
    // -------------------------------------------------------------------------

    private static string GetOutputPath(string projectDir, string configFilePath)
    {
        // GetFileNameWithoutExtension("Sample.toml.cfg") -> "Sample.toml"
        // which matches the file name the Roslyn build would produce.
        string stem = Path.GetFileNameWithoutExtension(configFilePath);
        string outputDir = Path.Combine(projectDir, GeneratedFilesSubPath);
        return Path.Combine(outputDir, stem + ".g.cs");
    }

    private static void WriteGeneratedFile(string outputPath, string content)
    {
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(outputPath, content, Encoding.UTF8);
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
