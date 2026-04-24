// <copyright file="TlumachPackage.cs" company="Allied Bits Ltd.">
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
using System.Runtime.InteropServices;
using System.Threading;
using AlliedBits.Tlumach.Extension.VisualStudio.Commands;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Tlumach.NET Generator Visual Studio package.
/// Provides on-demand execution of the Tlumach source generator via the
/// Solution Explorer project context menu and the Tools menu.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(
    Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionHasMultipleProjects_string,
    PackageAutoLoadFlags.BackgroundLoad)]
[ProvideAutoLoad(
    Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionHasSingleProject_string,
    PackageAutoLoadFlags.BackgroundLoad)]
public sealed class TlumachPackage : AsyncPackage
{
    /// <summary>Package GUID — must match source.extension.vsixmanifest and VSCommandTable.vsct.</summary>
    public const string PackageGuidString = "C3A5B7D1-E2F4-4A6B-8C9D-0E1F2A3B4C5D";

    /// <summary>Command set GUID — must match VSCommandTable.vsct.</summary>
    public const string CommandSetGuidString = "A1B2C3D4-E5F6-7890-ABCD-EF0123456789";

    /// <summary>Parsed form of <see cref="CommandSetGuidString"/>.</summary>
    public static readonly Guid CommandSetGuid = new(CommandSetGuidString);

    /// <inheritdoc />
    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(true);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await RunGeneratorCommand.InitializeAsync(this).ConfigureAwait(true);
        await RunAllGeneratorsCommand.InitializeAsync(this).ConfigureAwait(true);
        await GoToTranslationDefinitionCommand.InitializeAsync(this).ConfigureAwait(true);
    }
}
