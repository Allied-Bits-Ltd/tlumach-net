// <copyright file="TlumachModelBindingOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Options of <see cref="TlumachMvcBuilderExtensions.AddTlumachModelBindingMessages"/>.
/// </summary>
public sealed class TlumachModelBindingOptions
{
    /// <summary>
    /// Gets or sets the text prepended to the name of a message to form its key, e.g. "ModelBinding." for "ModelBinding.AttemptedValueIsInvalid". The default is "ModelBinding.".
    /// </summary>
    public string KeyPrefix { get; set; } = "ModelBinding.";

    /// <summary>
    /// Gets or sets the translation manager of the messages. When <see langword="null"/>, the manager of the default options of <c>AddTlumachLocalization</c> is used.
    /// </summary>
    public TranslationManager? TranslationManager { get; set; }
}
