// <copyright file="UntranslatedUnit.cs" company="Allied Bits Ltd.">
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

using System;
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.WinUI;

/// <summary>
/// This class can be used to present some constant string in situations where a TranslationUnit is required.
/// </summary>
public class UntranslatedUnit : Tlumach.WinUI.TranslationUnit
{
    private TranslationEntry? _sourceEntry;

    private string? _sourceValue;

    /// <summary>
    /// Gets or sets the string value that this translation unit exposes.
    /// </summary>
    public string? SourceValue
    {
        get => _sourceValue;
        set
        {
            if (!string.Equals(_sourceValue, value, StringComparison.Ordinal))
            {
                _sourceValue = value;
                _sourceEntry = null;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UntranslatedUnit"/> class.
    /// </summary>
    /// <param name="sourceValue">The string value to return.</param>
    /// <param name="translationManager">A reference to some instance of <seealso cref="TranslationManager"/>. It can be any instance - it is not used by the class.</param>
    /// <param name="translationConfiguration">A reference to an instance of <seealso cref="TranslationConfiguration"/>. If <paramref name="containsPlaceholders"/> is <see langword="true"/>, this configuration's TextProcessingMode is used to process the <paramref name="sourceValue"/>.</param>
    /// <param name="containsPlaceholders">Specifies whether <paramref name="sourceValue"/> contains placeholders and should be processed accordingly.</param>
    public UntranslatedUnit(string sourceValue, TranslationManager translationManager, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
        : base(translationManager, translationConfiguration, containsPlaceholders)
    {
        _sourceValue = sourceValue;
    }

    protected override string InternalGetValueAsText(CultureInfo culture) => _sourceValue!;

    protected override TranslationEntry? InternalGetEntry(CultureInfo cultureInfo)
    {
        return _sourceEntry ??= new TranslationEntry(string.Empty, _sourceValue);
    }
}
