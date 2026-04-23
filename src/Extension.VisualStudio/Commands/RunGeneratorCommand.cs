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

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Commands;

/// <summary>
/// "Run Tlumach Generator" command — appears in the Solution Explorer project
/// context menu when the selected project contains Tlumach config files.
/// </summary>
internal sealed class RunGeneratorCommand
{
    private const int CommandId = 0x0100;

    private readonly AsyncPackage _package;
    private readonly DTE2 _dte;

    private RunGeneratorCommand(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
    {
        _package = package;
        _dte = dte;

        var id = new CommandID(TlumachPackage.CommandSetGuid, CommandId);
        var cmd = new OleMenuCommand(OnExecute, id);
        cmd.BeforeQueryStatus += OnBeforeQueryStatus;
        commandService.AddCommand(cmd);
    }

    internal static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService))
            .ConfigureAwait(true) as OleMenuCommandService
            ?? throw new InvalidOperationException("IMenuCommandService not available.");

        var dte = await package.GetServiceAsync(typeof(DTE))
            .ConfigureAwait(true) as DTE2
            ?? throw new InvalidOperationException("DTE not available.");

        _ = new RunGeneratorCommand(package, commandService, dte);
    }

    private void OnBeforeQueryStatus(object? sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var cmd = (OleMenuCommand)sender!;
        cmd.Visible = false;
        cmd.Enabled = false;

        Project? project = GetSelectedProject();
        if (project is null)
            return;

        bool hasFiles = ProjectHelper.HasTlumachConfigFiles(project);
        cmd.Visible = hasFiles;
        cmd.Enabled = hasFiles;
    }

    private void OnExecute(object? sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Project? project = GetSelectedProject();
        if (project is null)
        {
            OutputWindowHelper.WriteLineOnUIThread(_package, "Tlumach Generator: no project selected.");
            return;
        }

        _ = Task.Run(() => GeneratorRunner.RunForProjectAsync(_package, project));
    }

    private Project? GetSelectedProject()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        SelectedItems? selected = _dte.SelectedItems;
        if (selected is null || selected.Count == 0)
            return null;

        return selected.Item(1)?.Project;
    }
}
