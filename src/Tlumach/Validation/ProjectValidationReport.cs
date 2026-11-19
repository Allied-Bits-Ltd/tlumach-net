// <copyright file="ProjectValidationReport.cs" company="Allied Bits Ltd.">
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
/// The result of the validation of the translations of one project.
/// </summary>
public sealed class ProjectValidationReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectValidationReport"/> class.
    /// </summary>
    /// <param name="projectPath">The full path of the project file.</param>
    public ProjectValidationReport(string projectPath)
    {
        ProjectPath = projectPath;
    }

    /// <summary>Gets the full path of the project file.</summary>
    public string ProjectPath { get; }

    /// <summary>Gets the reports of the Tlumach configurations of the project.</summary>
    public IList<ConfigurationValidationReport> Configurations { get; } = new List<ConfigurationValidationReport>();

    /// <summary>Gets the files that have the extension of a configuration file but are not Tlumach configurations (they are not generator inputs, and they do not parse as a configuration with a default file).</summary>
    public IList<string> IgnoredFiles { get; } = new List<string>();

    /// <summary>Gets all messages of all configurations.</summary>
    public IEnumerable<ValidationMessage> Messages => Configurations.SelectMany(c => c.Messages);

    /// <summary>Gets the number of errors.</summary>
    public int ErrorCount => Configurations.Sum(c => c.ErrorCount);

    /// <summary>Gets the number of warnings.</summary>
    public int WarningCount => Configurations.Sum(c => c.WarningCount);

    /// <summary>Gets the number of informational messages.</summary>
    public int InfoCount => Configurations.Sum(c => c.InfoCount);

    /// <summary>Gets the number of translation files that were checked.</summary>
    public int FileCount => Configurations.Sum(c => c.Files.Count);
}
