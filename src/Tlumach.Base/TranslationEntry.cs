// <copyright file="TranslationEntry.cs" company="Allied Bits Ltd.">
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

using System.Collections.Specialized;
using System.Globalization;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

    /// <summary>
    /// <para>
    /// Represents an entry in the translation file.
    /// An entry may have a value (some text in a specific language) or be a reference to an external file with a translation.
    /// </para>
    /// <para>
    /// Instances of this class are always owned by a dictionary which keeps the keys,
    /// and that dictionary is transferred in a way that specifies the locale.
    /// For this reason, TranslationEntry does not hold a key or locale ID.
    /// </para>
    /// </summary>
    public class TranslationEntry
    {
        private bool _locked;
        private string? _text;
        private string? _escapedText;
        private string? _reference;

        public static TranslationEntry Empty { get; }

        /// <summary>
        /// Gets the original key of the entry.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets or sets a localized text. This text has been un-escaped (if needed) during loading from the translation file.
        /// </summary>
        public string? Text
        {
            get => _text;
            set
            {
                CheckNotLocked();
                _text = value;
            }
        }

        /// <summary>
        /// Gets or sets a localized text that has not been un-escaped. This text is used when evaluating if the localized text contains templates and when processing those templates.
        /// </summary>
        public string? EscapedText
        {
            get => _escapedText;
            set
            {
                CheckNotLocked();
                _escapedText = value;
            }
        }

        /// <summary>
        /// Indicates that the text is a template. When it is, use the <see cref="ProcessTemplatedValue"/> method to format the template.
        /// </summary>
        public bool IsTemplated { get; set; }

        /// <summary>
        /// Gets or sets an optional reference to an external file with the translation value.
        /// <para>A reference is set by the parser when the text starts with '@' (at) and the <see cref="BaseParser.RecognizeFileRefs"/> property is <see langword="true"/>.</para>
        /// </summary>
        public string? Reference
        {
            get => _reference;
            set
            {
                CheckNotLocked();
                _reference = value;
            }
        }

        /// <summary>
        /// Gets or sets an optional target of the entry.
        /// <para>Targets are defined in the ARB specification as attributes of HTML elements to which the content should be assigned.</para>
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Gets or sets an optional type of the entry.
        /// <para>Describes the type of resource. Possible values are "text", "image", "css".</para>
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the context.
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        ///  Gets or sets an optional original text that was translated.
        /// </summary>
        public string? SourceText { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the entry.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets an optional reference to a screenshot of the entry.
        /// </summary>
        public string? Screen { get; set; }

        /// <summary>
        /// Gets or sets an optional reference to a video of the entry.
        /// </summary>
        public string? Video { get; set; }

        /// <summary>
        /// Gets an optional collection of placeholder descriptions.
        /// </summary>
        public List<Placeholder>? Placeholders { get; private set; }

        static TranslationEntry()
        {
            Empty = new TranslationEntry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationEntry"/> class.
        /// </summary>
        public TranslationEntry()
        {
            // Default constructor does nothing
            Key = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationEntry"/> class.
        /// </summary>
        /// <param name="key">The key to which the translation entry corresponds.</param>
        /// <param name="text">An optional localized text of the translation entry that has been un-escaped if necessary.</param>
        /// <param name="escapedText">An optional localized text of the translation entry that has not been un-escaped.</param>
        /// <param name="reference">An optional reference to an external file with the text.</param>
        public TranslationEntry(string key, string? text, string? escapedText = null, string? reference = null)
        {
            Key = key;
            Text = text;
            EscapedText = escapedText;
            Reference = reference;
        }

        public void AddPlaceholder(Placeholder placeholder)
        {
            Placeholders ??= [];
            Placeholders.Add(placeholder);
        }

        public string ProcessTemplatedValue(CultureInfo culture, TextFormat textProcessingMode, params object?[] parameters)
        {
            if (textProcessingMode != TextFormat.DotNet && textProcessingMode != TextFormat.Arb && textProcessingMode != TextFormat.ArbNoEscaping)
            {
                return Text ?? string.Empty;
            }
            else
            if (textProcessingMode == TextFormat.DotNet) // with .NET, we simply use the .NET formatter
            {
                return string.IsNullOrEmpty(Text)
                    ? string.Empty
                    : string.Format(culture, Text, parameters);
            }
            else
            {
                // In the case of Arb format, we need to pick parameters by index
                return InternalProcessTemplatedValue(
                    (key, position) =>
                    {
                        if (position >= 0 && position < parameters.Length)
                        {
                            object? value = parameters[position];
                            return value is null ? "null" : value;
                        }
                        else
                        {
                            return null;
                        }
                    },
                    culture,
                    textProcessingMode);
            }
        }

        public string ProcessTemplatedValue(CultureInfo culture, TextFormat textProcessingMode, IDictionary<string, object?> parameters)
        {
            return InternalProcessTemplatedValue(
                (key, _) =>
                {
                    // This will cover the case of named parameters, and if the parameters are requested by index, the caller can provide numbers as string keys.
                    if (parameters.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        object? value = parameters[key];
                        return value is null ? "null" : value;
                    }

                    if (textProcessingMode == TextFormat.DotNet)
                    {
                        // Probably, the key starts with a number that is the index of a parameter (like this works in .NET format strings).
                        // Try this assumption, and if there is an index in the key, use the index as a key in the 'parameters' parameter.
                        Utils.GetLeadingNonNegativeNumber(key, out int charsUsed);
                        if (charsUsed > 0 && parameters.TryGetValue(key.Substring(0, charsUsed), out object? result))
                        {
                            object? value = result;
                            return value is null ? "null" : value;
                        }
                    }

                    return null;
                },
                culture,
                textProcessingMode);
        }

        public string ProcessTemplatedValue(CultureInfo culture, TextFormat textProcessingMode, OrderedDictionary parameters)
        {
            return InternalProcessTemplatedValue(
                (key, index) =>
                {
                    // This will cover the case of named parameters, and if the parameters are requested by index, the caller can provide numbers as string keys.
                    if (parameters.Contains(key))
                    {
                        object? value = parameters[key];
                        return value is null ? "null" : value;
                    }

                    if (textProcessingMode == TextFormat.DotNet)
                    {
                        // Probably, the key starts with a number that is the index of a parameter (like this works in .NET format strings).
                        // Try this assumption, and if there is an index in the key, use the index as a key in the 'parameters' parameter.
                        int idx = Utils.GetLeadingNonNegativeNumber(key, out var charsUsed);
                        if (idx >= 0 && idx < parameters.Count)
                        {
                            object? value = parameters[idx];
                            return value is null ? "null" : value;
                        }
                        else
                        if (charsUsed > 0)
                        {
                            key = key.Substring(0, charsUsed);
                            if (parameters.Contains(key))
                            {
                                object? value = parameters[key];
                                return value is null ? "null" : value;
                            }
                        }
                    }

                    if (index >= 0 && index < parameters.Count)
                    {
                        object? value = parameters[index];
                        return value is null ? "null" : value;
                    }

                    return null;
                },
                culture,
                textProcessingMode);
        }

        public string ProcessTemplatedValue(CultureInfo culture, TextFormat textProcessingMode, object parameters)
        {
            return InternalProcessTemplatedValue(
                (key, index) =>
                {
                    object? value = null;

                    if (Utils.TryGetPropertyValue(parameters, key, out value))
                        return value is null ? "null" : value;

                    if (index >= 0)
                    {
                        if (parameters is object[] objArr)
                        {
                            if (index < objArr.Length)
                            {
                                value = objArr[index];
                                return value is null ? "null" : value;
                            }
                        }

                        if (parameters is Array arr)
                        {
                            if (index < arr.Length)
                            {
                                value = arr.GetValue(index);
                                return value is null ? "null" : value;
                            }
                        }

                        value = parameters;
                        return value is null ? "null" : value;
                    }

                    return null;
                },
                culture,
                textProcessingMode);
        }

        public string InternalProcessTemplatedValue(Func<string, int, object?> getParamValueFunc, CultureInfo culture, TextFormat textProcessingMode = TextFormat.None)
        {
            // No text to process. Return the empty string.
            if (string.IsNullOrEmpty(EscapedText) && string.IsNullOrEmpty(Text))
                return string.Empty;

            // No escaping, no placeholders. Just return the text.
            if (textProcessingMode == TextFormat.None)
                return EscapedText ?? Text ?? string.Empty;

            // No placeholders. Just return the text, possibly un-escaping it.
            if (textProcessingMode == TextFormat.BackslashEscaping)
            {
                if (EscapedText is not null)
                    return Utils.UnescapeString(EscapedText);
                else
                    return Text ?? string.Empty;
            }

            // validate some parameters (better late than never)
            if (getParamValueFunc is null)
                throw new ArgumentNullException(nameof(getParamValueFunc));

            culture ??= CultureInfo.InvariantCulture;

            bool shouldUnescape;
            string inputText;

            if (!string.IsNullOrEmpty(EscapedText))
            {
                // no un-escaping is done in ArbNoEscaping mode
                shouldUnescape = textProcessingMode != TextFormat.ArbNoEscaping;
                inputText = EscapedText!;
            }
            else
            {
                shouldUnescape = false;
                inputText = Text!;
            }

            StringBuilder builder = new(inputText.Length);
            int charCode;
            char nextChar;
            int pointer = 0;
            bool inQuotes = false;
            int openBraceCount = 0;
            int placeholderIndex = -1;

            while (pointer < inputText.Length)
            {
                char c = inputText[pointer];
                if (shouldUnescape && (textProcessingMode == TextFormat.DotNet))
                {
                    if (c == Utils.C_BACKSLASH)
                    {
                        pointer++;
                        if (pointer < inputText.Length)
                        {
                            nextChar = inputText[pointer];
                            switch (nextChar)
                            {
                                case Utils.C_DOUBLE_QUOTE:
                                    builder.Append(Utils.C_DOUBLE_QUOTE);
                                    pointer++;
                                    continue;
                                case Utils.C_BACKSLASH:
                                    builder.Append(Utils.C_BACKSLASH);
                                    pointer++;
                                    continue;
                                case '/':
                                    builder.Append('/');
                                    pointer++;
                                    continue;
                                case 'b':
                                    builder.Append('\b');
                                    pointer++;
                                    continue;
                                case 'f':
                                    builder.Append('\f');
                                    pointer++;
                                    continue;
                                case 'n':
                                    builder.Append('\n');
                                    pointer++;
                                    continue;
                                case 'r':
                                    builder.Append('\r');
                                    pointer++;
                                    continue;
                                case 't':
                                    builder.Append('\t');
                                    pointer++;
                                    continue;
                                case 'u':
                                    if (pointer + 4 < inputText.Length)
                                    {
                                        string hex = inputText.Substring(pointer + 1, 4);
                                        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, culture, out charCode))
                                            builder.Append((char)charCode);
                                        else
                                            builder.Append(Utils.C_BACKSLASH).Append('u').Append(hex);

                                        pointer += 4;
                                        continue;
                                    }
                                    else
                                    {
                                        // Incomplete sequence
                                        builder.Append(Utils.C_BACKSLASH).Append('u');
                                        pointer++;
                                        continue;
                                    }

                                default:
                                    builder.Append(Utils.C_BACKSLASH).Append(nextChar);
                                    pointer++;
                                    continue;
                            }
                        }
                        else
                        {
                            throw new TemplateParserException("Incomplete escape sequence (hanging backslash '\\' detected) in the following text:\n" + inputText);
                        }
                    }
                }

                if (shouldUnescape && (textProcessingMode == TextFormat.Arb))
                {
                    if (c == Utils.C_SINGLE_QUOTE)
                    {
                        pointer++;
                        if (pointer < inputText.Length)
                            nextChar = inputText[pointer];
                        else
                            nextChar = '\0';

                        if (nextChar == Utils.C_SINGLE_QUOTE)
                        {
                            builder.Append(Utils.C_SINGLE_QUOTE);
                            pointer++;
                            continue;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                }

                // Finally, get to placeholders
                if (c == '{')
                {
                    pointer++;
                    if (pointer < inputText.Length)
                    {
                        nextChar = inputText[pointer];
                        if (textProcessingMode == TextFormat.DotNet && nextChar == '{')
                        {
                            builder.Append(c);
                            pointer++;
                            continue;
                        }

                        int startOfParam = pointer;

                        openBraceCount = 1;

                        // grab everything until all braces are closed
                        while (pointer < inputText.Length && openBraceCount > 0)
                        {
                            if (inputText[pointer] == '}')
                            {
                                openBraceCount--;
                            }
                            else
                            if (inputText[pointer] == '{')
                            {
                                openBraceCount++;
                                if (textProcessingMode == TextFormat.DotNet)
                                    throw new TemplateParserException("An inlaid open curly brace ('{') detected in the following text:\n" + inputText);
                            }

                            pointer++;
                        }

                        // We have captured all of the placeholder
                        if (openBraceCount == 0)
                        {
                            placeholderIndex++; // we started with -1 in order to make index == 0 for the first encountered placeholder

                            // take the placeholder itself
                            string placeholderContent = inputText.Substring(startOfParam, pointer - startOfParam - 1);

                            try
                            {
                                // obtain the value to place instead of the placeholder
                                string placeholderValue = GetPlaceholderValue(placeholderContent, placeholderIndex, getParamValueFunc, textProcessingMode, culture);

                                // add the value to the string builder
                                builder.Append(placeholderValue);
                            }
                            catch (TemplateParserException ex)
                            {
                                throw new TemplateParserException(ex.Message + " in the following text:\n" + inputText, ex.InnerException is not null ? ex.InnerException : ex);
                            }

                            // at this point, pointer points beyond the last curly brace, so we can continue looking for the next ones
                            continue;
                        }

                        // have we reached the end of the text?
                        if (pointer == inputText.Length)
                            break;
                    }
                    else
                    if (inQuotes) // Quotes are currently used only in Arb
                    {
                        throw new TemplateParserException("A hanging open quote (') detected in the following text:\n" + inputText);
                    }
                    else
                    {
                        throw new TemplateParserException("Incomplete placeholder (hanging opening curly brace ('{') detected) in the following text:\n" + inputText);
                    }
                }

                builder.Append(c);
                pointer++;
            }

            if (inQuotes)
                throw new TemplateParserException("A hanging open quote (') detected in the following text:\n" + inputText);

            if (openBraceCount > 0)
                throw new GenericParserException("Unclosed opening curly brace ('{') in the following text:\n" + inputText);

            return builder.ToString();
        }

        private string GetPlaceholderValue(string placeholderContent, int placeholderIndex, Func<string, int, object?> getParamValueFunc, TextFormat textProcessingMode, CultureInfo culture)
        {
            string placeholderName;
            string tail;
            int placeholderPositional = -1; // a numeric representation of a positional placeholder in Arb

            int pointer = 0;
            char c;

            object? value;

            // if the placeholder is empty, we request the value by index. Since we have no formatting specifiers, we use default conversion to string.
            if (placeholderContent.Length == 0)
            {
                value = getParamValueFunc(string.Empty, placeholderIndex);
                if (value is null)
                    return string.Empty;

                return string.Format(culture, "{0}", value);
            }

            // Arb requires that the placeholder name is a valid Dart identifier, and identifiers may start with a letter or an underscore
            if (textProcessingMode == TextFormat.Arb || textProcessingMode == TextFormat.ArbNoEscaping)
            {
                c = placeholderContent[0];

                // arb uses '@' as the first character after '{' to indicate a non-translatable value, which emitted as is (without '@', of course)
                if (c == '@')
                    return placeholderContent.Substring(1);

                if (c != '_' && !char.IsLetter(c))
                    throw new TemplateParserException($"Invalid placeholder name in placeholder '{placeholderContent}'");
            }

            bool isPositional = true;

            // find the name of the placeholder
            while (pointer < placeholderContent.Length)
            {
                c = placeholderContent[pointer];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    break;
                if (isPositional && !char.IsDigit(c))
                    isPositional = false;
                pointer++;
            }

            // if we reached the end of the content, then the whole content is a name of the placeholder
            if (pointer == placeholderContent.Length)
            {
                placeholderName = placeholderContent;
                tail = string.Empty;
            }
            else
            {
                placeholderName = placeholderContent.Substring(0, pointer);
                tail = placeholderContent.Substring(pointer);
            }

            // if the placeholder is positional, i.e., is a number, we obtain the position
            if (isPositional && (!int.TryParse(placeholderName, out placeholderPositional)))
            {
                placeholderPositional = -1;
                isPositional = false;
            }

            // process a placeholder according to .NET rules, if requested
            if (textProcessingMode == TextFormat.DotNet)
            {
                // obtain the value for the placeholder
                value = getParamValueFunc(placeholderName, placeholderPositional >= 0 ? placeholderPositional : placeholderIndex);

                // a null value indicates that a placeholder value was not found. In this case, return an empty string. If one needs to return "null", they should include a placeholder value that is a string "null".
                if (value is null)
                    return string.Empty;

                if (tail.Length > 0)
                    return string.Format(culture, "{0:" + tail + '}', value);
                else
                    return string.Format(culture, "{0}", value);
            }

            // process a placeholder according to Arb rules, if requested
            if (textProcessingMode == TextFormat.Arb || textProcessingMode == TextFormat.ArbNoEscaping)
            {
                Placeholder? placeholder = null;

                string? placeholderType = null;

                // Here, there's some quite confusing logic happening.
                // A placeholder is replaced when it is declared in a placeholder attribute (see Arb description for details), or when a replacement value is provided in the parameters of the formatting function.

                if (isPositional)
                {
                    // obtain the value for the placeholder
                    value = getParamValueFunc(placeholderName, placeholderPositional);

                    // a null value indicates that a placeholder value was not found. In this case, return an empty string. If one needs to return "null", they should include a placeholder value that is a string "null".
                    if (value is null)
                        return placeholderContent;
                }
                else
                if (Placeholders is not null)
                {
                    // According to Arb rules, if placeholders are specified, then each placeholder used in the text must have an entry in the placeholder list.
                    // If it does not, the literal value of the placeholder is returned (i.e., the placeholder is considered to not be a placeholder).
                    placeholder = Placeholders.FirstOrDefault(p => p.Name.Equals(placeholderName, StringComparison.OrdinalIgnoreCase));
                    if (placeholder is null)
                        return string.Concat("{", placeholderContent, "}");

                    value = getParamValueFunc(placeholderName, placeholderIndex);
                    if (value is null)
                        return placeholderContent;

                    if (placeholder.Type is not null)
                    {
                        placeholderType = placeholder.Type;

                        if (placeholderType.Equals("num", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Utils.IsBoxedNumber(value))
                                // throw new TemplateParserException($"The placeholder '{placeholderName}' was declared as numeric, but a value of type {value.GetType().Name} was provided for use by the formatter");
                                placeholderType = "String";
                        }
                        else
                        if (placeholderType.Equals("int", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!Utils.IsBoxedIntegerNumber(value))
                                //throw new TemplateParserException($"The placeholder '{placeholderName}' was declared as integer, but a value of type {value.GetType().Name} was provided for use by the formatter");
                                placeholderType = "String";
                        }
                        else
                        if (placeholderType.Equals("DateTime", StringComparison.OrdinalIgnoreCase))
                        {
#if NET9_0_OR_GREATER
                            if (value is not DateTime && (value is not DateOnly) && (value is not TimeOnly))
                            {
#else
                            if (value is not DateTime)
                            {
#endif
                                //throw new TemplateParserException($"The placeholder '{placeholderName}' was declared as DateTime, but a value of type {value.GetType().Name} was provided for use by the formatter");
                                placeholderType = "String";
                            }
                        }
                    }
                }
                else
                {
                    value = getParamValueFunc(placeholderName, placeholderIndex);
                    if (value is null)
                        return placeholderContent;
                }

                try
                {
                    if (placeholderType is null)
                    {
                        return Utils.FormatArbUnknownPlaceholder(value, getParamValueFunc, tail, culture);
                    }
                    else
                    if (placeholderType.Equals("num", StringComparison.OrdinalIgnoreCase) || placeholderType.Equals("int", StringComparison.OrdinalIgnoreCase))
                    {
                        // format a number
                        if (!string.IsNullOrEmpty(placeholder!.Format))
                            return Utils.FormatArbNumber(value, getParamValueFunc, placeholder, tail, culture);
                    }
                    else
                    if (placeholderType.Equals("DateTime", StringComparison.OrdinalIgnoreCase))
                    {
                        // format a number
                        return Utils.FormatArbDateTime(value, placeholder!, culture);
                    }
                    else
                    {
                        // catch-all
                        return Utils.FormatArbString(value, getParamValueFunc, tail, culture);
                    }
                }
                catch (TemplateParserException ex)
                {
                    throw new TemplateParserException($"{ex.Message} in placeholder '{placeholderName}'", ex);
                }
            }

            return string.Empty;
        }

        #region Internal use

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void Lock()
        {
            _locked = true;
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void Unlock()
        {
            _locked = false;
        }

        private void CheckNotLocked()
        {
            if (_locked)
            {
                throw new InvalidOperationException("A translation entry should not be modified by event handlers. When handling an event, create a new entry or set Text or EscapedText.");
            }
        }
        #endregion
    }
}
