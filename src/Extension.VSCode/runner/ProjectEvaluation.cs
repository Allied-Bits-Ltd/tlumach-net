// <copyright file="ProjectEvaluation.cs" company="Allied Bits Ltd.">
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

using Tlumach.Validation;

namespace AlliedBits.Tlumach.Extension.Runner;

/// <summary>
/// The files of a project and how they were found.
/// </summary>
internal sealed class ProjectEvaluation
{
    public ProjectEvaluation(IReadOnlyList<ProjectFile> items, string? note)
    {
        Items = items;
        Note = note;
    }

    /// <summary>Gets the files of the project.</summary>
    public IReadOnlyList<ProjectFile> Items { get; }

    /// <summary>Gets a note for the user when the files were found in a less precise way, or <see langword="null"/>.</summary>
    public string? Note { get; }
}
