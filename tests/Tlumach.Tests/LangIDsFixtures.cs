// <copyright file="LangIDsFixtures.cs" company="Allied Bits Ltd.">
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
using System.Globalization;
using System.IO;
using System.Text;

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Shared fixtures for the <c>string[] langIDs</c> overload tests.
    /// <para>
    /// Builds two parallel "language families" (German and French), each with a neutral file, a specific
    /// "capital"/basic-culture file, and a regional file that deliberately omits the <c>welcome</c> key so
    /// that requesting it exercises the basic-culture fallback. Every file's texts carry a distinct marker
    /// (e.g., "(de)" vs. "(de-DE)") so a test can tell, from the returned text alone, exactly which file
    /// answered the request. "es" and "it" are used as language IDs with no matching translation file at all.
    /// </para>
    /// </summary>
    internal static class LangIDsFixtures
    {
        /// <summary>The default translation file name used by the fixtures.</summary>
        public const string DefaultFile = "LangStrings.arb";

        /// <summary>The locale declared by the default translation.</summary>
        public const string DefaultLocale = "en";

        private static readonly object _registrationLock = new();
        private static bool _registered;

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
        /// Creates a fresh temporary directory with the default file plus the German and French family
        /// files described in the class remarks.
        /// </summary>
        /// <returns>The full path of the created directory.</returns>
        public static string CreateDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "TlumachLangIDsTests", Path.GetRandomFileName());
            Directory.CreateDirectory(dir);

            WriteFile(dir, DefaultFile, DefaultArb);
            WriteFile(dir, "LangStrings_de.arb", GermanArb);
            WriteFile(dir, "LangStrings_de-DE.arb", GermanGermanyArb);
            WriteFile(dir, "LangStrings_de-AT.arb", GermanAustriaArb);
            WriteFile(dir, "LangStrings_fr.arb", FrenchArb);
            WriteFile(dir, "LangStrings_fr-FR.arb", FrenchFranceArb);
            WriteFile(dir, "LangStrings_fr-CA.arb", FrenchCanadaArb);

            return dir;
        }

        private static void WriteFile(string dir, string name, string content)
            => File.WriteAllText(Path.Combine(dir, name), content, Encoding.UTF8);

        /// <summary>
        /// Builds a configuration bound to the given directory. No explicit locale-to-file mapping is
        /// needed: <see cref="TranslationManager"/> locates <c>LangStrings_&lt;culture&gt;.arb</c> files by
        /// convention as long as <see cref="TranslationManager.LoadFromDisk"/> and
        /// <see cref="TranslationManager.TranslationsDirectory"/> are set.
        /// </summary>
        /// <param name="directory">The directory holding the translation files.</param>
        /// <returns>The configuration.</returns>
        public static TranslationConfiguration CreateConfiguration(string directory)
        {
            EnsureParsersRegistered();

            return new TranslationConfiguration(assembly: null, DefaultFile, DefaultLocale, TextFormat.Arb)
            {
                DirectoryHint = directory,
            };
        }

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

        internal const string DefaultArb = """
{
    "@@locale": "en",
    "hello": "Hello",
    "welcome": "Welcome",
    "onlyInDefault": "OnlyInDefault",
    "greeting": "Hello, {name}!",
    "@greeting": {
        "placeholders": {
            "name": { "type": "String" }
        }
    }
}
""";

        internal const string GermanArb = """
{
    "@@locale": "de",
    "hello": "Hallo (de)",
    "welcome": "Willkommen (de)"
}
""";

        internal const string GermanGermanyArb = """
{
    "@@locale": "de-DE",
    "hello": "Hallo (de-DE)",
    "welcome": "Willkommen (de-DE)"
}
""";

        // Deliberately omits "welcome" so that requesting it for de-AT falls back to de-DE.
        internal const string GermanAustriaArb = """
{
    "@@locale": "de-AT",
    "hello": "Servus (de-AT)"
}
""";

        internal const string FrenchArb = """
{
    "@@locale": "fr",
    "hello": "Salut (fr)",
    "welcome": "Bienvenue (fr)"
}
""";

        internal const string FrenchFranceArb = """
{
    "@@locale": "fr-FR",
    "hello": "Salut (fr-FR)",
    "welcome": "Bienvenue (fr-FR)"
}
""";

        // Deliberately omits "welcome" so that requesting it for fr-CA falls back to fr-FR.
        internal const string FrenchCanadaArb = """
{
    "@@locale": "fr-CA",
    "hello": "Allo (fr-CA)"
}
""";
    }
}
