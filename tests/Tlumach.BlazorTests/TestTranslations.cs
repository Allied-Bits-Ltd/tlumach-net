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

namespace Tlumach.BlazorTests;

/// <summary>
/// English and German ARB translations written to a temporary directory, with a manager and units for every kind of text the tests need.
/// </summary>
internal sealed class TestTranslations : IDisposable
{
    public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private const string DefaultArb = """
        {
            "@@locale": "en",
            "hello": "Hello",
            "greeting": "Hello, {name}!",
            "position": "Item {current} of {total}",
            "items": "{count, plural, =0{no items} =1{# item} other{# items}}",
            "rich": "Click <b>here</b>",
            "script": "<script>alert(1)</script>",
            "richGreeting": "Welcome, <b>{name}</b>"
        }
        """;

    private const string GermanArb = """
        {
            "@@locale": "de",
            "hello": "Hallo",
            "greeting": "Hallo, {name}!",
            "position": "Element {current} von {total}",
            "items": "{count, plural, =0{keine Elemente} =1{# Element} other{# Elemente}}",
            "rich": "Klicken Sie <b>hier</b>",
            "script": "<script>alert(2)</script>",
            "richGreeting": "Willkommen, <b>{name}</b>"
        }
        """;

    private readonly string _directory;

    public TestTranslations()
    {
        ArbParser.Use();

        _directory = Path.Combine(Path.GetTempPath(), "TlumachBlazorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "Strings.arb"), DefaultArb);
        File.WriteAllText(Path.Combine(_directory, "Strings_de.arb"), GermanArb);

        Configuration = new TranslationConfiguration(assembly: null, "Strings.arb", "en", TextFormat.Arb) { DirectoryHint = _directory };
        Manager = new TranslationManager(Configuration) { LoadFromDisk = true, TranslationsDirectory = _directory };

        Hello = new TranslationUnit(Manager, Configuration, "hello", containsPlaceholders: false);
        Greeting = new TranslationUnit(Manager, Configuration, "greeting", containsPlaceholders: true);
        Position = new TranslationUnit(Manager, Configuration, "position", containsPlaceholders: true);
        Items = new TranslationUnit(Manager, Configuration, "items", containsPlaceholders: true);
        Rich = new TranslationUnit(Manager, Configuration, "rich", containsPlaceholders: false);
        Script = new TranslationUnit(Manager, Configuration, "script", containsPlaceholders: false);
        RichGreeting = new TranslationUnit(Manager, Configuration, "richGreeting", containsPlaceholders: true);
    }

    public TranslationConfiguration Configuration { get; }

    public TranslationManager Manager { get; }

    public TranslationUnit Hello { get; }

    public TranslationUnit Greeting { get; }

    public TranslationUnit Position { get; }

    public TranslationUnit Items { get; }

    public TranslationUnit Rich { get; }

    public TranslationUnit Script { get; }

    public TranslationUnit RichGreeting { get; }

    public void Dispose()
    {
        Hello.Dispose();
        Greeting.Dispose();
        Position.Dispose();
        Items.Dispose();
        Rich.Dispose();
        Script.Dispose();
        RichGreeting.Dispose();
        Manager.Dispose();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary directory does not affect the results.
        }
    }
}
