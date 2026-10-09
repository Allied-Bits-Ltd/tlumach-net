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

namespace Tlumach.MudBlazorTests;

/// <summary>
/// English and German JSON translations written to a temporary directory: an application set with a "MudBlazor" group (<see cref="Manager"/>)
/// and a set that contains only MudBlazor keys at the root (<see cref="PlainManager"/>).
/// </summary>
internal sealed class TestTranslations : IDisposable
{
    public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    // English overrides one MudBlazor text, which shows that an English user still gets the texts of Tlumach.
    private const string DefaultJson = """
        {
            "Title": "Title",
            "MudBlazor": {
                "MudDataGrid_Contains": "includes"
            }
        }
        """;

    // MudTablePager_PreviousPage and MudDataGridPager_PreviousPage are left out deliberately: MudBlazor must show its English text for them.
    private const string GermanJson = """
        {
            "Title": "Titel",
            "MudBlazor": {
                "MudDataGrid_Contains": "enthält",
                "MudDataGridPager_InfoFormat": "{0}-{1} von {2}",
                "MudDataGridPager_NextPage": "Nächste Seite",
                "MudTablePager_NextPage": "Nächste Seite",
                "Broken": "{0} {5}"
            }
        }
        """;

    private const string PlainDefaultJson = """
        {
            "MudDataGrid_Contains": "contains (plain)"
        }
        """;

    private const string PlainGermanJson = """
        {
            "MudDataGrid_Contains": "enthält (plain)"
        }
        """;

    private readonly string _directory;

    public TestTranslations()
    {
        JsonParser.Use();

        _directory = Path.Combine(Path.GetTempPath(), "TlumachMudBlazorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "Strings.json"), DefaultJson);
        File.WriteAllText(Path.Combine(_directory, "Strings_de.json"), GermanJson);
        File.WriteAllText(Path.Combine(_directory, "Plain.json"), PlainDefaultJson);
        File.WriteAllText(Path.Combine(_directory, "Plain_de.json"), PlainGermanJson);

        Manager = CreateManager("Strings.json");
        PlainManager = CreateManager("Plain.json");
    }

    public TranslationManager Manager { get; }

    public TranslationManager PlainManager { get; }

    public void Dispose()
    {
        Manager.Dispose();
        PlainManager.Dispose();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary directory does not affect the results.
        }
    }

    private TranslationManager CreateManager(string defaultFile)
    {
        TranslationConfiguration configuration = new(assembly: null, defaultFile, "en", TextFormat.DotNet) { DirectoryHint = _directory };
        return new TranslationManager(configuration) { LoadFromDisk = true, TranslationsDirectory = _directory };
    }
}
