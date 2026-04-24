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
using System.ComponentModel.Design;
using System.Threading.Tasks;
using AlliedBits.Tlumach.Extension.VisualStudio.Navigation;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Tlumach.Generator;
using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Commands;

/// <summary>
/// "Go To Translation Definition" command — appears in the code editor right-click context menu
/// when the identifier under the caret resolves to a known translation key in the <see cref="KeyIndex"/>.
/// </summary>
internal sealed class GoToTranslationDefinitionCommand
{
    private const int CommandId = 0x0102;

    private readonly AsyncPackage _package;
    private readonly DTE2 _dte;

    private GoToTranslationDefinitionCommand(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
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

        _ = new GoToTranslationDefinitionCommand(package, commandService, dte);
    }

    private void OnBeforeQueryStatus(object? sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var cmd = (OleMenuCommand)sender!;
        cmd.Visible = false;
        cmd.Enabled = false;

        if (!TryResolveLocation(out _))
            return;

        cmd.Visible = true;
        cmd.Enabled = true;
    }

    private void OnExecute(object? sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (!TryResolveLocation(out KeyLocation? location) || location is null)
            return;

        _ = Task.Run(() => TranslationNavigator.NavigateToAsync(_package, location));
    }

    private bool TryResolveLocation(out KeyLocation? location)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        location = null;

        var (ns, className, identifier) = SymbolExtractor.ExtractSymbolFromDte(_dte);
        if (string.IsNullOrEmpty(identifier))
            return false;

        location = KeyIndex.FindDeclaration(ns, className, identifier!);
        return location is not null;
    }
}
