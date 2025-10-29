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
        /// Contexts are specified in ARB files and are supported and preservved by Tlumach converters.
        /// </summary>
        public string? Context { get; internal set; }

        public Translation(string? locale, string? context)
        {
            Locale = locale;
            Context = context;
        }

        public Translation SetOrigin(Assembly? originalAssembly, string? originalFile)
        {
            OriginalAssembly = originalAssembly;
            OriginalFile = originalFile;
            return this;
        }
    }
}
