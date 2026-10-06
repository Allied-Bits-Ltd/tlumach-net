// <copyright file="TlumachLanguageManagerOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.FluentValidation;

/// <summary>
/// The options of <see cref="TlumachLanguageManager"/>. The language manager copies the values when it is created.
/// </summary>
public class TlumachLanguageManagerOptions
{
    /// <summary>
    /// Gets or sets the group of the translation, in which the keys of FluentValidation are stored, for example <c>NotEmptyValidator</c> and the error codes set with
    /// <c>WithErrorCode</c>. A key is looked up as <c>{FluentValidationGroup}.{key}</c>, or as <c>{key}</c> when this property is <see langword="null"/> or empty.
    /// <para>The default value is <c>"FluentValidation"</c>. In a JSON translation file, that is an object named <c>FluentValidation</c> at the root, and Generator creates the
    /// nested class <c>Strings.FluentValidation</c> for it.</para>
    /// </summary>
    public string? FluentValidationGroup { get; set; } = "FluentValidation";

    /// <summary>
    /// Gets or sets the culture source used when neither the caller nor <c>LanguageManager.Culture</c> specifies a culture.
    /// The default value is <see cref="MessageCultureSource.CurrentUICulture"/>.
    /// </summary>
    public MessageCultureSource CultureSource { get; set; } = MessageCultureSource.CurrentUICulture;
}
