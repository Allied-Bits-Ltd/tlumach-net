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
    /// <inheritdoc/>
    public override string FormatName => "INI";

    /// <inheritdoc/>
    public override string ConfigExtension => ".cfg";

    /// <inheritdoc/>
    public override string TranslationExtension => ".ini";

    /// <inheritdoc/>
    protected override void WriteSection(string key, StringBuilder stringBuilder)
    {
        stringBuilder.Append('[').Append(key).AppendLine("]").AppendLine();
    }

    /// <inheritdoc/>
    protected override void WriteKeyValueLine(string key, string value, StringBuilder stringBuilder)
    {
        stringBuilder.Append(key).Append('=');

        stringBuilder.AppendLine(value);
    }

    /// <inheritdoc/>
    protected override bool ShouldWriteReference(TranslationEntry entry) => false;
}
