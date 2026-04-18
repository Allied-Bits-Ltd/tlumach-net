using System;
using System.Collections.Generic;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for the CSV (comma-separated values) format.
/// </summary>
public class CsvWriter : BaseTableWriter
{
    /// <summary>
    /// Gets or sets the separator character used to separate values. Default is comma, but Excel uses semicolon ';' as a separator for exported CSVs.
    /// </summary>
    public char SeparatorChar { get; set; } = ',';

    public override string FormatName => "CSV";

    public override string ConfigExtension => string.Empty;

    public override string TranslationExtension => ".csv";

    protected override void WriteCell(string value, StringBuilder sb)
    {
        if (value.Contains(SeparatorChar) || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
        {
            sb.Append('"');
            sb.Append(value.Replace("\"", "\"\""));
            sb.Append('"');
        }
        else
        {
            sb.Append(value);
        }

        sb.Append(SeparatorChar);
    }

    protected override void EndRow(StringBuilder sb)
    {
        if (sb.Length > 0 && sb[sb.Length - 1] == SeparatorChar)
            sb.Length--;

        sb.AppendLine();
    }

    protected override bool ShouldWriteReference(TranslationEntry entry) => !string.IsNullOrEmpty(entry.Reference);
}
