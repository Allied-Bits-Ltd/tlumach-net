// <copyright file="ProjectHelper.cs" company="Allied Bits Ltd.">
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

using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Helpers for querying VS project state used by command visibility checks.
/// </summary>
internal static class ProjectHelper
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="project"/> contains at least one
    /// file with an AdditionalFiles item type that matches a known Tlumach config extension.
    /// </summary>
    /// <param name="project">The VS project to inspect.</param>
    /// <returns><see langword="true"/> if any Tlumach config files are found.</returns>
    internal static bool HasTlumachConfigFiles(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return GeneratorRunner.CollectAdditionalTlumachFiles(project).Count > 0;
    }
}
