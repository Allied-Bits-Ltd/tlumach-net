// <copyright file="TemplateChecker.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
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
using System.Text.RegularExpressions;

using Tlumach.Base;

namespace Tlumach.Validation;

/// <summary>
/// Checks that the placeholder template of an entry can be processed. The library parses a template only when the text is formatted, so an error in a template otherwise shows up only at run time, when the text is requested with values.
/// </summary>
internal static class TemplateChecker
{
    /// <summary>
    /// A date with distinct components, so that any date or time format can be applied to it.
    /// </summary>
    private static readonly DateTime SampleDate = new(2001, 2, 3, 4, 5, 6, DateTimeKind.Unspecified);

    /// <summary>
    /// An ICU message argument, such as <c>{count, plural, ...}</c>.
    /// </summary>
    private static readonly Regex IcuArgument = new(@"\{\s*\w+\s*,\s*(?:plural|select|selectordinal|number|date|time)\b", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Processes the template of the entry: without values, which checks the structure of the template (braces, quotes, escapes), and with sample values of the types that the placeholders suggest, which also checks the nested messages of plural and select placeholders and the formats.
    /// </summary>
    /// <param name="entry">The entry to check.</param>
    /// <param name="culture">The culture of the translation.</param>
    /// <param name="textProcessingMode">The text processing mode of the configuration.</param>
    /// <returns>The problem, or <see langword="null"/> when the template can be processed.</returns>
    public static TemplateProblem? Check(TranslationEntry entry, CultureInfo culture, TextFormat textProcessingMode)
    {
        if (!entry.ContainsPlaceholders)
            return null;

        // Only these modes have placeholders; the others return the text as is
        if (textProcessingMode is not (TextFormat.DotNet or TextFormat.Arb or TextFormat.ArbNoEscaping or TextFormat.Apple))
            return null;

        string? text = entry.EscapedText ?? entry.Text;
        if (text is null || text.Length == 0)
            return null;

        // Without values, the placeholders are not formatted, so only the structure is checked
        string? error = TryProcess(entry, culture, textProcessingMode, static (_, _) => null, out bool unexpected);
        if (error is not null)
            return new TemplateProblem(error, definite: !unexpected);

        Func<string, int, object?>? typedValues = BuildTypedValues(entry, text, textProcessingMode);
        if (typedValues is null)
            return null;

        error = TryProcess(entry, culture, textProcessingMode, typedValues, out unexpected);
        if (error is null)
            return null;

        // An unexpected exception may come from the sample values rather than from the template
        if (unexpected)
            return new TemplateProblem(error, definite: false);

        if (textProcessingMode == TextFormat.DotNet)
        {
            if (IcuArgument.IsMatch(text))
                return new TemplateProblem($"{error} ICU messages (plural, select, number, date, and time arguments) are processed only in the Arb and ArbNoEscaping text processing modes; in the DotNet mode, formatting this text with any value fails.", definite: true);

            // .NET applies a format specifier according to the type of the value, and ignores it for strings. When the text can be formatted with strings, the error depends on the type of the values that the application passes.
            if (TryProcess(entry, culture, textProcessingMode, static (_, _) => "text", out _) is null)
                return new TemplateProblem($"{error} Formatting this text fails with a value of the type assumed by the check; check that the format suits the type of the value that the application passes.", definite: false);
        }

        return new TemplateProblem(error, definite: true);
    }

    private static Func<string, int, object?>? BuildTypedValues(TranslationEntry entry, string text, TextFormat textProcessingMode)
    {
        Dictionary<string, object> valuesByName = new(StringComparer.Ordinal);
        List<object> valuesByIndex = [];

#pragma warning disable CA1031 // A failure to infer the types is not an error of the template; the structural pass has succeeded
        try
        {
            foreach ((string name, string type) in entry.CollectPlaceholders(text, textProcessingMode))
            {
                object value = SampleValue(type);
                valuesByName[name] = value;
                valuesByIndex.Add(value);
            }
        }
        catch (Exception)
        {
            return null;
        }
#pragma warning restore CA1031

        return (name, index) =>
        {
            if (valuesByName.TryGetValue(name, out object? value))
                return value;

            if (index >= 0 && index < valuesByIndex.Count)
                return valuesByIndex[index];

            // A number fits plural placeholders, numeric formats, and plain text alike
            return 1;
        };
    }

    private static object SampleValue(string type) => type switch
    {
        "int" or "long" or "num" => 1,
        "double" or "float" or "decimal" => 1.5,
        "DateTime" => SampleDate,
        "string" => "text",
        "bool" => true,
        _ => 1,
    };

    /// <summary>
    /// Formats the text of the entry with the given values.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="culture">The culture of the translation.</param>
    /// <param name="textProcessingMode">The text processing mode.</param>
    /// <param name="getValue">Provides the values of the placeholders.</param>
    /// <param name="unexpected">Set to <see langword="true"/> when the failure is not one of the exceptions that the library throws for a bad template or format.</param>
    /// <returns>The description of the failure, or <see langword="null"/> when the text was formatted.</returns>
    private static string? TryProcess(TranslationEntry entry, CultureInfo culture, TextFormat textProcessingMode, Func<string, int, object?> getValue, out bool unexpected)
    {
        unexpected = false;
        try
        {
            entry.ProcessTemplatedValue(culture, textProcessingMode, getValue);
            return null;
        }
        catch (Exception ex) when (ex is TlumachException or FormatException or ArgumentException or InvalidOperationException or IndexOutOfRangeException)
        {
            return CleanMessage(ex.Message);
        }
        catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException or StackOverflowException or AccessViolationException))
        {
            unexpected = true;
            return CleanMessage($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string CleanMessage(string message)
    {
        // The library appends the whole text to some messages; the report names the key instead
        int textStart = message.IndexOf(" in the following text:", StringComparison.Ordinal);
        if (textStart > 0)
            message = message.Substring(0, textStart);

        message = message.Trim();
        return message.Length > 0 && message[message.Length - 1] == '.' ? message : message + ".";
    }
}
