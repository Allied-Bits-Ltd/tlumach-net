using System;
using System.Collections.Generic;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for the TSV (tab-separated values) format.
/// </summary>
public class TsvWriter : BaseTableWriter
{
    public override string FormatName => "TSV";

    public override string ConfigExtension => string.Empty;

    public override string TranslationExtension => ".tsv";

    protected override void WriteCell(string value, StringBuilder sb)
    {
        // A value with a tab or a line break is quoted, so that TsvParser (with ExpectQuotes, the default) reads it back. So is a value that starts with a double quote,
        // which the parser would otherwise take for the start of a quoted cell. A double quote elsewhere is written as it stands.
        if (value.IndexOf('\t') >= 0 || value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0 || (value.Length > 0 && value[0] == '"'))
        {
            sb.Append('"');
            sb.Append(value.Replace("\"", "\"\""));
            sb.Append('"');
        }
        else
        {
            sb.Append(value);
        }

        sb.Append('\t');
    }

    protected override void EndRow(StringBuilder sb)
    {
        if (sb.Length > 0 && sb[sb.Length - 1] == '\t')
            sb.Length--;

        sb.AppendLine();
    }
}
