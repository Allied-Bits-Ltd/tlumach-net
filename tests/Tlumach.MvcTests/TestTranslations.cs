// <copyright file="TestTranslations.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.MvcTests;

/// <summary>
/// English, German, and Ukrainian ARB translations written to a temporary directory, and a manager that reads them.
/// </summary>
internal sealed class TestTranslations : IDisposable
{
    public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk-UA");

    // Keys are added by later tasks; nested objects become dotted keys (Views.Home.Index.Title).
    private const string DefaultArb = """
        {
            "@@locale": "en",
            "hello": "Hello"
        }
        """;

    private const string GermanArb = """
        {
            "@@locale": "de",
            "hello": "Hallo"
        }
        """;

    private const string UkrainianArb = """
        {
            "@@locale": "uk",
            "hello": "Привіт"
        }
        """;

    private readonly List<TranslationUnit> _units = [];

    public TestTranslations()
    {
        ArbParser.Use();

        Directory = Path.Combine(Path.GetTempPath(), "TlumachMvcTests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(Path.Combine(Directory, "Strings.arb"), DefaultArb);
        File.WriteAllText(Path.Combine(Directory, "Strings_de.arb"), GermanArb);
        File.WriteAllText(Path.Combine(Directory, "Strings_uk.arb"), UkrainianArb);

        // ArbNoEscaping: apostrophes are text, so "The value '{value}' ..." keeps its placeholder (with Arb, quotes escape braces).
        Configuration = new TranslationConfiguration(assembly: null, "Strings.arb", "en", TextFormat.ArbNoEscaping) { DirectoryHint = Directory };
        Manager = new TranslationManager(Configuration) { LoadFromDisk = true, TranslationsDirectory = Directory };
    }

    public string Directory { get; }

    public TranslationConfiguration Configuration { get; }

    public TranslationManager Manager { get; }

    public TranslationUnit Unit(string key, bool containsPlaceholders)
    {
        TranslationUnit unit = new(Manager, Configuration, key, containsPlaceholders);
        _units.Add(unit);
        return unit;
    }

    public void Dispose()
    {
        foreach (TranslationUnit unit in _units)
            unit.Dispose();

        Manager.Dispose();

        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary directory does not affect the results.
        }
    }
}
