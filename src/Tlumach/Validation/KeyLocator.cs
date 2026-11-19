// <copyright file="KeyLocator.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach.Validation;

/// <summary>
/// Finds the line and column of keys in a translation file, so that messages about an entry point at it.
/// </summary>
internal static class KeyLocator
{
    /// <summary>
    /// Finds the positions of the given keys. The parsers that record the positions of keys (key-value and table formats) are asked first; for the other formats, the last segment of the key is searched for in the text.
    /// </summary>
    /// <param name="parser">The parser of the file.</param>
    /// <param name="content">The content of the file.</param>
    /// <param name="culture">The culture with which the file was loaded.</param>
    /// <param name="textProcessingMode">The text processing mode with which the file was loaded.</param>
    /// <param name="keys">The keys (as <see cref="TranslationEntry.Key"/> has them).</param>
    /// <returns>The 1-based line and column of each key that was found.</returns>
    public static Dictionary<string, (int Line, int Column)> Locate(BaseParser parser, string content, CultureInfo? culture, TextFormat? textProcessingMode, IReadOnlyCollection<string> keys)
    {
        Dictionary<string, (int Line, int Column)> result = new(StringComparer.OrdinalIgnoreCase);
        if (keys.Count == 0)
            return result;

        Translation? located = null;
        bool previous = BaseParser.PopulateKeyLocations;

#pragma warning disable CA1031 // The positions are a convenience: a parser that fails in this mode leaves the keys unlocated
        try
        {
            BaseParser.PopulateKeyLocations = true;
            located = parser.LoadTranslation(content, culture, textProcessingMode);
        }
        catch (Exception)
        {
            located = null;
        }
        finally
        {
            BaseParser.PopulateKeyLocations = previous;
        }
#pragma warning restore CA1031

        foreach (string key in keys)
        {
            if (located is not null
                && located.TryGetValue(key.ToUpperInvariant(), out TranslationEntry? entry)
                && entry.KeyLocated is { LineNumber: > 0 } location)
            {
                result[key] = (location.LineNumber, Math.Max(location.ColumnNumber, 1));
                continue;
            }

            if (FindInText(content, key) is { } found)
                result[key] = found;
        }

        return result;
    }

    /// <summary>
    /// Finds the first occurrence of the last segment of a key that stands as a whole word.
    /// </summary>
    private static (int Line, int Column)? FindInText(string content, string key)
    {
        int dot = key.LastIndexOf('.');
        string segment = dot >= 0 && dot < key.Length - 1 ? key.Substring(dot + 1) : key;
        if (segment.Length == 0)
            return null;

        int start = 0;
        while (start < content.Length)
        {
            int index = content.IndexOf(segment, start, StringComparison.Ordinal);
            if (index < 0)
                return null;

            bool wordStart = index == 0 || !IsKeyChar(content[index - 1]);
            int end = index + segment.Length;
            bool wordEnd = end >= content.Length || !IsKeyChar(content[end]);

            if (wordStart && wordEnd)
                return ToLineAndColumn(content, index);

            start = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Finds the first occurrence of a text that is not a part of a longer name (it is not preceded or followed by a letter, a digit, or an underscore), ignoring the case. An occurrence on a line that is not a comment is preferred, because configurations often mention their files in comments.
    /// </summary>
    /// <param name="content">The text to search.</param>
    /// <param name="text">The text to find.</param>
    /// <returns>The 1-based line and column, or (0, 0) when the text was not found.</returns>
    public static (int Line, int Column) FindWord(string content, string text)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(text))
            return (0, 0);

        int inComment = -1;
        int start = 0;
        while (start < content.Length)
        {
            int index = content.IndexOf(text, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                break;

            int end = index + text.Length;
            bool wordStart = index == 0 || !IsNameChar(content[index - 1]);
            bool wordEnd = end >= content.Length || !IsNameChar(content[end]);
            if (wordStart && wordEnd)
            {
                if (!IsInCommentLine(content, index))
                    return ToLineAndColumn(content, index);

                if (inComment < 0)
                    inComment = index;
            }

            start = index + 1;
        }

        return inComment >= 0 ? ToLineAndColumn(content, inComment) : (0, 0);
    }

    /// <summary>
    /// Determines whether the line of the offset is a comment of an INI or TOML file (";" or "#"), of a JSON file with comments ("//"), or of an XML file ("&lt;!--").
    /// </summary>
    private static bool IsInCommentLine(string content, int offset)
    {
        int lineStart = content.LastIndexOf('\n', Math.Max(offset - 1, 0)) + 1;
        string line = content.Substring(lineStart, offset - lineStart).TrimStart();
        return (line.Length > 0 && (line[0] == ';' || line[0] == '#'))
            || line.StartsWith("//", StringComparison.Ordinal)
            || line.StartsWith("<!--", StringComparison.Ordinal);
    }

    private static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static bool IsKeyChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';

    /// <summary>
    /// Converts an offset in the text to a 1-based line and column.
    /// </summary>
    /// <param name="content">The text.</param>
    /// <param name="offset">The 0-based offset.</param>
    /// <returns>The line and column.</returns>
    public static (int Line, int Column) ToLineAndColumn(string content, int offset)
    {
        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < offset && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, offset - lineStart + 1);
    }
}
