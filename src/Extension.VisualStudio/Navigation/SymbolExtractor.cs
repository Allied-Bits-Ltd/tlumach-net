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
    /// <summary>
    /// Returns <c>(Namespace, ClassName, Identifier)</c> for the word at the current caret position.
    /// Parses the dotted member-access chain on the cursor line to extract
    /// <c>Namespace.ClassName.Identifier</c> (or <c>ClassName.Identifier</c> when there is no
    /// leading namespace segment).  Returns all-nulls if a class name cannot be determined.
    /// </summary>
    internal static (string? Namespace, string? ClassName, string? Identifier) ExtractSymbolAtCaret(ITextView textView)
    {
        var snapshot = textView.TextBuffer.CurrentSnapshot;
        int caretPos = textView.Caret.Position.BufferPosition.Position;

        // Expand caret position to the full identifier word
        int identEnd = caretPos;
        int identStart = caretPos;

        while (identStart > 0 && IsIdentifierChar(snapshot[identStart - 1]))
            identStart--;
        while (identEnd < snapshot.Length && IsIdentifierChar(snapshot[identEnd]))
            identEnd++;

        if (identStart == identEnd)
            return (null, null, null);

        string identifier = snapshot.GetText(identStart, identEnd - identStart);

        // Walk backwards on the same line to find a dotted prefix chain:
        //   someNamespace.MyClass.   <-- segments we want
        int pos = identStart - 1;

        // Skip any whitespace between identifier and preceding dot (defensive)
        while (pos >= 0 && snapshot[pos] == ' ') pos--;

        if (pos < 0 || snapshot[pos] != '.')
            // No dotted prefix → no class name → fail
            return (null, null, null);

        pos--; // move past '.'

        // Read the class-name segment (immediately left of the dot)
        if (pos < 0 || !IsIdentifierChar(snapshot[pos]))
            return (null, null, null);

        int classEnd = pos + 1;
        while (pos > 0 && IsIdentifierChar(snapshot[pos - 1])) pos--;
        int classStart = pos;

        string className = snapshot.GetText(classStart, classEnd - classStart);

        pos = classStart - 1;

        // Skip whitespace
        while (pos >= 0 && snapshot[pos] == ' ') pos--;

        // Check for an optional namespace segment before another '.'
        string? namespaceName = null;
        if (pos >= 0 && snapshot[pos] == '.')
        {
            pos--; // move past '.'

            if (pos >= 0 && IsIdentifierChar(snapshot[pos]))
            {
                int nsEnd = pos + 1;
                while (pos > 0 && IsIdentifierChar(snapshot[pos - 1])) pos--;
                int nsStart = pos;
                namespaceName = snapshot.GetText(nsStart, nsEnd - nsStart);
            }
        }

        return (namespaceName, className, identifier);
    }

    /// <summary>
    /// DTE-based variant used from OleMenuCommand handlers that don't have an <see cref="ITextView"/>.
    /// Parses the dotted member-access chain on the cursor line to extract
    /// <c>Namespace.ClassName.Identifier</c> (or <c>ClassName.Identifier</c> when there is no
    /// leading namespace segment).  Returns all-nulls if a class name cannot be determined.
    /// </summary>
    internal static (string? Namespace, string? ClassName, string? Identifier) ExtractSymbolFromDte(DTE2 dte)
    {
        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

        if (dte.ActiveDocument?.Object("TextDocument") is not TextDocument textDoc)
            return (null, null, null);

        TextSelection selection = textDoc.Selection;
        int col = selection.ActivePoint.DisplayColumn; // 1-based

        // Get line text
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

        // Expand to the full identifier under the cursor
        int identStart = idx;
        int identEnd = idx;
        while (identStart > 0 && IsIdentifierChar(lineText[identStart - 1])) identStart--;
        while (identEnd < lineText.Length - 1 && IsIdentifierChar(lineText[identEnd + 1])) identEnd++;

        string identifier = lineText.Substring(identStart, identEnd - identStart + 1);

        // Walk backwards past the identifier to find a dotted prefix chain:
        //   someNamespace.MyClass.   <-- we want these segments
        // The character immediately before identStart must be '.' for a chain to exist.
        int pos = identStart - 1;

        // Skip any whitespace between the dot and the identifier (defensive)
        while (pos >= 0 && lineText[pos] == ' ') pos--;

        if (pos < 0 || lineText[pos] != '.')
            // No dotted prefix → no class name → fail
            return (null, null, null);

        pos--; // move past the '.'

        // Read the class-name segment (immediately left of the dot)
        if (pos < 0 || !IsIdentifierChar(lineText[pos]))
            return (null, null, null);

        int classEnd = pos;
        while (pos > 0 && IsIdentifierChar(lineText[pos - 1])) pos--;
        int classStart = pos;

        string className = lineText.Substring(classStart, classEnd - classStart + 1);

        pos = classStart - 1;

        // Skip whitespace
        while (pos >= 0 && lineText[pos] == ' ') pos--;

        // Check for an optional namespace segment before another '.'
        string? namespaceName = null;
        if (pos >= 0 && lineText[pos] == '.')
        {
            pos--; // move past '.'

            if (pos >= 0 && IsIdentifierChar(lineText[pos]))
            {
                int nsEnd = pos;
                while (pos > 0 && IsIdentifierChar(lineText[pos - 1])) pos--;
                int nsStart = pos;
                namespaceName = lineText.Substring(nsStart, nsEnd - nsStart + 1);
            }
        }

        return (namespaceName, className, identifier);
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
