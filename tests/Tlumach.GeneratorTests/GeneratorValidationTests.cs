// <copyright file="GeneratorValidationTests.cs" company="Allied Bits Ltd.">
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

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using TlumachIncrementalGenerator = Tlumach.Generator.Generator;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of the validation of translation files that the generator runs for every configuration it processes. Every problem is a warning (an informational message for missing keys);
    /// the problems that break the generated class remain the errors of the generator itself (TLUMACHGEN001).
    /// </summary>
    [Trait("Category", "Generator")]
    [Collection("Generator")]
    public sealed class GeneratorValidationTests : IDisposable
    {
        private const string DefaultJson = "{\n  \"Hello\": \"Hello\",\n  \"Welcome\": \"Welcome, {name}\"\n}\n";

        private const string GermanJson = "{\n  \"Hello\": \"Hallo\",\n  \"Welcome\": \"Willkommen, {name}\"\n}\n";

        private readonly string _directory = Path.Combine(Path.GetTempPath(), "TlumachGeneratorValidationTests", Guid.NewGuid().ToString("N"));

        public GeneratorValidationTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
                // a leftover temporary directory does not affect the results
            }
        }

        [Fact]
        public void ValidTranslationsGiveNoDiagnostics()
        {
            Write("Strings.json", DefaultJson);
            Write("Strings_de.json", GermanJson);

            GeneratorRunResult result = Run(Config());

            Assert.Empty(result.Diagnostics);
            Assert.Single(result.GeneratedSources);
        }

        [Fact]
        public void MissingTranslationFileNamedInTheConfigurationIsAWarningAtTheEntry()
        {
            Write("Strings.json", DefaultJson);
            string config = Config("\"de\": \"Strings_de.json\"");

            GeneratorRunResult result = Run(config);

            Assert.Single(result.GeneratedSources);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("TLUMACHVAL004", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(Path.Combine(_directory, "Strings.jsoncfg"), diagnostic.Location.GetLineSpan().Path);

            // The entry is on the third line of the configuration
            Assert.Equal(2, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        }

        [Fact]
        public void UnparsableTranslationFileIsAWarningWithItsPosition()
        {
            Write("Strings.json", DefaultJson);
            string german = Write("Strings_de.json", "{\n  \"Hello\": \"Hallo\"\n  \"Welcome\": \"Willkommen\"\n}\n");

            GeneratorRunResult result = Run(Config());

            // The error of the file does not break the generated class, so it is a warning
            Assert.Single(result.GeneratedSources);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("TLUMACHVAL005", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
            Assert.Equal(german, span.Path);
            Assert.Equal(2, span.StartLinePosition.Line);
        }

        [Fact]
        public void BrokenTemplateIsAWarning()
        {
            Write("Strings.json", "{\n  \"Items\": \"{count, plural, one {# item} other {# items}}\"\n}\n");
            Write("Strings_de.json", "{\n  \"Items\": \"{count, plural, one {# Artikel}}\"\n}\n");

            GeneratorRunResult result = Run(Config(extra: "\"textProcessingMode\": \"Arb\""));

            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("TLUMACHVAL006", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Contains("Items", diagnostic.GetMessage(), StringComparison.Ordinal);
        }

        [Fact]
        public void MissingAndExtraKeysAreInformational()
        {
            Write("Strings.json", DefaultJson);
            Write("Strings_de.json", "{\n  \"Hello\": \"Hallo\",\n  \"Bye\": \"Tschüss\"\n}\n");

            GeneratorRunResult result = Run(Config());

            Assert.Equal(["TLUMACHVAL007", "TLUMACHVAL012"], result.Diagnostics.Select(d => d.Id).Order(StringComparer.Ordinal));
            Assert.All(result.Diagnostics, d => Assert.Equal(DiagnosticSeverity.Info, d.Severity));
        }

        [Fact]
        public void BrokenDefaultFileIsReportedOnlyByTheGeneratorAndTheOtherFilesAreStillChecked()
        {
            Write("Strings.json", "{\n  \"Hello\": \"Hello\"\n  \"Welcome\": \"Welcome\"\n}\n");
            string german = Write("Strings_de.json", "{ \"Hello\": ");

            GeneratorRunResult result = Run(Config());

            Assert.Empty(result.GeneratedSources);
            Diagnostic error = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal("TLUMACHGEN001", error.Id);

            Diagnostic warning = Assert.Single(result.Diagnostics, d => d.Severity != DiagnosticSeverity.Error);
            Assert.Equal("TLUMACHVAL005", warning.Id);
            Assert.Equal(german, warning.Location.GetLineSpan().Path);
        }

        [Fact]
        public void NoValidationDiagnosticIsAnError()
        {
            // A missing default file breaks the class (an error of the generator); everything the validation finds is a warning at most
            Write("Strings_de.json", "{ \"Hello\": ");
            string config = Config("\"fr\": \"Missing.json\", \"xx-Nowhere\": \"Strings_de.json\"");

            GeneratorRunResult result = Run(config);

            Assert.All(result.Diagnostics.Where(d => d.Id.StartsWith("TLUMACHVAL", StringComparison.Ordinal)), d => Assert.NotEqual(DiagnosticSeverity.Error, d.Severity));
            Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id == "TLUMACHGEN001");
            Assert.Contains(result.Diagnostics, d => d.Id == "TLUMACHVAL004");
            Assert.Contains(result.Diagnostics, d => d.Id == "TLUMACHVAL009");
        }

        private string Write(string name, string content)
        {
            string path = Path.Combine(_directory, name);
            File.WriteAllText(path, content);
            return path;
        }

        private string Config(string? translations = null, string? extra = null)
        {
            string text = "{\n  \"defaultFile\": \"Strings.json\",\n"
                + (translations is null ? string.Empty : $"  \"translations\": {{ {translations} }},\n")
                + (extra is null ? string.Empty : $"  {extra},\n")
                + "  \"generatedNamespace\": \"GenValidation.Tests\",\n  \"generatedClass\": \"Strings\"\n}\n";
            return Write("Strings.jsoncfg", text);
        }

        private GeneratorRunResult Run(string configPath)
        {
            Dictionary<string, string> globalOptions = new(StringComparer.OrdinalIgnoreCase)
            {
                ["build_property.projectdir"] = _directory,
            };

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [new TlumachIncrementalGenerator().AsSourceGenerator()],
                additionalTexts: ImmutableArray.Create<AdditionalText>(new FileAdditionalText(configPath)),
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                optionsProvider: new OptionsProvider(globalOptions));

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName: $"GenValidation_{Guid.NewGuid():N}",
                syntaxTrees: [CSharpSyntaxTree.ParseText("namespace GenValidation.Host { internal static class Empty { } }")],
                references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return Assert.Single(driver.RunGenerators(compilation).GetRunResult().Results);
        }

        private sealed class FileAdditionalText : AdditionalText
        {
            public FileAdditionalText(string path)
            {
                Path = path;
            }

            public override string Path { get; }

            public override SourceText? GetText(CancellationToken cancellationToken = default) => SourceText.From(File.ReadAllText(Path));
        }

        private sealed class Options : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _values;

            public Options(Dictionary<string, string> values)
            {
                _values = values;
            }

            public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value) => _values.TryGetValue(key, out value);
        }

        private sealed class OptionsProvider : AnalyzerConfigOptionsProvider
        {
            private static readonly Options Empty = new(new Dictionary<string, string>());

            public OptionsProvider(Dictionary<string, string> globalOptions)
            {
                GlobalOptions = new Options(globalOptions);
            }

            public override AnalyzerConfigOptions GlobalOptions { get; }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Empty;
        }
    }
}
