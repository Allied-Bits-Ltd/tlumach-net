using System;
using System.Collections.Generic;
using System.Text;

namespace Tlumach.Base
{
    public class TsvParser : TableTextParser
    {

        public static TemplateStringEscaping TemplateEscapeMode { get; set; }

        private static BaseFileParser Factory() => new TsvParser();

        static TsvParser()
        {
            TemplateEscapeMode = TemplateStringEscaping.None;

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
    }
}
