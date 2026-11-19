// <copyright file="ValidationMessage.cs" company="Allied Bits Ltd.">
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
using System.Text;

#if GENERATOR
namespace Tlumach.Generator.Validation;
#else
namespace Tlumach.Validation;
#endif

/// <summary>
/// One problem found by <see cref="TranslationValidator"/>.
/// </summary>
public sealed class ValidationMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationMessage"/> class.
    /// </summary>
    /// <param name="severity">The severity.</param>
    /// <param name="code">The code of the check (one of <see cref="ValidationCodes"/>).</param>
    /// <param name="message">The text of the message.</param>
    /// <param name="filePath">The full path of the file that the message is about, if any.</param>
    /// <param name="line">The 1-based line in <paramref name="filePath"/>, or 0 when unknown.</param>
    /// <param name="column">The 1-based column in <paramref name="filePath"/>, or 0 when unknown.</param>
    public ValidationMessage(ValidationSeverity severity, string code, string message, string? filePath, int line = 0, int column = 0)
    {
        Severity = severity;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        FilePath = filePath;
        Line = line < 0 ? 0 : line;
        Column = column < 0 ? 0 : column;
    }

    /// <summary>Gets the severity.</summary>
    public ValidationSeverity Severity { get; }

    /// <summary>Gets the code of the check (one of <see cref="ValidationCodes"/>).</summary>
    public string Code { get; }

    /// <summary>Gets the text of the message.</summary>
    public string Message { get; }

    /// <summary>Gets the full path of the file that the message is about, or <see langword="null"/>.</summary>
    public string? FilePath { get; }

    /// <summary>Gets the 1-based line in <see cref="FilePath"/>, or 0 when the line is unknown.</summary>
    public int Line { get; }

    /// <summary>Gets the 1-based column in <see cref="FilePath"/>, or 0 when the column is unknown.</summary>
    public int Column { get; }

    /// <summary>
    /// Formats the message the way compilers do, which lets Visual Studio and VS Code turn the line in an output window into a link: <c>path(line,column): error CODE: text</c>.
    /// </summary>
    /// <returns>The formatted message.</returns>
    public override string ToString()
    {
        StringBuilder builder = new();

        if (!string.IsNullOrEmpty(FilePath))
        {
            builder.Append(FilePath);
            if (Line > 0)
            {
                builder.Append('(').Append(Line.ToString(CultureInfo.InvariantCulture));
                if (Column > 0)
                    builder.Append(',').Append(Column.ToString(CultureInfo.InvariantCulture));
                builder.Append(')');
            }

            builder.Append(": ");
        }

        builder.Append(Severity switch
        {
            ValidationSeverity.Error => "error",
            ValidationSeverity.Warning => "warning",
            _ => "info",
        });

        builder.Append(' ').Append(Code).Append(": ").Append(Message);
        return builder.ToString();
    }
}
