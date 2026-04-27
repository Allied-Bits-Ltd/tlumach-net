// <copyright file="GoToTranslationCommandHandler.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Tlumach.Generator;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Navigation;

/// <summary>
/// Intercepts the Go To Definition command (F12 / Ctrl+Click) in C# editors.
/// When the identifier under the caret matches a known translation key in <see cref="KeyIndex"/>,
/// navigates to the translation file instead of letting Roslyn handle the command.
/// </summary>
[Export(typeof(ICommandHandler))]
[ContentType("CSharp")]
[Name("TlumachGoToTranslationDefinition")]
[Order(Before = "default")]
internal sealed class GoToTranslationCommandHandler : ICommandHandler<GoToDefinitionCommandArgs>
{
    [Import]
    private SVsServiceProvider ServiceProvider { get; set; } = null!;

    public string DisplayName => "Tlumach Go To Translation Definition";

    public CommandState GetCommandState(GoToDefinitionCommandArgs args)
    {
        if (args?.TextView is null)
            return CommandState.Unspecified;

        var (ns, className, identifier) = SymbolExtractor.ExtractSymbolAtCaret(args.TextView);
        if (string.IsNullOrEmpty(identifier))
            return CommandState.Unspecified;

        if (!KeyIndex.IsPopulated)
            return CommandState.Unspecified;

        var location = KeyIndex.FindDeclaration(ns, className, identifier!);
        return location is not null ? CommandState.Available : CommandState.Unspecified;
    }

    public bool ExecuteCommand(GoToDefinitionCommandArgs args, CommandExecutionContext executionContext)
    {
        if (args?.TextView is null)
            return false;

        var (ns, className, identifier) = SymbolExtractor.ExtractSymbolAtCaret(args.TextView);
        if (string.IsNullOrEmpty(identifier))
            return false;

        if (!KeyIndex.IsPopulated)
            ProjectHelper.RegenerateIndex();

        var location = KeyIndex.FindDeclaration(ns, className, identifier!);
        if (location is null)
            return false;

        // Navigate asynchronously — fire and forget; return true to mark command as handled
        _ = NavigateAsync(location);
        return true;
    }

    private async Task NavigateAsync(KeyLocation location)
    {
        if (ServiceProvider is null)
            return;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        // Resolve the AsyncPackage from the service provider so we can use TranslationNavigator
        var shell = ServiceProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsShell))
            as Microsoft.VisualStudio.Shell.Interop.IVsShell;

        if (shell is null)
            return;

        Guid packageGuid = new(TlumachPackage.PackageGuidString);
        shell.LoadPackage(ref packageGuid, out var vsPackage);

        if (vsPackage is AsyncPackage asyncPackage)
        {
            await TranslationNavigator.NavigateToAsync(asyncPackage, location).ConfigureAwait(true);
        }
    }
}
