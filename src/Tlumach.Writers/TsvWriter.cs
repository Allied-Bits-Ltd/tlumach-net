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
        sb.Append(value);
        sb.Append('\t');
    }

    protected override void EndRow(StringBuilder sb)
    {
        if (sb.Length > 0 && sb[sb.Length - 1] == '\t')
            sb.Length--;

        sb.AppendLine();
    }
}
