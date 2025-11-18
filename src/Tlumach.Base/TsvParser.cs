using System;
using System.Collections.Generic;
using System.Text;

namespace Tlumach.Base
{
    public class TsvParser : BaseTableParser
    {
        /// <summary>
        /// Gets or sets the text processing mode to use when decoding potentially escaped strings and when recognizing template strings in translation entries.
        /// </summary>
        public static TextFormat TextProcessingMode { get; set; }

        /// <summary>
        /// Gets or sets the flag that tells the parser whether the TSV file uses quotes to wrap the text with unsafe characters (new-line characters and tabs that are a part of values).
        /// </summary>
        public static bool ExpectQuotes { get; set; }

        private static BaseParser Factory() => new TsvParser();

        static TsvParser()
        {
            TextProcessingMode = TextFormat.None;

            // Use configuration files in INI or TOML formats.
            FileFormats.RegisterParser(".tsv", Factory);
        }

        /// <summary>
        /// Initializes the parser class, making it available for use.
        /// </summary>
        public static void Use()
        {
            // The role of this method is just to exist so that calling it executes a static constructor of this class.
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".tsv", StringComparison.OrdinalIgnoreCase);
        }

        protected override void ReadCells(string content, int offset, int lineNumber, List<string> buffer, out int posAfterEnd)
        {
            ReadDelimitedLine(content, offset, lineNumber, buffer, out posAfterEnd, separator: '\t', quotedFields: ExpectQuotes);
        }
    }
}
