// <copyright file="MultiLocaleInspector.cs" company="Allied Bits Ltd.">
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
using System.Text.Json;
using System.Xml;

using Tlumach.Base;

namespace Tlumach.Validation;

/// <summary>
/// Lists the locales stored in the translation formats that keep several locales in one file: CSV and TSV (one column per locale), Apple String Catalog (localizations of every string), and XLIFF (source and target language).
/// </summary>
internal static class MultiLocaleInspector
{
    /// <summary>
    /// Determines whether the parser reads a format that can keep several locales in one file.
    /// </summary>
    /// <param name="parser">The parser of the file.</param>
    /// <returns><see langword="true"/> for CSV, TSV, String Catalog, and XLIFF.</returns>
    public static bool IsMultiLocale(BaseParser parser) => parser is BaseTableParser or StringCatParser or XliffParser;

    /// <summary>
    /// Lists the locales stored in the file. Locale names that are not known cultures are left out, because a translation manager never asks for them.
    /// <para>The method does not report errors: when the content cannot be read, it returns what it has found so far, and loading the file reports the error.</para>
    /// </summary>
    /// <param name="parser">The parser of the file.</param>
    /// <param name="content">The content of the file.</param>
    /// <returns>The distinct locale names in the order of their appearance; empty when the format keeps one locale or nothing was found.</returns>
    public static IList<string> GetLocales(BaseParser parser, string content)
    {
        List<string> result = [];

#pragma warning disable CA1031 // Errors are reported by the parser when the file is loaded
        try
        {
            switch (parser)
            {
                case CsvParser:
                    AddKnownCultures(result, TableHeaderReader.ReadCsvHeader(content));
                    break;
                case TsvParser:
                    AddKnownCultures(result, TableHeaderReader.ReadTsvHeader(content));
                    break;
                case StringCatParser:
                    AddKnownCultures(result, ReadStringCatalogLocales(content));
                    break;
                case XliffParser:
                    AddKnownCultures(result, ReadXliffLanguages(content));
                    break;
            }
        }
        catch (Exception)
        {
            // keep what was found
        }
#pragma warning restore CA1031

        return result;
    }

    private static void AddKnownCultures(List<string> result, IEnumerable<string> names)
    {
        foreach (string rawName in names)
        {
            string name = rawName.Trim();
            if (name.Length == 0 || result.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;

            if (CultureNames.TryGetCulture(name, out _))
                result.Add(name);
        }
    }

    private static List<string> ReadStringCatalogLocales(string content)
    {
        List<string> result = [];

        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return result;

        if (root.TryGetProperty("sourceLanguage", out JsonElement sourceLanguage) && sourceLanguage.ValueKind == JsonValueKind.String)
            result.Add(sourceLanguage.GetString() ?? string.Empty);

        if (!root.TryGetProperty("strings", out JsonElement strings) || strings.ValueKind != JsonValueKind.Object)
            return result;

        foreach (JsonProperty entry in strings.EnumerateObject())
        {
            if (entry.Value.ValueKind == JsonValueKind.Object
                && entry.Value.TryGetProperty("localizations", out JsonElement localizations)
                && localizations.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty localization in localizations.EnumerateObject())
                    result.Add(localization.Name);
            }
        }

        return result;
    }

    private static List<string> ReadXliffLanguages(string content)
    {
        List<string> result = [];

        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        using StringReader stringReader = new(content);
        using XmlReader reader = XmlReader.Create(stringReader, settings);

        if (reader.MoveToContent() == XmlNodeType.Element)
        {
            string? sourceLanguage = reader.GetAttribute("srcLang");
            string? targetLanguage = reader.GetAttribute("trgLang");

            if (sourceLanguage is { Length: > 0 })
                result.Add(sourceLanguage);

            if (targetLanguage is { Length: > 0 })
                result.Add(targetLanguage);
        }

        return result;
    }

    /// <summary>
    /// Reads the first line of a table with the cell reader of the parser itself, so that quoting and separators are handled exactly as when the file is loaded.
    /// </summary>
    private static class TableHeaderReader
    {
        public static List<string> ReadCsvHeader(string content) => ReadHeader(new CsvHeader(), content);

        public static List<string> ReadTsvHeader(string content) => ReadHeader(new TsvHeader(), content);

        private static List<string> ReadHeader(IHeaderReader reader, string content)
        {
            List<string> cells = [];

            // Skip empty lines, as the parser does
            int offset = 0;
            int lineNumber = 1;
            while (offset < content.Length && (content[offset] == '\r' || content[offset] == '\n'))
            {
                if (content[offset] == '\n')
                    lineNumber++;
                offset++;
            }

            if (offset >= content.Length)
                return cells;

            reader.Read(content, offset, lineNumber, cells);

            // The first column holds the keys; the description and comments columns are not locales
            List<string> result = [];
            for (int i = 1; i < cells.Count; i++)
            {
                string caption = cells[i].Trim();
                if (caption.Equals(BaseTableParser.DescriptionColumnCaption, StringComparison.OrdinalIgnoreCase)
                    || caption.Equals(BaseTableParser.CommentsColumnCaption, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(caption);
            }

            return result;
        }

        private interface IHeaderReader
        {
            void Read(string content, int offset, int lineNumber, List<string> buffer);
        }

        private sealed class CsvHeader : CsvParser, IHeaderReader
        {
            public void Read(string content, int offset, int lineNumber, List<string> buffer) => ReadCells(content, offset, lineNumber, buffer, out _);
        }

        private sealed class TsvHeader : TsvParser, IHeaderReader
        {
            public void Read(string content, int offset, int lineNumber, List<string> buffer) => ReadCells(content, offset, lineNumber, buffer, out _);
        }
    }
}
