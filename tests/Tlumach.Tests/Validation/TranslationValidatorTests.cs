// <copyright file="TranslationValidatorTests.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
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

using Tlumach.Validation;

using Tlumach.TestUtilities;

namespace Tlumach.Tests.Validation;

public class TranslationValidatorTests
{
    private const string DefaultJson = """
        {
            "Hello": "Hello",
            "Welcome": "Welcome, {name}"
        }
        """;

    private const string GermanJson = """
        {
            "Hello": "Hallo",
            "Welcome": "Willkommen, {name}"
        }
        """;

    // -------------------------------------------------------------------------
    // Discovery
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidProjectHasNoMessagesAndListsEveryFile()
    {
        using TempProject project = new();
        string config = project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Add("Strings_de.json", GermanJson);
        project.Add("Strings_de-AT.json", GermanJson);

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        ConfigurationValidationReport configuration = Assert.Single(report.Configurations);
        Assert.Equal(config, configuration.ConfigurationPath);
        Assert.True(configuration.IsGeneratorInput);
        Assert.Equal(project.PathOf("Strings.json"), configuration.DefaultFilePath);
        Assert.Collection(
            configuration.Files,
            f => AssertFile(f, project.PathOf("Strings.json"), TranslationFileOrigin.DefaultFile, string.Empty, 2),
            f => AssertFile(f, project.PathOf("Strings_de-AT.json"), TranslationFileOrigin.NamingConvention, "de-AT", 2),
            f => AssertFile(f, project.PathOf("Strings_de.json"), TranslationFileOrigin.NamingConvention, "de", 2));
    }

    [Fact]
    public void NamingConventionFindsFilesThatAreNotProjectItems()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Write("Strings_pl.json", "{ \"Hello\": \"Cześć\" }");

        ProjectValidationReport report = project.Validate();

        Assert.Contains(report.Configurations[0].Files, f => f.FilePath == project.PathOf("Strings_pl.json") && f.Locale == "pl");
    }

    [Fact]
    public void FilesThatOnlyStartWithTheBaseNameAreNotTranslations()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Add("Strings_backup.json", "not even JSON");
        project.Add("Strings_Extra_de.json", "not even JSON");
        project.Add("Other_de.json", "not even JSON");

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Single(report.Configurations[0].Files);
    }

    [Fact]
    public void TranslationFilesInOtherFormatsAreFound()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Add("Strings_fr.toml", "Hello = \"Bonjour\"\nWelcome = \"Bienvenue, {name}\"\n");
        project.Add("Strings_it.ini", "Hello=Ciao\nWelcome=Benvenuto, {name}\n");

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Contains(report.Configurations[0].Files, f => f.Locale == "fr" && f.EntryCount == 2);
        Assert.Contains(report.Configurations[0].Files, f => f.Locale == "it" && f.EntryCount == 2);
    }

    [Fact]
    public void FileLinkedIntoTheDirectoryOfTheDefaultFileIsChecked()
    {
        using TempProject project = new();
        project.AddConfig("Translations/Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Translations/Strings.json", DefaultJson);
        string linked = project.Add("../Shared/French.json", "{ \"Hello\": ", "EmbeddedResource", link: @"Translations\Strings_fr.json");

        ProjectValidationReport report = project.Validate();

        ValidatedFile file = Assert.Single(report.Configurations[0].Files, f => f.FilePath == linked);
        Assert.Equal(TranslationFileOrigin.LinkedItem, file.Origin);
        Assert.Equal("fr", file.Locale);
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.TranslationUnparsable && m.FilePath == linked);
    }

    [Fact]
    public void FileNamedAfterTheDefaultFileInAnotherDirectoryIsNotATranslationOfIt()
    {
        // The translation manager looks for translations only where the default file is; another directory may hold files of another configuration with the same base name
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Add("Other/Strings_fr.json", "not even JSON", "EmbeddedResource");

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Single(report.Configurations[0].Files);
    }

    [Fact]
    public void FilesNamedInTheConfigurationAreChecked()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json", "\"de\": \"Translations/German.json\", \"other\": \"Fallback.json\""));
        project.Add("Strings.json", DefaultJson);
        project.Add("Translations/German.json", GermanJson);
        project.Add("Fallback.json", DefaultJson);

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Contains(report.Configurations[0].Files, f => f.FilePath == project.PathOf("Translations/German.json") && f.Origin == TranslationFileOrigin.Configuration && f.Locale == "de");
        Assert.Contains(report.Configurations[0].Files, f => f.FilePath == project.PathOf("Fallback.json") && f.Origin == TranslationFileOrigin.Configuration && f.Locale.Length == 0);
    }

    [Fact]
    public void ProblemsWithTheTranslationsSectionAreReported()
    {
        using TempProject project = new();
        string config = project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json", "\"de\": \"Missing.json\", \"xx-Nowhere\": \"Strings.json\", \"fr\": \"French.docx\""));
        project.Add("Strings.json", DefaultJson);

        ProjectValidationReport report = project.Validate();

        // The messages point at the entry in the configuration
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.ReferencedFileMissing && m.Severity == ValidationSeverity.Warning && m.FilePath == config && m.Message.Contains("Missing.json", StringComparison.Ordinal) && m.Line == 3 && m.Column > 1);
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.UnknownLocale && m.Severity == ValidationSeverity.Warning && m.Message.Contains("xx-Nowhere", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.NoParser && m.Severity == ValidationSeverity.Error && m.Message.Contains("French.docx", StringComparison.Ordinal));
    }

    [Fact]
    public void ResxTranslationsWithTheDuplicateExtensionAreFound()
    {
        // A ResX translation is kept as "Strings.pl.resx.resx" so that it is embedded as "Strings.pl.resx" (see "File formats" in the documentation)
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <defaultFile>Strings.resx</defaultFile>
              <generatedNamespace>App</generatedNamespace>
              <generatedClass>Strings</generatedClass>
              <translations>
                <locale name="de">Strings.de.resx</locale>
              </translations>
            </root>
            """);
        project.Add("Strings.resx", Resx("Hello"));
        string german = project.Add("Strings.de.resx.resx", Resx("Hallo"), "EmbeddedResource");
        string polish = project.Add("Strings.pl.resx.resx", Resx("Cześć"), "EmbeddedResource");

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Contains(report.Configurations[0].Files, f => f.FilePath == german && f.Origin == TranslationFileOrigin.Configuration && f.Locale == "de" && f.EntryCount == 1);
        Assert.Contains(report.Configurations[0].Files, f => f.FilePath == polish && f.Origin == TranslationFileOrigin.NamingConvention && f.Locale == "pl" && f.EntryCount == 1);
    }

    // -------------------------------------------------------------------------
    // Embedding of ResX files (see "File formats" in the documentation; the rules were checked with MSBuild)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Strings.de.resx.resx", "Non-Resx", null)]
    [InlineData("Strings.de.resx", null, "satellite assembly")]
    [InlineData("Strings.de.resx", "Non-Resx", "satellite assembly")]
    [InlineData("Strings.de.resx.resx", null, "binary resources")]
    public void EmbeddingOfAResxTranslationIsChecked(string fileName, string? type, string? expectedReason)
    {
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", ResxConfig("<locale name=\"de\">Strings.de.resx</locale>"));

        // The generator reads the default file by its name, so it keeps the name and gets the name of the resource from LogicalName
        project.Add("Strings.resx", Resx("Hello"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["LogicalName"] = "App.Strings.resx" });
        string german = project.Add(fileName, Resx("Hallo"), "EmbeddedResource", metadata: type is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["Type"] = type });

        ProjectValidationReport report = project.Validate();

        if (expectedReason is null)
        {
            Assert.Empty(report.Messages);
            return;
        }

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.ResxNotEmbeddedAsText, message.Code);
        Assert.Equal(ValidationSeverity.Warning, message.Severity);
        Assert.Equal(german, message.FilePath);
        Assert.Contains(expectedReason, message.Message, StringComparison.Ordinal);
        Assert.Contains("Non-Resx", message.Message, StringComparison.Ordinal);
        if (!fileName.EndsWith(".resx.resx", StringComparison.Ordinal))
            Assert.Contains("'Strings.de.resx.resx'", message.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, "binary resources")]
    [InlineData("Non-Resx", "removes the extension")]
    public void EmbeddingOfAResxDefaultFileIsChecked(string? type, string expectedReason)
    {
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", ResxConfig(string.Empty));
        string strings = project.Add("Strings.resx", Resx("Hello"), "EmbeddedResource", metadata: type is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["Type"] = type });

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.ResxNotEmbeddedAsText, message.Code);
        Assert.Equal(strings, message.FilePath);
        Assert.Contains(expectedReason, message.Message, StringComparison.Ordinal);

        // The generator reads the default file by the name in the configuration, so renaming the file is not advised
        Assert.Contains("LogicalName", message.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("name the file", message.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmbeddingIsNotCheckedForFilesThatAreNotEmbeddedOrWithoutMetadata()
    {
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", ResxConfig("<locale name=\"de\">Strings.de.resx</locale>"));
        project.Add("Strings.resx", Resx("Hello"), "None", metadata: new Dictionary<string, string>());
        project.Add("Strings.de.resx", Resx("Hallo"), "EmbeddedResource", metadata: null);

        Assert.Empty(project.Validate().Messages);
    }

    [Fact]
    public void CultureNamedResxFileWithoutCultureAndWithLogicalNameIsEmbeddedAsText()
    {
        // WithCulture="false" keeps "LanguageResource.de.resx" in the main assembly, and LogicalName keeps its extension (as the MudBlazor sample does)
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", ResxConfig("<locale name=\"de\">Strings.de.resx</locale>"));
        project.Add("Strings.resx", Resx("Hello"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["LogicalName"] = "App.Strings.resx" });
        project.Add("Strings.de.resx", Resx("Hallo"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["WithCulture"] = "false", ["LogicalName"] = "App.Strings.de.resx" });

        Assert.Empty(project.Validate().Messages);
    }

    [Fact]
    public void CultureNamedResxFileWithoutCultureNeedsLogicalName()
    {
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", ResxConfig("<locale name=\"de\">Strings.de.resx</locale>"));
        project.Add("Strings.resx", Resx("Hello"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["LogicalName"] = "App.Strings.resx" });
        project.Add("Strings.de.resx", Resx("Hallo"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["WithCulture"] = "false" });

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.ResxNotEmbeddedAsText, message.Code);
        Assert.Contains("removes the extension", message.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResxFileWithLogicalNameIsEmbeddedAsText()
    {
        using TempProject project = new();
        project.AddConfig("Strings.resxcfg", ResxConfig("<locale name=\"de\">Strings_de.resx</locale>"));
        project.Add("Strings.resx", Resx("Hello"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["LogicalName"] = "App.Strings.resx" });
        project.Add("Strings_de.resx", Resx("Hallo"), "EmbeddedResource", metadata: new Dictionary<string, string> { ["Type"] = "Non-Resx", ["LogicalName"] = "App.Strings_de.resx" });

        Assert.Empty(project.Validate().Messages);
    }

    // -------------------------------------------------------------------------
    // Precedence
    // -------------------------------------------------------------------------

    [Fact]
    public void FileNamedByConventionIsShadowedByTheConfiguration()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json", "\"de\": \"German.json\""));
        project.Add("Strings.json", DefaultJson);
        project.Add("German.json", GermanJson);
        string conventional = project.Add("Strings_de.json", GermanJson);
        string regional = project.Add("Strings_de-CH.json", GermanJson);

        ProjectValidationReport report = project.Validate();

        // de-CH falls back to the configured language entry "de"
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.FileShadowed && m.FilePath == conventional && m.Message.Contains("German.json", StringComparison.Ordinal));
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.FileShadowed && m.FilePath == regional);
    }

    [Fact]
    public void OtherEntryShadowsEveryFileNamedByConvention()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json", "\"*\": \"Fallback.json\""));
        project.Add("Strings.json", DefaultJson);
        project.Add("Fallback.json", DefaultJson);
        string german = project.Add("Strings_de.json", GermanJson);

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.FileShadowed, message.Code);
        Assert.Equal(german, message.FilePath);
    }

    [Fact]
    public void FileWithTheExtensionOfTheDefaultFileTakesPrecedence()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.Add("Strings_de.json", GermanJson);
        string toml = project.Add("Strings_de.toml", "Hello = \"Hallo\"\nWelcome = \"Willkommen, {name}\"\n");

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.FileShadowed, message.Code);
        Assert.Equal(ValidationSeverity.Warning, message.Severity);
        Assert.Equal(toml, message.FilePath);
    }

    // -------------------------------------------------------------------------
    // Content of translation files
    // -------------------------------------------------------------------------

    [Fact]
    public void SyntaxErrorIsReportedWithItsPosition()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        string german = project.Add("Strings_de.json", "{\n    \"Hello\": \"Hallo\"\n    \"Welcome\": \"Willkommen\"\n}\n");

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.TranslationUnparsable, message.Code);
        Assert.Equal(ValidationSeverity.Error, message.Severity);
        Assert.Equal(german, message.FilePath);
        Assert.Equal(3, message.Line);
        Assert.True(message.Column > 0);

        ValidatedFile file = Assert.Single(report.Configurations[0].Files, f => f.FilePath == german);
        Assert.Equal(1, file.ErrorCount);
    }

    [Fact]
    public void KeyMissingFromTheDefaultFileIsInfoAtTheKey()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        string german = project.Add("Strings_de.json", "{\n    \"Hello\": \"Hallo\",\n    \"Welcome\": \"Willkommen, {name}\",\n    \"Goodbye\": \"Tschüss\"\n}\n");

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.KeyNotInDefault, message.Code);
        Assert.Equal(ValidationSeverity.Info, message.Severity);
        Assert.Equal(german, message.FilePath);
        Assert.Contains("Goodbye", message.Message, StringComparison.Ordinal);
        Assert.Equal(4, message.Line);
    }

    [Fact]
    public void KeysMissingFromTheDefaultFileAreReportedOncePerFile()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        string german = project.Add("Strings_de.json", "{\n  \"Hello\": \"Hallo\",\n  \"Zebra\": \"Zebra\",\n  \"Apple\": \"Apfel\",\n  \"Mango\": \"Mango\",\n  \"Welcome\": \"Willkommen, {name}\"\n}\n");

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.KeyNotInDefault, message.Code);
        Assert.Equal(german, message.FilePath);
        Assert.Contains("3 keys", message.Message, StringComparison.Ordinal);
        Assert.Contains("'Zebra', 'Apple', 'Mango'", message.Message, StringComparison.Ordinal);

        // The message points at the first of the keys in the file
        Assert.Equal(3, message.Line);
    }

    [Fact]
    public void MessageAboutASettingPointsAtTheSettingAndNotAtAComment()
    {
        using TempProject project = new();
        string config = project.Add("Strings.cfg", "; Strings.json is created by the build\n\ndefaultFile=Strings.json\n", "EmbeddedResource");

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.DefaultFileUnusable, message.Code);
        Assert.Equal(config, message.FilePath);
        Assert.Equal(3, message.Line);
        Assert.Equal(13, message.Column);
    }

    [Theory]
    [InlineData("EmbeddedResource")]
    [InlineData(ProjectFile.AdditionalFilesItemType)]
    public void MissingDefaultFileIsAnErrorAndMissingTranslationFileIsAWarning(string itemType)
    {
        // A missing translation file makes the application show the default texts, but it does not break the build or the application
        using TempProject project = new();
        project.Add("Strings.jsoncfg", TempProject.JsonConfig("Strings.json", "\"de\": \"Strings_de.json\""), itemType);

        ProjectValidationReport report = project.Validate();

        Assert.Equal(ValidationSeverity.Error, Assert.Single(report.Messages, m => m.Code == ValidationCodes.DefaultFileUnusable).Severity);
        Assert.Equal(ValidationSeverity.Warning, Assert.Single(report.Messages, m => m.Code == ValidationCodes.ReferencedFileMissing).Severity);
    }

    [Fact]
    public void KeysMissingFromATranslationAreInfo()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", "{ \"Hello\": \"Hello\", \"Welcome\": \"Welcome\", \"Bye\": \"Bye\" }");
        string german = project.Add("Strings_de.json", "{ \"Hello\": \"Hallo\" }");
        project.Add("Strings_fr.json", "{ \"Hello\": \"Bonjour\", \"Welcome\": \"Bienvenue\", \"Bye\": \"Au revoir\" }");

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.KeyMissingFromTranslation, message.Code);
        Assert.Equal(ValidationSeverity.Info, message.Severity);
        Assert.Equal(german, message.FilePath);
        Assert.Contains("2 keys", message.Message, StringComparison.Ordinal);
        Assert.Contains("'Bye', 'Welcome'", message.Message, StringComparison.Ordinal);
        Assert.Equal(1, report.InfoCount);
        Assert.Equal(1, Assert.Single(report.Configurations[0].Files, f => f.FilePath == german).InfoCount);
    }

    [Fact]
    public void BrokenTemplateIsAnErrorAtTheKey()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json", extra: "\"textProcessingMode\": \"Arb\""));
        project.Add("Strings.json", "{\n  \"Items\": \"{count, plural, one {# item} other {# items}}\"\n}\n");
        string german = project.Add("Strings_de.json", "{\n  \"Items\": \"{count, plural, one {# Artikel}}\"\n}\n");

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.TemplateInvalid, message.Code);
        Assert.Equal(ValidationSeverity.Error, message.Severity);
        Assert.Equal(german, message.FilePath);
        Assert.Contains("Items", message.Message, StringComparison.Ordinal);
        Assert.Equal(2, message.Line);
    }

    [Fact]
    public void EmptyTranslationFileIsAWarning()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        string german = project.Add("Strings_de.json", "  \r\n");

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.TranslationEmpty, message.Code);
        Assert.Equal(ValidationSeverity.Warning, message.Severity);
        Assert.Equal(german, message.FilePath);
    }

    [Fact]
    public void EveryLocaleOfACsvFileIsChecked()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.csv", extra: "\"defaultLocale\": \"en\", \"textProcessingMode\": \"Arb\""));
        string csv = project.Add("Strings.csv", "key,en,de,Description\nItems,\"{count, plural, one {# item} other {# items}}\",\"{count, plural, one {# Artikel}}\",Count\nHello,Hello,Hallo,\n");

        ProjectValidationReport report = project.Validate();

        ValidatedFile file = Assert.Single(report.Configurations[0].Files);
        Assert.Equal(["en", "de"], file.CheckedLocales);
        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.TemplateInvalid, message.Code);
        Assert.Equal(csv, message.FilePath);
        Assert.Contains("'de'", message.Message, StringComparison.Ordinal);
        Assert.Equal(2, message.Line);
    }

    [Fact]
    public void EveryLocaleOfAStringCatalogIsChecked()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.xcstrings", extra: "\"defaultLocale\": \"en\""));
        project.Add("Strings.xcstrings", """
            {
              "sourceLanguage" : "en",
              "strings" : {
                "Hello" : {
                  "localizations" : {
                    "en" : { "stringUnit" : { "state" : "translated", "value" : "Hello" } },
                    "de" : { "stringUnit" : { "state" : "translated", "value" : "Hallo" } },
                    "fr" : { "stringUnit" : { "state" : "translated", "value" : "Bonjour" } }
                  }
                }
              },
              "version" : "1.0"
            }
            """);

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Equal(["en", "de", "fr"], Assert.Single(report.Configurations[0].Files).CheckedLocales);
    }

    [Fact]
    public void XliffTranslationIsChecked()
    {
        using TempProject project = new();
        project.AddConfig("Strings.xlfcfg", """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <defaultFile>Strings.xlf</defaultFile>
              <defaultLocale>en</defaultLocale>
              <generatedNamespace>App</generatedNamespace>
              <generatedClass>Strings</generatedClass>
            </root>
            """);
        project.Add("Strings.xlf", Xliff("en", null, "<unit id=\"Hello\"><source>Hello</source></unit>"));
        project.Add("Strings_de.xlf", Xliff("en", "de", "<unit id=\"Hello\"><source>Hello</source><target>Hallo</target></unit>"));
        string broken = project.Add("Strings_fr.xlf", Xliff("en", "fr", "<unit id=\"Hello\"><source>Hello</source><target>Bonjour</target></unit"));

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.TranslationUnparsable, message.Code);
        Assert.Equal(broken, message.FilePath);
        Assert.Contains(report.Configurations[0].Files, f => f.Locale == "de" && f.EntryCount == 1 && f.ErrorCount == 0);
    }

    // -------------------------------------------------------------------------
    // Configuration and default file
    // -------------------------------------------------------------------------

    [Fact]
    public void GeneratorSettingsAreRequiredForGeneratorInputs()
    {
        using TempProject project = new();
        string config = project.AddConfig("Strings.jsoncfg", "{ \"defaultFile\": \"Strings.json\" }");
        project.Add("Strings.json", DefaultJson);

        ProjectValidationReport report = project.Validate();

        Assert.Equal(2, report.Messages.Count(m => m.Code == ValidationCodes.GeneratorSettingMissing && m.FilePath == config));
    }

    [Fact]
    public void ConfigurationThatIsNotAGeneratorInputNeedsNoGeneratorSettings()
    {
        using TempProject project = new();
        project.Add("Strings.jsoncfg", "{ \"defaultFile\": \"Strings.json\" }", "EmbeddedResource");
        project.Add("Strings.json", DefaultJson, "EmbeddedResource");
        project.Add("Strings_de.json", "{ \"Hello\": ", "EmbeddedResource");

        ProjectValidationReport report = project.Validate();

        ConfigurationValidationReport configuration = Assert.Single(report.Configurations);
        Assert.False(configuration.IsGeneratorInput);
        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.TranslationUnparsable, message.Code);
    }

    [Fact]
    public void FileWithAConfigurationExtensionThatIsNotAConfigurationIsIgnored()
    {
        using TempProject project = new();
        string settings = project.Add("settings.cfg", "[logging]\nlevel=debug\n");

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Configurations);
        Assert.Equal([settings], report.IgnoredFiles);
    }

    [Fact]
    public void UnparsableGeneratorConfigurationIsAnError()
    {
        using TempProject project = new();
        string config = project.AddConfig("Strings.jsoncfg", "{\n  \"defaultFile\": \"Strings.json\"\n  \"generatedClass\": \"Strings\"\n}\n");

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.ConfigurationUnreadable, message.Code);
        Assert.Equal(config, message.FilePath);
    }

    [Fact]
    public void GeneratorConfigurationWithoutDefaultFileIsAnError()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", "{ \"generatedNamespace\": \"App\", \"generatedClass\": \"Strings\" }");

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.ConfigurationUnreadable, message.Code);
        Assert.Contains("defaultFile", message.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingDefaultFileIsReportedOnce()
    {
        using TempProject project = new();
        string config = project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings_de.json", GermanJson);

        ProjectValidationReport report = project.Validate();

        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.DefaultFileUnusable, message.Code);
        Assert.Equal(ValidationSeverity.Error, message.Severity);
        Assert.Equal(config, message.FilePath);
        Assert.Contains("does not exist", message.Message, StringComparison.Ordinal);

        // The message points at the name of the file in the configuration
        Assert.Equal(2, message.Line);

        // The translations are still checked
        Assert.Contains(report.Configurations[0].Files, f => f.Locale == "de" && f.EntryCount == 2);
    }

    [Fact]
    public void UnparsableDefaultFileIsReportedOnceWithItsPosition()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        string defaultFile = project.Add("Strings.json", "{\n  \"Hello\": \"Hello\"\n  \"Welcome\": \"Welcome\"\n}\n");

        ValidationMessage message = Assert.Single(project.Validate().Messages);
        Assert.Equal(ValidationCodes.DefaultFileUnusable, message.Code);
        Assert.Equal(defaultFile, message.FilePath);
        Assert.Equal(3, message.Line);
    }

    [Fact]
    public void DefaultFileInASubdirectoryIsFound()
    {
        using TempProject project = new();
        project.AddConfig("Translations/Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Translations/Strings.json", DefaultJson);
        project.Add("Translations/Strings_de.json", GermanJson);

        ProjectValidationReport report = project.Validate();

        Assert.Empty(report.Messages);
        Assert.Equal(2, report.FileCount);
    }

    [Fact]
    public void SeveralConfigurationsAreReportedSeparately()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);
        project.AddConfig("Errors.cfg", "defaultFile=Errors.ini\ngeneratedNamespace=App\ngeneratedClass=Errors\n");
        project.Add("Errors.ini", "NotFound=Not found\n");
        project.Add("Errors_de.ini", "NotFound=Nicht gefunden\nExtra=Extra\n");

        ProjectValidationReport report = project.Validate();

        Assert.Equal(2, report.Configurations.Count);
        ValidationMessage message = Assert.Single(report.Messages);
        Assert.Equal(ValidationCodes.KeyNotInDefault, message.Code);
        Assert.Equal(project.PathOf("Errors_de.ini"), message.FilePath);
        Assert.Equal(2, message.Line);
    }

    [Fact]
    public void ItemListedWithSeveralItemTypesIsAGeneratorInputOnce()
    {
        using TempProject project = new();
        string config = project.Add("Strings.jsoncfg", "{ \"defaultFile\": \"Strings.json\" }", "None");
        project.Add("Strings.jsoncfg", "{ \"defaultFile\": \"Strings.json\" }", ProjectFile.AdditionalFilesItemType);
        project.Add("Strings.json", DefaultJson);

        ProjectValidationReport report = project.Validate();

        ConfigurationValidationReport configuration = Assert.Single(report.Configurations);
        Assert.True(configuration.IsGeneratorInput);
        Assert.Contains(report.Messages, m => m.Code == ValidationCodes.GeneratorSettingMissing && m.FilePath == config);
    }

    [Fact]
    public void ValidationCanBeCancelled()
    {
        using TempProject project = new();
        project.AddConfig("Strings.jsoncfg", TempProject.JsonConfig("Strings.json"));
        project.Add("Strings.json", DefaultJson);

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => TranslationValidator.ValidateProject(project.ProjectPath, project.Items, cancellation.Token));
    }

    [Theory]
    [InlineData("Strings.jsoncfg", true)]
    [InlineData(@"C:\App\Sample.CFG", true)]
    [InlineData("Strings.tomlcfg", true)]
    [InlineData("Strings.json", false)]
    [InlineData("App.csproj", false)]
    [InlineData("", false)]
    public void ConfigurationFilesAreRecognizedByExtension(string path, bool expected)
    {
        Assert.Equal(expected, TranslationValidator.IsConfigurationFile(path));
    }

    // -------------------------------------------------------------------------
    // Messages
    // -------------------------------------------------------------------------

    [Fact]
    public void MessageIsFormattedAsACompilerMessage()
    {
        ValidationMessage message = new(ValidationSeverity.Error, ValidationCodes.TranslationUnparsable, "Broken.", @"C:\App\Strings_de.json", 3, 7);
        ValidationMessage withoutPosition = new(ValidationSeverity.Warning, ValidationCodes.TranslationEmpty, "Empty.", @"C:\App\Strings_de.json");
        ValidationMessage withoutFile = new(ValidationSeverity.Info, "X", "Note.", null);

        Assert.Equal(@"C:\App\Strings_de.json(3,7): error TLUMACHVAL005: Broken.", message.ToString());
        Assert.Equal(@"C:\App\Strings_de.json: warning TLUMACHVAL008: Empty.", withoutPosition.ToString());
        Assert.Equal("info X: Note.", withoutFile.ToString());
    }

    private static string ResxConfig(string translations) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <defaultFile>Strings.resx</defaultFile>
          <generatedNamespace>App</generatedNamespace>
          <generatedClass>Strings</generatedClass>
          <translations>{translations}</translations>
        </root>
        """;

    private static string Resx(string hello) => $"""
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
          <resheader name="version"><value>2.0</value></resheader>
          <data name="Hello" xml:space="preserve"><value>{hello}</value></data>
        </root>
        """;

    private static string Xliff(string sourceLanguage, string? targetLanguage, string units)
    {
        string target = targetLanguage is null ? string.Empty : $" trgLang=\"{targetLanguage}\"";
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="2.0" xmlns="urn:oasis:names:tc:xliff:document:2.0" srcLang="{sourceLanguage}"{target}>
              <file id="Strings">
                {units}
              </file>
            </xliff>
            """;
    }

    private static void AssertFile(ValidatedFile file, string path, TranslationFileOrigin origin, string locale, int entryCount)
    {
        Assert.Equal(path, file.FilePath);
        Assert.Equal(origin, file.Origin);
        Assert.Equal(locale, file.Locale);
        Assert.Equal(entryCount, file.EntryCount);
        Assert.Equal(0, file.ErrorCount);
        Assert.Equal(0, file.WarningCount);
    }
}
