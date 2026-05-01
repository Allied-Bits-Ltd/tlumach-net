// <copyright file="RunGeneratorCommand.cs" company="Allied Bits Ltd.">
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

using System.Threading;
using System.Threading.Tasks;

using EnvDTE80;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Commands;

/// <summary>
/// "Run Tlumach Generator" command — appears in the Tlumach submenu of the Extensions menu
/// (new SDK path) and runs the Tlumach source generator for the selected project.
/// The old SDK path registers the same logical action in the project context menu via
/// <see cref="TlumachPackage"/>'s OleMenuCommand handler.
/// </summary>
[VisualStudioContribution]
internal sealed class RunGeneratorCommand : Command
{
    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration => new("%Commands.RunGenerator.DisplayName%")
    {
        Icon = new(ImageMoniker.Custom("AlliedBits.Tlumach.Extension.VisualStudio.Commands.RunGeneratorCommand.png"), IconSettings.IconAndText),
        Placements = [],  // submenu entry is the VSCT button RunGeneratorSubMenuId; new-SDK handles context menu via OleMenuCommand
        TooltipText = "%Commands.RunGenerator.ToolTip%",
    };

    /// <inheritdoc />
    public RunGeneratorCommand(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
    }

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
        EnvDTE.Project? project = dte?.SelectedItems?.Item(1)?.Project;

        if (project is null)
        {
            if (TlumachPackage.Instance is { } pkg)
                OutputWindowHelper.WriteLineOnUIThread(pkg, "Tlumach Generator: no project selected.");
            return;
        }

        _ = Task.Run(() => GeneratorRunner.RunForProjectAsync(TlumachPackage.Instance!, project), cancellationToken);
    }
}
