using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

/// <summary>
/// A writer for the ARB (Application Resource Bundle) format.
/// </summary>
public class ArbWriter : BaseJsonWriter
{
    public override string FormatName => "ARB";

    public override string ConfigExtension => ".arbcfg";

    public override string TranslationExtension => ".arb";

    protected override void InternalWriteTranslations(TranslationManager translationManager, IReadOnlyCollection<CultureInfo> cultures, Stream stream)
    {
        if (translationManager is null)
            throw new ArgumentNullException(nameof(translationManager));
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        CultureInfo culture = cultures.First();
        Translation? translation = translationManager.GetTranslation(culture);

        if (translation is null)
            throw new TlumachException(string.Format(BaseWriter.ErrNoTranslationForCultureS1, culture.Name));

        StringBuilder sb = new();

        sb.AppendLine("{");

        bool isFirstProperty = true;

        // Write file-level metadata
        if (!string.IsNullOrEmpty(translation.Locale))
        {
            WriteJsonProperty("@@locale", translation.Locale!, isFirstProperty, sb);
            isFirstProperty = false;
        }

        if (!string.IsNullOrEmpty(translation.Context))
        {
            WriteJsonProperty("@@context", translation.Context!, isFirstProperty, sb);
            isFirstProperty = false;
        }

        if (!string.IsNullOrEmpty(translation.Author))
        {
            WriteJsonProperty("@@author", translation.Author!, isFirstProperty, sb);
            isFirstProperty = false;
        }

        if (translation.LastModified.HasValue)
        {
            string lastModifiedStr = translation.LastModified.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            WriteJsonProperty("@@last_modified", lastModifiedStr, isFirstProperty, sb);
            isFirstProperty = false;
        }

        // Write custom properties (@@x-*)
        foreach (var customProp in translation.CustomProperties)
        {
            WriteJsonProperty("@@x-" + customProp.Key, customProp.Value, isFirstProperty, sb);
            isFirstProperty = false;
        }

        // Get and sort entries
        List<TranslationEntry> entryList = GetSortedEntries(translation);
        
        // Write translation entries and their metadata
        foreach (var entry in entryList)
        {
            // Write the entry's text value
            string keyName = entry.Key;
            if (!string.IsNullOrEmpty(entry.Target))
                keyName = entry.Key + "@" + entry.Target;

            string value = ShouldWriteReference(entry) ? "@" + entry.Reference ?? string.Empty : entry.Text ?? string.Empty;

            WriteJsonProperty(keyName, value, isFirstProperty, sb);
            isFirstProperty = false;

            // Write entry metadata if any exists
            if (HasEntryMetadata(entry))
            {
                WriteEntryMetadata(entry, sb);
                isFirstProperty = false;
            }
        }

        sb.AppendLine();
        sb.Append("}");

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    private static bool HasEntryMetadata(TranslationEntry entry)
    {
        return !string.IsNullOrEmpty(entry.Description)
            || !string.IsNullOrEmpty(entry.Type)
            || !string.IsNullOrEmpty(entry.Context)
            || !string.IsNullOrEmpty(entry.SourceText)
            || !string.IsNullOrEmpty(entry.Screen)
            || !string.IsNullOrEmpty(entry.Video)
            || (entry.Placeholders is not null && entry.Placeholders.Count > 0);
    }

    private static void WriteEntryMetadata(TranslationEntry entry, StringBuilder sb)
    {
        sb.AppendLine(",");
        sb.Append("  \"@").Append(entry.Key).AppendLine("\": {");

        bool isFirstMetadata = true;
        string metadataIndent = "    ";

        if (!string.IsNullOrEmpty(entry.Description))
        {
            sb.Append(metadataIndent);
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).Append("\"description\": ").Append(Utils.JsonEncode(entry.Description!));
            isFirstMetadata = false;
        }

        if (!string.IsNullOrEmpty(entry.Type))
        {
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).Append("\"type\": ").Append(Utils.JsonEncode(entry.Type!));
            isFirstMetadata = false;
        }

        if (!string.IsNullOrEmpty(entry.Context))
        {
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).Append("\"context\": ").Append(Utils.JsonEncode(entry.Context!));
            isFirstMetadata = false;
        }

        if (!string.IsNullOrEmpty(entry.SourceText))
        {
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).Append("\"source_text\": ").Append(Utils.JsonEncode(entry.SourceText!));
            isFirstMetadata = false;
        }

        if (!string.IsNullOrEmpty(entry.Screen))
        {
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).Append("\"screen\": ").Append(Utils.JsonEncode(entry.Screen!));
            isFirstMetadata = false;
        }

        if (!string.IsNullOrEmpty(entry.Video))
        {
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).Append("\"video\": ").Append(Utils.JsonEncode(entry.Video!));
            isFirstMetadata = false;
        }

        if (entry.Placeholders is not null && entry.Placeholders.Count > 0)
        {
            if (!isFirstMetadata)
                sb.AppendLine(",");
            sb.Append(metadataIndent).AppendLine("\"placeholders\": {");

            bool isFirstPlaceholder = true;
            string placeholderIndent = "      ";

            foreach (var placeholder in entry.Placeholders)
            {
                if (!isFirstPlaceholder)
                    sb.AppendLine(",");

                sb.Append(placeholderIndent).Append('"').Append(placeholder.Name).AppendLine("\": {");

                bool isFirstPlaceholderProp = true;
                string placeholderPropIndent = "        ";

                if (!string.IsNullOrEmpty(placeholder.Type))
                {
                    sb.Append(placeholderPropIndent).Append("\"type\": ").Append(Utils.JsonEncode(placeholder.Type!));
                    isFirstPlaceholderProp = false;
                }

                if (!string.IsNullOrEmpty(placeholder.Format))
                {
                    if (!isFirstPlaceholderProp)
                        sb.AppendLine(",");
                    sb.Append(placeholderPropIndent).Append("\"format\": ").Append(Utils.JsonEncode(placeholder.Format!));
                    isFirstPlaceholderProp = false;
                }

                if (!string.IsNullOrEmpty(placeholder.Example))
                {
                    if (!isFirstPlaceholderProp)
                        sb.AppendLine(",");
                    sb.Append(placeholderPropIndent).Append("\"example\": ").Append(Utils.JsonEncode(placeholder.Example!));
                    isFirstPlaceholderProp = false;
                }

                // Write additional properties
                foreach (var prop in placeholder.Properties)
                {
                    if (!isFirstPlaceholderProp)
                        sb.AppendLine(",");
                    sb.Append(placeholderPropIndent).Append('"').Append(prop.Key).Append("\": ").Append(Utils.JsonEncode(prop.Value));
                    isFirstPlaceholderProp = false;
                }

                // Write optional parameters if any exist
                if (placeholder.OptionalParameters.Count > 0)
                {
                    if (!isFirstPlaceholderProp)
                        sb.AppendLine(",");
                    sb.Append(placeholderPropIndent).AppendLine("\"optionalParameters\": {");

                    bool isFirstOptionalParam = true;
                    string optionalParamIndent = "          ";

                    foreach (var optParam in placeholder.OptionalParameters)
                    {
                        if (!isFirstOptionalParam)
                            sb.AppendLine(",");

                        sb.Append(optionalParamIndent).Append('"').Append(optParam.Key).Append("\": ").Append(Utils.JsonEncode(optParam.Value));
                        isFirstOptionalParam = false;
                    }

                    sb.Append('\n').Append(placeholderPropIndent).Append('}');
                }

                sb.Append('\n').Append(placeholderIndent).Append('}');
                isFirstPlaceholder = false;
            }

            sb.Append('\n').Append(metadataIndent).Append('}');
        }

        sb.Append('\n').Append("  }");
    }
}
