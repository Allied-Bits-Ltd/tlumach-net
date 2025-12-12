// <copyright file="Utils.cs" company="Allied Bits Ltd.">
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

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance
#pragma warning disable CA1305 // The behavior of '...' could vary based on the current user's locale settings. Replace this call ...
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Contains helper functions, usable across the library.
    /// </summary>
    public static class Utils
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public const char C_SINGLE_QUOTE = '\'';
        public const string S_SINGLE_QUOTE = "'";
        public const char C_DOUBLE_QUOTE = '"';
        public const string S_DOUBLE_QUOTE = "\"";
        public const char C_BACKSLASH = '\\';
        public const string S_BACKSLASH = "\\";

        public const string KEY_LOCALE = "locale";
        public const string KEY_NAME = "name";
        public const string KEY_DECIMAL_DIGITS = "decimalDigits";
        public const string KEY_SYMBOL = "symbol";
#pragma warning restore CA1707 // Identifiers should not contain underscores

        public static bool TryGetPropertyValue(object obj, string propertyName, out object? value)
        {
            value = null;
            if (obj is null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            // Get the type
            var type = obj.GetType();

            // Case-insensitive property lookup
            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is null)
                return false;

            // Get the value
            value = prop.GetValue(obj);
            return true;
        }

        public static string? ReadFileFromResource(Assembly assembly, string filename)
        {
            return ReadFileFromResource(assembly, filename, null, null);
        }

        public static string? ReadFileFromResource(Assembly assembly, string filename, string? baseDirectory, string? baseDirectory2)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            if (filename is null)
                throw new ArgumentNullException(nameof(filename));

            try
            {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                string sourceResourceName = filename.Replace('/', '.').Replace('\\', '.');
#pragma warning restore CA1307 // Specify StringComparison for clarity

                string resourceName = assembly.GetName().Name + "." + sourceResourceName;
                Stream? stream = assembly.GetManifestResourceStream(resourceName);

#pragma warning disable MA0001 // Specify StringComparison for clarity
#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable CS8602 // Dereference of a possibly null reference
                if (stream is null && !string.IsNullOrEmpty(baseDirectory))
                {
                    resourceName = assembly.GetName().Name + "." + baseDirectory.Replace('/', '.').Replace('\\', '.') + "." + sourceResourceName;
                    stream = assembly.GetManifestResourceStream(resourceName);
                }

                if (stream is null && !string.IsNullOrEmpty(baseDirectory2))
                {
                    resourceName = assembly.GetName().Name + "." + baseDirectory2.Replace('/', '.').Replace('\\', '.') + "." + sourceResourceName;
                    stream = assembly.GetManifestResourceStream(resourceName);
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference
#pragma warning restore MA0001 // Specify StringComparison for clarity
#pragma warning restore CA1307 // Specify StringComparison for clarity

                if (stream is not null)
                {
                    using StreamReader reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }

                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static FileStream? TryOpenStreamForReading(string filename)
        {
            try
            {
                return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static string? ReadFileFromDisk(string filename)
        {
            if (filename is null)
                throw new ArgumentNullException(nameof(filename));

            try
            {
                string attemptName = filename;
#pragma warning disable CA2000
                Stream? stream = TryOpenStreamForReading(attemptName);
#pragma warning restore CA2000

                if (stream is not null)
                {
                    using StreamReader reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }

                return null;
            }
#pragma warning disable CA1031 // Modify '...' to catch a more specific allowed exception type, or rethrow the exception
            catch (Exception)
            {
                return null;
            }
#pragma warning restore CA1031 // Modify '...' to catch a more specific allowed exception type, or rethrow the exception
        }

        public static string? ReadFileFromDisk(string filename, string? baseDirectory, string? baseDirectory2)
        {
            if (filename is null)
                throw new ArgumentNullException(nameof(filename));

            try
            {
                string attemptName = filename;
#pragma warning disable CA2000
                FileStream? stream = TryOpenStreamForReading(attemptName);

                if (stream is null && !string.IsNullOrEmpty(baseDirectory))
                {
                    attemptName = Path.Combine(baseDirectory, filename);
                    stream = TryOpenStreamForReading(attemptName);
                }

                if (stream is null && !string.IsNullOrEmpty(baseDirectory2))
                {
                    attemptName = Path.Combine(baseDirectory2, filename);
                    stream = TryOpenStreamForReading(attemptName);
                }
#pragma warning restore CA2000
                if (stream is not null)
                {
                    using StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    return reader.ReadToEnd();
                }

                return null;
            }
#pragma warning disable CA1031 // Modify '...' to catch a more specific allowed exception type, or rethrow the exception
            catch (Exception)
            {
                return null;
            }
#pragma warning restore CA1031 // Modify '...' to catch a more specific allowed exception type, or rethrow the exception
        }

        public static DateTime ParseDate(string date, string format)
        {
            if (date is null)
                return System.DateTime.MinValue;

            try
            {
                if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime result))
                    return result;
                else
                    return DateTime.MinValue;
            }
#pragma warning disable CA1031 // Modify '...' to catch a more specific allowed exception type, or rethrow the exception
            catch (Exception)
            {
                return DateTime.MinValue;
            }
#pragma warning restore CA1031 // Modify '...' to catch a more specific allowed exception type, or rethrow the exception
        }

        public static DateTime? ParseDateISO8601(string? date)
        {
            const string DATE_FORMAT_ISO_8601 = "yyyy-MM-dd'T'HH:mm:ss'Z'";
            const string DATE_FORMAT_ISO_8601_WITH_MS = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

            if (string.IsNullOrEmpty(date))
                return null;

            DateTime result = ParseDate(date!, DATE_FORMAT_ISO_8601_WITH_MS);
            if (result == DateTime.MinValue)
                result = ParseDate(date!, DATE_FORMAT_ISO_8601);
            if (result == DateTime.MinValue)
                return null;
            return result;
        }

        /// <summary>
        /// Decodes escaping used in JSON and TOML strings.
        /// </summary>
        /// <param name="value">original string to decode.</param>
        /// <returns>a decoded string.</returns>
        public static string UnescapeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder builder = new(value.Length);

            int charCode;
            char nextChar;
            int i = 0;
            while (i < value.Length)
            {
                char c = value[i];
                if (c == '\\')
                {
                    i++;
                    if (i < value.Length)
                    {
                        nextChar = value[i];
                        switch (nextChar)
                        {
                            case '"':
                                builder.Append('"'); break;
                            case '\\':
                                builder.Append('\\'); break;
                            case '/':
                                builder.Append('/'); break;
                            case 'b':
                                builder.Append('\b'); break;
                            case 'f':
                                builder.Append('\f'); break;
                            case 'n':
                                builder.Append('\n'); break;
                            case 'r':
                                builder.Append('\r'); break;
                            case 't':
                                builder.Append('\t'); break;
                            case 'u':
                                if (i + 4 < value.Length)
                                {
                                    string hex = value.Substring(i + 1, 4);
                                    try
                                    {
                                        charCode = int.Parse(hex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                        builder.Append((char)charCode);
                                    }
                                    catch (FormatException)
                                    {
                                        // Invalid sequence
                                        builder.Append('\\').Append('u').Append(hex);
                                    }

                                    i += 4;
                                }
                                else
                                {
                                    // Incomplete sequence
                                    builder.Append('\\').Append('u');
                                }

                                break;

                            default:
                                builder.Append('\\').Append(nextChar);
                                break;
                        }
                    }
                    else
                    {
                        throw new TemplateParserException("Incomplete escape sequence (hanging backslash detected) in the following text:\n" + value);
                    }
                }
                else
                {
                    builder.Append(c);
                }

                i++;
            }

            return builder.ToString();
        }

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.
        /// <summary>
        /// Extracts the number from a string when the string starts with a positive decimal number.
        /// </summary>
        /// <param name="text">The text to extract the number from.</param>
        /// <param name="charsUsed">The number of characters used from <paramref name="text"/> to parse as a number.</param>
        /// <returns>The extracted number or -1 in the case when a number was not extracted.</returns>
        public static int GetLeadingNonNegativeNumber(string text, out int charsUsed)
        {
            int i = 0;
            while (i < text.Length && char.IsDigit(text[i])) i++;

            if (int.TryParse(text.Substring(0, i), NumberStyles.Number, CultureInfo.InvariantCulture, out int result))
            {
                charsUsed = i;
                return result;
            }

            charsUsed = 0;
            return -1;
        }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

        /// <summary>
        /// Checks if the argument is of a numeric type.
        /// </summary>
        /// <param name="obj">An object to check.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a number and <see langword="false"/> otherwise.</returns>
        public static bool IsBoxedNumber(object? obj)
        {
            if (obj is null)
                return false;

            var t = obj.GetType();
            return
                // integer
                t == typeof(byte) || t == typeof(sbyte) ||
                t == typeof(short) || t == typeof(ushort) ||
                t == typeof(int) || t == typeof(uint) ||
                t == typeof(long) || t == typeof(ulong) ||
                // floating-point
                t == typeof(float) || t == typeof(double) || t == typeof(decimal);
        }

        /// <summary>
        /// Checks if the argument is of a integer type.
        /// </summary>
        /// <param name="obj">An object to check.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> is a number and <see langword="false"/> otherwise.</returns>
        public static bool IsBoxedIntegerNumber(object? obj)
        {
            if (obj is null)
                return false;

            var t = obj.GetType();
            return
                // integer
                t == typeof(byte) || t == typeof(sbyte) ||
                t == typeof(short) || t == typeof(ushort) ||
                t == typeof(int) || t == typeof(uint) ||
                t == typeof(long) || t == typeof(ulong);
        }

        public static long ConvertToLong(object a)
        {
            switch (a)
            {
                case byte value: return value;
                case sbyte value: return value;
                case short value: return value;
                case ushort value: return value;
                case int value: return value;
                case uint value: return value;
                case ulong value: return (long)value;
                default: return (long)a;
            }
        }

        public static double ConvertToDouble(object a)
        {
            switch (a)
            {
                case byte value: return value;
                case sbyte value: return value;
                case short value: return value;
                case ushort value: return value;
                case int value: return value;
                case uint value: return value;
                case long value: return value;
                case ulong value: return value;
                case float value: return value;
                case decimal value: return (double)value;
                default: return (double)a;
            }
        }

        public static string FormatArbNumber(ref int placeholderIndex, object value, Func<string, int, (object?, int)> getPlaceholderValueFunc, /*Func<string, int, object?> getParamValueFunc, */Placeholder placeholder, string placeholderContentTail, CultureInfo culture)
        {
            string pattern = placeholder?.Format ?? string.Empty;

            if (placeholderContentTail is null)
                throw new ArgumentNullException(nameof(placeholderContentTail));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (culture is null)
                throw new ArgumentNullException(nameof(culture));

            placeholderContentTail = placeholderContentTail.Trim();

            if (!string.IsNullOrEmpty(placeholderContentTail))
            {
                string? icuResult = IcuFragment.EvaluateNoName(placeholderContentTail, ref placeholderIndex, value, getPlaceholderValueFunc, /*getParamValueFunc, */culture);

                if (icuResult is not null)
                    return icuResult;
            }

            string? name = null;
            string? decimalDigitsStr = null;
            int? decimalDigits = null;
            string? symbol = null;
            if (placeholder is not null)
            {
                placeholder.OptionalParameters.TryGetValue(KEY_NAME, out name);
                if (placeholder.OptionalParameters.TryGetValue(KEY_DECIMAL_DIGITS, out decimalDigitsStr))
                {
                    int.TryParse(decimalDigitsStr, NumberStyles.Integer, culture, out int iVal);
                    decimalDigits = iVal;
                }

                placeholder.OptionalParameters.TryGetValue(KEY_SYMBOL, out symbol);
            }

            if (IsBoxedIntegerNumber(value))
            {
                long lValue = ConvertToLong(value);
                //if (string.IsNullOrEmpty(placeholderContentTail))
                return InternalFormatArbInteger(lValue, pattern, culture);
            }
            else
            if (IsBoxedNumber(value))
            {
                double dValue = ConvertToDouble(value);
                //if (string.IsNullOrEmpty(placeholderContentTail))
                return InternalFormatArbDouble(dValue, pattern, culture, decimalDigits, symbol, name);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string FormatArbDateTime(object value, Placeholder placeholder, CultureInfo culture)
        {
            if (value is null)
                return string.Empty;

            string pattern = placeholder?.Format ?? string.Empty;

            string? locale = null;
            if (placeholder is not null)
            {
                placeholder.OptionalParameters.TryGetValue(KEY_LOCALE, out locale);
            }

            if (locale?.Length > 0)
            {
                try
                {
                    culture = new CultureInfo(locale.Replace('_', '-'));
                }
                catch (CultureNotFoundException)
                {
                    // maybe, an invalid value in the file
                }
            }

            culture ??= CultureInfo.InvariantCulture;

            if (value is DateTime dt)
            {
                if (pattern.Length > 0)
                    return InternalFormatArbDateTime(dt, pattern, culture);
                else
                    return dt.ToString(culture);
            }

#if NET9_0_OR_GREATER
            if (value is DateTimeOffset dto)
            {
                if (pattern.Length > 0)
                    return InternalFormatArbDateTime(dto.DateTime, pattern, culture);
                else
                    return dto.ToString(culture);
            }

            if (value is DateOnly d)
            {
                if (pattern.Length > 0)
                    return InternalFormatArbDateTime(d.ToDateTime(new TimeOnly(0)), pattern, culture);
                else
                    return d.ToString(culture);
            }

            if (value is TimeOnly t)
            {
                if (pattern.Length > 0)
                    return InternalFormatArbDateTime(new DateTime(new DateOnly(1, 1, 1), t, DateTimeKind.Unspecified), pattern, culture);
                else
                    return t.ToString(culture);
            }
#endif

            return string.Format(culture, "{0}", value);
        }

        public static string FormatArbString(ref int placeholderIndex, object value, Func<string, int, (object?, int)> getPlaceholderValueFunc, /*Func<string, int, object?> getParamValueFunc, */string placeholderContentTail, CultureInfo culture)
        {
            if (placeholderContentTail is null)
                throw new ArgumentNullException(nameof(placeholderContentTail));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            placeholderContentTail = placeholderContentTail.Trim();

            if (!string.IsNullOrEmpty(placeholderContentTail))
            {
                string? icuResult = IcuFragment.EvaluateNoName(placeholderContentTail, ref placeholderIndex, value, getPlaceholderValueFunc, /*getParamValueFunc, */culture);

                if (icuResult is not null)
                    return icuResult;
            }

            return string.Format(culture, "{0}", value);
        }

        public static string FormatArbUnknownPlaceholder(ref int placeholderIndex, object value, Func<string, int, (object?, int)> getPlaceholderValueFunc, /*Func<string, int, object?> getParamValueFunc, */string placeholderContentTail, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty(placeholderContentTail))
            {
                string? icuResult = IcuFragment.EvaluateNoName(placeholderContentTail, ref placeholderIndex, value, getPlaceholderValueFunc, /* getParamValueFunc, */culture);

                if (icuResult is not null)
                    return icuResult;
            }

            return string.Format(culture, "{0}", value);
        }

        private static string InternalFormatArbDateTime(DateTime dt, string dartPatternOrSkeleton, CultureInfo culture)
        {
            // 1) Try skeletons first (exact match).
            if (TryMapSkeletonToDotNetPattern(dartPatternOrSkeleton, culture, out string netSkeletonPattern, out _ /* bool useStandardFormat*/))
            {
                return /*useStandardFormat
                    ? dt.ToString(netSkeletonPattern, culture)    // "d", "D", "t", "T", "g", etc.
                    : */dt.ToString(netSkeletonPattern, culture);   // custom pattern like "HH:mm"
            }

            // 2) Fall back to translating a Dart/ICU-like pattern to .NET custom format.
            var netPattern = ConvertDartDateTimePatternToDotNet(dartPatternOrSkeleton);
            return dt.ToString(netPattern, culture);
        }

        /// <summary>
        /// Maps a subset of intl (ICU-style) skeletons to .NET patterns.
        /// Returns true if matched. Some map to standard format strings ("d", "D", etc.).
        /// </summary>
        private static bool TryMapSkeletonToDotNetPattern(string skeleton, CultureInfo culture, out string dotNetPattern, out bool isStandardFormatString)
        {
            isStandardFormatString = false;

            // Culture helpers
            var dfi = culture.DateTimeFormat;
#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
            bool hasAmPm = dfi.ShortTimePattern.Contains('t'); // if "tt" appears, it's 12-hour
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

            string JHour(string one = "h", string twentyFour = "H") => hasAmPm ? one : twentyFour;
            string JAmPm() => hasAmPm ? " tt" : string.Empty;

            // Helper: remove year parts from a date pattern (rough but practical).
            static string RemoveYear(string pattern)
            {
                // remove runs of 'y' and nearby separators
                var chars = pattern.ToCharArray();
                var sb = new StringBuilder();
                int i = 0;
                while (i < chars.Length)
                {
                    if (chars[i] == 'y')
                    {
                        // skip all consecutive y
                        while (i < chars.Length && chars[i] == 'y')
                                i++;

                        // also trim one separator on either side if duplicated
                        // (lightweight heuristic – keeps most cultures tidy)
#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
                        while (sb.Length > 0 && " ./-,".Contains(sb[sb.Length - 1]))
                            sb.Length--;

                        // skip one separator on the right
                        if (i < chars.Length && " ./-,".Contains(chars[i]))
                            i++;
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

                    }
                    else
                    {
                        sb.Append(chars[i]);
                        i++;
                    }
                }

                // collapse double separators introduced by removal
                return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"([ ./,\-])\1+", "$1").Trim();
            }

            // Common intl skeletons (subset). Keys are exact skeleton strings.
            // Notes:
            // - When possible, use DateTimeFormatInfo’s built-ins (ShortDatePattern, YearMonthPattern, etc.).
            // - For 'j' (locale-dependent hour), choose 12/24-hour based on culture.
            var map = new Dictionary<string, Func<(string pattern, bool standard)>>(StringComparer.Ordinal)
            {
                // Date-only
                ["y"] = () => ("yyyy", false),
                ["M"] = () => ("M", false),
                ["MM"] = () => ("MM", false),
                ["MMM"] = () => ("MMM", false),
                ["MMMM"] = () => ("MMMM", false),

                // Very common: short date (yMd)
                ["yMd"] = () => ("d", true), // standard short date per culture
                ["Md"] = () => (RemoveYear(dfi.ShortDatePattern), false),
                ["yM"] = () => (dfi.YearMonthPattern, false),
                ["yMMM"] = () => ("MMM yyyy", false),
                ["yMMMM"] = () => ("MMMM yyyy", false),
                ["yMMMd"] = () => ("MMM d, yyyy", false),
                ["yMMMMd"] = () => ("MMMM d, yyyy", false),
                ["MEd"] = () => ("ddd, " + RemoveYear(dfi.ShortDatePattern), false),
                ["MMMEd"] = () => ("ddd, MMM d", false),
                ["Ed"] = () => ("ddd, d", false),

                // Weekday-only
                ["E"] = () => ("ddd", false),
                ["EEEE"] = () => ("dddd", false),

                // Times (24h)
                ["H"] = () => ("H", false),
                ["Hm"] = () => ("HH:mm", false),
                ["Hms"] = () => ("HH:mm:ss", false),

                // Times (locale 12h/24h via 'j')
                ["j"] = () => (JHour(), false),
                ["jm"] = () => ($"{JHour()}':'mm{JAmPm()}", false),
                ["jms"] = () => ($"{JHour()}':'mm':'ss{JAmPm()}", false),

                // Datetime combos (use standard formats where feasible)
                ["yMdHm"] = () => ("g", true), // short date + short time
                ["yMdHms"] = () => ("G", true), // short date + long time
                ["yMMMdjm"] = () => ($"MMM d, yyyy {JHour()}':'mm{JAmPm()}", false),
                ["yMMMdjms"] = () => ($"MMM d, yyyy {JHour()}':'mm':'ss{JAmPm()}", false),
            };

            if (map.TryGetValue(skeleton, out var thunk))
            {
                var (pat, std) = thunk();
                dotNetPattern = pat;
                isStandardFormatString = std;
                return true;
            }

            dotNetPattern = null!;
            return false;
        }

        // -----------------------
        // Pattern translator
        // -----------------------

        /// <summary>
        /// Translates a subset of Dart/ICU-like pattern letters to .NET custom DateTime format.
        /// Longest-token-first replacement to avoid overlaps.
        /// </summary>
        private static string ConvertDartDateTimePatternToDotNet(string pattern)
        {
            // Map tokens (subset commonly used in intl DateFormat)
            // ICU/Dart -> .NET
            var repl = new (string icu, string net)[]
            {
            // Year
            ("yyyy", "yyyy"),
            ("yyy",  "yyy"),
            ("yy",   "yy"),
            ("y",    "yyyy"),

            // Month
            ("MMMM", "MMMM"),
            ("MMM",  "MMM"),
            ("MM",   "MM"),
            ("M",    "M"),

            // Day
            ("dd",   "dd"),
            ("d",    "d"),

            // Weekday
            ("EEEE", "dddd"),
            ("EEE",  "ddd"),
            ("E",    "ddd"),

            // 24-hour
            ("HH",   "HH"),
            ("H",    "H"),

            // 12-hour
            ("hh",   "hh"),
            ("h",    "h"),

            // Minute
            ("mm",   "mm"),
            ("m",    "m"),

            // Second
            ("ss",   "ss"),
            ("s",    "s"),

            // AM/PM
            ("a",    "tt"),

            // Fractional seconds (ICU uses S/SSS). Map to .NET 'f' series.
            ("SSSSSSS", "fffffff"),
            ("SSSSSS",  "ffffff"),
            ("SSSSS",   "fffff"),
            ("SSSS",    "ffff"),
            ("SSS",     "fff"),
            ("SS",      "ff"),
            ("S",       "f"),

            // Era
            ("GGGGG", "g"),
            ("GGGG",  "g"),
            ("GGG",   "g"),
            ("GG",    "g"),
            ("G",     "g"),

            // Time zone (very rough; .NET tokens differ; map 'z' to 'zzz' offset)
            // Note: ICU 'z' may be name/abbrev; we map to numeric offset for practicality.
            ("ZZZZZ", "K"),      // e.g., +01:00 -> round-trippable offset token in .NET (DateTimeOffset)
            ("ZZZZ",  "zzz"),
            ("ZZZ",   "zzz"),
            ("ZZ",    "zz"),
            ("Z",     "zz"),
            ("zzzz",  "zzz"),
            ("zzz",   "zzz"),
            ("zz",    "zz"),
            ("z",     "zz"),
            };

            // Replace longest-first
            var ordered = repl.OrderByDescending(t => t.icu.Length).ToArray();
            var sb = new StringBuilder(pattern);
            foreach (var (icu, net) in ordered)
                sb.Replace(icu, net);

            return sb.ToString();
        }

        private static string InternalFormatArbDouble(double value, string pattern, CultureInfo culture, int? decimalDigits, string? currencySymbol, string? currencyName)
        {
            if (string.IsNullOrEmpty(pattern))
                pattern = "decimalPattern";

            CultureInfo? customCulture = null;
            if (currencySymbol?.Length > 0 || currencyName?.Length > 0 || currencySymbol is not null)
            {
                customCulture = culture.Clone() as CultureInfo;

                if (customCulture is not null)
                {
                    if (currencySymbol?.Length > 0)
                        customCulture.NumberFormat.CurrencySymbol = currencySymbol;
                    else
                    if (currencyName?.Length > 0)
                        customCulture.NumberFormat.CurrencySymbol = currencyName;
                    if (decimalDigits is not null)
                    {
                        customCulture.NumberFormat.CurrencyDecimalDigits = decimalDigits.Value;
                        customCulture.NumberFormat.NumberDecimalDigits = decimalDigits.Value;
                    }
                }
            }

            customCulture ??= culture;

            switch (pattern)
            {
                case "NumberFormat.decimalPattern":
                case "decimalPattern":
                    return value.ToString("N", customCulture);

                case "NumberFormat.percentPattern":
                case "percentPattern":
                    return value.ToString("P", customCulture);

                case "NumberFormat.scientificPattern":
                case "scientificPattern":
                    return value.ToString("E", customCulture);

                case "NumberFormat.compact":
                case "compact":
                    return DartToCompactString(value, customCulture);

                case "NumberFormat.currency":
                case "currency":
                case "NumberFormat.simpleCurrency":
                case "simpleCurrency":
                    return value.ToString("C", customCulture);

                case "NumberFormat.compactCurrency":
                case "compactCurrency":
                    return DartToCompactCurrency(value, customCulture);

                default:
                    // Assume it’s a custom pattern
                    return value.ToString(ConvertDartNumberPatternToDotNet(pattern), culture);
            }
        }

        private static string ConvertDartNumberPatternToDotNet(string pattern)
        {
            // Basic mapping for common Dart Intl symbols to .NET equivalents
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
#pragma warning disable CA1307 // Specify StringComparison for clarity
            return pattern
                .Replace("#,##0.###", "N")   // general decimal pattern
                .Replace("#,##0.00", "N2")
                .Replace("0.###E0", "E3")
                .Replace('¤', 'C')
                .Replace('%', 'P');
#pragma warning restore CA1307 // Specify StringComparison for clarity
#pragma warning restore SA1025 // Code should not contain multiple whitespace in a row
        }

        private static string DartToCompactString(double value, CultureInfo culture)
        {
            string suffix;
            double displayValue;

            // todo: use culture-specific suffixes if we find where to get them
            if (Math.Abs(value) >= 1_000_000_000)
            {
                displayValue = value / 1_000_000_000;
                suffix = "B";
            }
            else if (Math.Abs(value) >= 1_000_000)
            {
                displayValue = value / 1_000_000;
                suffix = "M";
            }
            else if (Math.Abs(value) >= 1_000)
            {
                displayValue = value / 1_000;
                suffix = "K";
            }
            else
            {
                displayValue = value;
                suffix = string.Empty;
            }

            return displayValue.ToString("0.##", culture) + suffix;
        }

        private static string DartToCompactCurrency(double value, CultureInfo culture)
        {
            bool isNegative = value < 0;

            var numberFormat = culture.NumberFormat;
            string result = DartToCompactString(isNegative ? -value : value, culture);

            int pattern = isNegative ? numberFormat.CurrencyNegativePattern : numberFormat.CurrencyPositivePattern;

            if (isNegative)
            {
                switch (pattern)
                {
                    case 0: return string.Format("( {0}{1} )", numberFormat.CurrencySymbol, result); // ( $n )
                    case 1: return string.Format("-{0}{1}", numberFormat.CurrencySymbol, result); // -$n
                    case 2: return string.Format("{0}-{1}", numberFormat.CurrencySymbol, result); // $-n
                    case 3: return string.Format("{0}{1}-", numberFormat.CurrencySymbol, result); // $n-
                    case 4: return string.Format("( {1}{0} )", numberFormat.CurrencySymbol, result); // ( n$ )
                    case 5: return string.Format("-{1}{0}", numberFormat.CurrencySymbol, result); // -n$
                    case 6: return string.Format("{1}-{0}", numberFormat.CurrencySymbol, result); // n-$
                    case 7: return string.Format("{1}{0}-", numberFormat.CurrencySymbol, result); // n$-
                    case 8: return string.Format("-{1} {0}", numberFormat.CurrencySymbol, result); // -n $
                    case 9: return string.Format("-{0} {1}", numberFormat.CurrencySymbol, result); // -$ n
                    case 10: return string.Format("{1} {0}-", numberFormat.CurrencySymbol, result); // n $-
                    case 11: return string.Format("{0} {1}-", numberFormat.CurrencySymbol, result); // $ n-
                    case 12: return string.Format("{0} -{1}", numberFormat.CurrencySymbol, result); // $ -n
                    case 13: return string.Format("{1}- {0}", numberFormat.CurrencySymbol, result); // n- $
                    case 14: return string.Format("( {0} {1} )", numberFormat.CurrencySymbol, result); // ( $ n )
                    case 15: return string.Format("( {1} {0} )", numberFormat.CurrencySymbol, result); // ( n $ )
                    default:
                        return string.Format("-{0}{1}", numberFormat.CurrencySymbol, result); // -$n
                }
            }
            else
            {
                switch (pattern)
                {
                    case 0: return string.Format("{0}{1}", numberFormat.CurrencySymbol, result); // $n
                    case 1: return string.Format("{1}{0}", numberFormat.CurrencySymbol, result); // n$
                    case 2: return string.Format("{0} {1}", numberFormat.CurrencySymbol, result); // $ n
                    case 3: return string.Format("{1} {0}", numberFormat.CurrencySymbol, result); // n $
                    default:
                        return string.Format("{0}{1}", numberFormat.CurrencySymbol, result); // $n
                }
            }
        }

        private static string InternalFormatArbInteger(long value, string pattern, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(pattern))
                pattern = "decimalPattern";

            switch (pattern)
            {
                case "NumberFormat.decimalPattern":
                case "decimalPattern":
                    return value.ToString("N", culture);

                case "NumberFormat.percentPattern":
                case "percentPattern":
                    return value.ToString("P", culture);

                case "NumberFormat.scientificPattern":
                case "scientificPattern":
                    return value.ToString("E", culture);

                case "NumberFormat.compact":
                case "compact":
                    return DartToCompactString(value, culture);

                case "NumberFormat.currency":
                case "currency":
                case "NumberFormat.simpleCurrency":
                case "simpleCurrency":
                    return value.ToString("C", culture);

                case "NumberFormat.compactCurrency":
                case "compactCurrency":
                    return DartToCompactCurrency(value, culture);

                default:
                    // Assume it’s a custom pattern
                    return value.ToString(ConvertDartNumberPatternToDotNet(pattern), culture);
            }
        }

        public static bool IsIdentifier(string? value)
        {
            if (value is null || value.Length == 0)
                return false;

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                    return false;
            }

            return true;
        }

        public static bool IsIdentifierWithDots(string? value)
        {
            if (value is null || value.Length == 0)
                return false;

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_' || (value[i] == '.' && i < value.Length - 1)))
                    return false;
            }

            return true;
        }
    }
#pragma warning restore CA1305 // The behavior of '...' could vary based on the current user's locale settings. Replace this call ...
}
