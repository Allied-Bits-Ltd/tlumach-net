// <copyright file="ValidationDiagnostics.cs" company="Allied Bits Ltd.">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Tlumach.Generator.Validation;

namespace Tlumach.Generator
{
    /// <summary>
    /// Reports the results of the validation of translation files as diagnostics of the generator.
    /// <para>The diagnostics have the codes of the validation (TLUMACHVAL004 and so on), so that a project can turn any of them off with NoWarn or with .editorconfig.
    /// A problem that breaks the generated class is reported by the generator itself (TLUMACHGEN001), as an error; everything that the validation finds besides is a warning,
    /// or an informational message for the keys that the default file or a translation lacks.</para>
    /// </summary>
    internal static class ValidationDiagnostics
    {
        private const string Category = "TlumachValidation";

        private const string HelpLink = "https://alliedbits.com/tlumach/articles/validation.html";

        private static readonly DiagnosticDescriptor ReferencedFileMissingDescriptor = new(
            id: ValidationCodes.ReferencedFileMissing,
            title: "A translation file named in the configuration does not exist",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor TranslationUnparsableDescriptor = new(
            id: ValidationCodes.TranslationUnparsable,
            title: "A translation file cannot be parsed",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor TemplateInvalidDescriptor = new(
            id: ValidationCodes.TemplateInvalid,
            title: "The text of a translation entry cannot be formatted",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor KeyNotInDefaultDescriptor = new(
            id: ValidationCodes.KeyNotInDefault,
            title: "A translation contains keys that the default file lacks",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor TranslationEmptyDescriptor = new(
            id: ValidationCodes.TranslationEmpty,
            title: "A translation file is empty or yields no translation",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor UnknownLocaleDescriptor = new(
            id: ValidationCodes.UnknownLocale,
            title: "A locale of the translations section is not a known culture",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor FileShadowedDescriptor = new(
            id: ValidationCodes.FileShadowed,
            title: "A translation file is never loaded",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor NoParserDescriptor = new(
            id: ValidationCodes.NoParser,
            title: "No parser reads a file named in the configuration",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor KeyMissingFromTranslationDescriptor = new(
            id: ValidationCodes.KeyMissingFromTranslation,
            title: "A translation lacks keys of the default file",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly DiagnosticDescriptor ResxNotEmbeddedAsTextDescriptor = new(
            id: ValidationCodes.ResxNotEmbeddedAsText,
            title: "A ResX file is embedded in a way that the translation manager cannot read",
            messageFormat: "{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink);

        private static readonly Dictionary<string, DiagnosticDescriptor> Descriptors = new(StringComparer.Ordinal)
        {
            [ValidationCodes.ReferencedFileMissing] = ReferencedFileMissingDescriptor,
            [ValidationCodes.TranslationUnparsable] = TranslationUnparsableDescriptor,
            [ValidationCodes.TemplateInvalid] = TemplateInvalidDescriptor,
            [ValidationCodes.KeyNotInDefault] = KeyNotInDefaultDescriptor,
            [ValidationCodes.TranslationEmpty] = TranslationEmptyDescriptor,
            [ValidationCodes.UnknownLocale] = UnknownLocaleDescriptor,
            [ValidationCodes.FileShadowed] = FileShadowedDescriptor,
            [ValidationCodes.NoParser] = NoParserDescriptor,
            [ValidationCodes.KeyMissingFromTranslation] = KeyMissingFromTranslationDescriptor,
            [ValidationCodes.ResxNotEmbeddedAsText] = ResxNotEmbeddedAsTextDescriptor,
        };

        /// <summary>
        /// Gets the descriptors of all diagnostics that the validation reports through the generator.
        /// </summary>
        internal static IEnumerable<DiagnosticDescriptor> All => Descriptors.Values;

        /// <summary>
        /// Validates the translation files of a configuration that the generator has processed and reports the problems.
        /// </summary>
        /// <param name="context">The context of the generator.</param>
        /// <param name="configurationPath">The configuration file.</param>
        /// <param name="projectDirectory">The directory of the project.</param>
        /// <param name="defaultFileFailed">Whether the generator has failed to process the configuration; the generator has reported that problem, so the default file is not checked again.</param>
        internal static void Report(SourceProductionContext context, string configurationPath, string projectDirectory, bool defaultFileFailed)
        {
            ConfigurationValidationReport report;
#pragma warning disable CA1031 // The validation must never make the generation fail
            try
            {
                report = TranslationValidator.ValidateForGenerator(configurationPath, projectDirectory, defaultFileFailed, context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return;
            }
#pragma warning restore CA1031

            foreach (ValidationMessage message in report.Messages)
            {
                // The problems with the configuration, the default file, and the settings of the generator are reported by the generator itself
                if (!Descriptors.TryGetValue(message.Code, out DiagnosticDescriptor? descriptor))
                    continue;

                string path = message.FilePath ?? configurationPath;
                LinePosition position = new(Math.Max(message.Line - 1, 0), Math.Max(message.Column - 1, 0));
                Location location = Location.Create(path, default(TextSpan), new LinePositionSpan(position, position));

                context.ReportDiagnostic(Diagnostic.Create(descriptor, location, message.Message));
            }
        }
    }
}
