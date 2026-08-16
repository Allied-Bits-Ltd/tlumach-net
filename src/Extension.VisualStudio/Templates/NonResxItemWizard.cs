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
using System.Globalization;
using System.IO;

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
/// The wizard also corrects the name of the generated file.  The Add New Item dialog makes a name
/// unique by appending a number to everything that precedes the last extension, so the duplicate
/// extension of the default name yields <c>Translation.resx1.resx</c> rather than
/// <c>Translation1.resx.resx</c>.  The name is normalised here rather than in the template,
/// because the dialog needs a <c>DefaultName</c> that carries both extensions in order to
/// recognise the files that are already in the folder.
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
    private const string ResxExtension = ".resx";
    private const string DoubleExtension = ".resx.resx";

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

            NormalizeFileName(projectItem);

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

    /// <summary>
    /// Renames the generated item so that its name ends with the duplicate extension and any
    /// number that the Add New Item dialog appended sits in front of it.
    /// </summary>
    /// <param name="projectItem">The item that the template engine has just added.</param>
    private static void NormalizeFileName(ProjectItem projectItem)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        string currentPath = projectItem.FileNames[1]; // COM collection is 1-based
        string currentName = Path.GetFileName(currentPath);
        string baseName = GetBaseName(currentName);

        string? directory = Path.GetDirectoryName(currentPath);
        if (string.IsNullOrEmpty(directory))
            return;

        // The dialog made the name unique among the names it saw, but it compared them before the
        // number was moved, so the corrected name may be taken by a file that was added earlier.
        // Numbering then continues from the name without the number, to keep "Translation2.resx.resx"
        // from turning into "Translation12.resx.resx".
        string stem = baseName.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        string candidate = baseName + DoubleExtension;
        for (int index = 2; File.Exists(Path.Combine(directory, candidate)); index++)
        {
            if (string.Equals(Path.Combine(directory, candidate), currentPath, StringComparison.OrdinalIgnoreCase))
                return; // the file already carries the name we want

            candidate = stem + index.ToString(CultureInfo.InvariantCulture) + DoubleExtension;
        }

        if (!string.Equals(currentName, candidate, StringComparison.Ordinal))
            projectItem.Name = candidate;
    }

    /// <summary>
    /// Strips the extensions and the trailing number from a generated file name, and puts the
    /// number back at the end of the remaining name.
    /// </summary>
    /// <param name="fileName">The file name produced by the Add New Item dialog.</param>
    /// <returns>The name without any extension, ending with the number if there was one.</returns>
    private static string GetBaseName(string fileName)
    {
        // "Translation.resx1.resx" -> "Translation.resx1"; a name the user typed may lack the extension.
        string name = fileName.EndsWith(ResxExtension, StringComparison.OrdinalIgnoreCase)
            ? fileName.Substring(0, fileName.Length - ResxExtension.Length)
            : fileName;

        // "Translation.resx1" -> "Translation.resx" + "1"
        int digitStart = name.Length;
        while (digitStart > 0 && char.IsDigit(name[digitStart - 1]))
            digitStart--;

        string number = name.Substring(digitStart);
        string head = name.Substring(0, digitStart);

        // "Translation.resx" + "1" -> "Translation" + "1"
        if (head.EndsWith(ResxExtension, StringComparison.OrdinalIgnoreCase))
            head = head.Substring(0, head.Length - ResxExtension.Length);

        return head + number;
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
