// <copyright file="RunAllGeneratorsCommand.cs" company="Allied Bits Ltd.">
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

using EnvDTE80;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Shell;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Commands;

/// <summary>
/// "Run Tlumach Generator (All Projects)" command — appears in the Tlumach submenu of the
/// Extensions menu (new SDK path) and runs the generator for every project in the solution.
/// The old SDK path registers the same logical action in the solution and project context
/// menus via <see cref="TlumachPackage"/>'s OleMenuCommand handler.
/// </summary>
[VisualStudioContribution]
internal sealed class RunAllGeneratorsCommand : Command
{
    /// <inheritdoc />
    public override CommandConfiguration CommandConfiguration => new("%Commands.RunAllGenerators.DisplayName%")
    {
        Icon = new(ImageMoniker.Custom("AlliedBits.Tlumach.Extension.VisualStudio.Commands.RunAllGeneratorsCommand.png"), IconSettings.IconAndText),
        Placements = [],   // handled by VSCT button + OleMenuCommand; no new-SDK placement needed
        TooltipText = "%Commands.RunAllGenerators.ToolTip%",
        VisibleWhen = ActivationConstraint.SolutionState(SolutionState.Exists),
        EnabledWhen = ActivationConstraint.SolutionState(SolutionState.Exists),
    };

    /// <inheritdoc />
    public RunAllGeneratorsCommand(VisualStudioExtensibility extensibility)
        : base(extensibility)
    {
    }

    /// <inheritdoc />
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (TlumachPackage.Instance is not { } pkg)
            {
                ActivityLog.TryLogError(
                    nameof(RunAllGeneratorsCommand),
                    "TlumachPackage.Instance is null — package may not have initialised. " +
                    "Run Generator (All Projects) command cannot proceed.");
                return;
            }

            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                ActivityLog.TryLogError(
                    nameof(RunAllGeneratorsCommand),
                    "DTE service is null — cannot enumerate projects.");
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await GeneratorRunner.RunForAllProjectsAsync(pkg, dte).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // VS is shutting down — ignore
                }
                catch (Exception ex)
                {
                    ActivityLog.TryLogError(
                        nameof(RunAllGeneratorsCommand),
                        $"RunForAllProjectsAsync failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected during VS shutdown
        }
        catch (Exception ex)
        {
            ActivityLog.TryLogError(
                nameof(RunAllGeneratorsCommand),
                $"ExecuteCommandAsync failed: {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
        }
#pragma warning restore CA1031
    }
}
