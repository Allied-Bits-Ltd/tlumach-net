// <copyright file="TlumachSyncfusionBlazorOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Syncfusion.Blazor;

/// <summary>
/// The options of <see cref="TlumachSyncfusionBlazorServiceCollectionExtensions.AddTlumachSyncfusionBlazor"/>.
/// </summary>
public sealed class TlumachSyncfusionBlazorOptions
{
    /// <summary>
    /// The default value of <see cref="Group"/>.
    /// </summary>
    public const string DefaultGroup = "Syncfusion";

    /// <summary>
    /// Gets or sets the translation manager that holds the texts of Syncfusion components, for example <c>Strings.TranslationManager</c> of a class created by Generator.
    /// <para>When it is <see langword="null"/>, the default manager of Tlumach.Blazor (<c>TlumachBlazorOptions.DefaultManager</c>) is used.</para>
    /// </summary>
    public TranslationManager? TranslationManager { get; set; }

    /// <summary>
    /// Gets or sets the group of the translation, in which the keys of Syncfusion, such as <c>Grid_EmptyRecord</c>, are stored.
    /// A key is looked up as <c>{Group}.{key}</c>, or as <c>{key}</c> when this property is <see langword="null"/> or empty.
    /// <para>The default value is <c>"Syncfusion"</c>. In a JSON translation file, that is an object named <c>Syncfusion</c> at the root.</para>
    /// </summary>
    public string? Group { get; set; } = DefaultGroup;

    /// <summary>
    /// Gets or sets an optional second translation manager, which is asked for the keys that <see cref="TranslationManager"/> does not have,
    /// for example a set made of the official <c>SfResources.*.resx</c> files of Syncfusion.
    /// <para>A text of the culture of the user (or its parent culture) in this set is preferred to a text of the default file of <see cref="TranslationManager"/>.</para>
    /// </summary>
    public TranslationManager? FallbackTranslationManager { get; set; }

    /// <summary>
    /// Gets or sets the group of <see cref="FallbackTranslationManager"/>, in which the keys are stored.
    /// A key is looked up as <c>{FallbackGroup}.{key}</c>, or as <c>{key}</c> when this property is <see langword="null"/> or empty.
    /// <para>The default value is <see langword="null"/>, as in the <c>SfResources.*.resx</c> files, which have the keys at the root.</para>
    /// </summary>
    public string? FallbackGroup { get; set; }
}
