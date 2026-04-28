// <copyright file="GoToTranslationDefinitionCommand.cs" company="Allied Bits Ltd.">
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
using System.Threading;
using System.Threading.Tasks;

using AlliedBits.Tlumach.Extension.VisualStudio.Navigation;

using EnvDTE80;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Tlumach.Generator;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Commands;

/// <summary>
/// "Go To Translation Definition" command — appears in the Tlumach submenu of the Extensions
/// menu (new SDK path) and navigates to the translation key under the caret when it matches
/// a known key in <see cref="KeyIndex"/>.
/// The old SDK path registers the same logical action in the editor context menu via
/// <see cref="TlumachPackage"/>'s OleMenuCommand handler.
/// </summary>
[VisualStudioContribution]
internal sealed class GoToTranslationDefinitionCommand : Command
{
    private static bool noPromptForReIndex = false;

    // TlumachSubMenuGroup (0x2010) inside the VSCT-defined "Tlumach" submenu under Extensions menu.
    private static readonly CommandPlacement TlumachSubMenuPlacement =
        CommandPlacement.VsctParent(new Guid("A1B2C3D4-E5F6-7890-ABCD-EF0123456789"), 0x2010u, 255);

    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration => new("%Commands.GoToTranslationDefinition.DisplayName%")
    {
        Icon = new(ImageMoniker.KnownValues.GoToDefinition, IconSettings.IconAndText),
        Placements = [TlumachSubMenuPlacement],
        TooltipText = "%Commands.GoToTranslationDefinition.ToolTip%",
    };

    /// <inheritdoc />
    public GoToTranslationDefinitionCommand(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
    }

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
        if (dte is null)
            return;

        var (ns, className, identifier) = SymbolExtractor.ExtractSymbolFromDte(dte);
        if (string.IsNullOrEmpty(identifier))
            return;

        /*int outcome = await ProjectHelper.CheckAndRegenerateIndexAsync(cancellationToken);
        if (outcome != 1)
            return; // User chose not to re-index, or index regeneration failed*/

        if (!KeyIndex.IsPopulated)
        {
            if (noPromptForReIndex)
                return;

            var uiShell = await ServiceProvider.GetGlobalServiceAsync<SVsUIShell, IVsUIShell>();//GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;

            if (uiShell == null)
                return; // no

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
                case 7: // IDNO
                    return;
                case 6: // IDYES
                    // User clicked Yes
                    try
                    {
                        await GeneratorRunner.RunForAllProjectsAsync(TlumachPackage.Instance!, dte);
                        if (!KeyIndex.IsPopulated)
                            return; // no - there is still nothing in the index.
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
                        return;
                    }

                    break;
                case 2: // IDCANCEL
                    noPromptForReIndex = true;
                    return;
            }
        }


        KeyLocation? location = KeyIndex.FindDeclaration(ns, className, identifier!);
        if (location is null)
            return;

        await TranslationNavigator.NavigateToAsync(TlumachPackage.Instance!, location).ConfigureAwait(true);
    }
}
