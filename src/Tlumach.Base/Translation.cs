// <copyright file="Translation.cs" company="Allied Bits Ltd.">
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

using System.Reflection;

namespace Tlumach.Base
{
    /// <summary>
    /// Contains translation entries that belong to one locale.
    /// Each entry is identified by the unique key, which is composed of the key name and the names of all its parent group names joined by dot ".", e.g., "group.subgroup.key".
    /// Key names are stored without case conversion but are searched for in a case-insensitive manner.
    /// </summary>
    public class Translation : Dictionary<string, TranslationEntry>
    {
        /// <summary>
        /// Optionally contains a reference to the assembly, from which the translation was loaded.
        /// </summary>
        public Assembly? OriginalAssembly { get; internal set; }

        /// <summary>
        /// Optionally contains the path to the original file, from which the translation was loaded.
        /// </summary>
        public string? OriginalFile { get; internal set; }

        /// <summary>
        /// Contains the locale of the file, from which the translation was loaded, if this locale was specified in the file.
        /// </summary>
        public string? Locale { get; internal set; }

        /// <summary>
        /// Contains the context of the file, from which the translation was loaded, if this context was specified in the file.
        /// Contexts are specified in ARB files and are supported and preserved by Tlumach converters.
        /// </summary>
        public string? Context { get; internal set; }

        /// <summary>
        /// May contain the value of the `@@last_modified` key of an ARB file.
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// May contain the value of the `@@author` key of an ARB file.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets the container that stores custom properties of an ARB file.
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; } = [];

        /// <summary>
        /// Gets or sets the indicator that the translation belongs to the "basic" culture. Basic cultures are the ones to which neutral cultures resolve. E.g.: "de-DE" is the basic culture for "de-AT" because "de" resolves to specific culture "de-DE".
        /// </summary>
        public bool IsBasicCulture { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Translation"/> class.
        /// </summary>
        /// <param name="locale">An optional locale if one was specified in the translation file.</param>
        /// <param name="context">An optional value of the Context property if one was specified in the translation file.</param>
        public Translation(string? locale, string? context = null)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            Locale = locale;
            Context = context;
        }

        /// <summary>
        /// Sets the source of the translation.
        /// </summary>
        /// <param name="originalAssembly">may specify an assembly if the translation was loaded from assembly resources.</param>
        /// <param name="originalFile">may specify the file if the translation was loaded from a file (and not obtained via the event).</param>
        /// <returns>The object, for which the method was called.</returns>
        public Translation SetOrigin(Assembly? originalAssembly, string? originalFile)
        {
            OriginalAssembly = originalAssembly;
            OriginalFile = originalFile;
            return this;
        }
    }
}
