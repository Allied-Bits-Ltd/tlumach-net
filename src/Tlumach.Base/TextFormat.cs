// <copyright file="TemplateStringEscaping.cs" company="Allied Bits Ltd.">
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
    /// <summary>
    /// Specifies how the library should deal with translation entries which may support placeholders or reserved characters.
    /// </summary>
    public enum TextFormat
    {
        /// <summary>
        /// No decoding of characters takes place, and placeholders are not detected.
        /// </summary>
        None,

        /// <summary>
        /// Strings may contain any characters, but "unsafe" characters should be prepended with a backslash ("\"), and encoded characters are supported.
        /// This is the format used in C++, JSON strings, and TOML basic strings. Placeholders are not supported in this format.
        /// </summary>
        BackslashEscaping,

        /// <summary>
        /// Curly brackets are used to denote placeholders according to the rules defined for Arb files (those used in Dart language and Flutter framework) including the "use-escaping: true" setting.
        /// </summary>
        Arb,

        /// <summary>
        /// Curly brackets are used to denote placeholders according to the rules defined for Arb files (those used in Dart language and Flutter framework).
        /// Unlike <seealso cref="Arb"/> mode, quote characters (') are not considered as escape symbols.
        /// </summary>
        ArbNoEscaping,

        /// <summary>
        /// In addition to <seealso cref="BackslashEscaping"/> processing, curly brackets are used to denote placeholders according to the .NET rules used by String.Format().
        /// </summary>
        DotNet,
    }
}
