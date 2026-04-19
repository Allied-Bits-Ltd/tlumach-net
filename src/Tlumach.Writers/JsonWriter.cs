using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for the basic Json format.
/// </summary>
public class JsonWriter : BaseJsonWriter
{
    public override string FormatName => "Json";

    public override string ConfigExtension => ".jsoncfg";

    public override string TranslationExtension => ".json";

    protected override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));

        CultureInfo culture = cultures.First();
        Translation? translation = translationManager.GetTranslation(culture);

        if (translation is null)
            throw new TlumachException(string.Format(BaseWriter.ErrNoTranslationForCultureS1, culture.Name));

        StringBuilder sb = new();

        List<TranslationEntry> entryList = GetSortedEntries(translation);

        List<TranslationEntry>.Enumerator enumerator = entryList.GetEnumerator();
        if (enumerator.MoveNext())
            InternalWriteSection(enumerator, string.Empty, 0, sb);

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    private bool InternalWriteSection(List<TranslationEntry>.Enumerator enumerator, string currentGroup, int indent, StringBuilder sb)
    {
        bool result = true;
        bool firstInObject = true;

        // Pick current value. The caller must have positioned the pointer properly.
        TranslationEntry entry;

        sb.AppendLine("{"); // No indentation here because in the root, it is absent, and in children, the curly bracket goes on the same line with the group name.

        string indentString = new string(' ', (indent + 1) * IndentationStep);

        while (result)
        {
            entry = enumerator.Current;

            string key = entry.Key;

            (string section, string keyName) = GetSectionAndKeyName(key);

            if (!section.Equals(currentGroup, StringComparison.OrdinalIgnoreCase))
            {
                if (IsParentKey(currentGroup, section))
                {
                    // We have found some child key. Obtain the immediate child of the current group in section (e.g., from "a" and "a.b.c.d" obtain "b") and write it as a new section.
                    string? child = GetImmediateChild(currentGroup, section);
                    if (string.IsNullOrEmpty(child))
                        break;

                    sb.Append(indentString).Append('"').Append(child).Append("\": ");
                    result = InternalWriteSection(enumerator, currentGroup + '.' + child, indent + 1, sb);

                    // When the child session is written, it either writes the entry (so we don't need to write it again and may skip to the next entry) or leaves the entry unwritten.
                    // In the latter case, we will pick the same entry on the next iteration and decide what to do with it.
                    continue;
                }
                else
                {
                    break;
                }
            }

            // Write a value
            if (!firstInObject)
                sb.AppendLine(",");

            string value = ShouldWriteReference(entry) ? "@" + entry.Reference ?? string.Empty : entry.Text ?? string.Empty;

            sb.Append(indentString).Append('"').Append(keyName).Append("\": \"").Append(Utils.JsonEncode(value)).Append('"');
            firstInObject = false;

            result = enumerator.MoveNext();
        }

        if (indent > 0)
            sb.Append(new string(' ', indent * IndentationStep));

        sb.AppendLine("}");

        return result;
    }
}
