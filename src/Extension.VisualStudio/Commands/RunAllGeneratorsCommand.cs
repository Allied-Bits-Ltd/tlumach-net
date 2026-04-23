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
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Commands;

/// <summary>
/// "Run Tlumach Generator (All Projects)" command — appears in the Tools menu and
/// runs the generator for every project in the current solution.
/// </summary>
internal sealed class RunAllGeneratorsCommand
{
    private const int CommandId = 0x0101;

    private readonly AsyncPackage _package;
    private readonly DTE2 _dte;

    private RunAllGeneratorsCommand(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
    {
        _package = package;
        _dte = dte;

        var id = new CommandID(TlumachPackage.CommandSetGuid, CommandId);
        var cmd = new MenuCommand(OnExecute, id);
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

        _ = new RunAllGeneratorsCommand(package, commandService, dte);
    }

    private void OnExecute(object? sender, EventArgs e)
    {
        _ = Task.Run(() => GeneratorRunner.RunForAllProjectsAsync(_package, _dte));
    }
}
