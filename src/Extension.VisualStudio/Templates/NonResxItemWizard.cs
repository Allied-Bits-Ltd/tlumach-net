// <copyright file="NonResxItemWizard.cs" company="Allied Bits Ltd.">
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

using System;
using System.Collections.Generic;
using System.Diagnostics;

using EnvDTE;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Templates;

/// <summary>
/// Template wizard for the "Tlumach Translation File (ResX)" item template.
/// <para>
/// A .vstemplate can set the MSBuild item type of a generated file (<c>ItemType</c> on the
/// ProjectItem element), but it cannot set item metadata.  A ResX translation file needs
/// <c>&lt;Type&gt;Non-Resx&lt;/Type&gt;</c>, otherwise the .NET toolchain compiles the file into a
/// .resources stream (and, because the name contains a dot, moves it into a satellite assembly)
/// instead of embedding the text that Tlumach reads.  This wizard stamps that metadata on the
/// item right after the template engine has added it.
/// </para>
/// <para>
/// The wizard never throws: a failure here must not abort the Add New Item operation.  The file
/// is still created and can be fixed by hand as described in the Files and Formats documentation.
/// </para>
/// </summary>
public sealed class NonResxItemWizard : IWizard
{
    private const string TypeMetadataName = "Type";
    private const string NonResxMetadataValue = "Non-Resx";

    /// <inheritdoc />
    public void RunStarted(
        object automationObject,
        Dictionary<string, string> replacementsDictionary,
        WizardRunKind runKind,
        object[] customParams)
    {
        // Nothing to prepare; all work happens once the item exists.
    }

    /// <inheritdoc />
    public bool ShouldAddProjectItem(string filePath) => true;

    /// <inheritdoc />
    public void ProjectItemFinishedGenerating(ProjectItem projectItem)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031 // A wizard failure must not abort the Add New Item operation
        try
        {
            if (projectItem is null || projectItem.FileCount < 1)
                return;

            SetNonResxType(projectItem.ContainingProject, projectItem.FileNames[1]); // COM collection is 1-based
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Tlumach: could not mark the ResX translation file as Non-Resx: {ex.Message}");
        }
#pragma warning restore CA1031
    }

    /// <inheritdoc />
    public void ProjectFinishedGenerating(Project project)
    {
        // Item templates do not generate projects.
    }

    /// <inheritdoc />
    public void BeforeOpeningFile(ProjectItem projectItem)
    {
        // Nothing to do.
    }

    /// <inheritdoc />
    public void RunFinished()
    {
        // Nothing to do.
    }

    private static void SetNonResxType(Project? project, string? filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (project is null || string.IsNullOrEmpty(filePath))
            return;

        var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
        if (solution is null)
            return;

        if (ErrorHandler.Failed(solution.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy hierarchy))
            || hierarchy is null)
            return;

        if (hierarchy is not IVsProject vsProject || hierarchy is not IVsBuildPropertyStorage storage)
            return;

        var priority = new VSDOCUMENTPRIORITY[1];
        if (ErrorHandler.Failed(vsProject.IsDocumentInProject(filePath, out int found, priority, out uint itemId))
            || found == 0
            || itemId == (uint)VSConstants.VSITEMID.Nil)
        {
            return;
        }

        storage.SetItemAttribute(itemId, TypeMetadataName, NonResxMetadataValue);
    }
}
