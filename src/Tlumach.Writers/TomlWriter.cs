using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Writers;

internal class TomlWriter : BaseKeyValueWriter
{
    public override string FormatName => "TOML";

    public override string ConfigExtension => ".tomlcfg";

    public override string TranslationExtension => ".toml";

    public override void WriteConfiguration(TranslationManager translationManager, Stream stream)
    {
        TranslationConfiguration? config = translationManager.DefaultConfiguration;
        if (config is null)
            throw new TlumachException(BaseWriter.ErrNoConfigInTranslationManager);

        StringBuilder sb = new();

        if (!string.IsNullOrEmpty(config.DefaultFile))
            WriteKeyValueLine(TranslationConfiguration.KEY_DEFAULT_FILE, config.DefaultFile, sb);
        if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            WriteKeyValueLine(TranslationConfiguration.KEY_DEFAULT_LOCALE, config.DefaultFileLocale, sb);
        if (!string.IsNullOrEmpty(config.Namespace))
            WriteKeyValueLine(TranslationConfiguration.KEY_GENERATED_NAMESPACE, config.Namespace, sb);
        if (!string.IsNullOrEmpty(config.ClassName))
            WriteKeyValueLine(TranslationConfiguration.KEY_GENERATED_CLASS, config.ClassName, sb);

        WriteKeyValueLine(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, config.DelayedUnitsCreation ? "true" : "false", sb);
        WriteKeyValueLine(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, config.OnlyDeclareKeys ? "true" : "false", sb);

        if (config.TextProcessingMode.HasValue)
            WriteKeyValueLine(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, config.TextProcessingMode.ToString() ?? string.Empty, sb);

        if (config.Translations.Count > 0)
        {
            // Write a section name
            WriteSection(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, sb);

            foreach (KeyValuePair<string, string> kvp in config.Translations)
                WriteKeyValueLine(kvp.Key, kvp.Value, sb);
        }

        byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
        stream.Write(buf, 0, buf.Length);
    }

    private bool KeyRequiresQuotes(string key)
    {
        if (string.IsNullOrEmpty(key))
            return true;

        foreach (char c in key)
        {
            bool isBare =
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '_' ||
                c == '-';

            if (!isBare)
                return true;
        }

        return false;
    }

    protected override void WriteSection(string key, StringBuilder stringBuilder)
    {
        if (KeyRequiresQuotes(key))
            stringBuilder.Append("[\"").Append(key).AppendLine("\"]").AppendLine();
        else
            stringBuilder.Append('[').Append(key).AppendLine("]").AppendLine();
    }

    protected override void WriteKeyValueLine(string key, string value, StringBuilder stringBuilder)
    {
        if (KeyRequiresQuotes(key))
            stringBuilder.Append('"').Append(key).Append("\"=");
        else
            stringBuilder.Append(key).Append('=');

        stringBuilder.AppendLine(TomlStringQuoter.QuoteTomlString(value));
    }

    protected override bool WriteReference(TranslationEntry entry) => !string.IsNullOrEmpty(entry.Reference);

    public static class TomlStringQuoter
    {
        public static string QuoteTomlString(string value)
        {
            if (value.Length == 0)
                return "\"\"";

            bool isMultiLine = ContainsNewline(value!);

            if (!isMultiLine)
            {
                if (CanUseSingleLineLiteral(value))
                    return "'" + value + "'";

                return "\"" + EscapeText(value, multiLine: false) + "\"";
            }

            if (CanUseMultiLineLiteral(value))
                return QuoteMultiLineLiteral(value);

            return QuoteMultiLineBasic(value);
        }

        private static bool CanUseSingleLineLiteral(string value)
        {
            if (ContainsNewline(value))
                return false;

#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
            if (value.Contains('\''))
                return false;
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

            foreach (char ch in value)
            {
                if (char.IsControl(ch))
                    return false;
            }

            return true;
        }

        private static bool CanUseMultiLineLiteral(string value)
        {
            if (!ContainsNewline(value))
                return false;

            if (value.Contains("'''", StringComparison.Ordinal))
                return false;

            foreach (char ch in value)
            {
                if (IsDisallowedInMultiLineLiteral(ch))
                    return false;
            }

            return true;
        }

        private static string QuoteMultiLineLiteral(string value)
        {
            if (StartsWithNewline(value))
                return "'''" + value + "'''";

            return "'''\n" + value + "'''";
        }

        private static string QuoteMultiLineBasic(string value)
        {
            string escaped = EscapeText(value, multiLine: true);

            if (StartsWithNewline(value))
                return "\"\"\"" + escaped + "\"\"\"";

            return "\"\"\"\n" + escaped + "\"\"\"";
        }

        public static string EscapeText(string value, bool multiLine)
        {
            var sb = new StringBuilder(value.Length + 16);
            int quoteRunLength = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];

                if (!multiLine && ch == '"')
                {
                    sb.Append("\\\"");
                    continue;
                }

                if (multiLine && ch == '"')
                {
                    quoteRunLength++;

                    if (quoteRunLength == 3)
                    {
                        sb.Append("\\\"");
                        quoteRunLength = 0;
                    }
                    else
                    {
                        sb.Append('"');
                    }

                    continue;
                }

                quoteRunLength = 0;

                switch (ch)
                {
                    case '\\':
                        sb.Append(@"\\");
                        break;

                    case '\b':
                        sb.Append(@"\b");
                        break;

                    case '\t':
                        sb.Append(@"\t");
                        break;

                    case '\n':
                        if (multiLine)
                            sb.Append('\n');
                        else
                            sb.Append(@"\n");
                        break;

                    case '\f':
                        sb.Append(@"\f");
                        break;

                    case '\r':
                        if (multiLine)
                            sb.Append('\r');
                        else
                            sb.Append(@"\r");
                        break;

                    default:
                        if (RequiresUnicodeEscape(ch))
                            AppendUnicodeEscape(sb, ch);
                        else
                            sb.Append(ch);

                        break;
                }
            }

            return sb.ToString();
        }

        private static bool RequiresUnicodeEscape(char ch)
        {
            return ch < 0x20 || ch == 0x7F;
        }

        private static void AppendUnicodeEscape(StringBuilder sb, char ch)
        {
            sb.Append(@"\u");
            sb.Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
        }

        private static bool ContainsNewline(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == '\r' || ch == '\n')
                    return true;
            }

            return false;
        }

        private static bool StartsWithNewline(string value)
        {
            if (value.Length == 0)
                return false;

            char ch = value[0];
            return ch == '\r' || ch == '\n';
        }

        private static bool IsDisallowedInMultiLineLiteral(char ch)
        {
            if (ch == '\r' || ch == '\n')
                return false;

            return char.IsControl(ch);
        }
    }
}
