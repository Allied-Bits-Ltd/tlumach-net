// <copyright file="ReportWriter.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Tlumach.Validation;

namespace AlliedBits.Tlumach.Extension.Runner;

/// <summary>
/// Writes validation reports as text for people and as JSON for the extension.
/// </summary>
internal static class ReportWriter
{
    /// <summary>
    /// The version of the JSON report. The extension checks it, so a change of the layout that the extension cannot read must increase it.
    /// </summary>
    public const int JsonVersion = 1;

    public static void WriteText(TextWriter writer, ProjectValidationReport report, string? note)
        => ValidationReportWriter.WriteText(writer, report, note, FormatMessage);

    public static void WriteTotals(TextWriter writer, IReadOnlyCollection<ProjectValidationReport> reports)
        => ValidationReportWriter.WriteTotals(writer, reports);

    /// <summary>
    /// Formats a message so that the VS Code terminal and output panel turn its location into a link: <c>path:line:column: error CODE: text</c>.
    /// </summary>
    public static string FormatMessage(ValidationMessage message)
    {
        StringBuilder builder = new();
        if (!string.IsNullOrEmpty(message.FilePath))
        {
            builder.Append(message.FilePath);
            if (message.Line > 0)
            {
                builder.Append(':').Append(message.Line.ToString(CultureInfo.InvariantCulture));
                if (message.Column > 0)
                    builder.Append(':').Append(message.Column.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(": ");
        }

        builder.Append(SeverityName(message.Severity)).Append(' ').Append(message.Code).Append(": ").Append(message.Message);
        return builder.ToString();
    }

    public static void WriteJson(TextWriter output, IReadOnlyList<(ProjectValidationReport Report, string? Note)> reports)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter json = new(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            json.WriteStartObject();
            json.WriteNumber("version", JsonVersion);
            json.WriteNumber("errors", reports.Sum(r => r.Report.ErrorCount));
            json.WriteNumber("warnings", reports.Sum(r => r.Report.WarningCount));
            json.WriteNumber("infos", reports.Sum(r => r.Report.InfoCount));

            json.WriteStartArray("projects");
            foreach ((ProjectValidationReport report, string? note) in reports)
                WriteProject(json, report, note);
            json.WriteEndArray();

            json.WriteEndObject();
        }

        output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static void WriteProject(Utf8JsonWriter json, ProjectValidationReport report, string? note)
    {
        json.WriteStartObject();
        json.WriteString("project", report.ProjectPath);
        json.WriteString("note", note);
        json.WriteNumber("errors", report.ErrorCount);
        json.WriteNumber("warnings", report.WarningCount);
        json.WriteNumber("infos", report.InfoCount);

        json.WriteStartArray("ignoredFiles");
        foreach (string file in report.IgnoredFiles)
            json.WriteStringValue(file);
        json.WriteEndArray();

        json.WriteStartArray("configurations");
        foreach (ConfigurationValidationReport configuration in report.Configurations)
        {
            json.WriteStartObject();
            json.WriteString("configuration", configuration.ConfigurationPath);
            json.WriteBoolean("generatorInput", configuration.IsGeneratorInput);
            json.WriteString("defaultFile", configuration.DefaultFilePath);

            json.WriteStartArray("files");
            foreach (ValidatedFile file in configuration.Files)
            {
                json.WriteStartObject();
                json.WriteString("path", file.FilePath);
                json.WriteString("origin", OriginName(file.Origin));
                json.WriteString("locale", file.Locale);
                json.WriteStartArray("checkedLocales");
                foreach (string locale in file.CheckedLocales)
                    json.WriteStringValue(locale);
                json.WriteEndArray();
                json.WriteNumber("entries", file.EntryCount);
                json.WriteNumber("errors", file.ErrorCount);
                json.WriteNumber("warnings", file.WarningCount);
                json.WriteNumber("infos", file.InfoCount);
                json.WriteEndObject();
            }

            json.WriteEndArray();

            json.WriteStartArray("messages");
            foreach (ValidationMessage message in configuration.Messages)
            {
                json.WriteStartObject();
                json.WriteString("severity", SeverityName(message.Severity));
                json.WriteString("code", message.Code);
                json.WriteString("message", message.Message);
                json.WriteString("file", message.FilePath);
                json.WriteNumber("line", message.Line);
                json.WriteNumber("column", message.Column);
                json.WriteEndObject();
            }

            json.WriteEndArray();
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteEndObject();
    }

    private static string SeverityName(ValidationSeverity severity) => severity switch
    {
        ValidationSeverity.Error => "error",
        ValidationSeverity.Warning => "warning",
        _ => "info",
    };

    private static string OriginName(TranslationFileOrigin origin) => origin switch
    {
        TranslationFileOrigin.DefaultFile => "defaultFile",
        TranslationFileOrigin.Configuration => "configuration",
        TranslationFileOrigin.NamingConvention => "namingConvention",
        _ => "linkedItem",
    };
}
