// <copyright file="RunnerCommand.cs" company="Allied Bits Ltd.">
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

using Tlumach.Validation;

namespace AlliedBits.Tlumach.Extension.Runner;

/// <summary>
/// The command line of the runner.
/// <code>
/// verify --project &lt;file.csproj&gt; [--project &lt;file.csproj&gt; ...] [--json]
/// verify --config &lt;file&gt; [--project-dir &lt;directory&gt;] [--json]
/// </code>
/// Without <c>--json</c>, the report is written as text to the standard output. With <c>--json</c>, the JSON report is written to the standard output and the text to the standard error, so that the extension can show the text while it reads the JSON.
/// </summary>
internal static class RunnerCommand
{
    /// <summary>No errors were found.</summary>
    public const int ExitSuccess = 0;

    /// <summary>The translations contain errors.</summary>
    public const int ExitErrorsFound = 1;

    /// <summary>The command line is wrong, or the validation could not run.</summary>
    public const int ExitFailure = 2;

    public const string Usage = """
        Usage:
          Tlumach.Extension.Runner verify --project <file.csproj> [--project <file.csproj> ...] [--json]
          Tlumach.Extension.Runner verify --config <configuration file> [--project-dir <directory>] [--json]

        Validates the Tlumach configurations of the projects and the translation files that they lead to.
        Exit codes: 0 - no errors, 1 - errors were found, 2 - the validation could not run.
        """;

    public static int Run(string[] args, TextWriter output, TextWriter error, IProjectEvaluator evaluator, CancellationToken cancellationToken)
    {
        if (!TryParse(args, out Arguments? arguments, out string? problem))
        {
            if (problem is not null)
                error.WriteLine(problem);

            error.WriteLine(Usage);
            return ExitFailure;
        }

        TextWriter text = arguments!.Json ? error : output;

        List<(ProjectValidationReport Report, string? Note)> reports = [];
        try
        {
            if (arguments.ConfigPath is not null)
            {
                string configPath = Path.GetFullPath(arguments.ConfigPath);
                if (!File.Exists(configPath))
                {
                    error.WriteLine($"The configuration file '{configPath}' does not exist.");
                    return ExitFailure;
                }

                string projectDirectory = Path.GetFullPath(arguments.ProjectDirectory ?? Path.GetDirectoryName(configPath) ?? Environment.CurrentDirectory);
                ConfigurationValidationReport configuration = TranslationValidator.ValidateConfiguration(configPath, isGeneratorInput: true, projectDirectory, projectItems: null, cancellationToken);

                ProjectValidationReport report = new(configPath);
                report.Configurations.Add(configuration);
                reports.Add((report, null));
                ReportWriter.WriteText(text, report, null);
            }
            else
            {
                foreach (string project in arguments.ProjectPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string projectPath = Path.GetFullPath(project);
                    if (!File.Exists(projectPath))
                    {
                        error.WriteLine($"The project file '{projectPath}' does not exist.");
                        return ExitFailure;
                    }

                    text.WriteLine($"=== Tlumach: validating the translations of '{Path.GetFileName(projectPath)}' ===");

                    ProjectEvaluation evaluation = evaluator.Evaluate(projectPath, cancellationToken);
                    ProjectValidationReport report = TranslationValidator.ValidateProject(projectPath, evaluation.Items, cancellationToken);
                    reports.Add((report, evaluation.Note));
                    ReportWriter.WriteText(text, report, evaluation.Note);
                }
            }
        }
        catch (OperationCanceledException)
        {
            error.WriteLine("The validation was cancelled.");
            return ExitFailure;
        }
#pragma warning disable CA1031 // The last resort: the extension shows this message instead of a crash of the process
        catch (Exception ex)
        {
            error.WriteLine($"The validation failed: {ex.GetType().Name}: {ex.Message}");
            error.WriteLine(ex.StackTrace);
            return ExitFailure;
        }
#pragma warning restore CA1031

        if (reports.Count > 1)
            ReportWriter.WriteTotals(text, reports.Select(r => r.Report).ToList());

        if (arguments.Json)
            ReportWriter.WriteJson(output, reports);

        output.Flush();
        error.Flush();

        return reports.Exists(r => r.Report.ErrorCount > 0) ? ExitErrorsFound : ExitSuccess;
    }

    internal static bool TryParse(string[] args, out Arguments? arguments, out string? problem)
    {
        arguments = null;
        problem = null;

        if (args.Length == 0 || args[0] is "-h" or "--help" or "-?" or "/?")
            return false;

        if (!args[0].Equals("verify", StringComparison.Ordinal))
        {
            problem = $"Unknown command '{args[0]}'.";
            return false;
        }

        Arguments result = new();

        Queue<string> pending = new(args.Skip(1));
        while (pending.Count > 0)
        {
            string option = pending.Dequeue();
            switch (option)
            {
                case "--json":
                    result.Json = true;
                    break;

                case "--project" or "--config" or "--project-dir":
                    if (pending.Count == 0 || pending.Peek().Length == 0)
                    {
                        problem = $"The option '{option}' requires a value.";
                        return false;
                    }

                    string value = pending.Dequeue();
                    switch (option)
                    {
                        case "--project":
                            result.ProjectPaths.Add(value);
                            break;
                        case "--config":
                            result.ConfigPath = value;
                            break;
                        default:
                            result.ProjectDirectory = value;
                            break;
                    }

                    break;

                default:
                    problem = $"Unknown option '{option}'.";
                    return false;
            }
        }

        if (result.ConfigPath is null && result.ProjectPaths.Count == 0)
        {
            problem = "Specify a project with --project or a configuration with --config.";
            return false;
        }

        if (result.ConfigPath is not null && result.ProjectPaths.Count > 0)
        {
            problem = "The options --project and --config cannot be combined.";
            return false;
        }

        if (result.ProjectDirectory is not null && result.ConfigPath is null)
        {
            problem = "The option --project-dir is used only with --config.";
            return false;
        }

        arguments = result;
        return true;
    }

    internal sealed class Arguments
    {
        public List<string> ProjectPaths { get; } = [];

        public string? ConfigPath { get; set; }

        public string? ProjectDirectory { get; set; }

        public bool Json { get; set; }
    }
}
