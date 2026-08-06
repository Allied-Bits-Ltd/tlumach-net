// <copyright file="OptimizationFixtures.cs" company="Allied Bits Ltd.">
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
using System.Globalization;
using System.IO;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Tests.Optimizations
{
    /// <summary>
    /// Shared helpers for the optimization characterization tests.
    /// <para>
    /// These tests exist to pin down observable behaviour so that the performance work described in the
    /// optimization review can proceed without silently changing what the library does. Every assertion
    /// records behaviour that was verified against the implementation at the time of writing; where the
    /// current behaviour is believed to be a defect, the test says so explicitly and states what the
    /// corrected value should be.
    /// </para>
    /// </summary>
    internal static class OptimizationFixtures
    {
        /// <summary>The default translation file name used by the fixtures.</summary>
        public const string DefaultFile = "OptStrings.arb";

        /// <summary>The locale declared by the default translation.</summary>
        public const string DefaultLocale = "en";

        private static readonly object _registrationLock = new();
        private static bool _registered;

        /// <summary>
        /// Registers the Arb parser. The parser's static <c>TextProcessingMode</c> is deliberately NOT set
        /// here: these tests always pass the processing mode explicitly, so they cannot be disturbed by
        /// another test class mutating that global.
        /// </summary>
        public static void EnsureParsersRegistered()
        {
            lock (_registrationLock)
            {
                if (_registered)
                    return;

                ArbParser.Use();
                _registered = true;
            }
        }

        /// <summary>
        /// Creates a fresh temporary directory and writes the given files into it.
        /// </summary>
        /// <param name="files">Pairs of file name and file content.</param>
        /// <returns>The full path of the created directory.</returns>
        public static string CreateTranslationDirectory(params (string Name, string Content)[] files)
        {
            string dir = Path.Combine(Path.GetTempPath(), "TlumachOptTests", Path.GetRandomFileName());
            Directory.CreateDirectory(dir);

            foreach ((string name, string content) in files)
                File.WriteAllText(Path.Combine(dir, name), content, Encoding.UTF8);

            return dir;
        }

        /// <summary>
        /// Creates the standard fixture directory: a default English translation, a German one, a
        /// German-Germany one, and a German-Austria one that deliberately omits the <c>welcome</c> key so
        /// that requesting it for <c>de-AT</c> exercises the basic-culture fallback.
        /// </summary>
        /// <returns>The full path of the created directory.</returns>
        public static string CreateStandardDirectory()
            => CreateTranslationDirectory(
                (DefaultFile, DefaultArb),
                ("OptStrings_de.arb", GermanArb),
                ("OptStrings_de-DE.arb", GermanGermanyArb),
                ("OptStrings_de-AT.arb", GermanAustriaArb),
                ("OptStrings_fr.arb", FrenchArb));

        /// <summary>
        /// Builds a configuration bound to a directory. Locale keys are uppercased because
        /// <see cref="TranslationConfiguration.Translations"/> is case-sensitive; see the item 20 tests.
        /// </summary>
        /// <param name="directory">The directory holding the translation files.</param>
        /// <param name="translations">Locale-key to file-name mappings.</param>
        /// <returns>The configuration.</returns>
        public static TranslationConfiguration CreateConfiguration(string directory, params (string Key, string File)[] translations)
        {
            EnsureParsersRegistered();

            TranslationConfiguration config = new(assembly: null, DefaultFile, DefaultLocale, TextFormat.Arb)
            {
                DirectoryHint = directory,
            };

            foreach ((string key, string file) in translations)
                config.Translations.Add(key, file);

            return config;
        }

        /// <summary>
        /// Builds the configuration used by most fixtures, mapping the German and French variants.
        /// </summary>
        /// <param name="directory">The directory holding the translation files.</param>
        /// <returns>The configuration.</returns>
        public static TranslationConfiguration CreateStandardConfiguration(string directory)
            => CreateConfiguration(
                directory,
                ("DE", "OptStrings_de.arb"),
                ("DE-DE", "OptStrings_de-DE.arb"),
                ("DE-AT", "OptStrings_de-AT.arb"),
                ("FR", "OptStrings_fr.arb"),
                (TranslationConfiguration.KEY_TRANSLATION_OTHER, DefaultFile));

        /// <summary>
        /// Creates a manager bound to the given directory and configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="directory">The directory holding the translation files.</param>
        /// <param name="cacheDefaultTranslations">
        /// When <see langword="false"/>, a value found through a fallback is not copied into the
        /// culture-local translation, so every lookup re-runs the fallback chain.
        /// </param>
        /// <returns>The manager. The caller owns it and should dispose it.</returns>
        public static TranslationManager CreateManager(TranslationConfiguration config, string directory, bool cacheDefaultTranslations = true)
            => new(config)
            {
                LoadFromDisk = true,
                TranslationsDirectory = directory,
                CacheDefaultTranslations = cacheDefaultTranslations,
            };

        /// <summary>
        /// Parses an Arb document into a translation, without depending on the parser's static
        /// text-processing mode.
        /// </summary>
        /// <param name="arb">The Arb document.</param>
        /// <param name="mode">The text-processing mode to apply.</param>
        /// <returns>The parsed translation.</returns>
        public static Translation ParseArb(string arb, TextFormat mode = TextFormat.Arb)
        {
            EnsureParsersRegistered();

            return TranslationManager.LoadTranslation(arb, ".arb", CultureInfo.InvariantCulture, mode)
                ?? throw new InvalidOperationException("The Arb sample did not parse.");
        }

        /// <summary>
        /// Creates a templated entry directly from text, so that template processing can be measured
        /// without any parsing or lookup in the way. <see cref="TranslationEntry.EscapedText"/> is left
        /// null, so no un-escaping pass runs.
        /// </summary>
        /// <param name="text">The template text.</param>
        /// <returns>The entry.</returns>
        public static TranslationEntry TemplatedEntry(string text)
            => new("key", text) { ContainsPlaceholders = true };

        /// <summary>
        /// Creates a templated entry whose text is in escaped form, so the un-escaping pass runs.
        /// </summary>
        /// <param name="escapedText">The escaped template text.</param>
        /// <returns>The entry.</returns>
        public static TranslationEntry EscapedEntry(string escapedText)
            => new("key", text: null, escapedText: escapedText) { ContainsPlaceholders = true };

        /// <summary>Builds a case-insensitive placeholder value dictionary.</summary>
        /// <param name="pairs">Name/value pairs.</param>
        /// <returns>The dictionary.</returns>
        public static Dictionary<string, object?> Values(params (string Name, object? Value)[] pairs)
        {
            Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string name, object? value) in pairs)
                result[name] = value;

            return result;
        }

        internal const string DefaultArb = """
{
    "@@locale": "en",
    "hello": "Hello",
    "welcome": "Welcome",
    "goodbye": "Goodbye",
    "grusse": "Greetings",
    "greeting": "Hello, {name}!",
    "@greeting": {
        "placeholders": {
            "name": { "type": "String" }
        }
    },
    "plain": "Plain text"
}
""";

        internal const string GermanArb = """
{
    "@@locale": "de",
    "hello": "Hallo",
    "welcome": "Willkommen",
    "goodbye": "Auf Wiedersehen"
}
""";

        internal const string GermanGermanyArb = """
{
    "@@locale": "de-DE",
    "hello": "Hallo",
    "welcome": "Willkommen (de-DE)",
    "goodbye": "Auf Wiedersehen"
}
""";

        // Deliberately omits "welcome" so that requesting it for de-AT falls back to de-DE.
        internal const string GermanAustriaArb = """
{
    "@@locale": "de-AT",
    "hello": "Servus",
    "goodbye": "Baba"
}
""";

        internal const string FrenchArb = """
{
    "@@locale": "fr",
    "hello": "Salut",
    "welcome": "Bienvenue",
    "goodbye": "Au revoir"
}
""";
    }

    /// <summary>
    /// Serializes the optimization tests that mutate process-global state
    /// (<see cref="TranslationUnit.CacheValues"/> in particular) so that they cannot interfere with one
    /// another.
    /// </summary>
    [CollectionDefinition("Optimizations")]
    public sealed class OptimizationsCollection
    {
    }
}
