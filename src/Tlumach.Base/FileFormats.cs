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

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    public static class FileFormats
    {
        private static readonly Dictionary<string, Func<BaseParser>> _parserFactories = [];
        private static readonly Dictionary<string, Func<BaseParser>> _configParserFactories = [];
        private static readonly Dictionary<string, BaseParser> _parserSingletons = [];

        /// <summary>
        /// Returns a value that indicates whether a configuration parser is registered for a given extension.
        /// </summary>
        /// <param name="extension">The extension for which the parser is checked.</param>
        /// <returns><see langword="true"/> if the parser was found or <see langword="false"/> otherwise.</returns>
        public static bool HasConfigParser(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            return _configParserFactories.ContainsKey(extension.ToLowerInvariant());
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
        }

        /// <summary>
        /// Returns a registered parser of configuration files with the given extension.
        /// </summary>
        /// <param name="extension">The extension for which the parser is needed.</param>
        /// <returns>An instance of the found parser or <see langword="null"/> otherwise.</returns>
        public static BaseParser? GetConfigParser(string extension)
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
        /// <param name="getStaticInstance">When set to <see langword="true"/>, uses a cached singleton (or creates one if it does not yet exist). <para>This parameter is used by the helper functions.</para></param>
        /// <returns>An instance of the found parser or <see langword="null"/> otherwise.</returns>
        public static BaseParser? GetParser(string extension, bool getStaticInstance = false)
        {
            if (string.IsNullOrEmpty(extension))
                return null;

            BaseParser? parser = null;

            lock (_parserFactories)
            {
#pragma warning disable CA1308
                string extLower = extension.ToLowerInvariant();
#pragma warning restore CA1308
                if (getStaticInstance)
                {
                    lock (_parserSingletons)
                    {
#pragma warning disable CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method
                        if (_parserSingletons.ContainsKey(extLower))
                            return _parserSingletons[extLower];
#pragma warning restore CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method

                        if (!_parserFactories.TryGetValue(extLower, out var parserFunc) || parserFunc is null)
                            return null;

                        parser = parserFunc.Invoke();
                        if (parser is not null)
                            _parserSingletons.Add(extLower, parser);

                        return parser;
                    }
                }
                else
                {
                    if (!_parserFactories.TryGetValue(extLower, out var parserFunc) || parserFunc is null)
                        return null;

                    parser = parserFunc.Invoke();
                    lock (_parserSingletons)
                    {
                        if (parser is not null && !_parserSingletons.ContainsKey(extLower))
                            _parserSingletons.Add(extLower, parser);
                    }

                    return parser;
                }
            }
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
        internal static void RegisterConfigParser(string extension, Func<BaseParser> factory)
        {
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            string extLower = extension.ToLowerInvariant();
            lock (_configParserFactories)
            {
                if (!_configParserFactories.ContainsKey(extLower))
                    _configParserFactories.Add(extLower, factory);
            }
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
        }

        internal static void RegisterParser(string extension, Func<BaseParser> factory)
        {
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            string extLower = extension.ToLowerInvariant();
            lock (_parserFactories)
            {
                if (!_parserFactories.ContainsKey(extLower))
                        _parserFactories.Add(extLower, factory);
            }
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'

        }
#pragma warning restore CA1864 // To avoid double lookup, call 'TryAdd' instead of calling 'Add' with a 'ContainsKey' guard
    }
}
