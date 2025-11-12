// <copyright file="FileFormats.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

namespace Tlumach.Base
{
    public static class FileFormats
    {
        private static readonly Dictionary<string, Func<BaseFileParser>> _parserFactories = [];
        private static readonly Dictionary<string, Func<BaseFileParser>> _configParserFactories = [];

        /// <summary>
        /// Returns a registered parser of configuration files with the given extension.
        /// </summary>
        /// <param name="extension">The extension for which the parser is needed.</param>
        /// <returns>An instance of the found parser or <see langword="null"/> otherwise.</returns>
        public static BaseFileParser? GetConfigParser(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return null;
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            if (_configParserFactories.TryGetValue(extension.ToLowerInvariant(), out var parserFunc) && parserFunc is not null)
                return parserFunc.Invoke();
            else
                return null;
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
        }

        /// <summary>
        /// Returns a registered parser of translation files with the given extension.
        /// </summary>
        /// <param name="extension">The extension for which the parser is needed.</param>
        /// <returns>An instance of the found parser or <see langword="null"/> otherwise.</returns>
        public static BaseFileParser? GetParser(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return null;
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            if (_parserFactories.TryGetValue(extension.ToLowerInvariant(), out var parserFunc) && parserFunc is not null)
                return parserFunc.Invoke();
            else
                return null;
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
        }

            /// <summary>
            /// Returns the list of extensions registered as recognized for translation files.
            /// </summary>
            /// <returns>A list of registered extensions, in lowercase.</returns>
        public static IList<string> GetSupportedExtensions()
        {
            return _parserFactories.Keys.ToList();
        }

#pragma warning disable CA1864 // To avoid double lookup, call 'TryAdd' instead of calling 'Add' with a 'ContainsKey' guard
        internal static void RegisterConfigParser(string extension, Func<BaseFileParser> factory)
        {
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            string extLower = extension.ToLowerInvariant();
            if (!_configParserFactories.ContainsKey(extLower))
                _configParserFactories.Add(extLower, factory);
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
        }

        internal static void RegisterParser(string extension, Func<BaseFileParser> factory)
        {
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            string extLower = extension.ToLowerInvariant();
            if (!_parserFactories.ContainsKey(extLower))
                _parserFactories.Add(extLower, factory);
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'

        }
#pragma warning restore CA1864 // To avoid double lookup, call 'TryAdd' instead of calling 'Add' with a 'ContainsKey' guard
    }
}
