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

using EnvDTE80;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Tlumach.Generator;

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

    internal static async void RegenerateIndex()
    {
        try
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
                return;

            await GeneratorRunner.RunForAllProjectsAsync(TlumachPackage.Instance!, dte);
        }
        catch (Exception ex)
        {
            IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(TlumachPackage.Instance!);
            OutputWindowHelper.Activate(pane);
            OutputWindowHelper.WriteLine(pane, "=== Tlumach: navigate to translation definition ===");
            if (ex is TaskCanceledException)
            {
                OutputWindowHelper.WriteLine(pane, "The task has been cancelled");
            }
            else
            {
                OutputWindowHelper.WriteLine(pane, "Tlumach: an exception has occurred while re-generating translation files:");
                OutputWindowHelper.WriteLine(pane, ex.Message);
            }
        }
    }

    /*internal static async Task<int> CheckAndRegenerateIndexAsync(CancellationToken cancellationToken = default)
    {
        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
        if (dte is null)
            return 7; // No

        if (!KeyIndex.IsPopulated)
        {
            if (noPromptForReIndex)
                return 7;

            var uiShell = await ServiceProvider.GetGlobalServiceAsync<SVsUIShell, IVsUIShell>(cancellationToken);//GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;

            if (uiShell == null)
                return 7; // no

            Guid clsid = Guid.Empty;
            int result = 0;

            uiShell.ShowMessageBox(
                dwCompRole: 0,
                rclsidComp: ref clsid,
                pszTitle: "Tlumach",
                pszText: "The GoTo Translation Definition function was invoked, but the translation index has not been built. Process the translations now (click Cancel to not be prompted again)?",
                pszHelpFile: string.Empty,
                dwHelpContextID: 0,
                msgbtn: OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                msgicon: OLEMSGICON.OLEMSGICON_QUERY,
                msgdefbtn: OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                fSysAlert: 0,
                pnResult: out result);

            switch (result)
            {
                case 7: // no
                    return 2;
                case 6:
                    // User clicked Yes
                    try
                    {
                        await GeneratorRunner.RunForAllProjectsAsync(TlumachPackage.Instance!, dte);
                        if (!KeyIndex.IsPopulated)
                            return 7; // no - there is still nothing in the index.
                    }
                    catch (Exception ex)
                    {
                        IVsOutputWindowPane pane = OutputWindowHelper.GetOrCreatePane(TlumachPackage.Instance!);
                        OutputWindowHelper.Activate(pane);
                        OutputWindowHelper.WriteLine(pane, "=== Tlumach: navigate to translation definition ===");
                        if (ex is TaskCanceledException)
                        {
                            OutputWindowHelper.WriteLine(pane, "The task has been cancelled");
                        }
                        else
                        {
                            OutputWindowHelper.WriteLine(pane, "Tlumach: an exception has occurred while re-generating translation files:");
                            OutputWindowHelper.WriteLine(pane, ex.Message);
                        }
                    }

                    return 6;
                case 1:
                    noPromptForReIndex = true;
                    return 0;
            }
            return result;
        }

        return 6; // already indexed

    }*/
}
