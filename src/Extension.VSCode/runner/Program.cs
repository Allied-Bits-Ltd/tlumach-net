// <copyright file="Program.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Xml.Linq;
using AlliedBits.Tlumach.Extension.VSCode.Runner;
using Tlumach.Generator;

// Initialize all parsers before any generation call
ArbParser.Use();
IniParser.Use();
JsonParser.Use();
ResxParser.Use();
CsvParser.Use();
TomlParser.Use();
TsvParser.Use();

// Parse command-line arguments
var parsedArgs = Args.Parse(args);
if (parsedArgs is null)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Runner --project <path-to.csproj>");
    Console.Error.WriteLine("  Runner --config <path-to-cfg> --project-dir <dir> [--namespace <ns>] [--extra-parsers <p>] [--delayed-units <true|false>]");
    return 2;
}

#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
return parsedArgs switch
{
    ProjectArgs p => RunProject(p),
    ConfigArgs c  => RunConfig(c),
    _             => 2,
};
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row

// ---------------------------------------------------------------------------
// Run an entire project: parse the .csproj, find AdditionalFiles, generate
// ---------------------------------------------------------------------------

static int RunProject(ProjectArgs args)
{
    string projectFile = Path.GetFullPath(args.ProjectPath);
    if (!File.Exists(projectFile))
    {
        Console.Error.WriteLine($"ERROR: project file not found: {projectFile}");
        return 2;
    }

    string projectDir = Path.GetDirectoryName(projectFile)!;
    Console.WriteLine($"--- Tlumach Generator: {Path.GetFileName(projectFile)} ---");

    ProjectInfo info;
    try
    {
        info = ProjectReader.Read(projectFile, projectDir);
    }
#pragma warning disable CA1031
    catch (Exception ex)
#pragma warning restore CA1031
    {
        Console.Error.WriteLine($"ERROR: could not read project: {ex.Message}");
        return 2;
    }

    if (info.ConfigFiles.Count == 0)
    {
        Console.WriteLine("  No Tlumach configuration files found (no AdditionalFiles with known extensions).");
        return 0;
    }

    return GenerateFiles(info.ConfigFiles, projectDir, info.Options);
}

// ---------------------------------------------------------------------------
// Direct single-file mode (for scripting / advanced use)
// ---------------------------------------------------------------------------

static int RunConfig(ConfigArgs args)
{
    string configFile = Path.GetFullPath(args.ConfigPath);
    string projectDir = Path.GetFullPath(args.ProjectDir);

    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["projectdir"] = projectDir,
    };

    if (!string.IsNullOrEmpty(args.Namespace))
        options["UsingNamespace"] = args.Namespace;
    if (!string.IsNullOrEmpty(args.ExtraParsers))
        options["ExtraParsers"] = args.ExtraParsers;
    if (!string.IsNullOrEmpty(args.DelayedUnits))
        options["DelayedUnitCreation"] = args.DelayedUnits;

    return GenerateFiles([configFile], projectDir, options);
}

// ---------------------------------------------------------------------------
// Core generation loop
// ---------------------------------------------------------------------------

static int GenerateFiles(
    IReadOnlyList<string> configFiles,
    string projectDir,
    Dictionary<string, string> options)
{
    int success = 0;
    int errors = 0;

    foreach (string configPath in configFiles)
    {
        string fileName = Path.GetFileName(configPath);
        try
        {
            string? generatedCode = BridgeGenerator.InvokeGenerateClass(configPath, projectDir, options);

            if (generatedCode is null)
            {
                Console.WriteLine($"  SKIP: {fileName} (no output produced)");
                continue;
            }

            string outputPath = GetOutputPath(projectDir, configPath);
            WriteFile(outputPath, generatedCode);
            Console.WriteLine($"  OK:   {fileName}  ->  {outputPath}");
            success++;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Console.WriteLine($"  ERROR: {fileName}: {ex.Message}");
            errors++;
        }
    }

    Console.WriteLine($"--- Done: {success} file(s) generated, {errors} error(s). ---");
    return errors > 0 ? 1 : 0;
}

static string GetOutputPath(string projectDir, string configFilePath)
{
    string stem = Path.GetFileNameWithoutExtension(configFilePath);
    string outputDir = Path.Combine(
        projectDir, "obj", "GeneratedFiles", "AlliedBits.Tlumach.Generator", "Generator");
    return Path.Combine(outputDir, stem + ".g.cs");
}

static void WriteFile(string outputPath, string content)
{
    string? dir = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        Directory.CreateDirectory(dir);
    File.WriteAllText(outputPath, content, Encoding.UTF8);
}

// ===========================================================================
// Supporting types
// ===========================================================================

namespace AlliedBits.Tlumach.Extension.VSCode.Runner
{
    /// <summary>
    /// Exposes the protected BaseGenerator.GenerateClass — mirrors the pattern
    /// used in Tlumach.GeneratorTests and the Visual Studio extension.
    /// </summary>
    internal sealed class BridgeGenerator : BaseGenerator
    {
        internal static string? InvokeGenerateClass(
            string configFilePath,
            string projectDir,
            Dictionary<string, string> options)
            => GenerateClass(configFilePath, projectDir, options);
    }

    // -----------------------------------------------------------------------
    // Argument parsing
    // -----------------------------------------------------------------------

    internal abstract record Args
    {
        /// <summary>
        /// Parses command-line arguments. Returns null if the arguments are invalid.
        /// </summary>
        internal static Args? Parse(string[] rawArgs)
        {
            if (rawArgs.Length == 0)
                return null;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rawArgs.Length - 1; i += 2)
            {
                if (!rawArgs[i].StartsWith("--", StringComparison.Ordinal))
                    return null;
                map[rawArgs[i][2..]] = rawArgs[i + 1];
            }

            if (map.TryGetValue("project", out string? proj))
                return new ProjectArgs(proj);

            if (map.TryGetValue("config", out string? cfg) &&
                map.TryGetValue("project-dir", out string? dir))
            {
                map.TryGetValue("namespace", out string? ns);
                map.TryGetValue("extra-parsers", out string? ep);
                map.TryGetValue("delayed-units", out string? du);
                return new ConfigArgs(cfg, dir, ns, ep, du);
            }

            return null;
        }
    }

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    internal sealed record ProjectArgs(string ProjectPath) : Args;

    internal sealed record ConfigArgs(
        string ConfigPath,
        string ProjectDir,
        string? Namespace,
        string? ExtraParsers,
        string? DelayedUnits) : Args;

    // -----------------------------------------------------------------------
    // .csproj reader
    // -----------------------------------------------------------------------

    internal sealed record ProjectInfo(
        IReadOnlyList<string> ConfigFiles,
        Dictionary<string, string> Options);
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

    internal static class ProjectReader
    {
         internal static ProjectInfo Read(string projectFilePath, string projectDir)
        {
            var doc = XDocument.Load(projectFilePath);

            var configFiles = doc
                .Descendants()
                .Where(e => e.Name.LocalName == "AdditionalFiles")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => ResolveInclude(v!, projectDir))
                .Where(IsTlumachConfigFile)
                .Where(File.Exists)
                .ToList();

            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["projectdir"] = projectDir,
            };

            string? ReadProp(string name) =>
                doc.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == name)
                    ?.Value;

            string? ns = ReadProp("TlumachGeneratorUsingNamespace");
            string? ep = ReadProp("TlumachGeneratorExtraParsers");
            string? du = ReadProp("TlumachGeneratorDelayedUnitCreation");

#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
            if (!string.IsNullOrEmpty(ns)) options["UsingNamespace"]    = ns;
            if (!string.IsNullOrEmpty(ep)) options["ExtraParsers"]      = ep;
            if (!string.IsNullOrEmpty(du)) options["DelayedUnitCreation"] = du;
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row

            return new ProjectInfo(configFiles, options);
        }

        private static string ResolveInclude(string include, string projectDir)
        {
            // Absolute paths are returned as-is; relative paths resolve from projectDir
            return Path.IsPathRooted(include)
                ? include
                : Path.GetFullPath(Path.Combine(projectDir, include));
        }

        private static bool IsTlumachConfigFile(string filePath) => FileFormats.HasConfigParser(Path.GetExtension(filePath));
    }
}
