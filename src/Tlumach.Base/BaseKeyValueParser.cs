// <copyright file="BaseKeyValueParser.cs" company="Allied Bits Ltd.">
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

using System;
using System.Globalization;
using System.Reflection;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// The base parser for key-value configuration and translation files (ini and toml).
    /// </summary>
    public abstract class BaseKeyValueParser : BaseParser
    {
        private enum TextParserState
        {
            LookingForLineStart,
            SkippingTillEOL,
            SkippingWSOnlyTillEOL,
            CapturingKey,
            LookingForSeparator,
            LookingForValueStart,
            CapturingValue,
            LookingForNextNonWhitespaceInValue,
            CapturingSectionName,
        }

        protected virtual char LineCommentChar { get; }

        /// <summary>
        /// Gets or sets the character that is used to separate the locale name from the base name in the names of locale-specific translation files.
        /// </summary>
        public static char LocaleSeparatorChar { get; set; } = '_';

        public override char GetLocaleSeparatorChar()
        {
            return LocaleSeparatorChar;
        }

        public override Translation? LoadTranslation(string translationText, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            string currentGroup = string.Empty;
            string key;
            string? value, escapedValue, reference;

            Translation result = new(locale: null);
            TranslationEntry entry;

            if (string.IsNullOrEmpty(translationText))
                return null;

            Dictionary<string, (string? escaped, string unescaped)?> lines = LoadAsDictionary(translationText);

            foreach (var line in lines)
            {
                if (line.Value is null)
                {
                    currentGroup = line.Key.Trim();
                }
                else
                {
                    if (currentGroup.Length == 0)
                        key = line.Key.Trim();
                    else
                        key = currentGroup + "." + line.Key.Trim();

                    value = line.Value.Value.unescaped;
                    escapedValue = line.Value.Value.escaped;

                    if (IsReference(value))
                    {
                        reference = value.Substring(1).Trim();
                        value = null;
                    }
                    else
                    {
                        reference = null;
                    }

                    entry = new TranslationEntry(key, value, escapedValue, reference);

                    if (reference is null)
                    {
                        if (escapedValue is not null)
                            entry.ContainsPlaceholders = IsTemplatedText(escapedValue, textProcessingMode); // an 'escaped' value is present only when it was explicitly returned by the TOML parser to indicate that the text is escaped and must be handled as such
                        else
                        if (value is not null)
                            entry.ContainsPlaceholders = IsTemplatedText(value, textProcessingMode);
                    }

                    result.Add(key.ToUpperInvariant(), entry);
                }
            }

            return result;
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly)
        {
            if (string.IsNullOrEmpty(fileContent))
                return null;

            Dictionary<string, (string? escaped, string unescaped)?> lines = LoadAsDictionary(fileContent);

            (string? escaped, string unescaped)? valueTuple;

            lines.TryGetValue(TranslationConfiguration.KEY_DEFAULT_FILE, out valueTuple);
            string? defaultFile = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_DEFAULT_LOCALE, out valueTuple);
            string? defaultLocale = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_GENERATED_NAMESPACE, out valueTuple);
            string? generatedNamespace = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_GENERATED_CLASS, out valueTuple);
            string? generatedClassName = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_DELAYED_UNITS_CREATION, out valueTuple);
            string? delayedUnitCreationStr = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_ONLY_DECLARE_KEYS, out valueTuple);
            string? onlyDeclareKeysStr = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_TEXT_PROCESSING_MODE, out valueTuple);
            string? textProcessingModeStr = valueTuple?.unescaped?.Trim();

            TextFormat textProcessingMode = DecodeTextProcessingMode(textProcessingModeStr) ?? GetTextProcessingMode();

            TranslationConfiguration result = new TranslationConfiguration(assembly, defaultFile ?? string.Empty, generatedNamespace, generatedClassName, defaultLocale, textProcessingMode, "true".Equals(delayedUnitCreationStr, StringComparison.OrdinalIgnoreCase), "true".Equals(onlyDeclareKeysStr, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(defaultFile))
                return result;

            string currentGroup = string.Empty;

            // If the configuration contains the Translations section, parse it
            foreach (var key in lines.Keys)
            {
                if (lines[key] is null)
                {
                    currentGroup = key;
                    continue;
                }

                if (!currentGroup.Equals(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, StringComparison.OrdinalIgnoreCase))
                    continue;

                string? value = lines[key]?.unescaped.Trim();

                if (value is null)
                    continue;

                string? lang = key;

                if (lang.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal))
                    lang = TranslationConfiguration.KEY_TRANSLATION_OTHER;
                else
                if (!lang.Equals(TranslationConfiguration.KEY_TRANSLATION_OTHER, StringComparison.OrdinalIgnoreCase))
                    lang = lang.ToUpperInvariant();

                if (result.Translations.ContainsKey(lang))
                    throw new GenericParserException($"Duplicate translation reference '{lang}' specified in the list of translations");
                result.Translations.Add(lang, value);
            }

            return result;
        }

        /// <summary>
        /// This method loads the file as a list of key-value pairs.
        /// If sections are detected, they are added as key with values set to null.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <returns>The list of key-value pairs.</returns>
        internal Dictionary<string, (string? escaped, string unescaped)?> LoadAsDictionary(string content)
        {
            Dictionary<string, (string? escaped, string unescaped)?> result = new Dictionary<string, (string? escaped, string unescaped)?>(StringComparer.OrdinalIgnoreCase);

            char lineCommentChar = LineCommentChar;

            var currentLineNumber = 1;
            var currentColumnNumber = 1;

            int offset = 0;

            int lineStartPos = 0;
            int keyStartPos = -1;
            //int keyEndPos = -1;
            int valueStartPos = -1;

            string capturedKey = string.Empty;
            StringBuilder? valueBuilder = null;

            TextParserState state = TextParserState.LookingForLineStart;

            // walk through the lines one by one
            while (offset <= content.Length)
            {
                switch (state)
                {
                    case TextParserState.LookingForLineStart:

                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        if (content[offset] == '\n')
                        {
                            currentLineNumber++;
                            currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                            lineStartPos = offset + 1;
                        }
                        else
                        if (char.IsWhiteSpace(content[offset]))
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == lineCommentChar) // skip lines that start with a comment char
                        {
                            state = TextParserState.SkippingTillEOL;
                        }
                        else
                        if (content[offset] == '[')
                        {
                            state = TextParserState.CapturingSectionName;
                            keyStartPos = offset + 1;
                        }
                        else
                        if (IsStartOfKey(content, offset))
                        {
                            state = TextParserState.CapturingKey;
                            keyStartPos = offset;
                        }
                        else
                        {
                            throw new TextParseException($"Unexpected character '{content[offset]}' at {currentLineNumber}:{currentColumnNumber}", keyStartPos, keyStartPos, currentLineNumber, currentColumnNumber);
                        }

                        break;

                    case TextParserState.SkippingTillEOL:
                    case TextParserState.SkippingWSOnlyTillEOL:

                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        if (content[offset] == '\n') // detect EOL
                        {
                            state = TextParserState.LookingForLineStart;
                            currentLineNumber++;
                            currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                            lineStartPos = offset + 1;
                        }
                        else
                        if (state == TextParserState.SkippingWSOnlyTillEOL && !char.IsWhiteSpace(content[offset]))
                        {
                            throw new TextParseException($"Unexpected character '{content[offset]}' at {currentLineNumber}:{currentColumnNumber}", keyStartPos, keyStartPos, currentLineNumber, currentColumnNumber);
                        }

                        break;

                    case TextParserState.CapturingKey:

                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        bool? keyEndCheck = IsEndOfKey(content, offset, out int posAfterKey);
                        if (keyEndCheck is null)
                        {
                            throw new TextParseException($"Unexpected end of line at {currentLineNumber}:{currentColumnNumber}", keyStartPos, offset, currentLineNumber, currentColumnNumber);
                        }
                        else
                        if (keyEndCheck == true) // found end of key
                        {
                            capturedKey = UnwrapKey(content.Substring(keyStartPos, posAfterKey - keyStartPos));

                            if (result.ContainsKey(capturedKey))
                                throw new TextParseException($"Duplicate key `{capturedKey}`", keyStartPos, posAfterKey, currentLineNumber, keyStartPos - lineStartPos + 1);

                            if (IsSeparatorChar(content[offset])) // now, we can look for a value
                                state = TextParserState.LookingForValueStart;
                            else
                                state = TextParserState.LookingForSeparator;

                            //keyEndPos = offset;

                            break;
                        }
                        else
                        if (IsValidKeyChar(content, offset)) // acceptable characters
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == '\n') // detect EOL as it is not permitted
                        {
                            throw new TextParseException($"Line {currentLineNumber} does not contain a key/value pair", lineStartPos, offset, currentLineNumber, currentColumnNumber);
                        }
                        else
                        {
                            throw new TextParseException($"Character '{content[offset]}' at {currentLineNumber}:{currentColumnNumber} is not valid for a key name", lineStartPos, offset, currentLineNumber, currentColumnNumber);
                        }

                        break;

                    case TextParserState.LookingForSeparator:

                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        if (content[offset] == '\r' || content[offset] == '\n') // detect EOL as it is not permitted
                        {
                            throw new TextParseException($"Line {currentLineNumber} does not contain a key/value pair", lineStartPos, offset, currentLineNumber, currentColumnNumber);
                        }
                        else
                        if (char.IsWhiteSpace(content[offset]))
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (IsSeparatorChar(content[offset]))
                        {
                            state = TextParserState.LookingForValueStart;
                        }
                        else
                        {
                            throw new TextParseException($"Key-value separator expected, character '${content[offset]}' found instead at {currentLineNumber}:{currentColumnNumber}", lineStartPos, offset, currentLineNumber, currentColumnNumber);
                        }

                        break;

                    case TextParserState.LookingForValueStart:

                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        if (content[offset] == '\n') // detect EOL - it is the end of an empty value
                        {
                            if (!AcceptUnquotedEmptyValues())
                                throw new TextParseException($"Unexpected end of line at {currentLineNumber}:{currentColumnNumber}", valueStartPos, offset, currentLineNumber, currentColumnNumber);

                            // Add an empty value to the resulting dictionary
                            if (!string.IsNullOrEmpty(capturedKey))
                            {
                                result[capturedKey] = (null, string.Empty);
                            }

                            // ... and switch to the new line
                            state = TextParserState.LookingForLineStart;
                            currentLineNumber++;
                            currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                            lineStartPos = offset + 1;
                        }
                        else
                        if (char.IsWhiteSpace(content[offset]))
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (IsStartOfValue(content, offset, out int posAfterStart))
                        {
                            state = TextParserState.CapturingValue;
                            valueBuilder = new StringBuilder(1024);
                            if (posAfterStart > offset)
                            {
                                valueBuilder.Append(content, offset, posAfterStart - offset);
                                currentColumnNumber += posAfterStart - offset - 1;
                                offset = posAfterStart - 1; // it will be incremented after the switch and thus get to the right value
                            }

                            valueStartPos = offset;
                        }

                        break;

                    case TextParserState.LookingForNextNonWhitespaceInValue:
                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        if (!char.IsWhiteSpace(content[offset]))
                        {
                            offset--;
                            currentColumnNumber--;
                            state = TextParserState.CapturingValue;
                        }

                        break;

                    case TextParserState.CapturingValue:
                        if (valueBuilder is null)
                            throw new TextParseException("Internal exception occurred when parsing the translation. Please report this bug to the developers.", valueStartPos, offset, currentLineNumber, currentColumnNumber);

                        if (offset < content.Length)
                        {
                            if (IsEscapedEOLInValue(content, offset))
                            {
                                state = TextParserState.LookingForNextNonWhitespaceInValue;
                                break;
                            }
                        }

                        bool? valueEndCheck = IsEndOfValue(content, offset, out int posAfterValue);

                        if (valueEndCheck is null)
                        {
                            throw new TextParseException($"Unexpected end of line at {currentLineNumber}:{currentColumnNumber}", valueStartPos, offset, currentLineNumber, currentColumnNumber);
                        }
                        else
                        if (valueEndCheck == false)
                        {
                            // we skip CR (\r) unless it is not followed by LF (\n)
                            if (content[offset] != '\r' ||
                                (content[offset] == '\r' &&
                                 (offset == content.Length - 1 || content[offset + 1] != '\n')))
                                valueBuilder.Append(content[offset]);
                        }
                        else
                        if (valueEndCheck == true)
                        {
                            // Add a value to the resulting dictionary
                            if (!string.IsNullOrEmpty(capturedKey))
                            {
                                if (posAfterValue > offset)
                                    valueBuilder.Append(content, offset, posAfterValue - offset);
                                result[capturedKey] = UnwrapValue(valueBuilder.ToString());
                                valueBuilder = null;
                            }

                            if (offset < content.Length && content[offset] != '\n')
                            {
                                currentColumnNumber += posAfterValue - offset;
                                offset = posAfterValue;

                                if (offset < content.Length - 1 && content[offset] == '\r' && content[offset + 1] == '\n')
                                {
                                    offset++;
                                    currentColumnNumber++;
                                }
                            }

                            if (offset < content.Length)
                            {
                                if (content[offset] == '\n')
                                {
                                    // ... and switch to the new line
                                    state = TextParserState.LookingForLineStart;
                                    currentLineNumber++;
                                    currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                                    lineStartPos = offset + 1;
                                }
                                else
                                {
                                    offset--; // we need to decrement it so that after the switch, it gets incremented back and points at the first character after the end of value
                                    currentColumnNumber--;
                                    state = TextParserState.SkippingWSOnlyTillEOL;
                                }
                            }
                            else
                            {
                                state = TextParserState.LookingForLineStart; // makes no sense at the end, but the end of the method checks the current state for consistency (no pending opened keys or values)
                            }
                        }

                        break;

                    case TextParserState.CapturingSectionName:

                        // this call safely breaks the loop
                        if (offset == content.Length)
                            break;

                        if (IsValidSectionNameChar(content, offset)) // acceptable characters
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == ']') // found the end of a section name
                        {
                            // check for validity of the name
                            if (keyStartPos == offset)
                                throw new TextParseException($"A section name may not be empty at {currentLineNumber}:{keyStartPos - 1}", keyStartPos, offset, currentLineNumber, offset - lineStartPos + 1);

                            if (content[keyStartPos] == '_' || char.IsLetter(content[keyStartPos]))
                            {
                                capturedKey = content.Substring(keyStartPos, offset - keyStartPos);
                            }
                            else
                            {
                                throw new TextParseException($"A section name may not start with '{content[keyStartPos]}' at {currentLineNumber}:{keyStartPos - lineStartPos + 1}", keyStartPos, offset, currentLineNumber, offset - lineStartPos + 1);
                            }

                            // Add a null value to the resulting dictionary
                            if (!string.IsNullOrEmpty(capturedKey))
                            {
                                /*
                                // We use '^' (caret) to distinguish between section names and keys - neither of them may start with a caret anyway
                                if (result.ContainsKey('^' + capturedKey))
                                    throw new TextParseException($"Duplicate section name `{capturedKey}`", keyStartPos, offset, currentLineNumber, keyStartPos - lineStartPos + 1);
                                result['^' + capturedKey] = null;
                                */
                                if (result.ContainsKey(capturedKey))
                                    throw new TextParseException($"Duplicate section name `{capturedKey}`", keyStartPos, offset, currentLineNumber, keyStartPos - lineStartPos + 1);
                                result[capturedKey] = null;
                            }
                            else
                            {
                                throw new TextParseException($"Empty section name", keyStartPos, offset, currentLineNumber, offset - lineStartPos + 1);
                            }

                            state = TextParserState.SkippingTillEOL;

                            break;
                        }
                        else
                        if (content[offset] == '\n') // detect EOL as it is not permitted
                        {
                            throw new TextParseException($"Unexpected end of line at {currentLineNumber}:{currentColumnNumber}", keyStartPos, offset, currentLineNumber, currentColumnNumber);
                        }
                        else
                        {
                            throw new TextParseException($"Character '{content[offset]}' at {currentLineNumber}:{currentColumnNumber} is not valid for a section name", offset, offset, currentLineNumber, currentColumnNumber);
                        }

                        break;
                }

                offset++;
                currentColumnNumber++;
            }

            if (state != TextParserState.LookingForLineStart && state != TextParserState.SkippingWSOnlyTillEOL && state != TextParserState.SkippingTillEOL)
                throw new TextParseException($"Unexpected end of file at {currentLineNumber}:{currentColumnNumber}", offset, offset, currentLineNumber, currentColumnNumber);

            return result;
        }

        protected abstract bool IsStartOfKey(string content, int offset);

        protected abstract bool? IsEndOfKey(string content, int offset, out int newPosition);

        /// <summary>
        /// Strips format-specific markers that denote the beginning and the end of a value.
        /// </summary>
        /// <param name="value">The value to strip.</param>
        /// <returns>The text inside the markers.</returns>
        protected abstract string UnwrapKey(string value);

        protected abstract bool IsSeparatorChar(char candidate);

        protected abstract bool IsStartOfValue(string content, int offset, out int posAfterStart);

        /// <summary>
        /// Detects if the end of a value has been reached.
        /// </summary>
        /// <param name="content">content to check.</param>
        /// <param name="offset">current position within the content.</param>
        /// <param name="newPosition">upon return is set to a position next to the end-of-value marker (indicates to which value the pointer must be set).</param>
        /// <returns><see langword="true"/> if the end of the value has been reached, <see langword="false"/> if the end has not been reached, and <see langword="null"/> if the end of line was encountered and a single-line value was not closed.</returns>
        /// <exception cref="TextParseException">thrown when an error occurs.</exception>
        protected abstract bool? IsEndOfValue(string content, int offset, out int newPosition);

        /// <summary>
        /// Strips format-specific markers that denote the beginning and the end of a value.
        /// </summary>
        /// <param name="value">The value to strip.</param>
        /// <returns>The text inside the markers.</returns>
        protected abstract (string? escaped, string unescaped) UnwrapValue(string value);

        protected internal virtual bool IsValidKeyChar(string content, int offset)
        {
#pragma warning disable CA1062
            char ch = content[offset];
            return char.IsLetterOrDigit(ch) ||
                   (ch == '_') ||
                   (ch == '-') ||
                   (ch == '.');
#pragma warning restore CA1062
        }

        protected virtual bool IsValidSectionNameChar(string content, int offset) => IsValidKeyChar(content, offset);

        protected internal virtual bool IsEscapedEOLInValue(string content, int offset) => false;

        protected abstract bool AcceptUnquotedEmptyValues();

        protected override TranslationTree? InternalLoadTranslationStructure(string content, TextFormat? textProcessingMode)
        {
            string currentGroup = string.Empty;

            string key, value;

            TranslationTree result = new();

            if (string.IsNullOrEmpty(content))
                return result;

            TranslationTreeNode? node = result.RootNode;
            TranslationTreeLeaf leaf;

            Dictionary<string, (string? escaped, string unescaped)?> lines = LoadAsDictionary(content);

            foreach (var line in lines)
            {
                if (line.Value is null /*&& line.Key[0] == '^'*/)
                {
                    currentGroup = line.Key; // .Substring(1);
                    node = result.MakeNode(currentGroup);
                    if (node is null) // this should not normally happen - MakeNode returns null when the name is invalid, and we control names during parsing.
                        continue;
                }
                else
                {
                    key = line.Key;
                    if (line.Value.Value.escaped is not null)
                    {
                        // an 'escaped' value is present only when it was explicitly returned by the TOML parser to indicate that the text is escaped and must be handled as such
                        value = line.Value.Value.escaped;
                    }
                    else
                    {
                        value = line.Value.Value.unescaped;
                    }

                    leaf = new TranslationTreeLeaf(key, !IsReference(value) && IsTemplatedText(value, textProcessingMode));
                    node!.Keys.Add(key, leaf);
                }
            }

            return result;
        }
    }
}
