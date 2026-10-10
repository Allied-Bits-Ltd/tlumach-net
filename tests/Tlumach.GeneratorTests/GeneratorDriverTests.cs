// <copyright file="GeneratorDriverTests.cs" company="Allied Bits Ltd.">
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

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using TlumachIncrementalGenerator = Tlumach.Generator.Generator;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests that run the real incremental generator, <see cref="TlumachIncrementalGenerator"/>, through <see cref="CSharpGeneratorDriver"/>.
    /// <para>Unlike the tests that call BaseGenerator.GenerateClass directly, these go through the pipeline built in Initialize: the filtering of additional files, the build_property options read from
    /// the global analyzer config options, the language version taken from the parse options, and the diagnostics reported on failures.</para>
    /// </summary>
    [Trait("Category", "Generator")]
    [Collection("Generator")]
    public class GeneratorDriverTests
    {
        private const string GeneratedNamespace = "GenDriver.Tests";

        /// <summary>
        /// The folder with the test data. The generator reads the default translation file from the disk relative to the configuration file, so the data are used in place and the path is built
        /// from the location of the test assembly (bin/{Configuration}/{TFM}/), which makes the tests independent of the current directory.
        /// </summary>
        private static readonly string TestDataDir = Path.Combine(Tlumach.TestUtilities.TestDirectories.Project, "TestData", "GeneratorDriver");

        /// <summary>
        /// The parent folder of <see cref="TestDataDir"/>, used to check the DirectoryHint that the generator emits when the configuration file is in a subfolder of the project.
        /// </summary>
        private static readonly string TestDataParentDir = Path.GetFullPath(Path.Combine(TestDataDir, ".."));

        // ----------------------------------------------------------------------------------------
        // One test per format of the default file
        // ----------------------------------------------------------------------------------------

        [Fact]
        public void ShouldGenerateFromJsonDefaultFile()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg");

            AssertClassHeader(source, "JsonStrings", "JsonStrings.json", "JsonParser");
            Assert.Contains("public const string helloKey = \"hello\";", source, StringComparison.Ordinal);
            Assert.Contains("public static readonly TranslationUnit hello;", source, StringComparison.Ordinal);
            Assert.Contains("public static class ui", source, StringComparison.Ordinal);
            Assert.Contains("public const string greetingKey = \"greeting\";", source, StringComparison.Ordinal);
            Assert.Contains("greeting = new TranslationUnit(TranslationManager, _translationConfiguration, \"ui.greeting\", false);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromArbDefaultFile()
        {
            string source = GenerateSingleAndCompile("Arb.arbcfg");

            AssertClassHeader(source, "ArbStrings", "ArbStrings.arb", "ArbParser");
            Assert.Contains("public const string helloKey = \"hello\";", source, StringComparison.Ordinal);
            Assert.Contains("public static class ui", source, StringComparison.Ordinal);
            Assert.Contains("greeting = new TranslationUnit(TranslationManager, _translationConfiguration, \"ui.greeting\", false);", source, StringComparison.Ordinal);

            // The metadata entries of ARB are not keys
            Assert.DoesNotContain("@hello", source, StringComparison.Ordinal);
            Assert.DoesNotContain("@@locale", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromIniDefaultFile()
        {
            string source = GenerateSingleAndCompile("Ini.cfg");

            AssertClassHeader(source, "IniStrings", "IniStrings.ini", "IniParser");
            Assert.Contains("public const string helloKey = \"hello\";", source, StringComparison.Ordinal);
            Assert.Contains("public static class ui", source, StringComparison.Ordinal);
            Assert.Contains("greeting = new TranslationUnit(TranslationManager, _translationConfiguration, \"ui.greeting\", false);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromTomlDefaultFile()
        {
            string source = GenerateSingleAndCompile("Toml.tomlcfg");

            AssertClassHeader(source, "TomlStrings", "TomlStrings.toml", "TomlParser");
            Assert.Contains("public const string helloKey = \"hello\";", source, StringComparison.Ordinal);
            Assert.Contains("public static class ui", source, StringComparison.Ordinal);
            Assert.Contains("greeting = new TranslationUnit(TranslationManager, _translationConfiguration, \"ui.greeting\", false);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromCsvDefaultFile()
        {
            string source = GenerateSingleAndCompile("Csv.cfg");

            AssertClassHeader(source, "CsvStrings", "CsvStrings.csv", "CsvParser");
            AssertFlatKeys(source);
        }

        [Fact]
        public void ShouldGenerateCompilableClassFromCsvDefaultFile()
        {
            // Covers the CSV path of the generator while ShouldGenerateFromCsvDefaultFile is skipped. It does not check the first key, which the generator currently loses.
            string source = GenerateSingleAndCompile("Csv.cfg");
            AssertClassHeader(source, "CsvStrings", "CsvStrings.csv", "CsvParser");
            Assert.Contains("public const string welcomeKey = \"welcome\";", source, StringComparison.Ordinal);
            Assert.Contains("welcome = new TranslationUnit(TranslationManager, _translationConfiguration, \"welcome\", false);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromTsvDefaultFile()
        {
            string source = GenerateSingleAndCompile("Tsv.cfg");

            AssertClassHeader(source, "TsvStrings", "TsvStrings.tsv", "TsvParser");
            AssertFlatKeys(source);
        }

        [Fact]
        public void ShouldGenerateCompilableClassFromTsvDefaultFile()
        {
            // Covers the TSV path of the generator while ShouldGenerateFromTsvDefaultFile is skipped. It does not check the first key, which the generator currently loses.
            string source = GenerateSingleAndCompile("Tsv.cfg");

            AssertClassHeader(source, "TsvStrings", "TsvStrings.tsv", "TsvParser");
            Assert.Contains("public const string welcomeKey = \"welcome\";", source, StringComparison.Ordinal);
            Assert.Contains("welcome = new TranslationUnit(TranslationManager, _translationConfiguration, \"welcome\", false);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromResxDefaultFile()
        {
            string source = GenerateSingleAndCompile("Resx.resxcfg");

            AssertClassHeader(source, "ResxStrings", "ResxStrings.resx", "ResxParser");
            AssertFlatKeys(source);
        }

        [Fact]
        public void ShouldGenerateFromXliffDefaultFile()
        {
            string source = GenerateSingleAndCompile("Xliff.xlfcfg");

            AssertClassHeader(source, "XliffStrings", "XliffStrings.xlf", "XliffParser");
            AssertFlatKeys(source);
            Assert.Contains("_defaultFileLocale = \"en\";", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldGenerateFromStringCatalogDefaultFile()
        {
            string source = GenerateSingleAndCompile("StringCat.jsoncfg");

            AssertClassHeader(source, "CatStrings", "CatStrings.xcstrings", "StringCatParser");
            AssertFlatKeys(source);
            Assert.Contains("_defaultFileLocale = \"en\";", source, StringComparison.Ordinal);
        }

        // ----------------------------------------------------------------------------------------
        // build_property options
        // ----------------------------------------------------------------------------------------

        [Fact]
        public void ShouldNotApplyOptionsWhenNoneAreSet()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg");

            Assert.DoesNotContain("UseContextCulture", source, StringComparison.Ordinal);
            Assert.DoesNotContain("public static class Texts", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DirectoryHint", source, StringComparison.Ordinal);
            Assert.DoesNotContain("private static TranslationUnit", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using System.Globalization;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyUsingNamespaceOption()
        {
            // The option names the namespace of the TranslationUnit class of a XAML integration, so the generated code needs such a class to compile
            const string customUnit = """
                namespace GenDriver.CustomUnits
                {
                    public class TranslationUnit : Tlumach.TranslationUnit
                    {
                        public TranslationUnit(Tlumach.TranslationManager translationManager, Tlumach.Base.TranslationConfiguration translationConfiguration, string key, bool containsPlaceholders)
                            : base(translationManager, translationConfiguration, key, containsPlaceholders)
                        {
                        }
                    }
                }
                """;

            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorUsingNamespace", "GenDriver.CustomUnits")), extraSources: [customUnit]);

            Assert.Contains("using GenDriver.CustomUnits;", source, StringComparison.Ordinal);
            Assert.Contains("public static readonly GenDriver.CustomUnits.TranslationUnit hello;", source, StringComparison.Ordinal);
            Assert.Contains("hello = new GenDriver.CustomUnits.TranslationUnit(TranslationManager, _translationConfiguration, \"hello\", false);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyExtraParsersOption()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorExtraParsers", "TomlParser;CsvParser JsonParser")));

            Assert.Contains("            JsonParser.Use();", source, StringComparison.Ordinal);
            Assert.Contains("            TomlParser.Use();", source, StringComparison.Ordinal);
            Assert.Contains("            CsvParser.Use();", source, StringComparison.Ordinal);

            // The parser of the default file is not repeated when it is listed again
            Assert.Equal(1, CountOccurrences(source, "JsonParser.Use();"));
        }

        [Fact]
        public void ShouldApplyDelayedUnitCreationOption()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorDelayedUnitCreation", "true")));

            Assert.Contains("private static TranslationUnit? _hello;", source, StringComparison.Ordinal);
            Assert.Contains("public static TranslationUnit hello", source, StringComparison.Ordinal);
            Assert.Contains("if (_hello is null)", source, StringComparison.Ordinal);
            Assert.Contains("_hello = new TranslationUnit(TranslationManager, _translationConfiguration, \"hello\", false);", source, StringComparison.Ordinal);
            Assert.Contains("_greeting = new TranslationUnit(TranslationManager, _translationConfiguration, \"ui.greeting\", false);", source, StringComparison.Ordinal);
            Assert.DoesNotContain("public static readonly TranslationUnit", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyCreateFilledMethodsOption()
        {
            string source = GenerateSingleAndCompile("Templated.cfg", Options(("TlumachGeneratorCreateFilledMethods", "true")));

            Assert.Contains("using System.Globalization;", source, StringComparison.Ordinal);
            Assert.Contains("public class GreetingTranslationUnit: TranslationUnit", source, StringComparison.Ordinal);
            Assert.Contains("public static readonly GreetingTranslationUnit greeting;", source, StringComparison.Ordinal);
            Assert.Contains("greeting = new GreetingTranslationUnit(TranslationManager, _translationConfiguration, \"greeting\", true);", source, StringComparison.Ordinal);
            Assert.Contains("public string Filled(", source, StringComparison.Ordinal);
            Assert.Contains("public string Filled(CultureInfo culture, ", source, StringComparison.Ordinal);

            // A key without placeholders gets no subclass
            Assert.DoesNotContain("PlainTranslationUnit", source, StringComparison.Ordinal);
            Assert.Contains("public static readonly TranslationUnit plain;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldNotCreateFilledMethodsWithoutOption()
        {
            string source = GenerateSingleAndCompile("Templated.cfg");

            Assert.DoesNotContain("GreetingTranslationUnit", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Filled(", source, StringComparison.Ordinal);
            Assert.Contains("greeting = new TranslationUnit(TranslationManager, _translationConfiguration, \"greeting\", true);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyUseContextCultureOption()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorUseContextCulture", "true")));

            Assert.Contains("TranslationManager.UseContextCulture = true;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyCreateStringAccessorsOption()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorCreateStringAccessors", "true")));

            Assert.Contains("public static class Texts", source, StringComparison.Ordinal);
            Assert.Contains("public static string hello => global::GenDriver.Tests.JsonStrings.hello.CurrentTemplate;", source, StringComparison.Ordinal);
            Assert.Contains("public static string greeting => global::GenDriver.Tests.JsonStrings.ui.greeting.CurrentTemplate;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyStringAccessorsClassOption()
        {
            string source = GenerateSingleAndCompile(
                "Json.jsoncfg",
                Options(("TlumachGeneratorCreateStringAccessors", "true"), ("TlumachGeneratorStringAccessorsClass", "Resources")));

            Assert.Contains("public static class Resources", source, StringComparison.Ordinal);
            Assert.DoesNotContain("public static class Texts", source, StringComparison.Ordinal);
            Assert.Contains("public static string hello => global::GenDriver.Tests.JsonStrings.hello.CurrentTemplate;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldApplyStringAccessorsCultureOption()
        {
            string source = GenerateSingleAndCompile(
                "Json.jsoncfg",
                Options(("TlumachGeneratorCreateStringAccessors", "true"), ("TlumachGeneratorStringAccessorsCulture", "ambient")));

            Assert.Contains("public static class Texts", source, StringComparison.Ordinal);
            Assert.Contains("public static string hello => global::GenDriver.Tests.JsonStrings.hello.GetValueAsTemplate(System.Globalization.CultureInfo.CurrentCulture);", source, StringComparison.Ordinal);
            Assert.DoesNotContain(".CurrentTemplate;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldIgnoreStringAccessorOptionsWithoutCreateStringAccessors()
        {
            string source = GenerateSingleAndCompile(
                "Json.jsoncfg",
                Options(("TlumachGeneratorStringAccessorsClass", "Resources"), ("TlumachGeneratorStringAccessorsCulture", "ambient")));

            Assert.DoesNotContain("public static class Resources", source, StringComparison.Ordinal);
            Assert.DoesNotContain("GetValueAsTemplate", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldEmitDirectoryHintWhenConfigurationIsInSubfolderOfProjectDir()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("projectdir", TestDataParentDir)));

            Assert.Contains("{ DirectoryHint = @\"GeneratorDriver\", }", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldNotEmitDirectoryHintWhenConfigurationIsInProjectDir()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("projectdir", TestDataDir)));

            Assert.DoesNotContain("DirectoryHint", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldReadOptionsCaseInsensitively()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("tlumachgeneratorusecontextculture", "TRUE")));

            Assert.Contains("TranslationManager.UseContextCulture = true;", source, StringComparison.Ordinal);
        }

        // ----------------------------------------------------------------------------------------
        // Language version
        // ----------------------------------------------------------------------------------------

        [Fact]
        public void ShouldOmitNullableAnnotationsForCSharp73()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorDelayedUnitCreation", "true")), LanguageVersion.CSharp7_3);

            Assert.DoesNotContain("#nullable", source, StringComparison.Ordinal);
            Assert.DoesNotContain("?", source.Replace("?.", string.Empty, StringComparison.Ordinal).Replace("??", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
            Assert.Contains("private static string _defaultFileLocale = null;", source, StringComparison.Ordinal);
            Assert.Contains("private static TranslationUnit _hello;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldOmitNullableAnnotationsInFilledMethodsForCSharp73()
        {
            string source = GenerateSingleAndCompile("Templated.cfg", Options(("TlumachGeneratorCreateFilledMethods", "true")), LanguageVersion.CSharp7_3);

            Assert.DoesNotContain("#nullable", source, StringComparison.Ordinal);
            Assert.Contains("public class GreetingTranslationUnit: TranslationUnit", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldEmitNullableAnnotationsForCSharp8()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", Options(("TlumachGeneratorDelayedUnitCreation", "true")), LanguageVersion.CSharp8);

            Assert.Contains("#nullable enable", source, StringComparison.Ordinal);
            Assert.Contains("private static TranslationUnit? _hello;", source, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldEmitNullableAnnotationsForLatestLanguageVersion()
        {
            string source = GenerateSingleAndCompile("Json.jsoncfg", languageVersion: LanguageVersion.Latest);

            Assert.Contains("#nullable enable", source, StringComparison.Ordinal);
            Assert.Contains("private static string? _defaultFileLocale = null;", source, StringComparison.Ordinal);
        }

        // ----------------------------------------------------------------------------------------
        // Filtering of additional files
        // ----------------------------------------------------------------------------------------

        [Fact]
        public void ShouldIgnoreAdditionalFilesThatAreNotConfigurationFiles()
        {
            // A plain text file and a translation file are not configuration files
            GeneratorRunResult result = RunGenerator(["NotAConfig.txt", "JsonStrings.json", "TomlStrings.toml", "CsvStrings.csv"]);

            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.Empty(result.GeneratedSources);
        }

        [Fact]
        public void ShouldGenerateOnlyForConfigurationFilesAmongAdditionalFiles()
        {
            GeneratorRunResult result = RunGenerator(["NotAConfig.txt", "JsonStrings.json", "Json.jsoncfg", "Toml.tomlcfg", "IniStrings.ini"]);

            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["Json.g.cs", "Toml.g.cs"], result.GeneratedSources.Select(s => s.HintName).OrderBy(s => s, StringComparer.Ordinal), StringComparer.Ordinal);

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result.GeneratedSources.Select(s => s.SourceText.ToString()));
            AssertCompiled(ok, diags);
        }

        [Fact]
        public void ShouldProduceNoSourcesWithoutAdditionalFiles()
        {
            GeneratorRunResult result = RunGenerator([]);

            Assert.Null(result.Exception);
            Assert.Empty(result.Diagnostics);
            Assert.Empty(result.GeneratedSources);
        }

        // ----------------------------------------------------------------------------------------
        // Diagnostics
        // ----------------------------------------------------------------------------------------

        [Fact]
        public void ShouldReportErrorWhenDefaultFileDoesNotExist()
        {
            GeneratorRunResult result = RunGenerator(["MissingDefaultFile.jsoncfg"]);

            Diagnostic diagnostic = AssertSingleGeneratorError(result);
            Assert.Contains("MissingDefaultFile.jsoncfg", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
            Assert.Contains("DoesNotExist.json", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldReportErrorWhenNamespaceIsMissing()
        {
            GeneratorRunResult result = RunGenerator(["NoNamespace.jsoncfg"]);

            Diagnostic diagnostic = AssertSingleGeneratorError(result);
            Assert.Contains("generatedNamespace", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
            Assert.EndsWith("NoNamespace.jsoncfg", diagnostic.Location.GetLineSpan().Path, StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldReportErrorWhenDefaultFileIsMalformed()
        {
            GeneratorRunResult result = RunGenerator(["Malformed.jsoncfg"]);

            Diagnostic diagnostic = AssertSingleGeneratorError(result);
            Assert.Contains("MalformedStrings.json", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        [Fact]
        public void ShouldKeepGeneratingOtherFilesWhenOneFails()
        {
            GeneratorRunResult result = RunGenerator(["NoNamespace.jsoncfg", "Json.jsoncfg"]);

            Assert.Null(result.Exception);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("TLUMACHGEN001", diagnostic.Id);
            GeneratedSourceResult generated = Assert.Single(result.GeneratedSources);
            Assert.Equal("Json.g.cs", generated.HintName);
        }

        // ----------------------------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------------------------

        private static void AssertClassHeader(string source, string className, string defaultFile, string parserClassName)
        {
            Assert.StartsWith("// " + defaultFile, source, StringComparison.Ordinal);
            Assert.Contains("// <auto-generated/>", source, StringComparison.Ordinal);
            Assert.Contains("namespace " + GeneratedNamespace, source, StringComparison.Ordinal);
            Assert.Contains("public sealed class " + className, source, StringComparison.Ordinal);
            Assert.Contains("new TranslationConfiguration(typeof(" + className + ").Assembly, @\"" + defaultFile + "\"", source, StringComparison.Ordinal);
            Assert.Contains("            " + parserClassName + ".Use();", source, StringComparison.Ordinal);
        }

        private static void AssertFlatKeys(string source)
        {
            Assert.Contains("public const string helloKey = \"hello\";", source, StringComparison.Ordinal);
            Assert.Contains("public const string welcomeKey = \"welcome\";", source, StringComparison.Ordinal);
            Assert.Contains("public static readonly TranslationUnit hello;", source, StringComparison.Ordinal);
            Assert.Contains("public static readonly TranslationUnit welcome;", source, StringComparison.Ordinal);
            Assert.Contains("hello = new TranslationUnit(TranslationManager, _translationConfiguration, \"hello\", false);", source, StringComparison.Ordinal);
            Assert.Contains("welcome = new TranslationUnit(TranslationManager, _translationConfiguration, \"welcome\", false);", source, StringComparison.Ordinal);
        }

        private static Diagnostic AssertSingleGeneratorError(GeneratorRunResult result)
        {
            // The failure must be reported as a diagnostic of the generator, not as an exception that escapes it (which the driver would turn into the CS8785 warning)
            Assert.Null(result.Exception);
            Assert.Empty(result.GeneratedSources);
            Diagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("TLUMACHGEN001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            return diagnostic;
        }

        private static void AssertCompiled(bool ok, ImmutableArray<Diagnostic> diags)
        {
            if (!ok)
            {
                var msg = string.Join(
                    Environment.NewLine,
                    diags.Where(d => d.Severity >= DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }

            Assert.DoesNotContain(diags, d => d.Severity == DiagnosticSeverity.Error);
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static Dictionary<string, string> Options(params (string Name, string Value)[] options)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in options)
                result["build_property." + name] = value;

            return result;
        }

        /// <summary>
        /// Runs the generator for one configuration file, checks that it produced exactly one source without diagnostics, compiles that source with the given language version, and returns it.
        /// </summary>
        private static string GenerateSingleAndCompile(string configFile, Dictionary<string, string>? options = null, LanguageVersion languageVersion = LanguageVersion.Latest, IEnumerable<string>? extraSources = null)
        {
            GeneratorRunResult result = RunGenerator([configFile], options, languageVersion);

            Assert.Null(result.Exception);
            Assert.True(result.Diagnostics.IsEmpty, "Unexpected diagnostics: " + string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString())));
            GeneratedSourceResult generated = Assert.Single(result.GeneratedSources);
            Assert.Equal(Path.GetFileNameWithoutExtension(configFile) + ".g.cs", generated.HintName);

            string source = generated.SourceText.ToString();

            // The trusted platform assemblies of the test host include Tlumach and Tlumach.Base, which the generated code references
            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(new[] { source }.Concat(extraSources ?? []), langVersion: languageVersion);
            AssertCompiled(ok, diags);

            return source;
        }

        private static GeneratorRunResult RunGenerator(string[] additionalFiles, Dictionary<string, string>? options = null, LanguageVersion languageVersion = LanguageVersion.Latest)
        {
            var parseOptions = new CSharpParseOptions(languageVersion);

            var globalOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_property.projectdir"] = TestDataDir,
            };

            if (options is not null)
            {
                foreach (var pair in options)
                    globalOptions[pair.Key] = pair.Value;
            }

            ImmutableArray<AdditionalText> texts = additionalFiles.Select(f => (AdditionalText)new FileAdditionalText(Path.Combine(TestDataDir, f))).ToImmutableArray();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [new TlumachIncrementalGenerator().AsSourceGenerator()],
                additionalTexts: texts,
                parseOptions: parseOptions,
                optionsProvider: new TestAnalyzerConfigOptionsProvider(globalOptions));

            var compilation = CSharpCompilation.Create(
                assemblyName: $"GenDriverHost_{Guid.NewGuid():N}",
                syntaxTrees: [CSharpSyntaxTree.ParseText("namespace GenDriver.Host { internal static class Empty { } }", parseOptions)],
                references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            driver = driver.RunGenerators(compilation);

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            return Assert.Single(runResult.Results);
        }

        /// <summary>
        /// An additional file that is read from the disk.
        /// </summary>
        private sealed class FileAdditionalText : AdditionalText
        {
            public FileAdditionalText(string path)
            {
                Path = path;
            }

            public override string Path { get; }

            public override SourceText? GetText(CancellationToken cancellationToken = default)
                => File.Exists(Path) ? SourceText.From(File.ReadAllText(Path)) : null;
        }

        /// <summary>
        /// Analyzer config options backed by a dictionary. The keys are case-insensitive, as they are in the options that the compiler builds from the .editorconfig and the MSBuild properties.
        /// </summary>
        private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            public static readonly TestAnalyzerConfigOptions Empty = new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            private readonly Dictionary<string, string> _options;

            public TestAnalyzerConfigOptions(Dictionary<string, string> options)
            {
                _options = options;
            }

            public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
                => _options.TryGetValue(key, out value);

            public override IEnumerable<string> Keys => _options.Keys;
        }

        private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
            {
                GlobalOptions = new TestAnalyzerConfigOptions(globalOptions);
            }

            public override AnalyzerConfigOptions GlobalOptions { get; }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
        }
    }
}
