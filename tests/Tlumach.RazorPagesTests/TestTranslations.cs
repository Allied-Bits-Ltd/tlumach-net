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

namespace Tlumach.RazorPagesTests;

/// <summary>
/// English, German, and Ukrainian ARB translations written to a temporary directory, and a manager that reads them.
/// </summary>
internal sealed class TestTranslations : IDisposable
{
    public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    public static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk-UA");

    // Nested objects become dotted keys (Pages.Index.Title).
    private const string DefaultArb = """
        {
            "@@locale": "en",
            "Welcome": "Welcome (shared)",
            "Pages": {
                "Index": { "Title": "Home page", "Intro": "Hello, <b>{name}</b>!" },
                "Contact": { "Title": "Contact us" },
                "Shared": { "_Layout": { "Footer": "Layout footer" }, "_Header": { "Text": "Header" } }
            },
            "Areas": { "Admin": { "Pages": { "Users": { "List": { "Title": "Users" } } } } },
            "ModelBinding": { "AttemptedValueIsInvalid": "The value '{value}' is not valid for {field}." },
            "DisplayNames": {
                "Email": "E-mail address",
                "Title": "Title",
                "Pages": {
                    "ContactModel": { "InputModel": { "Age": "Your age" } },
                    "Movies": { "CreateModel": { "InputModel": { "Title": "Movie title" } } },
                    "Actors": { "CreateModel": { "InputModel": { "Title": "Stage name" } } }
                }
            }
        }
        """;

    private const string GermanArb = """
        {
            "@@locale": "de",
            "Welcome": "Willkommen (gemeinsam)",
            "Pages": {
                "Index": { "Title": "Startseite", "Intro": "Hallo, <b>{name}</b>!" },
                "Contact": { "Title": "Kontakt" },
                "Shared": { "_Layout": { "Footer": "Fußzeile" }, "_Header": { "Text": "Kopfzeile" } }
            },
            "Areas": { "Admin": { "Pages": { "Users": { "List": { "Title": "Benutzer" } } } } },
            "ModelBinding": { "AttemptedValueIsInvalid": "Der Wert '{value}' ist für {field} ungültig." },
            "DisplayNames": {
                "Email": "E-Mail-Adresse",
                "Title": "Titel",
                "Pages": {
                    "ContactModel": { "InputModel": { "Age": "Ihr Alter" } },
                    "Movies": { "CreateModel": { "InputModel": { "Title": "Filmtitel" } } },
                    "Actors": { "CreateModel": { "InputModel": { "Title": "Künstlername" } } }
                }
            }
        }
        """;

    private const string UkrainianArb = """
        {
            "@@locale": "uk",
            "Welcome": "Ласкаво просимо (спільне)",
            "Pages": {
                "Index": { "Title": "Головна сторінка", "Intro": "Привіт, <b>{name}</b>!" },
                "Contact": { "Title": "Контакти" },
                "Shared": { "_Layout": { "Footer": "Нижній колонтитул" }, "_Header": { "Text": "Заголовок" } }
            },
            "Areas": { "Admin": { "Pages": { "Users": { "List": { "Title": "Користувачі" } } } } },
            "ModelBinding": { "AttemptedValueIsInvalid": "Значення '{value}' недійсне для {field}." },
            "DisplayNames": {
                "Email": "Адреса електронної пошти",
                "Title": "Назва",
                "Pages": {
                    "ContactModel": { "InputModel": { "Age": "Ваш вік" } },
                    "Movies": { "CreateModel": { "InputModel": { "Title": "Назва фільму" } } },
                    "Actors": { "CreateModel": { "InputModel": { "Title": "Сценічне ім'я" } } }
                }
            }
        }
        """;

    private readonly List<TranslationUnit> _units = [];

    public TestTranslations(string? defaultArb = null, string? germanArb = null, string? ukrainianArb = null, TextFormat textFormat = TextFormat.ArbNoEscaping)
    {
        ArbParser.Use();

        Directory = Path.Combine(Path.GetTempPath(), "TlumachRazorPagesTests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(Path.Combine(Directory, "Strings.arb"), defaultArb ?? DefaultArb);
        File.WriteAllText(Path.Combine(Directory, "Strings_de.arb"), germanArb ?? GermanArb);
        File.WriteAllText(Path.Combine(Directory, "Strings_uk.arb"), ukrainianArb ?? UkrainianArb);

        // ArbNoEscaping (the default): apostrophes are text, so "The value '{value}' ..." keeps its placeholder (with Arb, quotes escape braces). Indexed placeholders ({0}) are rejected by the Arb modes; pass TextFormat.DotNet for texts that use them.
        Configuration = new TranslationConfiguration(assembly: null, "Strings.arb", "en", textFormat) { DirectoryHint = Directory };
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
