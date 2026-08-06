// <copyright file="Program.cs" company="Allied Bits Ltd.">
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using BenchmarkDotNet.Running;

namespace Tlumach.Benchmarks;

/// <summary>
/// Entry point for the benchmark harness.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the benchmarks.
    /// </summary>
    /// <param name="args">
    /// Standard BenchmarkDotNet arguments, plus an optional <c>--label &lt;name&gt;</c> that names the
    /// directory the results are stored in. The label may also be supplied through the
    /// <c>TLUMACH_BENCH_LABEL</c> environment variable.
    /// </param>
    /// <returns>Zero on success.</returns>
    public static int Main(string[] args)
    {
        (string label, string[] benchmarkArgs) = ExtractLabel(args);

        WriteRunInfo(label, benchmarkArgs);

        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(benchmarkArgs, BenchmarkConfig.Create(label));

        Console.WriteLine();
        Console.WriteLine("Results stored under: " + Path.Combine(BenchmarkConfig.ResultsRoot, BenchmarkConfig.SanitizeLabel(label)));
        Console.WriteLine("Compare two runs with: ./Compare-Results.ps1 -Baseline <label> -Candidate <label>");

        return 0;
    }

    /// <summary>
    /// Pulls the <c>--label</c> argument out of the command line so that it is not passed on to
    /// BenchmarkDotNet, which would reject it as unknown.
    /// </summary>
    /// <param name="args">The raw command line.</param>
    /// <returns>The resolved label and the remaining arguments.</returns>
    private static (string Label, string[] Remaining) ExtractLabel(string[] args)
    {
        List<string> remaining = new(args.Length);
        string? label = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--label", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                label = args[i + 1];
                i++;
                continue;
            }

            remaining.Add(args[i]);
        }

        label ??= Environment.GetEnvironmentVariable("TLUMACH_BENCH_LABEL");
        label = string.IsNullOrWhiteSpace(label) ? BenchmarkConfig.CreateDefaultLabel() : label;

        return (label, remaining.ToArray());
    }

    /// <summary>
    /// Records what was run and on what, next to the results, so that a stored run can be interpreted
    /// months later without guessing.
    /// </summary>
    /// <param name="label">The run label.</param>
    /// <param name="benchmarkArgs">The arguments handed to BenchmarkDotNet.</param>
    private static void WriteRunInfo(string label, string[] benchmarkArgs)
    {
        try
        {
            string dir = Path.Combine(BenchmarkConfig.ResultsRoot, BenchmarkConfig.SanitizeLabel(label));
            Directory.CreateDirectory(dir);

            StringBuilder info = new();
            info.Append("label            : ").AppendLine(label);
            info.Append("utc              : ").AppendLine(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            info.Append("machine          : ").AppendLine(Environment.MachineName);
            info.Append("processorCount   : ").AppendLine(Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture));
            info.Append("os               : ").AppendLine(RuntimeInformation.OSDescription);
            info.Append("runtime          : ").AppendLine(RuntimeInformation.FrameworkDescription);
            info.Append("architecture     : ").AppendLine(RuntimeInformation.ProcessArchitecture.ToString());
            info.Append("tlumachAssembly  : ").AppendLine(typeof(TranslationManager).Assembly.GetName().Version?.ToString() ?? "(unknown)");
            info.Append("informational    : ").AppendLine(
                typeof(TranslationManager).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "(unknown)");
            info.Append("arguments        : ").AppendLine(benchmarkArgs.Length == 0 ? "(none)" : string.Join(' ', benchmarkArgs));

            File.WriteAllText(Path.Combine(dir, "run-info.txt"), info.ToString());
        }
        catch (IOException ex)
        {
            Console.WriteLine("Warning: could not write run-info.txt: " + ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine("Warning: could not write run-info.txt: " + ex.Message);
        }
    }
}
