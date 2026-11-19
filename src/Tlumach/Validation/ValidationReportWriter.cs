// <copyright file="ValidationReportWriter.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

#if GENERATOR
namespace Tlumach.Generator.Validation;
#else
namespace Tlumach.Validation;
#endif

/// <summary>
/// Writes a validation report as text for the output windows of the IDEs. The IDEs turn different forms of a location into a link, so the form of a message is given by the caller.
/// </summary>
public static class ValidationReportWriter
{
    /// <summary>
    /// Writes the report of a project.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="report">The report.</param>
    /// <param name="note">An optional note about how the files of the project were found.</param>
    /// <param name="formatMessage">Formats a message, including its location.</param>
    public static void WriteText(TextWriter writer, ProjectValidationReport report, string? note, Func<ValidationMessage, string> formatMessage)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (report is null)
            throw new ArgumentNullException(nameof(report));

        if (formatMessage is null)
            throw new ArgumentNullException(nameof(formatMessage));

        if (note is not null)
            writer.WriteLine($"  Note: {note}");

        if (report.Configurations.Count == 0)
        {
            writer.WriteLine("  No Tlumach configuration files found.");
            return;
        }

        foreach (ConfigurationValidationReport configuration in report.Configurations)
        {
            writer.WriteLine();
            writer.WriteLine($"  Configuration '{configuration.ConfigurationPath}'{(configuration.IsGeneratorInput ? " (processed by the generator)" : string.Empty)}");

            foreach (ValidatedFile file in configuration.Files)
                writer.WriteLine($"    {Status(file),-7} {file.FilePath} ({Describe(file)})");

            foreach (ValidationMessage message in configuration.Messages)
                writer.WriteLine("    " + formatMessage(message));
        }

        writer.WriteLine();
        writer.WriteLine($"  Done: {Plural(report.FileCount, "file")} checked in {Plural(report.Configurations.Count, "configuration")}, {Plural(report.ErrorCount, "error")}, {Plural(report.WarningCount, "warning")}, {Plural(report.InfoCount, "info message")}.");
    }

    /// <summary>
    /// Writes the totals of several projects.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="reports">The reports of the projects.</param>
    public static void WriteTotals(TextWriter writer, IReadOnlyCollection<ProjectValidationReport> reports)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (reports is null)
            throw new ArgumentNullException(nameof(reports));

        writer.WriteLine();
        writer.WriteLine($"=== Total: {Plural(reports.Count, "project")}, {Plural(reports.Sum(r => r.FileCount), "file")}, {Plural(reports.Sum(r => r.ErrorCount), "error")}, {Plural(reports.Sum(r => r.WarningCount), "warning")}, {Plural(reports.Sum(r => r.InfoCount), "info message")} ===");
    }

    /// <summary>
    /// Builds a phrase with a number and a noun in the right form, such as "1 file" or "2 files".
    /// </summary>
    /// <param name="count">The number.</param>
    /// <param name="singular">The noun in the singular.</param>
    /// <param name="plural">The noun in the plural, when it is not the singular with "s".</param>
    /// <returns>The phrase.</returns>
    public static string Plural(int count, string singular, string? plural = null)
        => count.ToString(CultureInfo.InvariantCulture) + " " + (count == 1 ? singular : plural ?? singular + "s");

    private static string Status(ValidatedFile file)
    {
        if (file.ErrorCount > 0)
            return "ERROR";

        if (file.WarningCount > 0)
            return "WARNING";

        return file.InfoCount > 0 ? "INFO" : "OK";
    }

    private static string Describe(ValidatedFile file)
    {
        List<string> parts = [];

        parts.Add(file.Origin switch
        {
            TranslationFileOrigin.DefaultFile => "default file",
            TranslationFileOrigin.Configuration => file.Locale.Length == 0 ? "named in the configuration for other locales" : $"named in the configuration for '{file.Locale}'",
            TranslationFileOrigin.LinkedItem => $"'{file.Locale}', linked into the project",
            _ => $"'{file.Locale}'",
        });

        if (file.CheckedLocales.Count > 1)
            parts.Add("locales " + string.Join(", ", file.CheckedLocales));

        if (file.EntryCount > 0)
            parts.Add(Plural(file.EntryCount, "entry", "entries"));

        return string.Join(", ", parts);
    }
}
