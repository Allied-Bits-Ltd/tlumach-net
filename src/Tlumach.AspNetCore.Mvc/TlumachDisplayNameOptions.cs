// <copyright file="TlumachDisplayNameOptions.cs" company="Allied Bits Ltd.">
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

using System.Reflection;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Options of <see cref="TlumachMvcBuilderExtensions.AddTlumachDisplayNames"/>.
/// </summary>
public sealed class TlumachDisplayNameOptions
{
    /// <summary>Gets or sets the text that starts every display-name key. The default is "DisplayNames.".</summary>
    public string KeyPrefix { get; set; } = "DisplayNames.";

    /// <summary>Gets or sets how the key of the container type is formed. The default is <see cref="TlumachDisplayNameKeyStyle.RelativeTypeName"/>.</summary>
    public TlumachDisplayNameKeyStyle KeyStyle { get; set; } = TlumachDisplayNameKeyStyle.RelativeTypeName;

    /// <summary>
    /// Gets or sets the function that returns the root namespace of an assembly, which <see cref="TlumachDisplayNameKeyStyle.RelativeTypeName"/> removes.
    /// The default is <see cref="DefaultRootNamespace"/>; set it when a project sets its own <c>RootNamespace</c>. Returning <see langword="null"/> removes nothing.
    /// </summary>
    public Func<Assembly, string?> RootNamespace { get; set; } = DefaultRootNamespace;

    /// <summary>Gets or sets a function that returns the key of a container type; when set, <see cref="KeyStyle"/> and <see cref="RootNamespace"/> are not used.</summary>
    public Func<Type, string>? ContainerKey { get; set; }

    /// <summary>Gets or sets the translation manager of the display names. When <see langword="null"/>, the manager of the default options of <c>AddTlumachLocalization</c> is used.</summary>
    public TranslationManager? TranslationManager { get; set; }

    /// <summary>
    /// Returns the default root namespace of an assembly: its simple name with "-" replaced by "_", which is the default <c>RootNamespace</c> of an SDK project.
    /// </summary>
    /// <param name="assembly">The assembly of a model type.</param>
    /// <returns>The root namespace, or <see langword="null"/> if the assembly has no name.</returns>
    public static string? DefaultRootNamespace(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return assembly.GetName().Name?.Replace('-', '_');
    }
}
