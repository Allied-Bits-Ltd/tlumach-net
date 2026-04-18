using System;
using System.Collections.Generic;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for the simple INI format.
/// </summary>
public class IniWriter : BaseKeyValueWriter
{
    public override string FormatName => "Ini";

    public override string ConfigExtension => ".cfg";

    public override string TranslationExtension => ".ini";

    protected override void WriteSection(string key, StringBuilder stringBuilder)
    {
        stringBuilder.Append('[').Append(key).AppendLine("]").AppendLine();
    }

    protected override void WriteKeyValueLine(string key, string value, StringBuilder stringBuilder)
    {
        stringBuilder.Append(key).Append('=');

        stringBuilder.AppendLine(value);
    }

    protected override bool ShouldWriteReference(TranslationEntry entry) => false;
}
