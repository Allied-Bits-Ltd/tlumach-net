using System;
using System.Collections.Generic;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator;
#else
namespace Tlumach.Base;
#endif

public class TsvParser : BaseTableParser
{
    /// <summary>
    /// Gets or sets the text processing mode to use when decoding potentially escaped strings and when recognizing template strings in translation entries.
    /// </summary>
    public static TextFormat TextProcessingMode { get; set; }

    /// <summary>
    /// Gets or sets the flag that tells the parser whether the TSV file uses quotes to wrap the text with unsafe characters (new-line characters and tabs that are a part of values).
    /// <para>The default value is <see langword="true"/>: a cell that starts with a double quote is a quoted cell, in which a doubled double quote stands for one double quote, as <c>TsvWriter</c>
    /// of Tlumach.Writers writes the values with tabs and line breaks. A double quote elsewhere in a cell is a part of the value. Set the property to <see langword="false"/> to read every cell as it stands.</para>
    /// </summary>
    public static bool ExpectQuotes { get; set; } = true;

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

    protected override TranslationTree? InternalLoadTranslationStructure(string content, TextFormat? textProcessingMode)
    {
        if (textProcessingMode is not null)
            TsvParser.TextProcessingMode = textProcessingMode.Value;
        return base.InternalLoadTranslationStructure(content, textProcessingMode);
    }

    protected override void ReadCells(string content, int offset, int lineNumber, List<string> buffer, out int posAfterEnd)
    {
        ReadDelimitedLine(content, offset, lineNumber, buffer, out posAfterEnd, separator: '\t', quotedFields: ExpectQuotes, quotesOnlyAtCellStart: true);
    }
}
