// <copyright file="TlumachMudBlazorOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.MudBlazor;

/// <summary>
/// The options of <see cref="TlumachMudBlazorServiceCollectionExtensions.AddTlumachMudBlazor"/>.
/// </summary>
public sealed class TlumachMudBlazorOptions
{
    /// <summary>
    /// The default value of <see cref="Group"/>.
    /// </summary>
    public const string DefaultGroup = "MudBlazor";

    /// <summary>
    /// Gets or sets the translation manager that holds the texts of MudBlazor, for example <c>Strings.TranslationManager</c> of a class created by Generator.
    /// <para>When it is <see langword="null"/>, the default manager of Tlumach.Blazor (<c>TlumachBlazorOptions.DefaultManager</c>) is used.</para>
    /// </summary>
    public TranslationManager? TranslationManager { get; set; }

    /// <summary>
    /// Gets or sets the group of the translation, in which the keys of MudBlazor, such as <c>MudDataGrid_Contains</c>, are stored.
    /// A key is looked up as <c>{Group}.{key}</c>, or as <c>{key}</c> when this property is <see langword="null"/> or empty.
    /// <para>The default value is <c>"MudBlazor"</c>. In a JSON translation file, that is an object named <c>MudBlazor</c> at the root.
    /// Set the property to <see langword="null"/> for a translation set that contains only the keys of MudBlazor, such as one made of the .resx files of MudBlazor.Translations.</para>
    /// </summary>
    public string? Group { get; set; } = DefaultGroup;
}
