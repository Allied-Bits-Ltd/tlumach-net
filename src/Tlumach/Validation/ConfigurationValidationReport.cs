// <copyright file="ConfigurationValidationReport.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Validation;

/// <summary>
/// The result of the validation of one Tlumach configuration and of the translation files that it leads to.
/// </summary>
public sealed class ConfigurationValidationReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationReport"/> class.
    /// </summary>
    /// <param name="configurationPath">The full path of the configuration file.</param>
    /// <param name="isGeneratorInput">Whether the generator processes the configuration.</param>
    public ConfigurationValidationReport(string configurationPath, bool isGeneratorInput)
    {
        ConfigurationPath = configurationPath;
        IsGeneratorInput = isGeneratorInput;
    }

    /// <summary>Gets the full path of the configuration file.</summary>
    public string ConfigurationPath { get; }

    /// <summary>Gets a value indicating whether the generator processes the configuration (it is included as <c>AdditionalFiles</c>).</summary>
    public bool IsGeneratorInput { get; }

    /// <summary>Gets or sets the full path of the default translation file, or <see langword="null"/> when it was not found.</summary>
    public string? DefaultFilePath { get; set; }

    /// <summary>Gets the translation files that were checked, the default file first.</summary>
    public IList<ValidatedFile> Files { get; } = new List<ValidatedFile>();

    /// <summary>Gets the messages about the configuration and its translation files.</summary>
    public IList<ValidationMessage> Messages { get; } = new List<ValidationMessage>();

    /// <summary>Gets the number of errors in <see cref="Messages"/>.</summary>
    public int ErrorCount => Messages.Count(m => m.Severity == ValidationSeverity.Error);

    /// <summary>Gets the number of warnings in <see cref="Messages"/>.</summary>
    public int WarningCount => Messages.Count(m => m.Severity == ValidationSeverity.Warning);

    /// <summary>Gets the number of informational messages in <see cref="Messages"/>.</summary>
    public int InfoCount => Messages.Count(m => m.Severity == ValidationSeverity.Info);
}
