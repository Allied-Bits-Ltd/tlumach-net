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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tlumach.Base
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
        /// <param name="value">The value of the variable for the given placeholder name.</param>
        /// <param name="getParamValueFunc">The function that returns the value of the placeholder by its name or index. It is used for inner placeholders with no index, so the index passed to it is always -1.</param>
        /// <param name="culture">The culture to use in conversions.</param>
        /// <param name="pluralCategory">An optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>The resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">thrown if an error or an unsupported ICU feature is detected.</exception>
        public static string? Evaluate(string content, object value, Func<string, int, object?> getParamValueFunc, CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
        {
            content = content.Trim();

            var reader = new Reader(content);
            var name = reader.ReadIdentifier(out bool simpleIdentifier);

            if (simpleIdentifier)
                return value.ToString() ?? string.Empty;

            return EvaluateNoName(content.Substring(name.Length), value, getParamValueFunc, culture, pluralCategory);
        }

        /// <summary>
        /// Tries to parse the ICU-compatible tail of a placeholder and produce a text result.
        /// </summary>
        /// <param name="content">The tail to parse. The tail is everything after the placeholder name, which the caller deals with.</param>
        /// <param name="value">The value of the variable for the given placeholder name.</param>
        /// <param name="getParamValueFunc">The function that returns the value of the placeholder by its name or index. It is used for inner placeholders with no index, so the index passed to it is always -1.</param>
        /// <param name="culture">The culture to use in conversions.</param>
        /// <param name="pluralCategory">An optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>The resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">thrown if an error or an unsupported ICU feature is detected.</exception>
        internal static string? EvaluateNoName(string content, object value, Func<string, int, object?> getParamValueFunc, CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
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

            if (kind == "select")
            {
                var options = ReadOptions(reader);
                var key = value != null ? value.ToString()! : "other";

                if (!options.TryGetValue(key, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new TemplateParserException("ICU select: missing 'other' branch in 'select'");

                return RenderText(chosen, getParamValueFunc);
            }
            else
            if (kind == "plural")
            {
                // plural header can contain things like: offset:1
                int offset = 0;

                if (reader.Peek() != '{')
                {
                    // read tokens until '{'
                    while (true)
                    {
                        reader.SkipWs();
                        if (reader.Peek() == '{')
                            break;

                        var tok = reader.ReadIdentifier(out _);
                        reader.SkipWs();

                        if (tok.Equals("offset", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!reader.TryReadChar(':'))
                                throw new TemplateParserException("plural: expected ':' after offset");

                            var offNum = reader.ReadNumberToken(); // integer
                            offset = int.Parse(offNum, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            throw new TemplateParserException($"plural: unexpected token '{tok}'");
                        }

                        reader.SkipWs();
                    }
                }

                var options = ReadOptions(reader);

                if (!TryGetNumeric(value, out var n))
                    n = 0m;

                // exact match first (=n)
                if (options.TryGetValue("=" + n.ToString(CultureInfo.InvariantCulture), out var exact))
                    return RenderPluralText(exact, n, offset, value, getParamValueFunc, culture);

                var cat = pluralCategory(n, culture); // "one", "few", etc. (here: zero/one/other)

                if (!options.TryGetValue(cat, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new TemplateParserException("ICU plural: missing 'other' branch");

                return RenderPluralText(chosen, n, offset, value, getParamValueFunc, culture);
            }
            else
            {
                throw new TemplateParserException($"ICU kind '{kind}' not supported (supported: select, plural)");
            }
        }

#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
        private static string RenderPluralText(string template, decimal n, int offset, object? value, Func<string, int, object?> getParamValueFunc, CultureInfo culture)
        {
            // Replace '#' with (n - offset) using culture
            var number = n - offset;
            var replaced = template.Replace("#", number.ToString(culture));

            return RenderText(replaced, getParamValueFunc);
        }
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

        // Very small {name} expander for nested simple placeholders inside branch text.
        // Escaping/advanced ICU nesting is intentionally out of scope for this subset.
        private static string RenderText(string s, Func<string, int, object?> getParamValueFunc)
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
                        val = getParamValueFunc(inner, -1);

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

        private static Dictionary<string, string> ReadOptions(Reader reader)
        {
            // Grammar: '{' (key '{' text '}' )+ '}'
            reader.SkipWs();
            if (!reader.TryReadChar('{'))
                throw new TemplateParserException("Expected '{' to start options");

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                reader.SkipWs();
                if (reader.Peek() == '}')
                {
                    reader.SkipChar();
                    break;
                }

                var key = reader.ReadOptionKey(); // e.g., "one", "other", "=2"
                reader.SkipWs();

                var text = reader.ReadBracedText(); // reads '{...}' and returns inner
                result[key] = text;
                reader.SkipWs();
            }

            return result;
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

            public Reader(string sourceText)
            {
                _sourceText = sourceText;
                _offset = 0;
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
        }
    }
}
