// <copyright file="SymbolExtractor.cs" company="Allied Bits Ltd.">
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

using System.Text.RegularExpressions;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Text.Editor;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Navigation;

/// <summary>
/// Extracts the identifier under the caret together with its enclosing class and namespace
/// using a text-based scan of the buffer (no Roslyn dependency).
/// </summary>
internal static class SymbolExtractor
{
    private static readonly Regex ClassPattern =
        new(@"\bclass\s+(\w+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NamespacePattern =
        new(@"\bnamespace\s+([\w.]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Returns <c>(Namespace, ClassName, Identifier)</c> for the word at the current caret position.
    /// Any component may be <see langword="null"/> if it cannot be determined.
    /// </summary>
    internal static (string? Namespace, string? ClassName, string? Identifier) ExtractSymbolAtCaret(ITextView textView)
    {
        var snapshot = textView.TextBuffer.CurrentSnapshot;
        int caretPos = textView.Caret.Position.BufferPosition.Position;

        // Expand caret position to the full identifier word
        int start = caretPos;
        int end = caretPos;

        while (start > 0 && IsIdentifierChar(snapshot[start - 1]))
            start--;
        while (end < snapshot.Length && IsIdentifierChar(snapshot[end]))
            end++;

        if (start == end)
            return (null, null, null);

        string identifier = snapshot.GetText(start, end - start);

        // Scan the text before the caret to find enclosing class and namespace
        string prefix = snapshot.GetText(0, start);

        string? className = null;
        string? namespaceName = null;

        // Use the LAST match (innermost declaration before the caret)
        foreach (Match m in ClassPattern.Matches(prefix))
            className = m.Groups[1].Value;

        foreach (Match m in NamespacePattern.Matches(prefix))
            namespaceName = m.Groups[1].Value;

        return (namespaceName, className, identifier);
    }

    /// <summary>
    /// DTE-based variant used from OleMenuCommand handlers that don't have an <see cref="ITextView"/>.
    /// </summary>
    internal static (string? Namespace, string? ClassName, string? Identifier) ExtractSymbolFromDte(DTE2 dte)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

        if (dte.ActiveDocument?.Object("TextDocument") is not TextDocument textDoc)
            return (null, null, null);

        TextSelection selection = textDoc.Selection;
        int col = selection.ActivePoint.DisplayColumn; // 1-based

        // Get line text to extract the identifier
        var editPoint = selection.ActivePoint.CreateEditPoint();
        string? lineText = editPoint.GetLines(selection.ActivePoint.Line, selection.ActivePoint.Line + 1)
            ?.TrimEnd('\r', '\n');

        if (string.IsNullOrEmpty(lineText))
            return (null, null, null);

        // col is 1-based; adjust to 0-based index (clamped to valid range)
        int idx = Math.Max(0, Math.Min(col - 1, lineText!.Length - 1));

        // Ensure we're on an identifier character; try one position back if needed
        if (!IsIdentifierChar(lineText[idx]) && idx > 0)
            idx--;

        if (!IsIdentifierChar(lineText[idx]))
            return (null, null, null);

        int start = idx;
        int end = idx;
        while (start > 0 && IsIdentifierChar(lineText[start - 1])) start--;
        while (end < lineText.Length - 1 && IsIdentifierChar(lineText[end + 1])) end++;

        string identifier = lineText.Substring(start, end - start + 1);

        // Get text before cursor for class/namespace extraction
        editPoint.StartOfDocument();
        int absoluteOffset = selection.ActivePoint.AbsoluteCharOffset;
        string prefix = editPoint.GetText(Math.Max(0, absoluteOffset - 1));

        string? className = null;
        string? namespaceName = null;

        foreach (Match m in ClassPattern.Matches(prefix))
            className = m.Groups[1].Value;

        foreach (Match m in NamespacePattern.Matches(prefix))
            namespaceName = m.Groups[1].Value;

        return (namespaceName, className, identifier);
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
