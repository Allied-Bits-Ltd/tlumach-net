// <copyright file="TableTextParser.cs" company="Allied Bits Ltd.">
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

using System.Data.Common;
using System.Globalization;
using System.Reflection;

namespace Tlumach.Base
{
    /// <summary>
    ///  The base class for CSV and TSV parsers.
    /// </summary>
    public abstract class BaseTableParser : BaseParser
    {

        /// <summary>
        /// Use this property to override the caption by which the description column was detected.
        /// </summary>
        public static string DescriptionColumnCaption { get; set; } = "Description";

        /*
        public static string CommentsColumnCaption { get; set; } = "Comments";

        public static string ExampleColumnCaption { get; set; } = "Example";
        */

        /// <summary>
        /// Gets or sets the character that is used to separate the locale name from the base name in the names of locale-specific translation files.
        /// </summary>
        public static char LocaleSeparatorChar { get; set; } = '_';

        /// <summary>
        /// Gets or sets the indicator that tells the parser to treat empty values as missing instead of being empty.
        /// <para>When the text is missing from the specific translation, the text from the basic locale or from the default locale will be used. When the text is empty, this empty value will be used without a fallback to the basic or default locale.</para>
        /// </summary>
        public static bool TreatEmptyValuesAsAbsent { get; set; }

        public override bool UseDefaultFileForTranslations => true;

        /// <summary>
        /// Handle this event to match the column caption to a specific locale. For example, if the request is made for "de-AT" locale, you can provide the translation from the column captioned "German".
        /// </summary>
        public static event EventHandler<CultureNameMatchEventArgs>? OnCultureNameMatchCheck;

        public override char GetLocaleSeparatorChar()
        {
            return LocaleSeparatorChar;
        }

        public override Translation? LoadTranslation(string translationText, CultureInfo? culture)
        {
            string key;
            string? value, escapedValue, reference;

            Translation result = new(locale: null);
            TranslationEntry entry;

            if (string.IsNullOrEmpty(translationText))
                return null;

            /*
            int commentsColumn = -1;
            int exampleColumn = -1;
            */

            List<(string Locale, List<string> Values)> columns = LoadAsListOfLists(translationText, false, culture, out int specificLocaleColumn, out int descriptionColumn);

            // We can't accept the files with no columns or with just keys
            if (columns.Count < 2)
                return null;

            if (specificLocaleColumn == -1)
                return null;
            //throw new GenericParserException($"Translation for locale '{culture}' was not found.");

            // Iterate through each line and add the keys. Use values (if present) to determine if the text is templated.
            // We start from 1, because the first line (the one with index 0) contains the empty value in the first column and, at best, the locale name in the second column.
            for (int i = 0; i < columns[0].Values.Count; i++)
            {
                key = columns[0].Values[i];

                value = columns[specificLocaleColumn].Values[i];
                if (value.Length == 0 && TreatEmptyValuesAsAbsent)
                    continue;

                escapedValue = null;
                reference = null;

                if (value is not null && IsReference(value))
                {
                    reference = value.Substring(1).Trim();
                    value = null;
                }

                // If the settings specify that values can be escaped (e.g., in TSV), we should keep both an escaped and un-escaped text
                if ((GetEscapeMode() == TextFormat.BackslashEscaping || GetEscapeMode() == TextFormat.DotNet) && value is not null)
                {
                    escapedValue = value;
                    value = Utils.UnescapeString(value);
                    entry = new TranslationEntry(key, text: value, escapedText: escapedValue, reference);
                }
                else
                {
                    entry = new TranslationEntry(key, text: value, escapedText: null, reference);
                }

                if (reference is null && value is not null)
                    entry.IsTemplated = IsTemplatedText((escapedValue is not null) ? escapedValue : value);

                if (descriptionColumn != -1)
                    entry.Description = columns[descriptionColumn].Values[i];

                /*
                if (commentsColumn != -1)
                    entry.Comment = columns[commentsColumn].Values[i];

                if (exampleColumn != -1)
                    entry.Example = columns[exampleColumn].Values[i];
                */

                result.Add(key.ToUpperInvariant(), entry);
            }

            return result;
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly)
        {
            // table parsers don't have own configuration format but use simple INI format supported by IniParser
            throw new NotImplementedException("Table parsers don't have own configuration format but use simple INI format supported by IniParser");
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
        {
            string key, value;
            bool valuesPresent;

            if (string.IsNullOrEmpty(content))
                return null;

            List<(string Locale, List<string> Values)> columns = LoadAsListOfLists(content, true, null, out _, out _);

            // We can't accept the text with no columns
            if (columns.Count == 0)
                return null;

            TranslationTree result = new();
            TranslationTreeNode node = result.RootNode;
            TranslationTreeLeaf leaf;

            valuesPresent = columns.Count > 1;

            // Iterate through each line and add the keys. Use values (if present) to determine if the text is templated.
            // We start from 1, because the first line (the one with index 0) contains the empty value in the first column and, at best, the locale name in the second column.
            for (int i = 1; i < columns[0].Values.Count; i++)
            {
                key = columns[0].Values[i];

                if (valuesPresent)
                {
                    value = columns[1].Values[i];
                    leaf = new TranslationTreeLeaf(key, !IsReference(value) && IsTemplatedText(value));
                }
                else
                {
                    leaf = new TranslationTreeLeaf(key, false);
                }

                node.Keys.Add(key, leaf);
            }

            return result;
        }

        /// <summary>
        /// This method loads the file as a list of keys and their texts.
        /// <para>The 0th item in the list has an empty locale and it contains keys. All other items contains the locale name and the list of values. Empty cells are represented as empty values.</para>
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <param name="onlyStructure">Specifies if only the structure (the keys, the first translation column and, if present, descriptions, examples, and comments) should be read. If the value is <see langword="false"/>, the translation or translations are loaded as well depending on the value of the `specificLocale` parameter.</param>
        /// <param name="specificCulture">When set, should contain the reference to the locale to load from the file. If not set, all locales are loaded.</param>
        /// <returns>The list of key-value pairs.</returns>
        internal List<(string Locale, List<string> Values)> LoadAsListOfLists(string content, bool onlyStructure, CultureInfo? specificCulture, out int specificLocaleColumn, out int descriptionColumn)
        {
            List<string> cells = [];
            List<(string locale, List<string> values)> result = new List<(string locale, List<string> values)>();

            bool firstLine = true;

            bool useSpecificLocale = specificCulture is not null;

            int defaultLocaleColumnInput = -1;
            int defaultLocaleColumn = -1;
            specificLocaleColumn = -1;
            descriptionColumn = -1;

            int specificLocaleColumnInput = -1;
            int descriptionColumnInput = -1;

            /*
            int commentsColumn = -1;
            int exampleColumn = -1;
            */

            int cellIndex = 1; // we start from 1, because the 0th item is always a key

            int lineNumber = 1;
            // int lineStartPos = 0;
            int offset = 0;
            int posAfterEnd = 0;
            int numberOfColumns = 0;

            string cellValue;

            while (offset < content.Length)
            {
                // Skip empty lines
                if (content[offset] == '\n' || content[offset] == '\r')
                {
                    offset++;
                    if (content[offset] == '\n' ||
                        (content[offset] == '\r' && offset < content.Length - 1 && content[offset] != '\n'))
                    {
                        lineNumber++;
                        // lineStartPos = offset;
                    }

                    continue;
                }

                cells.Clear();
                ReadCells(content, offset, lineNumber, cells, out posAfterEnd);

                // Did we reach the end of the content?
                if (cells.Count == 0 && posAfterEnd >= content.Length)
                    break;

                // the end has not been reached, but something went wrong
                if (cells.Count == 0 || posAfterEnd == offset)
                    throw new TextParseException($"Malformed line detected on line {lineNumber}", offset, posAfterEnd, lineNumber, 1);

                // if it is the first line, take locale names and other column captions from it
                if (firstLine)
                {
                    firstLine = false;

                    numberOfColumns = cells.Count;

                    if (numberOfColumns == 0)
                        throw new GenericParserException("There is no data to load");

                    // At this point, we have at least one column, which is generally ok

                    // Add the entry for keys
                    result.Add((string.Empty, new List<string>()));

                    // Copy locale names
                    for (int i = 1; i < cells.Count; i++)
                    {
                        bool addValue = false;
                        if (cells.Count > 2 && cells[i].Trim().Length == 0)
                            throw new TextParseException("Multiple columns are provided, but the locale name is empty for at least one column. Locale names must be listed as column captions on the first non-empty text line.", offset, offset, lineNumber, 1);

                        cellValue = cells[i];

                        // Add an entry for each locale
                        if (cellValue.Equals(DescriptionColumnCaption, StringComparison.OrdinalIgnoreCase))
                        {
                            descriptionColumnInput = i;
                            descriptionColumn = cellIndex;
                            cellIndex++;
                            addValue = !onlyStructure;
                        }
                        else
                        /*
                        if (cellValue.Equals(CommentsColumnCaption, StringComparison.OrdinalIgnoreCase))
                        {
                            commentsColumnInput = i;
                            commentsColumn = cellIndex;
                            cellIndex++;
                            addValue = !onlyStructure;
                        }
                        else
                        if (cellValue.Equals(ExampleColumnCaption, StringComparison.OrdinalIgnoreCase))
                        {
                            exampleColumnInput = i;
                            exampleColumn = cellIndex;
                            cellIndex++;
                            addValue = !onlyStructure;
                        }
                        else
                        */
                        if (onlyStructure)
                        {
                            if (i >= 1 && defaultLocaleColumnInput == -1)
                            {
                                defaultLocaleColumnInput = i;
                                defaultLocaleColumn = cellIndex;
                                cellIndex++;
                                addValue = true;
                            }
                        }
                        else
                        if (!useSpecificLocale)
                        {
                            if (i >= 1 && defaultLocaleColumnInput == -1)
                            {
                                defaultLocaleColumnInput = i;
                                defaultLocaleColumn = cellIndex;
                                cellIndex++;
                            }

                            addValue = true;
                        }
                        else
                        if (string.IsNullOrEmpty(specificCulture?.Name)) // For invariant culture, use the first available translation.
                        {
                            if (specificLocaleColumnInput == -1)
                            {
                                specificLocaleColumnInput = i;
                                specificLocaleColumn = cellIndex;
                                cellIndex++;
                                addValue = true;
                            }
                        }
                        else
                        if (CultureNamesMatch(cellValue, specificCulture))
                        {
                            specificLocaleColumnInput = i;
                            specificLocaleColumn = cellIndex;
                            cellIndex++;
                            addValue = true;
                        }

                        if (addValue)
                            result.Add((cellValue, new List<string>()));
                    }

                    // If we have found no exact match of locale names, try to find the language column for the requested locale ("de" for "de-AT")
                    if (useSpecificLocale && !string.IsNullOrEmpty(specificCulture?.Name) && specificLocaleColumnInput == -1 && specificCulture.Name.IndexOf('-') == 2)
                    {
                        string lang = specificCulture.Name.Substring(0, 2);
                        specificLocaleColumnInput = cells.FindIndex(c => c.Equals(lang, StringComparison.OrdinalIgnoreCase));
                        if (specificLocaleColumnInput != -1)
                        {
                            specificLocaleColumn = 1;
                            if (descriptionColumnInput >= 0 && specificLocaleColumnInput > descriptionColumnInput)
                                specificLocaleColumn++;
                            result.Insert(specificLocaleColumn, (cells[specificLocaleColumnInput], new List<string>()));
                        }
                    }

                    if (useSpecificLocale && specificLocaleColumnInput == -1)
                        return result;
                }
                else
                {
                    // Pick translations and descriptions

                    // Pick and validate the key
                    cellValue = cells[0].Trim();
                    if (cellValue.Length == 0)
                        throw new TextParseException($"Empty key detected on line {lineNumber}", offset, posAfterEnd, lineNumber, 1);

                    if (result[0].values.FirstOrDefault(c => cellValue.Equals(c, StringComparison.OrdinalIgnoreCase)) != null)
                        throw new TextParseException($"A duplicate key {cellValue} detected on line {lineNumber}", offset, posAfterEnd, lineNumber, 1);

                    result[0].values.Add(cellValue);

                    cellIndex = 1;  // we start from 1, because the 0th item is always a key

                    // Pick and optionally store each translation
                    for (int i = 1; i < numberOfColumns; i++)
                    {
                        if (i >= cells.Count)
                            throw new TextParseException($"Insufficient number of columns detected on line {lineNumber} ({numberOfColumns} columns expected, {cells.Count} columns found)", offset, posAfterEnd, lineNumber, 1);

                        cellValue = cells[i].Trim();

                        if (i == defaultLocaleColumnInput || i == specificLocaleColumnInput || i == descriptionColumnInput /*|| i == exampleColumnInput || i == commentsColumnInput*/)
                        {
                            result[cellIndex].values.Add(cellValue);
                            cellIndex++;
                        }

                        // it could be that some line contains more cells than the first line (aka than the number of columns defined in the file)
                        if (cellIndex == result.Count)
                            break;
                    }
                }

                offset = posAfterEnd;
                lineNumber++;
            }

            if (onlyStructure)
                specificLocaleColumn = defaultLocaleColumn;

            return result;
        }

        /// <summary>
        /// Reads cells from the provided text starting at position <see cref="offset"/>.
        /// <para>This is an abstract function overridden by specific parsers which implement the logic or call the <seealso cref="ReadCells(string, int, int, List{string}, out int)"/> method with the required parameters for the default implementation.</para>
        /// </summary>
        /// <param name="content">The text, from which the cells are read.</param>
        /// <param name="offset">The starting position of the text to read from.</param>
        /// <param name="lineNumber">The current line being processed. Used for error reporting.</param>
        /// <param name="buffer">The container for read cells.</param>
        /// <param name="posAfterEnd">Upon return, is set to the position of the first character beyond the read one. EOL characters are skipped, so the value must reference the start of the next line.</param>
        protected abstract void ReadCells(string content, int offset, int lineNumber, List<string> buffer, out int posAfterEnd);

        /// <summary>
        /// The implementation of reading cells from the provided text starting at position <see cref="offset"/>.
        /// </summary>
        /// <param name="content">The text, from which the cells are read.</param>
        /// <param name="offset">The starting position of the text to read from.</param>
        /// <param name="lineNumber">The current line being processed. Used for error reporting.</param>
        /// <param name="buffer">The container for read cells.</param>
        /// <param name="posAfterEnd">Upon return, is set to the position of the first character beyond the read one. EOL characters are skipped, so the value must reference the start of the next line.</param>
        /// <param name="separator">The separator character (comma, tab, maybe something else).</param>
        /// <param name="quotedFields">Specifies whether cells are expected to be enclosed in quotes.</param>
        /// <exception cref="ArgumentNullException">Thrown if `content` or `buffer` is null. </exception>
        /// <exception cref="TextParseException">Thrown when parsing ends with unclosed quotes (when they are used).</exception>
        protected static void ReadDelimitedLine(string content, int offset, int lineNumber, List<string> buffer, out int posAfterEnd, char separator, bool quotedFields)
        {
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance
#pragma warning disable MA0015
            if (content is null)
                throw new ArgumentNullException(nameof(content));

            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
#pragma warning restore MA0015
#pragma warning restore CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

            buffer.Clear();

            if (offset >= content.Length) // this covers the case when content is empty
            {
                posAfterEnd = content.Length;
                return;
            }

            var sb = new System.Text.StringBuilder(64);
            int i = offset;
            int n = content.Length;
            bool inQuotes = false;
            int quoteStart = 0;

            while (i < n)
            {
                char ch = content[i];

                if (inQuotes)
                {
                    if (ch == Utils.C_DOUBLE_QUOTE)
                    {
                        // If doubled quote inside a quoted field, it's an escaped quote.
                        if (i + 1 < n && content[i + 1] == Utils.C_DOUBLE_QUOTE)
                        {
                            sb.Append(Utils.C_DOUBLE_QUOTE);
                            i += 2;
                        }
                        else
                        {
                            inQuotes = false;
                            i++; // consume closing quote
                        }
                    }
                    else
                    {
                        if ((ch != '\r') || (i == n - 1) || (i < n - 1 && content[i + 1] != '\n')) // \r gets appended only when it is not followed by \n
                            sb.Append(ch);
                        i++;
                    }
                }
                else
                {
                    if (quotedFields && ch == Utils.C_DOUBLE_QUOTE)
                    {
                        inQuotes = true;
                        quoteStart = i;
                        i++; // start quoted field
                    }
                    else
                    if (ch == separator)
                    {
                        buffer.Add(sb.ToString());
                        sb.Clear();
                        i++; // consume separator
                    }
                    else
                    if (ch == '\r' || ch == '\n')
                    {
                        break; // end of line (outside quotes)
                    }
                    else
                    {
                        sb.Append(ch);
                        i++;
                    }
                }
            }

            if (inQuotes)
                throw new TextParseException($"Unclosed quote at {lineNumber}:{quoteStart - offset + 1}", quoteStart, i, lineNumber, quoteStart - offset + 1);

            // add the last (or only) cell
            buffer.Add(sb.ToString());

            // Advance past the end-of-line to the start of the next line.
            if (i < n)
            {
                if (content[i] == '\r')
                {
                    i++;

                    if (i < n && content[i] == '\n')
                        i++; // consume CRLF
                }
                else
                if (content[i] == '\n')
                {
                    i++;
                }
            }

            posAfterEnd = i;
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return false; // this class doesn't handle anything on its own. Its descendants do.
        }

        private static bool CultureNamesMatch(string candidate, CultureInfo? culture)
        {
            if (culture is null)
                return false;
            if (candidate.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
                return true;

            if (OnCultureNameMatchCheck is not null)
            {
                CultureNameMatchEventArgs args = new(candidate, culture);
                OnCultureNameMatchCheck.Invoke(null, args);
                return args.Match;
            }

            return false;
        }
    }
}
