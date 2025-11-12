// <copyright file="TableTextParser.cs" company="Allied Bits Ltd.">
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
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Tlumach.Base
{
    /// <summary>
    ///  The base class for CSV and TSV parsers.
    /// </summary>
    public abstract class TableTextParser : BaseFileParser
    {
        public override Translation? LoadTranslation(string translationText)
        {
            throw new NotImplementedException();
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly)
        {
            // table parsers don't have own configuration format but use simple INI format supported by IniParser
            throw new NotImplementedException();
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
        {
            throw new NotImplementedException();
        }

        /*
        /// <summary>
        /// This method loads the file as a list of key-value pairs.
        /// If sections are detected, they are added as key with values set to null.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <param name="storeValues">specifies whether the actual values should be added. If the value is <see langword="false"/>, empty values added to the list.</param>
        /// <returns>The list of key-value pairs</returns>
        internal Dictionary<string, (string? escaped, string unescaped)?> LoadAsDictionary(string content, bool storeValues)
        {
            Dictionary<string, (string? escaped, string unescaped)?> result = new Dictionary<string, (string? escaped, string unescaped)?>(StringComparer.OrdinalIgnoreCase);

        }*/
    }
}
