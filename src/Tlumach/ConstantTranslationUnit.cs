// <copyright file="ConstantTranslationUnit.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach;

/// <summary>
/// This class can be used to present some constant string in situations where a TranslationUnit is required.
/// </summary>
public class ConstantTranslationUnit : TranslationUnit
{
    public override string CurrentValue => _constantValue!;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstantTranslationUnit"/> class.
    /// </summary>
    /// <param name="constantValue">The string value to return.</param>
    /// <param name="translationManager">A reference to some instance of <seealso cref="TranslationManager"/>. It can be any instance - it is not used by the class.</param>
    /// <param name="translationConfiguration">A reference to an instance of <seealso cref="TranslationConfiguration"/>. If <paramref name="containsPlaceholders"/> is <see langword="true"/>, this configuration's TextProcessingMode is used to process the <paramref name="constantValue"/>.</param>
    /// <param name="containsPlaceholders">Specifies whether <paramref name="constantValue"/> contains placeholders and should be processed accordingly.</param>
    public ConstantTranslationUnit(string constantValue, TranslationManager translationManager, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
        : base(constantValue, translationConfiguration, containsPlaceholders)
    {
    }

    protected override string InternalGetValueAsText(CultureInfo culture) => _constantValue!;

    protected override TranslationEntry? InternalGetEntry(CultureInfo cultureInfo)
    {
        return _constantEntry ??= new TranslationEntry(string.Empty, _constantValue);
    }
}
