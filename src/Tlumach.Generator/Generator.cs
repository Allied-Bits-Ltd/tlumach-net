// <copyright file="Generator.cs" company="Allied Bits Ltd.">
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

using System.Diagnostics;

using Microsoft.CodeAnalysis;

#if !GENERATOR
using Tlumach.Base;
#endif

namespace Tlumach.Generator
{
    /// <summary>
    /// Generates source code files with translation units from all configuration files found in the project.
    /// </summary>
    [Generator]
    public sealed class Generator : BaseGenerator, IIncrementalGenerator
    {
        internal static class Diags
        {
            internal static readonly DiagnosticDescriptor TlumachGenError = new(
                id: "TLUMACHGEN001",
                title: "Tlumach generator failed",
                messageFormat: "Failed while processing {0}: {1}",
                category: "TlumachGenerator",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            internal static readonly DiagnosticDescriptor TlumachGenInfo = new(
                id: "TLUMACHGEN100",
                title: "Tlumach generator info",
                messageFormat: "{0}",
                category: "TlumachGenerator",
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true);
        }

        private static void InitializeParsers()
        {
            JsonParser.Use();
            ArbParser.Use();
            IniParser.Use();
            TomlParser.Use();
            CsvParser.Use();
            TsvParser.Use();
            ResxParser.Use();
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            InitializeParsers();

            IncrementalValueProvider<Dictionary<string, string>> optionsProvider = context.AnalyzerConfigOptionsProvider
                .Select(static (provider, _) =>
                {
                    Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string? value;

                    // Keys are case-insensitive; the conventional key is build_property.<Name>
                    provider.GlobalOptions.TryGetValue("build_property.TlumachGenerator" + OPTION_USING_NAMESPACE, out value);
                    if (value?.Length > 0)
                        result.Add(OPTION_USING_NAMESPACE, value);
                    provider.GlobalOptions.TryGetValue("build_property.TlumachGenerator" + OPTION_EXTRA_PARSERS, out value);
                    if (value?.Length > 0)
                        result.Add(OPTION_EXTRA_PARSERS, value);
                    provider.GlobalOptions.TryGetValue("build_property.TlumachGenerator" + OPTION_DELAYED_UNITS, out value);
                    if (value?.Length > 0)
                        result.Add(OPTION_DELAYED_UNITS, value);
                    provider.GlobalOptions.TryGetValue("build_property.projectdir", out value);
                    if (value?.Length > 0)
                        result.Add("projectdir", value);

                    return result;
                });

            // Scan the project directory for translation configuration files and put down their list
            IncrementalValuesProvider<AdditionalText> translationFiles = context.AdditionalTextsProvider.Where(IsTranslationConfigurationFile);

            var combinedProvider = translationFiles.Combine(optionsProvider);

            context.RegisterSourceOutput(combinedProvider, static (spc, source) =>
            {
                AdditionalText text = source.Left;
                try
                {
                    Dictionary<string, string> options = source.Right;
                    string projectDir = string.Empty;
                    options.TryGetValue("projectdir", out projectDir);

                    var fileNameOnly = Path.GetFileNameWithoutExtension(text.Path);
                    var content = GenerateClass(text.Path, projectDir, options);

                    if (content is not null)
                        spc.AddSource($"{fileNameOnly}.g.cs", content);
                }
                catch (Exception ex)
                {
                    ReportError(spc, text.Path, ex);
                }
            });
        }

        private static void ReportError(SourceProductionContext spc, string path, Exception ex)
        {
            string fileName = Path.GetFileName(path);
            if (ex is TextFileParseException fpex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diags.TlumachGenError,
                    Location.Create(
                        fpex.FileName,
                        new Microsoft.CodeAnalysis.Text.TextSpan(fpex.StartPosition, fpex.EndPosition - fpex.StartPosition + 1),
                        new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                            new Microsoft.CodeAnalysis.Text.LinePosition(fpex.LineNumber, fpex.ColumnNumber),
                            new Microsoft.CodeAnalysis.Text.LinePosition(fpex.LineNumber, fpex.ColumnNumber))),
                    /* arg0 */ Path.GetFileName(fpex.FileName),
                    /* arg1 */ fpex.Message));
            }
            else
            if (ex is TextParseException pex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diags.TlumachGenError,
                    Location.Create(
                        path,
                        new Microsoft.CodeAnalysis.Text.TextSpan(pex.StartPosition, pex.EndPosition - pex.StartPosition + 1),
                        new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                            new Microsoft.CodeAnalysis.Text.LinePosition(pex.LineNumber, pex.ColumnNumber),
                            new Microsoft.CodeAnalysis.Text.LinePosition(pex.LineNumber, pex.ColumnNumber))),
                    /* arg0 */ fileName,
                    /* arg1 */ pex.Message));
            }
            else
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diags.TlumachGenError,
                    Location.Create(path, default(Microsoft.CodeAnalysis.Text.TextSpan), default(Microsoft.CodeAnalysis.Text.LinePositionSpan)),
                    /* arg0 */ fileName,
                    /* arg1 */ ex.Message));
            }
        }

        private static bool IsTranslationConfigurationFile(AdditionalText? candidateFile)
        {
            if (candidateFile is null)
                return false;

            string? filename = candidateFile.Path;
            BaseParser? parser = FileFormats.GetConfigParser(Path.GetExtension(filename.ToLowerInvariant()));

            return parser is not null;
        }
    }
}
