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
using System.Text;

using Tlumach.Base;

namespace Tlumach.SyncfusionTests;

/// <summary>
/// Translations written to a temporary directory: an application set with a "Syncfusion" group (<see cref="Manager"/>), a set with Syncfusion keys at the root
/// (<see cref="PlainManager"/>), and a set made of .resx files in the format of Syncfusion's blazor-locale repository (<see cref="OfficialManager"/>).
/// </summary>
internal sealed class TestTranslations : IDisposable
{
    public static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    // English overrides two Syncfusion texts: Grid_Search exists in German only in the official set, Grid_FilterButton is empty in German.
    private const string DefaultJson = """
        {
            "Title": "Title",
            "Syncfusion": {
                "Grid_Search": "Find",
                "Grid_FilterButton": "Apply filter"
            }
        }
        """;

    // Grid_Contains and Grid_FilterButton are empty deliberately: an empty text counts as missing, and it must not hide the default file (English overrides Grid_FilterButton).
    private const string GermanJson = """
        {
            "Title": "Titel",
            "Syncfusion": {
                "Grid_EmptyRecord": "Keine Datensätze vorhanden",
                "Pager_CurrentPageInfo": "{0} von {1} Seiten",
                "Grid_Contains": "",
                "Grid_FilterButton": ""
            }
        }
        """;

    private const string PlainDefaultJson = """
        {
            "Grid_EmptyRecord": "No rows (plain)"
        }
        """;

    private const string PlainGermanJson = """
        {
            "Grid_EmptyRecord": "Keine Zeilen (plain)"
        }
        """;

    private const string OfficialCfg = """
        defaultFile=SfResources.resx
        defaultLocale=en
        textProcessingMode=DotNet

        [translations]
        de=SfResources.de.resx
        """;

    // Written by us in the format of github.com/syncfusion/blazor-locale: UTF-8 with BOM, LF line endings, xml:space="preserve", and a key repeated with the same value,
    // as the official files do.
    private const string OfficialNeutralResx = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype">
            <value>text/microsoft-resx</value>
          </resheader>
          <data name="Grid_EmptyRecord" xml:space="preserve">
            <value>No records (official)</value>
          </data>
          <data name="Grid_FilterButton" xml:space="preserve">
            <value>Filter (official)</value>
          </data>
          <data name="Grid_EndsWith" xml:space="preserve">
            <value>Ends With (official)</value>
          </data>
          <data name="Grid_EndsWith" xml:space="preserve">
            <value>Ends With (official)</value>
          </data>
        </root>
        """;

    private const string OfficialGermanResx = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype">
            <value>text/microsoft-resx</value>
          </resheader>
          <data name="Grid_EmptyRecord" xml:space="preserve">
            <value>Keine Datensätze (offiziell)</value>
          </data>
          <data name="Grid_Search" xml:space="preserve">
            <value>Suchen (offiziell)</value>
          </data>
          <data name="Grid_Contains" xml:space="preserve">
            <value>Enthält (offiziell)</value>
          </data>
          <data name="Grid_Contains" xml:space="preserve">
            <value>Enthält (offiziell)</value>
          </data>
        </root>
        """;

    public TestTranslations()
    {
        JsonParser.Use();
        IniParser.Use();
        ResxParser.Use();

        Directory = Path.Combine(Path.GetTempPath(), "TlumachSyncfusionTests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        File.WriteAllText(Path.Combine(Directory, "Strings.json"), DefaultJson);
        File.WriteAllText(Path.Combine(Directory, "Strings_de.json"), GermanJson);
        File.WriteAllText(Path.Combine(Directory, "Plain.json"), PlainDefaultJson);
        File.WriteAllText(Path.Combine(Directory, "Plain_de.json"), PlainGermanJson);

        UTF8Encoding withBom = new(encoderShouldEmitUTF8Identifier: true);
        File.WriteAllText(Path.Combine(Directory, "Syncfusion.cfg"), OfficialCfg);
        File.WriteAllText(Path.Combine(Directory, "SfResources.resx"), OfficialNeutralResx.ReplaceLineEndings("\n"), withBom);
        File.WriteAllText(Path.Combine(Directory, "SfResources.de.resx"), OfficialGermanResx.ReplaceLineEndings("\n"), withBom);

        Manager = CreateManager("Strings.json");
        PlainManager = CreateManager("Plain.json");
        OfficialManager = new TranslationManager(Path.Combine(Directory, "Syncfusion.cfg")) { LoadFromDisk = true, TranslationsDirectory = Directory };
    }

    public string Directory { get; }

    public TranslationManager Manager { get; }

    public TranslationManager PlainManager { get; }

    public TranslationManager OfficialManager { get; }

    public void Dispose()
    {
        Manager.Dispose();
        PlainManager.Dispose();
        OfficialManager.Dispose();

        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary directory does not affect the results.
        }
    }

    private TranslationManager CreateManager(string defaultFile)
    {
        TranslationConfiguration configuration = new(assembly: null, defaultFile, "en", TextFormat.DotNet) { DirectoryHint = Directory };
        return new TranslationManager(configuration) { LoadFromDisk = true, TranslationsDirectory = Directory };
    }
}
