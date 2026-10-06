// <copyright file="TlumachFluentValidationOptions.cs" company="Allied Bits Ltd.">
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
/// The options of <see cref="TlumachFluentValidationServiceCollectionExtensions.AddTlumachFluentValidation"/>.
/// </summary>
public sealed class TlumachFluentValidationOptions : TlumachLanguageManagerOptions
{
    /// <summary>
    /// Gets or sets the translation manager that provides the messages, typically <c>Strings.TranslationManager</c> of a class created by Generator. Required.
    /// </summary>
    public TranslationManager? TranslationManager { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="TlumachDisplayNameResolver"/> is installed as <c>ValidatorOptions.Global.DisplayNameResolver</c>. The default is <see langword="false"/>.
    /// </summary>
    public bool UseDisplayNameResolver { get; set; }

    /// <summary>
    /// Gets or sets the group, in which the display names are stored, when <see cref="UseDisplayNameResolver"/> is set. The default is <c>"DisplayNames"</c>.
    /// </summary>
    public string? DisplayNamesGroup { get; set; } = "DisplayNames";
}
