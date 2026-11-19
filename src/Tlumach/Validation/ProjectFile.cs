// <copyright file="ProjectFile.cs" company="Allied Bits Ltd.">
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
/// A file that a project includes, as the IDE or MSBuild reports it.
/// </summary>
public sealed class ProjectFile
{
    /// <summary>
    /// The item type of the items that the Tlumach generator processes.
    /// </summary>
    public const string AdditionalFilesItemType = "AdditionalFiles";

    /// <summary>
    /// The item type of the files that a project embeds into its assembly.
    /// </summary>
    public const string EmbeddedResourceItemType = "EmbeddedResource";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectFile"/> class.
    /// </summary>
    /// <param name="fullPath">The full path of the file.</param>
    /// <param name="itemType">The MSBuild item type, such as <c>AdditionalFiles</c>, <c>EmbeddedResource</c>, or <c>None</c>.</param>
    /// <param name="link">The path at which the project shows the file, relative to the project directory, when it differs from the location of the file (the <c>Link</c> metadata of a linked file); otherwise <see langword="null"/>.</param>
    /// <param name="metadata">The MSBuild metadata of the item, such as <c>Type</c> or <c>LogicalName</c>, or <see langword="null"/> when they are not known. The validation uses them to check how ResX translations are embedded; when they are not known, that check is skipped.</param>
    public ProjectFile(string fullPath, string? itemType, string? link = null, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrEmpty(fullPath))
            throw new ArgumentException("The path of a project item must not be empty.", nameof(fullPath));

        FullPath = fullPath;
        ItemType = itemType ?? string.Empty;
        Link = link is null || string.IsNullOrWhiteSpace(link) ? null : link.Trim();

        if (metadata is not null)
        {
            Dictionary<string, string> copy = new(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> pair in metadata)
                copy[pair.Key] = pair.Value;

            Metadata = copy;
        }
    }

    /// <summary>Gets the MSBuild metadata of the item (compared without regard to case), or <see langword="null"/> when they are not known.</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>Gets a value indicating whether the project embeds the file into its assembly (the item type is <c>EmbeddedResource</c>).</summary>
    public bool IsEmbeddedResource => string.Equals(ItemType, EmbeddedResourceItemType, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the full path of the file.</summary>
    public string FullPath { get; }

    /// <summary>Gets the MSBuild item type.</summary>
    public string ItemType { get; }

    /// <summary>Gets the path at which the project shows a linked file, relative to the project directory, or <see langword="null"/> for a file that the project shows where it is. The name of an embedded resource follows this path.</summary>
    public string? Link { get; }

    /// <summary>Gets a value indicating whether the generator processes the file (the item type is <c>AdditionalFiles</c>).</summary>
    public bool IsGeneratorInput => string.Equals(ItemType, AdditionalFilesItemType, StringComparison.OrdinalIgnoreCase);
}
