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

namespace Tlumach.Base
{

    public class Placeholder
    {
    }

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
        /// <summary>
        /// The localized text.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// An optional reference to an external file with the translation value.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// An optional type of the entry.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// An optional description of the context.
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        ///  An optional original text that was translated.
        /// </summary>
        public string? SourceText { get; set; }

        /// <summary>
        /// An optional description of the entry
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// An optional collection of placeholder descriptions.
        /// </summary>
        public Placeholder[]? Placeholders { get; set; }

        public TranslationEntry()
        {
            // Default constructor does nothing
        }

        public TranslationEntry(string? text)
        {
            Text = text;
        }
    }
}
