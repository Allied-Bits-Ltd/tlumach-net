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

    // Nested objects become dotted keys (Views.Home.Index.Title).
    private const string DefaultArb = """
        {
            "@@locale": "en",
            "hello": "Hello",
            "greeting": "Hello, {name}!",
            "rich": "Click <b>here</b>",
            "richGreeting": "Welcome, <b>{name}</b>",
            "items": "{count, plural, =0{no items} =1{# item} other{# items}}",
            "due": "Due {date, date}",
            "Welcome": "Welcome (shared)",
            "Title": "Title (shared)",
            "Intro": "Hello, <b>{name}</b>!",
            "Views": {
                "Home": {
                    "Index": {
                        "Title": "Home page",
                        "Intro": "Hello, <b>{name}</b>!"
                    },
                    "Sections": { "Title": "Sections title" }
                },
                "Shared": {
                    "_Layout": { "Footer": "Layout footer" },
                    "_Status": { "Text": "Partial text" }
                }
            },
            "Areas": { "Admin": { "Views": { "Users": { "List": { "Title": "Users" } } } } },
            "ModelBinding": {
                "AttemptedValueIsInvalid": "The value '{value}' is not valid for {field}.",
                "ValueMustBeANumber": "The field {0} must be a number."
            },
            "DisplayNames": {
                "Email": "E-mail address",
                "Models": { "RegisterViewModel": { "Email": "Your e-mail", "Age": "Your age" } }
            }
        }
        """;

    private const string GermanArb = """
        {
            "@@locale": "de",
            "hello": "Hallo",
            "greeting": "Hallo, {name}!",
            "rich": "Klicken Sie <b>hier</b>",
            "richGreeting": "Willkommen, <b>{name}</b>",
            "items": "{count, plural, =0{keine Elemente} =1{# Element} other{# Elemente}}",
            "due": "Fällig am {date, date}",
            "Welcome": "Willkommen (gemeinsam)",
            "Title": "Titel (gemeinsam)",
            "Intro": "Hallo, <b>{name}</b>!",
            "Views": {
                "Home": {
                    "Index": {
                        "Title": "Startseite",
                        "Intro": "Hallo, <b>{name}</b>!"
                    },
                    "Sections": { "Title": "Abschnittstitel" }
                },
                "Shared": {
                    "_Layout": { "Footer": "Fußzeile" },
                    "_Status": { "Text": "Teilansicht" }
                }
            },
            "Areas": { "Admin": { "Views": { "Users": { "List": { "Title": "Benutzer" } } } } },
            "ModelBinding": {
                "AttemptedValueIsInvalid": "Der Wert '{value}' ist für {field} ungültig.",
                "ValueMustBeANumber": "Das Feld {0} muss eine Zahl sein."
            },
            "DisplayNames": {
                "Email": "E-Mail-Adresse",
                "Models": { "RegisterViewModel": { "Email": "Ihre E-Mail", "Age": "Ihr Alter" } }
            }
        }
        """;

    private const string UkrainianArb = """
        {
            "@@locale": "uk",
            "hello": "Привіт",
            "greeting": "Привіт, {name}!",
            "rich": "Натисніть <b>тут</b>",
            "richGreeting": "Ласкаво просимо, <b>{name}</b>",
            "items": "{count, plural, =0{немає елементів} other{# елементів}}",
            "due": "Термін {date, date}",
            "Welcome": "Ласкаво просимо (спільне)",
            "Title": "Заголовок (спільний)",
            "Intro": "Привіт, <b>{name}</b>!",
            "Views": {
                "Home": {
                    "Index": {
                        "Title": "Головна сторінка",
                        "Intro": "Привіт, <b>{name}</b>!"
                    },
                    "Sections": { "Title": "Заголовок розділу" }
                },
                "Shared": {
                    "_Layout": { "Footer": "Нижній колонтитул" },
                    "_Status": { "Text": "Частковий вигляд" }
                }
            },
            "Areas": { "Admin": { "Views": { "Users": { "List": { "Title": "Користувачі" } } } } },
            "ModelBinding": {
                "AttemptedValueIsInvalid": "Значення '{value}' недійсне для {field}.",
                "ValueMustBeANumber": "Поле {0} має бути числом."
            },
            "DisplayNames": {
                "Email": "Адреса електронної пошти",
                "Models": { "RegisterViewModel": { "Email": "Ваша пошта", "Age": "Ваш вік" } }
            }
        }
        """;

    private readonly List<TranslationUnit> _units = [];

    public TestTranslations(string? defaultArb = null, string? germanArb = null, string? ukrainianArb = null)
    {
        ArbParser.Use();

        Directory = Path.Combine(Path.GetTempPath(), "TlumachMvcTests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(Path.Combine(Directory, "Strings.arb"), defaultArb ?? DefaultArb);
        File.WriteAllText(Path.Combine(Directory, "Strings_de.arb"), germanArb ?? GermanArb);
        File.WriteAllText(Path.Combine(Directory, "Strings_uk.arb"), ukrainianArb ?? UkrainianArb);

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
