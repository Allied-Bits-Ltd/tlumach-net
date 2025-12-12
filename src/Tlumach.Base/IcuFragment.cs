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
        private enum DateTimeStyleOption
        {
            Short,
            Medium,
            Long,
            Full,
            Custom,
        }

        /// <summary>
        /// Tries to parse the ICU-compatible placeholder and produce a text result.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <param name="placeholderIndex">The index of the placeholder in the translated string.</param>
        /// <param name="value">The value of the variable for the given placeholder name.</param>
        /// <param name="getPlaceholderValueFunc">The function that returns the value of the placeholder by its name or index.</param>
        /// <param name="culture">The culture to use in conversions.</param>
        /// <param name="pluralCategory">An optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>The resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">Thrown if an error or an unsupported ICU feature is detected.</exception>
        public static string? Evaluate(string content, ref int placeholderIndex, object value, Func<string, int, (object?, int)> getPlaceholderValueFunc, /*Func<string, int, object?> getParamValueFunc, */CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
        {
            content = content.Trim();

            var reader = new Reader(content);
            var name = reader.ReadIdentifier(out bool simpleIdentifier);

            if (simpleIdentifier)
                return value.ToString() ?? string.Empty;

            return EvaluateNoName(content.Substring(name.Length), ref placeholderIndex, value, getPlaceholderValueFunc, /*getParamValueFunc, */culture, pluralCategory);
        }

        /// <summary>
        /// Tries to parse the ICU-compatible tail of a placeholder and produce a text result.
        /// </summary>
        /// <param name="content">The tail to parse. The tail is everything after the placeholder name, which the caller deals with.</param>
        /// <param name="placeholderIndex">The index of the placeholder in the translated string.</param>
        /// <param name="value">The value of the variable for the given placeholder name.</param>
        /// <param name="getPlaceholderValueFunc">The function that returns the value of the placeholder by its name or index.</param>
        /// <param name="culture">The culture to use in conversions.</param>
        /// <param name="pluralCategory">An optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>The resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">thrown if an error or an unsupported ICU feature is detected.</exception>
        internal static string? EvaluateNoName(string content, ref int placeholderIndex, object value, Func<string, int, (object?, int)> getPlaceholderValueFunc, /*Func<string, int, object?> getParamValueFunc, */CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
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

            bool doReadOptions = reader.TryReadChar(',');

            reader.SkipWs();

            if(string.Equals(kind, "select", StringComparison.OrdinalIgnoreCase))
            {
                if (!doReadOptions)
                    throw new TemplateParserException("ICU fragment: 'select' placeholders expect options as the third part of the placeholder");
                var options = ReadOptions(reader);
                var key = value?.ToString() ?? "other";

                if (!options.TryGetValue(key, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new TemplateParserException("ICU select: missing 'other' branch");

                return RenderPlaceholderText(chosen, ref placeholderIndex, getPlaceholderValueFunc/*getParamValueFunc*/);
            }
            else
            if (string.Equals(kind, "selectordinal", StringComparison.OrdinalIgnoreCase))
            {
                if (!doReadOptions)
                    throw new TemplateParserException("ICU fragment: 'select' placeholders expect options as the third part of the placeholder");
                var options = ReadOptions(reader);
                if (!TryGetNumeric(value, out var n))
                    n = 0m;

                var cat = OrdinalCategory(n, culture);
                if (!options.TryGetValue(cat, out var chosen) &&
                    !options.TryGetValue("other", out chosen))
                    throw new FormatException("ICU selectordinal: missing 'other' branch.");

                return RenderPlaceholderText(chosen.Replace("#", n.ToString(culture)), ref placeholderIndex, getPlaceholderValueFunc);
            }
            else
            if (string.Equals(kind, "plural", StringComparison.OrdinalIgnoreCase))
            {
                if (!doReadOptions)
                    throw new TemplateParserException("ICU fragment: 'select' placeholders expect options as the third part of the placeholder");

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
                    return RenderPluralText(exact, n, offset, ref placeholderIndex, value, getPlaceholderValueFunc, /*getParamValueFunc, */culture);

                var cat = pluralCategory(n, culture); // "one", "few", ...
                if (!options.TryGetValue(cat, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new FormatException("ICU plural: missing 'other' branch");

                return RenderPluralText(chosen, n, offset, ref placeholderIndex, value, getPlaceholderValueFunc, /*getParamValueFunc, */culture);
            }
            else
            if (string.Equals(kind, "number", StringComparison.OrdinalIgnoreCase))
            {
                var numOpts = doReadOptions
                    ? ReadNumberOptions(reader)
                    : new NumberOptions { Style = "integer" };
                if (!TryGetNumeric(value, out var n))
                    n = 0m;
                return FormatNumber(n, numOpts, culture);
            }
            else
            if (string.Equals(kind, "datetime", StringComparison.OrdinalIgnoreCase))
            {
                var dateOpts = doReadOptions
                    ? ReadDateOptions(reader)
                    : new DateTimeOptions();
                if (!TryGetDateTimeWithOffset(value, out var dto, out var hasOffset))
                    return string.Empty;
                return FormatDateTime(dto, hasOffset, dateOpts, culture);
            }
            else
            if (string.Equals(kind, "date", StringComparison.OrdinalIgnoreCase))
            {
                var dateOpts = doReadOptions
                    ? ReadDateOptions(reader)
                    : new DateTimeOptions();
                if (!TryGetDateTimeWithOffset(value, out var dto, out var hasOffset))
                    return string.Empty;
                return FormatDate(dto, hasOffset, dateOpts, culture);
            }
            else
            if (string.Equals(kind, "time", StringComparison.OrdinalIgnoreCase))
            {
                var timeOpts = doReadOptions
                    ? ReadTimeOptions(reader)
                    : new DateTimeOptions();
                if (!TryGetDateTimeWithOffset(value, out var dto, out var hasOffset))
                    return string.Empty;
                return FormatTime(dto, hasOffset, timeOpts, culture);
            }
            else
            {
                throw new TemplateParserException($"ICU kind '{kind}' not supported (supported: select, plural, number, selectordinal)");
            }
        }

        // todo: expand the method with i18n support for choosing between one, two, few, and other
        private static string OrdinalCategory(decimal n, CultureInfo culture)
        {
            // For now: English-like logic.
            int i = (int)n;
            int mod10 = i % 10;
            int mod100 = i % 100;

            if (mod10 == 1 && mod100 != 11)
                return "one"; // 1st, 21st

            if (mod10 == 2 && mod100 != 12)
                return "two"; // 2nd, 22nd

            if (mod10 == 3 && mod100 != 13)
                return "few"; // 3rd, 23rd

            return "other";                               // 4th, 11th, 12th, 13th, etc.
        }

        // ---------- date ----------

        private sealed class DateTimeOptions
        {
            public DateTimeStyleOption Style { get; set; } = DateTimeStyleOption.Medium; // "short" | "medium" | "long" | "full" | "custom"

            public string? CustomDotNetFormat { get; set; } // e.g., "yyyy-MM-dd"
        }

        private static DateTimeOptions ReadDateOptions(Reader r)
        {
            var opts = new DateTimeOptions();

            r.SkipWs();
            if (r.Peek() == '{' || r.Peek() == '\0')
                return opts;

            if (r.TryReadLiteral("::"))
            {
                // ICU date/time skeletons not supported here
                var ident = r.ReadIdentifier(out _);
                throw new FormatException($"date: ICU skeletons '::{ident}' are not supported.");
            }

            // Remember where options start so we can fall back to "whole remainder is pattern"
            int startPos = r.Position;

            if (r.Peek() == '\'')
            {
                var fmt = r.ReadQuoted();
                opts.Style = DateTimeStyleOption.Custom;
                opts.CustomDotNetFormat = fmt;
                return opts;
            }

            if (r.TryReadIdentifier(out var identToken))
            {
#pragma warning disable CA1308 // In method ..., replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                var style = identToken.ToLowerInvariant();
#pragma warning restore CA1308 // In method ..., replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                switch (style)
                {
                    case "short":
                        opts.Style = DateTimeStyleOption.Short;
                        return opts;
                    case "medium":
                        opts.Style = DateTimeStyleOption.Medium;
                        return opts;
                    case "long":
                        opts.Style = DateTimeStyleOption.Long;
                        return opts;
                    case "full":
                        opts.Style = DateTimeStyleOption.Full;
                        return opts;
                    default:
                        // Not a known style (e.g., "yyyy") → treat entire remainder as a custom pattern
                        r.Restore(startPos);
                        var patternFromIdent = r.ReadToEndOrBrace();
                        opts.Style = DateTimeStyleOption.Custom;
                        opts.CustomDotNetFormat = patternFromIdent.Trim();
                        return opts;
                }
            }

            // Case 3: no identifier (e.g., starts with digit/symbol) → entire tail is pattern
            r.Restore(startPos);
            var pattern = r.ReadToEndOrBrace();
            opts.Style = opts.Style = DateTimeStyleOption.Custom;
            opts.CustomDotNetFormat = pattern.Trim();
            return opts;
        }

        private static string FormatDate(DateTimeOffset value, bool hasOffset, DateTimeOptions opts, CultureInfo culture)
        {
            switch (opts.Style)
            {
                case DateTimeStyleOption.Custom:
                    return value.ToString(opts.CustomDotNetFormat ?? "d", culture);
                case DateTimeStyleOption.Short:
                    return value.ToString("d", culture); // short date
                case DateTimeStyleOption.Medium:
                    // Try culture-specific medium pattern first
                    if (!TryGetMediumDatePattern(culture, out var pattern))
                    {
                        // Fallback: use short date if we don't know better for this culture
                        pattern = culture.DateTimeFormat.ShortDatePattern;
                    }

                    return value.ToString(pattern, culture);

                case DateTimeStyleOption.Long:
                //return value.ToString("T", culture); // long/full time
                case DateTimeStyleOption.Full:
                    var baseDate = value.ToString("D", culture);

                    if (!hasOffset || value.Offset == TimeSpan.Zero)
                        return baseDate + (opts.Style == DateTimeStyleOption.Full ? " Universal Coordinated Time" : " UTC");

                    // e.g. "14:35:42 +01:00"
                    var offset = value.ToString(opts.Style == DateTimeStyleOption.Full ? "zzzz" : "z", culture);

                    return baseDate + " " + offset;

                default:
                    return value.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
            }
        }

        private static string FormatDateTime(DateTimeOffset value, bool hasOffset, DateTimeOptions opts, CultureInfo culture)
        {
            switch (opts.Style)
            {
                case DateTimeStyleOption.Custom:
                    return value.ToString(opts.CustomDotNetFormat ?? "G", culture);
                case DateTimeStyleOption.Short:
                    return value.ToString("g", culture); // short date/time
                case DateTimeStyleOption.Medium:
                    // Combine your medium date & time patterns, or fall back to culture's short ones.
                    if (!TryGetMediumDatePattern(culture, out var datePattern))
                        datePattern = culture.DateTimeFormat.ShortDatePattern;

                    if (!TryGetMediumTimePattern(culture, out var timePattern))
                        timePattern = culture.DateTimeFormat.ShortTimePattern;

                    var pattern = datePattern + " " + timePattern;
                    return value.ToString(pattern, culture);

                case DateTimeStyleOption.Long:

                case DateTimeStyleOption.Full:
                    var baseDateTime = value.ToString("D", culture) + ' ' + value.ToString("T", culture);

                    if (!hasOffset || value.Offset == TimeSpan.Zero)
                        return baseDateTime + (opts.Style == DateTimeStyleOption.Full ? " Universal Coordinated Time" : " UTC");

                    // e.g. "14:35:42 +01:00"
                    var offset = value.ToString(opts.Style == DateTimeStyleOption.Full ? "zzzz" : "z", culture);

                    return baseDateTime + " " + offset;

                default:
                    return value.ToString(culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern, culture);
            }
        }

        // CLDR-inspired medium date patterns, converted to .NET custom format strings.
        // Keys are culture names; we also keep language-only codes for neutral cultures.
        private static readonly Dictionary<string, string> MediumDatePatterns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // English
                ["en"] = "MMM d, yyyy",    // Jan 12, 1952
                ["en-US"] = "MMM d, yyyy",
                ["en-GB"] = "d MMM yyyy",
                ["en-CA"] = "MMM d, yyyy",
                ["en-AU"] = "d MMM yyyy",

                // German
                ["de"] = "dd.MM.yyyy",
                ["de-DE"] = "dd.MM.yyyy",
                ["de-AT"] = "dd.MM.yyyy",
                ["de-CH"] = "dd.MM.yyyy",

                // French
                ["fr"] = "d MMM yyyy",
                ["fr-FR"] = "d MMM yyyy",
                ["fr-CA"] = "d MMM yyyy",

                // Spanish
                ["es"] = "d MMM yyyy",
                ["es-ES"] = "d MMM yyyy",
                ["es-MX"] = "d MMM yyyy",

                // Italian
                ["it"] = "d MMM yyyy",
                ["it-IT"] = "d MMM yyyy",

                // Portuguese
                ["pt"] = "d MMM yyyy",
                ["pt-PT"] = "d MMM yyyy",
                ["pt-BR"] = "d MMM yyyy",

                // Slavic
                ["uk"] = "dd.MM.yyyy",
                ["uk-UA"] = "dd.MM.yyyy",
                ["pl"] = "dd.MM.yyyy",
                ["pl-PL"] = "dd.MM.yyyy",
                ["cs"] = "d. M. yyyy",
                ["cs-CZ"] = "d. M. yyyy",
                ["sk"] = "d. M. yyyy",
                ["sk-SK"] = "d. M. yyyy",
            };

        private static bool TryGetMediumDatePattern(CultureInfo culture, out string? pattern)
        {
            // 1. exact match (e.g., "de-DE")
            if (MediumDatePatterns.TryGetValue(culture.Name, out pattern))
                return true;

            // 2. language-only fallback (e.g., "de")
            if (MediumDatePatterns.TryGetValue(culture.TwoLetterISOLanguageName, out pattern))
                return true;

            // 3. not found
            pattern = string.Empty;
            return false;
        }

        private static bool TryGetDateTimeWithOffset(
            object? value,
            out DateTimeOffset dto,
            out bool hasOffset)
        {
            dto = default;
            hasOffset = false;

            if (value is null)
                return false;

            switch (value)
            {
                case DateTimeOffset d:
                    dto = d;
                    hasOffset = true;
                    return true;

                case DateTime d:
                    // Map DateTimeKind to an offset-aware DateTimeOffset
                    if (d.Kind == DateTimeKind.Utc)
                    {
                        dto = new DateTimeOffset(d, TimeSpan.Zero);
                        hasOffset = true;
                    }
                    else if (d.Kind == DateTimeKind.Local)
                    {
                        dto = new DateTimeOffset(d); // local offset
                        hasOffset = true;
                    }
                    else // Unspecified – we do NOT know the zone
                    {
                        dto = new DateTimeOffset(DateTime.SpecifyKind(d, DateTimeKind.Unspecified));
                        hasOffset = false;
                    }

                    return true;

                case long ticks:
                    dto = new DateTimeOffset(new DateTime(ticks, DateTimeKind.Unspecified));
                    hasOffset = false;
                    return true;

                case string s:
                    DateTime? parsed = Utils.ParseDateISO8601(s);
                    if (parsed?.Kind == DateTimeKind.Unspecified)
                    {
                        dto = s.Length > 0 && s[s.Length - 1] == 'Z' ? new DateTimeOffset(new DateTime(parsed.Value.Ticks, DateTimeKind.Utc)) : new DateTimeOffset(DateTime.SpecifyKind(parsed.Value, DateTimeKind.Unspecified));
                        hasOffset = false;
                        return true;
                    }

                    if (DateTimeOffset.TryParse(
                                      s,
                                      CultureInfo.InvariantCulture,
                                      DateTimeStyles.RoundtripKind,
                                      out var parsedDto))
                    {
                        dto = parsedDto;
                        hasOffset = true;
                        return true;
                    }

                    if (DateTime.TryParse(
                        s,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDt))
                    {
                        dto = new DateTimeOffset(DateTime.SpecifyKind(parsedDt, DateTimeKind.Unspecified));
                        hasOffset = false;
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        /*private static bool TryGetDateTime(object? value, out DateTime dt)
        {
            dt = default;
            if (value is null)
                return false;

            switch (value)
            {
                case DateTime d:
                    dt = d;
                    return true;
                case DateTimeOffset dto:
                    dt = dto.LocalDateTime;
                    return true;
                case long ticks:
                    dt = new DateTime(ticks, DateTimeKind.Unspecified);
                    return true;
                case string s:
                    DateTime? parsed = Utils.ParseDateISO8601(s);
                    if (parsed is not null)
                    {
                        if (parsed.Value.Kind == DateTimeKind.Unspecified && s.EndsWith("Z", StringComparison.Ordinal))
                        {
                            dt = new DateTime(parsed.Value.Ticks, DateTimeKind.Utc);
                            return true;
                        }

                    }

                    return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
                default:
                    return false;
            }
        }
        */

        // ---------- time ----------

        private static DateTimeOptions ReadTimeOptions(Reader r)
        {
            var opts = new DateTimeOptions();

            r.SkipWs();
            if (r.Peek() == '{' || r.Peek() == '\0')
                return opts;

            if (r.TryReadLiteral("::"))
            {
                var ident = r.ReadIdentifier(out _);
                throw new FormatException($"time: ICU skeletons '::{ident}' are not supported.");
            }

            int startPos = r.Position;

            if (r.Peek() == '\'')
            {
                var fmt = r.ReadQuoted();
                opts.Style = DateTimeStyleOption.Custom;
                opts.CustomDotNetFormat = fmt;
                return opts;
            }

            // Case 2: maybe a named style (short/medium/long/full)
            if (r.TryReadIdentifier(out var identToken))
            {
#pragma warning disable CA1308 // In method ..., replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                var style = identToken.ToLowerInvariant();
#pragma warning restore CA1308 // In method ..., replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                switch (style)
                {
                    case "short":
                        opts.Style = DateTimeStyleOption.Short;
                        return opts;
                    case "medium":
                        opts.Style = DateTimeStyleOption.Medium;
                        return opts;
                    case "long":
                        opts.Style = DateTimeStyleOption.Long;
                        return opts;
                    case "full":
                        opts.Style = DateTimeStyleOption.Full;
                        return opts;
                    default:
                        // Not a known style (e.g., "yyyy") → treat entire remainder as a custom pattern
                        r.Restore(startPos);
                        var patternFromIdent = r.ReadToEndOrBrace();
                        opts.Style = DateTimeStyleOption.Custom;
                        opts.CustomDotNetFormat = patternFromIdent.Trim();
                        return opts;
                }
            }

            // Case 3: no identifier (e.g., starts with digit/symbol) → entire tail is pattern
            r.Restore(startPos);
            var pattern = r.ReadToEndOrBrace();
            opts.Style = DateTimeStyleOption.Custom;
            opts.CustomDotNetFormat = pattern.Trim();

            return opts;
        }

        // CLDR-inspired medium time patterns.
        private static readonly Dictionary<string, string> MediumTimePatterns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // English
                ["en"] = "h:mm:ss tt",
                ["en-US"] = "h:mm:ss tt",
                ["en-GB"] = "HH:mm:ss",
                ["en-CA"] = "h:mm:ss tt",
                ["en-AU"] = "h:mm:ss tt",

                // Central/Western Europe
                ["de"] = "HH:mm:ss",
                ["de-DE"] = "HH:mm:ss",
                ["de-AT"] = "HH:mm:ss",
                ["de-CH"] = "HH:mm:ss",

                ["fr"] = "HH:mm:ss",
                ["fr-FR"] = "HH:mm:ss",
                ["fr-CA"] = "HH:mm:ss",

                ["es"] = "HH:mm:ss",
                ["es-ES"] = "HH:mm:ss",
                ["es-MX"] = "HH:mm:ss",

                ["it"] = "HH:mm:ss",
                ["it-IT"] = "HH:mm:ss",

                ["pt"] = "HH:mm:ss",
                ["pt-PT"] = "HH:mm:ss",
                ["pt-BR"] = "HH:mm:ss",

                // Slavic
                ["uk"] = "HH:mm:ss",
                ["uk-UA"] = "HH:mm:ss",
                ["pl"] = "HH:mm:ss",
                ["pl-PL"] = "HH:mm:ss",
                ["cs"] = "HH:mm:ss",
                ["cs-CZ"] = "HH:mm:ss",
                ["sk"] = "HH:mm:ss",
                ["sk-SK"] = "HH:mm:ss",
            };

        private static bool TryGetMediumTimePattern(CultureInfo culture, out string? pattern)
        {
            if (MediumTimePatterns.TryGetValue(culture.Name, out pattern))
                return true;

            if (MediumTimePatterns.TryGetValue(culture.TwoLetterISOLanguageName, out pattern))
                return true;

            pattern = string.Empty;
            return false;
        }

        private static string FormatTime(DateTimeOffset value, bool hasOffset, DateTimeOptions opts, CultureInfo culture)
        {
            switch (opts.Style)
            {
                case DateTimeStyleOption.Custom:
                    return value.ToString(opts.CustomDotNetFormat ?? "t", culture);

                case DateTimeStyleOption.Short:
                    return value.ToString("t", culture); // short time

                case DateTimeStyleOption.Medium:
                    // Try culture-specific medium pattern first
                    if (!TryGetMediumTimePattern(culture, out var pattern))
                    {
                        // Fallback: use short date if we don't know better for this culture
                        pattern = culture.DateTimeFormat.ShortTimePattern;
                    }

                    return value.ToString(pattern, culture);

                case DateTimeStyleOption.Long:
                    //return value.ToString("T", culture); // long/full time
                case DateTimeStyleOption.Full:
                    var baseTime = value.ToString("T", culture);

                    if (!hasOffset || value.Offset == TimeSpan.Zero)
                        return baseTime + (opts.Style == DateTimeStyleOption.Full ? " Universal Coordinated Time" : " UTC");

                    // e.g. "14:35:42 +01:00"
                    var offset = value.ToString(opts.Style == DateTimeStyleOption.Full ? "zzzz" : "z", culture);

                    return baseTime + " " + offset;

                default:
                    return value.ToString(culture.DateTimeFormat.ShortTimePattern, culture);
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
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                var ident = r.ReadIdentifier(out _).ToLowerInvariant(); // e.g., "compact-short"
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'

                if (string.Equals(ident, "compact-short", StringComparison.OrdinalIgnoreCase))
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
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            var style = r.ReadIdentifier(out _).ToLowerInvariant();
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
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
        private static string RenderPluralText(string template, decimal n, int offset, ref int placeholderIndex, object? value, Func<string, int, (object?, int)> getPlaceholderValueFunc, /*Func<string, int, object?> getParamValueFunc, */CultureInfo culture)
        {
            // Replace '#' with (n - offset) using culture
            var number = n - offset;
            var replaced = template.Replace("#", number.ToString(culture));

            return RenderPlaceholderText(replaced, ref placeholderIndex, getPlaceholderValueFunc);
        }
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

        private static string RenderPlaceholderText(string s, ref int placeholderIndex, Func<string, int, (object?, int)> getPlaceholderValueFunc)
        {
            //placeholderIndex++;

            (object? val, placeholderIndex) = getPlaceholderValueFunc(s, placeholderIndex);

            // Only support simple identifiers inside branch text
            return val?.ToString() ?? string.Empty;
        }

        // Very small {name} expander for nested simple placeholders inside branch text.
        /*private static string RenderValueText(string s, ref int placeholderIndex, Func<string, int, object?> getParamValueFunc)
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
        }*/

        private static Dictionary<string, string> ReadOptions(Reader r)
        {
            r.SkipWs();

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // GROUPED: "{ key{...} key{...} }"
            if (r.TryReadChar('{'))
            {
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

            public string ReadToEndOrBrace()
            {
                SkipWs();
                int start = _offset;

                // For date/time/datetime options there is usually no '}' inside,
                // but we stop on '}' just in case.
                while (_offset < _sourceText.Length && _sourceText[_offset] != '}')
                    _offset++;

                return _sourceText.Substring(start, _offset - start);
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
