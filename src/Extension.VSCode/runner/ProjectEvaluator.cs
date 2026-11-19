// <copyright file="ProjectEvaluator.cs" company="Allied Bits Ltd.">
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

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using Tlumach.Validation;

namespace AlliedBits.Tlumach.Extension.Runner;

/// <summary>
/// Lists the files of a project with MSBuild (<c>dotnet msbuild -getItem</c>), which evaluates globs, conditions, and imported files exactly as a build does. When MSBuild is not available, the items written in the project file and the files in the project directory are used.
/// </summary>
internal sealed class ProjectEvaluator : IProjectEvaluator
{
    /// <summary>The item types that can hold Tlumach configuration and translation files.</summary>
    internal static readonly string[] ItemTypes = [ProjectFile.AdditionalFilesItemType, "EmbeddedResource", "Content", "None"];

    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git", ".vs", ".vscode", "node_modules"];

    public ProjectEvaluation Evaluate(string projectPath, CancellationToken cancellationToken)
    {
        string? failure;
        try
        {
            List<ProjectFile>? items = EvaluateWithMsBuild(projectPath, cancellationToken, out failure);
            if (items is not null)
                return new ProjectEvaluation(items, null);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException))
        {
            // E.g., no .NET SDK (Win32Exception), output that is not JSON, or a path that MSBuild reports but .NET rejects
            failure = ex.Message;
        }

        List<ProjectFile> fallback = EvaluateFromFiles(projectPath);
        return new ProjectEvaluation(fallback, $"MSBuild could not list the files of the project ({failure}); the items written in the project file and the files in its directory were used instead.");
    }

    /// <summary>
    /// Finds the executable of the .NET host: the one that runs this process when it is started with "dotnet", otherwise the one on the PATH.
    /// </summary>
    private static string DotNetHost()
    {
        string? host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrEmpty(host) && File.Exists(host))
            return host;

        string? process = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(process) && Path.GetFileNameWithoutExtension(process).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            return process;

        return "dotnet";
    }

    private static List<ProjectFile>? EvaluateWithMsBuild(string projectPath, CancellationToken cancellationToken, out string? failure)
    {
        ProcessStartInfo startInfo = new(DotNetHost())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory,
        };

        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-nologo");

        // No MSBuild node should outlive the evaluation
        startInfo.ArgumentList.Add("-nodeReuse:false");
        foreach (string itemType in ItemTypes)
            startInfo.ArgumentList.Add("-getItem:" + itemType);

        // Keep the output in English and free of first-run messages
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("The .NET host could not be started.");

        Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> errors = process.StandardError.ReadToEndAsync(cancellationToken);

        using (cancellationToken.Register(() => TryKill(process)))
        {
            if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
            {
                TryKill(process);
                failure = "MSBuild did not finish in time";
                return null;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        string text = output.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            string details = (text + Environment.NewLine + errors.GetAwaiter().GetResult()).Trim();
            failure = FirstLines(details, 3);
            return null;
        }

        failure = null;
        return ParseItems(text, projectPath);
    }

    /// <summary>
    /// Reads the JSON that <c>dotnet msbuild -getItem</c> writes: <c>{"Items": {"None": [{"Identity": "...", "FullPath": "..."}]}}</c>.
    /// </summary>
    /// <param name="json">The output of MSBuild.</param>
    /// <param name="projectPath">The project file, against whose directory relative paths are resolved.</param>
    /// <returns>The items.</returns>
    internal static List<ProjectFile> ParseItems(string json, string projectPath)
    {
        List<ProjectFile> result = [];
        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? string.Empty;

        int start = json.IndexOf('{', StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidOperationException("MSBuild did not list the items of the project.");

        using JsonDocument document = JsonDocument.Parse(json.AsMemory(start));
        if (!document.RootElement.TryGetProperty("Items", out JsonElement itemGroups) || itemGroups.ValueKind != JsonValueKind.Object)
            return result;

        foreach (JsonProperty group in itemGroups.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Array)
                continue;

            foreach (JsonElement item in group.Value.EnumerateArray())
            {
                string? path = null;
                if (item.TryGetProperty("FullPath", out JsonElement fullPath) && fullPath.ValueKind == JsonValueKind.String)
                    path = fullPath.GetString();
                else if (item.TryGetProperty("Identity", out JsonElement identity) && identity.ValueKind == JsonValueKind.String)
                {
                    // Project files write paths with backslashes, which are not separators on Linux and macOS
                    string relative = (identity.GetString() ?? string.Empty).Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                    path = Path.Combine(projectDirectory, relative);
                }

                string? link = item.TryGetProperty("Link", out JsonElement linkElement) && linkElement.ValueKind == JsonValueKind.String ? linkElement.GetString() : null;

                // Every other property of an item is its metadata (the well-known ones included)
                Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
                foreach (JsonProperty property in item.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        metadata[property.Name] = property.Value.GetString() ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(path))
                    result.Add(new ProjectFile(Path.GetFullPath(path), group.Name, link, metadata));
            }
        }

        return result;
    }

    /// <summary>
    /// Lists the items written in the project file (with simple wildcards expanded) and the files in the project directory, which an SDK-style project includes by default.
    /// </summary>
    /// <param name="projectPath">The project file.</param>
    /// <returns>The items.</returns>
    internal static List<ProjectFile> EvaluateFromFiles(string projectPath)
    {
        List<ProjectFile> result = [];
        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? string.Empty;

#pragma warning disable CA1031 // A project file that cannot be read leaves only the files of the directory
        try
        {
            XDocument document = XDocument.Load(projectPath);
            foreach (XElement element in document.Descendants())
            {
                string itemType = element.Name.LocalName;
                if (!ItemTypes.Contains(itemType, StringComparer.OrdinalIgnoreCase))
                    continue;

                string? include = (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update");
                if (string.IsNullOrWhiteSpace(include))
                    continue;

                // The Link metadata may be an attribute or a child element; it applies only to a single file
                string? link = (string?)element.Attribute("Link") ?? element.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "Link", StringComparison.Ordinal))?.Value;

                // The metadata may be attributes or child elements
                Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
                foreach (XAttribute attribute in element.Attributes())
                {
                    if (attribute.Name.LocalName is not ("Include" or "Update" or "Remove" or "Exclude" or "Condition"))
                        metadata[attribute.Name.LocalName] = attribute.Value;
                }

                foreach (XElement child in element.Elements())
                    metadata[child.Name.LocalName] = child.Value.Trim();

                foreach (string pattern in include.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    List<string> paths = ExpandPattern(projectDirectory, pattern).ToList();
                    foreach (string path in paths)
                        result.Add(new ProjectFile(path, itemType, paths.Count == 1 && link is not null && !link.Contains("%(", StringComparison.Ordinal) ? link : null, metadata));
                }
            }
        }
        catch (Exception)
        {
            // fall through to the directory
        }
#pragma warning restore CA1031

        foreach (string path in EnumerateProjectDirectory(projectDirectory))
            result.Add(new ProjectFile(path, "None"));

        return result;
    }

    private static IEnumerable<string> ExpandPattern(string projectDirectory, string pattern)
    {
        // MSBuild properties cannot be evaluated here
        if (pattern.Contains("$(", StringComparison.Ordinal) || pattern.Contains("@(", StringComparison.Ordinal))
            return [];

        string normalized = pattern.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        if (normalized.IndexOfAny(['*', '?']) < 0)
            return [Path.GetFullPath(Path.Combine(projectDirectory, normalized))];

        // Supported: "dir/*.ext" and "dir/**/*.ext"
        string fileName = Path.GetFileName(normalized);
        string directory = Path.GetDirectoryName(normalized) ?? string.Empty;
        bool recursive = false;
        if (directory.EndsWith("**", StringComparison.Ordinal))
        {
            recursive = true;
            directory = directory.Substring(0, directory.Length - 2).TrimEnd(Path.DirectorySeparatorChar);
        }

        if (directory.IndexOfAny(['*', '?']) >= 0 || fileName.Length == 0)
            return [];

        string root = Path.GetFullPath(Path.Combine(projectDirectory, directory));
        if (!Directory.Exists(root))
            return [];

        try
        {
            return Directory.EnumerateFiles(root, fileName, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<string> EnumerateProjectDirectory(string directory)
    {
        Stack<string> pending = new();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            string current = pending.Pop();
            string[] files;
            string[] subdirectories;
            try
            {
                files = Directory.GetFiles(current);
                subdirectories = Directory.GetDirectories(current);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (string file in files)
                yield return file;

            foreach (string subdirectory in subdirectories)
            {
                if (!SkippedDirectories.Contains(Path.GetFileName(subdirectory), StringComparer.OrdinalIgnoreCase))
                    pending.Push(subdirectory);
            }
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // the process has already exited
        }
    }

    private static string FirstLines(string text, int count)
    {
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Length == 0 ? "no output" : string.Join(" ", lines.Take(count));
    }
}
