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
        // Concurrent maps, because parsers are commonly registered from static constructors on one thread
        // while another thread is already looking a parser up. The lookups used to read these dictionaries
        // without taking the lock that registration wrote under, which can make a lookup miss a parser that
        // is in fact registered.
        // The comparer is case-insensitive so that a lookup does not have to lowercase the extension first.
        // Registration still lowercases the key, because GetSupportedExtensions promises lowercase names.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Func<BaseParser>> _parserFactories = new(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Func<BaseParser>> _configParserFactories = new(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BaseParser> _parserSingletons = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns a value that indicates whether a configuration parser is registered for a given extension.
        /// </summary>
        /// <param name="extension">The extension for which the parser is checked.</param>
        /// <returns><see langword="true"/> if the parser was found or <see langword="false"/> otherwise.</returns>
        public static bool HasConfigParser(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            return _configParserFactories.ContainsKey(extension);
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

            if (_configParserFactories.TryGetValue(extension, out var parserFunc) && parserFunc is not null)
                return parserFunc.Invoke();
            else
                return null;
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

            if (getStaticInstance && _parserSingletons.TryGetValue(extension, out BaseParser? cached))
                return cached;

            if (!_parserFactories.TryGetValue(extension, out var parserFunc) || parserFunc is null)
                return null;

            BaseParser? parser = parserFunc.Invoke();
            if (parser is null)
                return null;

            if (getStaticInstance)
            {
                // If another thread created one first, use theirs so that the singleton really is one.
                return _parserSingletons.GetOrAdd(extension, parser);
            }

            _parserSingletons.TryAdd(extension, parser);
            return parser;
        }

        /// <summary>
        /// Returns the list of extensions registered as recognized for translation files.
        /// </summary>
        /// <returns>A list of registered extensions, in lowercase.</returns>
        public static IList<string> GetSupportedExtensions()
        {
            return _parserFactories.Keys.ToList();
        }

        internal static void RegisterConfigParser(string extension, Func<BaseParser> factory)
        {
#pragma warning disable CA1308 // GetSupportedExtensions promises lowercase names, so the key is lowercased.
            _configParserFactories.TryAdd(extension.ToLowerInvariant(), factory);
#pragma warning restore CA1308
        }

        internal static void RegisterParser(string extension, Func<BaseParser> factory)
        {
#pragma warning disable CA1308 // GetSupportedExtensions promises lowercase names, so the key is lowercased.
            _parserFactories.TryAdd(extension.ToLowerInvariant(), factory);
#pragma warning restore CA1308
        }
    }
}
