using System;
using System.Collections.Generic;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    public class CsvParser : BaseTableParser
    {
        /// <summary>
        /// Gets or sets the text processing mode to use when decoding potentially escaped strings and when recognizing template strings in translation entries.
        /// </summary>
        public static TextFormat TextProcessingMode { get; set; }

        /// <summary>
        /// Gets or sets the separator char used to separate values. Default is comma, but Excel uses semicolon ';' as a separator for exported CSVs.
        /// </summary>
        public static char SeparatorChar { get; set; } = ',';

        private static BaseParser Factory() => new CsvParser();

        static CsvParser()
        {
            TextProcessingMode = TextFormat.None;

            // Use configuration files in INI or TOML formats.
            FileFormats.RegisterParser(".csv", Factory);
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
            return !string.IsNullOrEmpty(fileExtension) && fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
        }

        protected override void ReadCells(string content, int offset, int lineNumber, List<string> buffer, out int posAfterEnd)
        {
            ReadDelimitedLine(content, offset, lineNumber, buffer, out posAfterEnd, separator: SeparatorChar, quotedFields: true);
        }
    }
}
