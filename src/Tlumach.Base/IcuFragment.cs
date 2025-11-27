// <copyright file="IcuFragment.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
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

// The formats of ICU fragments are defined on https://github.com/unicode-org/icu/tree/main/docs/userguide/format_parse and around.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// The parser to handle ICU-compatible placeholders in templated strings.
    /// </summary>
    internal static class IcuFragment
    {
        /// <summary>
        /// Tries to parse the ICU-compatible placeholder and produce a text result.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <param name="placeholderIndex">The index of the placeholder in the translated string.</param>
        /// <param name="value">The value of the variable for the given placeholder name.</param>
        /// <param name="getParamValueFunc">The function that returns the value of the placeholder by its name or index. It is used for inner placeholders with no index, so the index passed to it is always -1.</param>
        /// <param name="culture">The culture to use in conversions.</param>
        /// <param name="pluralCategory">An optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>The resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">Thrown if an error or an unsupported ICU feature is detected.</exception>
        public static string? Evaluate(string content, ref int placeholderIndex, object value, Func<string, int, object?> getParamValueFunc, CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
        {
            content = content.Trim();

            var reader = new Reader(content);
            var name = reader.ReadIdentifier(out bool simpleIdentifier);

            if (simpleIdentifier)
                return value.ToString() ?? string.Empty;

            return EvaluateNoName(content.Substring(name.Length), ref placeholderIndex, value, getParamValueFunc, culture, pluralCategory);
        }

        /// <summary>
        /// Tries to parse the ICU-compatible tail of a placeholder and produce a text result.
        /// </summary>
        /// <param name="content">The tail to parse. The tail is everything after the placeholder name, which the caller deals with.</param>
        /// <param name="placeholderIndex">The index of the placeholder in the translated string.</param>
        /// <param name="value">The value of the variable for the given placeholder name.</param>
        /// <param name="getParamValueFunc">The function that returns the value of the placeholder by its name or index. It is used for inner placeholders with no index, so the index passed to it is always -1.</param>
        /// <param name="culture">The culture to use in conversions.</param>
        /// <param name="pluralCategory">An optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>The resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">thrown if an error or an unsupported ICU feature is detected.</exception>
        internal static string? EvaluateNoName(string content, ref int placeholderIndex, object value, Func<string, int, object?> getParamValueFunc, CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
        {
            culture ??= CultureInfo.InvariantCulture;
            pluralCategory ??= SimplePluralCategory; // swap with a CLDR-aware resolver later

            content = content.Trim();

            // Try to parse: " , <kind> , <body>"
            // skip the name as we have one
            var reader = new Reader(content);

            reader.SkipWs();
            if (!reader.TryReadChar(',')) // Not ICU pattern; treat as simple`
                return null;

#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            reader.SkipWs();
            var kind = reader.ReadIdentifier(out _).ToLowerInvariant();
            reader.SkipWs();
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'

            if (!reader.TryReadChar(','))
                throw new TemplateParserException("ICU fragment: expected second comma");

            reader.SkipWs();

            if(kind == "select")
            {
                var options = ReadOptions(reader);
                var key = value is not null ? value.ToString()! : "other";

                if (!options.TryGetValue(key, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new TemplateParserException("ICU select: missing 'other' branch");

                return RenderText(chosen, ref placeholderIndex, getParamValueFunc);
            }
            else
            if (kind == "plural")
            {
                // Parse optional "offset:n" if present; otherwise leave stream untouched.
                int offset = 0;
                int mark = reader.Position;
                if (reader.TryReadIdentifier(out var maybeOffset) &&
                    maybeOffset.Equals("offset", StringComparison.OrdinalIgnoreCase))
                {
                    reader.SkipWs();
                    if (!reader.TryReadChar(':'))
                    {
                        reader.Restore(mark); // not actually an offset header → rewind
                    }
                    else
                    {
                        var offNum = reader.ReadNumberToken();
                        offset = int.Parse(offNum, CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    reader.Restore(mark); // no identifier at all (could be "=0" inline key) → rewind
                }

                reader.SkipWs();

                var options = ReadOptions(reader); // now supports inline and grouped

                if (!TryGetNumeric(value, out var n))
                    n = 0m;

                // exact match first (=n)
                if (options.TryGetValue("=" + n.ToString(CultureInfo.InvariantCulture), out var exact))
                    return RenderPluralText(exact, n, offset, ref placeholderIndex, value, getParamValueFunc, culture);

                var cat = pluralCategory(n, culture); // "one", "few", ...
                if (!options.TryGetValue(cat, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new FormatException("ICU plural: missing 'other' branch");

                return RenderPluralText(chosen, n, offset, ref placeholderIndex, value, getParamValueFunc, culture);
            }
            else
            if (kind == "number")
            {
                var numOpts = ReadNumberOptions(reader);
                if (!TryGetNumeric(value, out var n))
                    n = 0m;
                return FormatNumber(n, numOpts, culture);
            }
            else
            {
                throw new TemplateParserException($"ICU kind '{kind}' not supported (supported: select, plural)");
            }
        }

        // ---------- number ----------

        private sealed class NumberOptions
        {
#pragma warning disable SA1401
            internal string Style = "number";            // "number" | "integer" | "percent" | "currency" | "custom" | "compact-short"
            internal string? CurrencyIso;                // e.g., "USD"
            internal string? CustomDotNetFormat;         // e.g., "#,0.##"
#pragma warning restore SA1401
        }

        private static NumberOptions ReadNumberOptions(Reader r)
        {
            // Accept one of:
            //   - integer
            //   - percent
            //   - currency
            //   - currency:USD
            //   - 'custom .NET format'
            //   - ::compact-short
            //   - (nothing) => default "number"
            var opts = new NumberOptions();

            // If the next token starts a branch, there are no options (we don't use branch text for number)
            r.SkipWs();

            if (r.Peek() == '{' || r.Peek() == '\0')
                return opts;

            // Parse up to end (or a stray '{' which we don't expect for number)
            // We accept one primary style token.
            if (r.TryReadLiteral("::"))
            {
                // ICU skeleton-ish
                var ident = r.ReadIdentifier(out _).ToLowerInvariant(); // e.g., "compact-short"

                if (ident == "compact-short")
                {
                    opts.Style = "compact-short";
                    return opts;
                }

                throw new FormatException($"number: unsupported skeleton '::{ident}'.");
            }

            // Quoted custom .NET format
            if (r.Peek() == '\'')
            {
                var fmt = r.ReadQuoted(); // content inside single quotes
                opts.Style = "custom";
                opts.CustomDotNetFormat = fmt;
                return opts;
            }

            // Identifier styles
            var style = r.ReadIdentifier(out _).ToLowerInvariant();
            switch (style)
            {
                case "number":
                    opts.Style = "number";
                    break;

                case "integer":
                    opts.Style = "integer";
                    break;

                case "percent":
                    opts.Style = "percent";
                    break;

                case "currency":
                    opts.Style = "currency";
                    r.SkipWs();
                    if (r.TryReadChar(':'))
                    {
                        r.SkipWs();
                        // ISO code token (letters)
                        var iso = r.ReadIdentifier(out _);
                        opts.CurrencyIso = iso.ToUpperInvariant();
                    }

                    break;

                default:
                    throw new FormatException($"number: unsupported style '{style}'.");
            }

            return opts;
        }

        private static string FormatNumber(decimal value, NumberOptions opts, CultureInfo culture)
        {
            switch (opts.Style)
            {
                case "custom":
                    return value.ToString(opts.CustomDotNetFormat ?? "G", culture);

                case "integer":
                    return Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("N0", culture);

                case "percent":
                    // .NET expects fraction input for "P"
                    return value.ToString("P", culture);

                case "currency":
                    {
                        // If ISO matches culture's, use symbol formatting; else prefix ISO.
                        var region = TryRegion(culture);
                        var iso = opts.CurrencyIso ?? region?.ISOCurrencySymbol;
                        if (iso is not null && region is not null && string.Equals(iso, region.ISOCurrencySymbol, StringComparison.OrdinalIgnoreCase))
                        {
                            // Native currency formatting for the culture
                            return value.ToString("C", culture);
                        }
                        else if (iso is not null)
                        {
                            // Fallback: prefix ISO code + localized number
                            var amount = value.ToString("N2", culture);
                            return iso + " " + amount;
                        }
                        else
                        {
                            // No ISO at all; use culture currency symbol formatting
                            return value.ToString("C", culture);
                        }
                    }

                case "compact-short":
                    return FormatCompactShort(value, culture);

                case "number":
                default:
                    // Locale-aware general number with grouping; keep decimals as needed.
                    return value.ToString("N", culture);
            }
        }

        private static string FormatCompactShort(decimal value, CultureInfo culture)
        {
            // Simple, culture-agnostic short compact: K, M, B, T with one decimal when needed.
            // Sign-aware; uses culture decimal separator via standard formatting.
            var abs = Math.Abs(value);
            string suffix;
            decimal scaled;

            if (abs >= 1_000_000_000_000m)
            {
                suffix = "T";
                scaled = value / 1_000_000_000_000m;
            }
            else
            if (abs >= 1_000_000_000m)
            {
                suffix = "B";
                scaled = value / 1_000_000_000m;
            }
            else
            if (abs >= 1_000_000m)
            {
                suffix = "M";
                scaled = value / 1_000_000m;
            }
            else
            if (abs >= 1_000m)
            {
                suffix = "K";
                scaled = value / 1_000m;
            }
            else
            {
                return value.ToString("N0", culture);
            }

            // One decimal when < 10; otherwise no decimals.
            var format = (Math.Abs(scaled) < 10m) ? "0.#" : "0";

            return scaled.ToString(format, culture) + suffix;
        }

        private static RegionInfo? TryRegion(CultureInfo culture)
        {
            try
            {
                // For neutral cultures, this may throw
                return new RegionInfo(culture.Name);
            }
            catch(ArgumentException)
            {
                return null;
            }
        }

#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
        private static string RenderPluralText(string template, decimal n, int offset, ref int placeholderIndex, object? value, Func<string, int, object?> getParamValueFunc, CultureInfo culture)
        {
            // Replace '#' with (n - offset) using culture
            var number = n - offset;
            var replaced = template.Replace("#", number.ToString(culture));

            return RenderText(replaced, ref placeholderIndex, getParamValueFunc);
        }
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

        // Very small {name} expander for nested simple placeholders inside branch text.
        // Escaping/advanced ICU nesting is intentionally out of scope for this subset.
        private static string RenderText(string s, ref int placeholderIndex, Func<string, int, object?> getParamValueFunc)
        {
            var sb = new StringBuilder();
            int i = 0;

            while (i < s.Length)
            {
                if (s[i] == '{')
                {
                    int start = i + 1;
                    int openBraceCount = 1;

                    i++;

                    while (i < s.Length && openBraceCount > 0)
                    {
                        if (s[i] == '{')
                            openBraceCount++;
                        else
                        if (s[i] == '}')
                            openBraceCount--;
                        i++;
                    }

                    if (openBraceCount != 0)
                        throw new TemplateParserException("Unbalanced braces in branch");

                    var inner = s.Substring(start, (i - 1) - start).Trim();

                    object? val = null;

                    if (Utils.IsIdentifier(inner))
                    {
                        placeholderIndex++;
                        val = getParamValueFunc(inner, placeholderIndex);
                    }

                    // Only support simple identifiers inside branch text
                    if (val is not null)
                        sb.Append(val.ToString());
                    else
                        sb.Append(string.Empty);

                    i--; // i points at '}' right now; loop will ++
                }
                else
                {
                    sb.Append(s[i]);
                }

                i++;
            }

            return sb.ToString();
        }

        private static Dictionary<string, string> ReadOptions(Reader r)
        {
            r.SkipWs();

            // GROUPED: "{ key{...} key{...} }"
            if (r.TryReadChar('{'))
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    r.SkipWs();
                    if (r.Peek() == '}')
                    {
                        r.ReadChar();
                        break;
                    }

                    var key = r.ReadOptionKey();   // "=2" | "one" | "other"
                    r.SkipWs();
                    var text = r.ReadBracedText(); // "{...}" → inner text
                    result[key] = text;
                    r.SkipWs();
                }

                return result;
            }

            // INLINE: "key{...} key{...}" until end of string
            {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    r.SkipWs();
                    if (r.EOF)
                        break;

                    var key = r.ReadOptionKey();   // "=2" | "one" | "other"
                    r.SkipWs();
                    var text = r.ReadBracedText(); // "{...}"
                    result[key] = text;
                    r.SkipWs();
                }

                return result;
            }
        }

        private static bool TryGetNumeric(object value, out decimal n)
        {
            n = 0;

            switch (value)
            {
                case byte b: n = b; return true;
                case sbyte sb: n = sb; return true;
                case short s: n = s; return true;
                case ushort us: n = us; return true;
                case int i: n = i; return true;
                case uint ui: n = ui; return true;
                case long l: n = l; return true;
                case ulong ul: n = ul; return true;
                case float f: n = (decimal)f; return true;
                case double d: n = (decimal)d; return true;
                case decimal dec: n = dec; return true;
                case string str when decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var dv):
                    n = dv; return true;
                default: return false;
            }
        }

        private static string SimplePluralCategory(decimal num, CultureInfo? culture)
        {
            return num switch
            {
                0 => "zero",
                1 => "one",
                2 => "two",
                _ => "other",
            };
        }

        private sealed class Reader
        {
            private readonly string _sourceText;

            private int _offset;

            public int Position => _offset;

            public bool EOF => _offset >= _sourceText.Length;

            public Reader(string sourceText)
            {
                _sourceText = sourceText;
                _offset = 0;
            }

            public void Restore(int pos)
            {
                _offset = pos;
            }

            public char Peek() => _offset < _sourceText.Length ? _sourceText[_offset] : '\0';

            public bool SkipChar()
            {
                if (_offset < _sourceText.Length)
                {
                    _offset++;
                    return true;
                }

                return false;
            }

            public char ReadChar() => _offset < _sourceText.Length ? _sourceText[_offset++] : '\0';

            public void SkipWs()
            {
                while (_offset < _sourceText.Length && char.IsWhiteSpace(_sourceText[_offset]))
                    _offset++;
            }

            public bool TryReadChar(char c)
            {
                if (Peek() == c)
                {
                    _offset++;
                    return true;
                }

                return false;
            }

            public bool TryReadLiteral(string literal)
            {
                if (_offset + literal.Length > _sourceText.Length)
                    return false;

                for (int j = 0; j < literal.Length; j++)
                {
                    if (_sourceText[_offset + j] != literal[j])
                        return false;
                }

                _offset += literal.Length;
                return true;
            }

            public bool TryReadIdentifier(out string ident)
            {
                SkipWs();

                if (_offset >= _sourceText.Length || !(char.IsLetter(_sourceText[_offset]) || _sourceText[_offset] == '_'))
                {
                    ident = string.Empty;
                    return false;
                }

                int start = _offset++;

                while (_offset < _sourceText.Length && (char.IsLetterOrDigit(_sourceText[_offset]) || _sourceText[_offset] == '_'))
                    _offset++;

                ident = _sourceText.Substring(start, _offset - start);

                return true;
            }

            public string ReadIdentifier(out bool simpleIdentifier)
            {
                SkipWs();
                int start = _offset;

                if (_offset >= _sourceText.Length || !(char.IsLetter(_sourceText[_offset]) || _sourceText[_offset] == '_'))
                    throw new TemplateParserException("Could not find an expected identifier");

                _offset++;

                while (_offset < _sourceText.Length && (char.IsLetterOrDigit(_sourceText[_offset]) || _sourceText[_offset] == '_'))
                    _offset++;

                simpleIdentifier = _offset == _sourceText.Length;

                return _sourceText.Substring(start, _offset - start);
            }

            public string ReadNumberToken()
            {
                SkipWs();
                int start = _offset;

                if (_offset < _sourceText.Length && (_sourceText[_offset] == '+' || _sourceText[_offset] == '-'))
                    _offset++;

                bool any = false;

                while (_offset < _sourceText.Length && char.IsDigit(_sourceText[_offset]))
                {
                    _offset++;
                    any = true;
                }

                if (!any)
                    throw new TemplateParserException("Could not find an expected number");

                return _sourceText.Substring(start, _offset - start);
            }

            public string ReadOptionKey()
            {
                SkipWs();
                if (Peek() == '=')
                {
                    if (!SkipChar())
                        throw new TemplateParserException("Unexpected end of expression");

                    var num = ReadNumberToken();
                    return "=" + num;
                }
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                return ReadIdentifier(out _).ToLowerInvariant();
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            }

            public string ReadBracedText()
            {
                SkipWs();

                if (!TryReadChar('{'))
                    throw new TemplateParserException("Could not find an expected '{' starting branch text");

                int depth = 1;
                int start = _offset;

                while (_offset < _sourceText.Length && depth > 0)
                {
                    if (_sourceText[_offset] == '{')
                        depth++;
                    else
                    if (_sourceText[_offset] == '}')
                        depth--;

                    _offset++;
                }

                if (depth != 0)
                    throw new TemplateParserException("Unbalanced braces in branch text");

                // remove outer braces
                return _sourceText.Substring(start, (_offset - 1) - start);
            }

            public string ReadQuoted()
            {
                SkipWs();

                if (!TryReadChar(Utils.C_SINGLE_QUOTE))
                    throw new FormatException("Expected starting single quote for custom format.");

                var sb = new StringBuilder();
                while (_offset < _sourceText.Length)
                {
                    var c = ReadChar();
                    if (c == '\0') break;
                    if (c == Utils.C_SINGLE_QUOTE)
                    {
                        // doubled quotes -> escaped '
                        if (Peek() == Utils.C_SINGLE_QUOTE)
                        {
                            ReadChar();
                            sb.Append(Utils.C_SINGLE_QUOTE);
                            continue;
                        }

                        // closing
                        return sb.ToString();
                    }

                    sb.Append(c);
                }

                throw new FormatException("Unterminated quoted string.");
            }
        }
    }
}
