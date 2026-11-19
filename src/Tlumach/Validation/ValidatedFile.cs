// <copyright file="ValidatedFile.cs" company="Allied Bits Ltd.">
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

#if GENERATOR
namespace Tlumach.Generator.Validation;
#else
namespace Tlumach.Validation;
#endif

/// <summary>
/// A translation file checked by <see cref="TranslationValidator"/>.
/// </summary>
public sealed class ValidatedFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatedFile"/> class.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="origin">How the file was found.</param>
    /// <param name="locale">The locale for which the file is meant (the culture in its name, or the locale given in the configuration), or an empty string for the default file and for the <c>other</c> entry.</param>
    public ValidatedFile(string filePath, TranslationFileOrigin origin, string locale)
    {
        FilePath = filePath;
        Origin = origin;
        Locale = locale;
    }

    /// <summary>Gets the full path of the file.</summary>
    public string FilePath { get; }

    /// <summary>Gets how the file was found.</summary>
    public TranslationFileOrigin Origin { get; }

    /// <summary>Gets the locale for which the file is meant, or an empty string for the default file and for the <c>other</c> entry of the configuration.</summary>
    public string Locale { get; }

    /// <summary>Gets the locales whose translations were loaded from the file (several for the formats that keep many locales in one file).</summary>
    public IList<string> CheckedLocales { get; } = new List<string>();

    /// <summary>Gets or sets the number of entries loaded from the file (for a file with several locales, the largest number among them).</summary>
    public int EntryCount { get; set; }

    /// <summary>Gets or sets the number of errors found in the file.</summary>
    public int ErrorCount { get; set; }

    /// <summary>Gets or sets the number of warnings about the file.</summary>
    public int WarningCount { get; set; }

    /// <summary>Gets or sets the number of informational messages about the file.</summary>
    public int InfoCount { get; set; }
}
