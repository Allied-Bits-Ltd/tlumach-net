// <copyright file="BenchmarkConfig.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;

namespace Tlumach.Benchmarks;

/// <summary>
/// Builds the BenchmarkDotNet configuration used by every run.
/// <para>
/// Results are written under <c>tests/Benchmarks/BenchmarkResults/&lt;label&gt;/results</c> so that a
/// run taken before an optimization can be compared with a run taken after it. Use
/// <c>Compare-Results.ps1</c> to produce the delta table.
/// </para>
/// </summary>
public static class BenchmarkConfig
{
    /// <summary>The directory name, under the benchmark project, that holds all stored runs.</summary>
    public const string ResultsFolderName = "BenchmarkResults";

    /// <summary>
    /// Creates the configuration for a run.
    /// </summary>
    /// <param name="label">The label that names the run's result directory.</param>
    /// <returns>The BenchmarkDotNet configuration.</returns>
    public static IConfig Create(string label)
    {
        string artifactsPath = Path.Combine(ResultsRoot, SanitizeLabel(label));

        return ManualConfig.Create(DefaultConfig.Instance)

            // Almost every optimization under review is an allocation reduction, so allocation
            // measurement is not optional here.
            .AddDiagnoser(MemoryDiagnoser.Default)

            // JSON is what Compare-Results.ps1 reads. The GitHub-flavoured Markdown and CSV exporters are
            // already part of DefaultConfig, so adding them again only produces a "already present" warning.
            .AddExporter(JsonExporter.Full)
            .WithArtifactsPath(artifactsPath);
    }

    /// <summary>
    /// Gets the directory that holds all stored benchmark runs.
    /// </summary>
    public static string ResultsRoot
    {
        get
        {
            string? fromEnvironment = Environment.GetEnvironmentVariable("TLUMACH_BENCH_RESULTS");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            // Walk up from the running assembly looking for the benchmark project file. BenchmarkDotNet
            // runs benchmarks from a generated project nested under the original output directory, so the
            // project root is always an ancestor.
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Tlumach.Benchmarks.csproj")))
                    return Path.Combine(dir.FullName, ResultsFolderName);

                dir = dir.Parent;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), ResultsFolderName);
        }
    }

    /// <summary>
    /// Produces the default label for an unlabelled run: a sortable UTC timestamp.
    /// </summary>
    /// <returns>A label of the form <c>run-yyyyMMdd-HHmmss</c>.</returns>
    public static string CreateDefaultLabel()
        => "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Removes characters that are not valid in a directory name.
    /// </summary>
    /// <param name="label">The raw label.</param>
    /// <returns>A label usable as a directory name.</returns>
    public static string SanitizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return CreateDefaultLabel();

        char[] invalid = Path.GetInvalidFileNameChars();
        Span<char> buffer = stackalloc char[label.Length];
        for (int i = 0; i < label.Length; i++)
            buffer[i] = Array.IndexOf(invalid, label[i]) >= 0 ? '_' : label[i];

        return new string(buffer).Trim();
    }
}
