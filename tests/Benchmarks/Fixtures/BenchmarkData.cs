// <copyright file="BenchmarkData.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach.Benchmarks.Fixtures;

/// <summary>
/// Shared data for all benchmark classes.
/// <para>
/// Translation files are written to a per-process temporary directory instead of being shipped as
/// content files. BenchmarkDotNet builds and runs a generated project that references the benchmark
/// assembly, and content files are not reliably copied into that generated project's output; writing
/// the files from string constants at <c>[GlobalSetup]</c> time removes that whole class of failure.
/// </para>
/// </summary>
public static class BenchmarkData
{
    /// <summary>The base name of the translation file set (without locale suffix or extension).</summary>
    public const string FileBase = "BenchStrings";

    /// <summary>The name of the default (fallback) translation file.</summary>
    public const string DefaultFileName = FileBase + ".arb";

    /// <summary>The locale declared by the default translation file.</summary>
    public const string DefaultLocale = "en";

    /// <summary>A key that exists in every translation and is not templated.</summary>
    public const string PlainKey = "hello";

    /// <summary>
    /// A key that exists in the default translation and in <c>de-DE</c> but NOT in <c>de-AT</c>.
    /// Requesting it for <c>de-AT</c> forces the basic-culture fallback through
    /// <c>TranslationManager.FindBasicCulture</c>.
    /// </summary>
    public const string BasicCultureFallbackKey = "welcome";

    /// <summary>A key that exists in no translation at all, forcing the whole fallback chain to run.</summary>
    public const string MissingKey = "thisKeyExistsNowhere";

    /// <summary>An Arb key whose text has one declared placeholder.</summary>
    public const string ArbOnePlaceholderKey = "greeting";

    /// <summary>An Arb key whose text has eight declared placeholders.</summary>
    public const string ArbEightPlaceholderKey = "manyArgs";

    /// <summary>An Arb key whose text is an ICU plural expression.</summary>
    public const string ArbPluralKey = "itemCount";

    /// <summary>An Arb key whose text is an ICU select expression that nests other keys' text.</summary>
    public const string ArbSelectKey = "pronoun";

    /// <summary>Cultures used by the concurrency benchmarks. Each has its own translation file.</summary>
    public static readonly string[] ConcurrentCultureNames = ["fr", "es", "it", "nl", "pl", "sk", "hr", "cs"];

    private static readonly object _dirLock = new();
    private static string? _directory;

    /// <summary>
    /// Gets the directory that holds the materialized translation files, creating and populating it
    /// on first access.
    /// </summary>
    public static string Directory
    {
        get
        {
            lock (_dirLock)
            {
                if (_directory is not null)
                    return _directory;

                string dir = Path.Combine(
                    Path.GetTempPath(),
                    "TlumachBenchData-" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

                System.IO.Directory.CreateDirectory(dir);

                WriteFile(dir, DefaultFileName, DefaultArb);
                WriteFile(dir, FileBase + "_de.arb", GermanArb);
                WriteFile(dir, FileBase + "_de-DE.arb", GermanGermanyArb);
                WriteFile(dir, FileBase + "_de-AT.arb", GermanAustriaArb);

                foreach (string culture in ConcurrentCultureNames)
                    WriteFile(dir, FileBase + "_" + culture + ".arb", MakeSimpleArb(culture));

                _directory = dir;
                return dir;
            }
        }
    }

    /// <summary>
    /// Registers the Arb parser and puts it into Arb text-processing mode. Safe to call repeatedly.
    /// </summary>
    public static void EnsureParsersRegistered()
    {
        ArbParser.Use();
        ArbParser.TextProcessingMode = TextFormat.Arb;
    }

    /// <summary>
    /// Builds a configuration whose <see cref="TranslationConfiguration.Translations"/> map is populated
    /// with explicit, uppercase locale keys, matching what the config parsers produce.
    /// </summary>
    /// <param name="includeConcurrentCultures">Whether to map the cultures used by the concurrency benchmarks.</param>
    /// <returns>A configuration bound to the materialized translation directory.</returns>
    public static TranslationConfiguration CreateConfiguration(bool includeConcurrentCultures = true)
    {
        EnsureParsersRegistered();
        string dir = Directory;

        TranslationConfiguration config = new(assembly: null, DefaultFileName, DefaultLocale, TextFormat.Arb)
        {
            DirectoryHint = dir,
        };

        // Keys are uppercased because TranslationConfiguration.Translations uses the default (ordinal)
        // comparer and the manager probes it with culture.Name.ToUpperInvariant(). See optimization item 20.
        config.Translations.Add("DE", FileBase + "_de.arb");
        config.Translations.Add("DE-DE", FileBase + "_de-DE.arb");
        config.Translations.Add("DE-AT", FileBase + "_de-AT.arb");

        if (includeConcurrentCultures)
        {
            foreach (string culture in ConcurrentCultureNames)
                config.Translations.Add(culture.ToUpperInvariant(), FileBase + "_" + culture + ".arb");
        }

        config.Translations.Add(TranslationConfiguration.KEY_TRANSLATION_OTHER, DefaultFileName);

        return config;
    }

    /// <summary>
    /// Creates a translation manager bound to the materialized translation directory.
    /// </summary>
    /// <param name="cacheDefaultTranslations">
    /// When <see langword="false"/>, values resolved from a fallback translation are not copied into the
    /// culture-local translation, so every call re-runs the full fallback chain. Set this to
    /// <see langword="false"/> when the fallback chain itself is what is being measured.
    /// </param>
    /// <param name="includeConcurrentCultures">Whether to map the cultures used by the concurrency benchmarks.</param>
    /// <returns>A ready-to-use translation manager.</returns>
    public static TranslationManager CreateManager(bool cacheDefaultTranslations = true, bool includeConcurrentCultures = true)
    {
        TranslationConfiguration config = CreateConfiguration(includeConcurrentCultures);

        return new TranslationManager(config)
        {
            LoadFromDisk = true,
            TranslationsDirectory = Directory,
            CacheDefaultTranslations = cacheDefaultTranslations,
        };
    }

    /// <summary>
    /// Creates a translation entry directly from text, bypassing the parsers. Used by the benchmarks that
    /// isolate template-processing cost from lookup and parsing cost.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="text">The entry text. Assigned to <see cref="TranslationEntry.Text"/>, leaving
    /// <see cref="TranslationEntry.EscapedText"/> null so that no un-escaping pass runs.</param>
    /// <returns>A templated translation entry.</returns>
    public static TranslationEntry CreateTemplatedEntry(string key, string text)
        => new(key, text) { ContainsPlaceholders = true };

    /// <summary>
    /// Creates a translation entry whose text is stored in escaped form, so that the un-escaping pass runs
    /// during template processing.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="escapedText">The escaped entry text.</param>
    /// <returns>A templated translation entry with escaped text.</returns>
    public static TranslationEntry CreateEscapedTemplatedEntry(string key, string escapedText)
        => new(key, text: null, escapedText: escapedText) { ContainsPlaceholders = true };

    private static void WriteFile(string dir, string name, string content)
        => File.WriteAllText(Path.Combine(dir, name), content, System.Text.Encoding.UTF8);

    private static string MakeSimpleArb(string culture)
        => "{\n" +
           "    \"@@locale\": \"" + culture + "\",\n" +
           "    \"hello\": \"Hello-" + culture + "\",\n" +
           "    \"welcome\": \"Welcome-" + culture + "\",\n" +
           "    \"goodbye\": \"Goodbye-" + culture + "\"\n" +
           "}\n";

    private const string DefaultArb = """
{
    "@@locale": "en",
    "@@author": "Tlumach benchmark suite",
    "hello": "Hello",
    "welcome": "Welcome",
    "goodbye": "Goodbye",
    "farewell": "Farewell",
    "about": "About this application",
    "greeting": "Hello, {name}!",
    "@greeting": {
        "type": "text",
        "description": "A greeting with one declared placeholder",
        "placeholders": {
            "name": {
                "type": "String",
                "example": "Alice"
            }
        }
    },
    "manyArgs": "{a} {b} {c} {d} {e} {f} {g} {h}",
    "@manyArgs": {
        "type": "text",
        "description": "Eight declared placeholders, to expose the linear placeholder scan",
        "placeholders": {
            "a": { "type": "String" },
            "b": { "type": "String" },
            "c": { "type": "String" },
            "d": { "type": "String" },
            "e": { "type": "String" },
            "f": { "type": "String" },
            "g": { "type": "String" },
            "h": { "type": "String" }
        }
    },
    "itemCount": "{count, plural, =0{no items} one{one item} other{several items}}",
    "@itemCount": {
        "type": "text",
        "description": "An ICU plural expression. The 'format' key is required: without it, FormatArbNumber is never reached and the ICU tail is ignored entirely.",
        "placeholders": {
            "count": {
                "type": "num",
                "format": "decimal"
            }
        }
    },
    "pronoun": "{gender, select, male{He} female{She} other{They}}",
    "@pronoun": {
        "type": "text",
        "description": "An ICU select expression",
        "placeholders": {
            "gender": {
                "type": "String"
            }
        }
    }
}
""";

    private const string GermanArb = """
{
    "@@locale": "de",
    "hello": "Hallo",
    "welcome": "Willkommen",
    "goodbye": "Auf Wiedersehen",
    "greeting": "Hallo, {name}!",
    "@greeting": {
        "placeholders": {
            "name": { "type": "String" }
        }
    }
}
""";

    private const string GermanGermanyArb = """
{
    "@@locale": "de-DE",
    "hello": "Hallo",
    "welcome": "Willkommen",
    "goodbye": "Auf Wiedersehen"
}
""";

    // Deliberately missing the "welcome" key so that requesting it for de-AT falls back to de-DE.
    private const string GermanAustriaArb = """
{
    "@@locale": "de-AT",
    "hello": "Servus",
    "goodbye": "Baba"
}
""";
}
