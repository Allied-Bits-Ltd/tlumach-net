// <copyright file="OutputWindowHelper.cs" company="Allied Bits Ltd.">
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Manages a dedicated "Tlumach Generator" pane in the VS Output window.
/// </summary>
internal static class OutputWindowHelper
{
    private const string PaneTitle = "Tlumach Generator";

    private static readonly Guid PaneGuid = new("D7A8E3C1-F2B4-4C6A-9E0D-1F3A5B7C9E2D");

    private static IVsOutputWindowPane? _pane;

    internal static IVsOutputWindowPane GetOrCreatePane(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_pane is not null)
            return _pane;

        var outputWindow = serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow
            ?? throw new InvalidOperationException("SVsOutputWindow service not available.");

        Guid guid = PaneGuid;
        if (outputWindow.GetPane(ref guid, out IVsOutputWindowPane? pane) != VSConstants.S_OK || pane is null)
        {
            guid = PaneGuid;
            outputWindow.CreatePane(ref guid, PaneTitle, fInitVisible: 1, fClearWithSolution: 0);
            guid = PaneGuid;
            outputWindow.GetPane(ref guid, out pane);
        }

        _pane = pane!;
        return _pane;
    }

    internal static void Activate(IVsOutputWindowPane pane)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        pane.Activate();
    }

    /// <summary>
    /// Writes a line to <paramref name="pane"/>. Safe to call from any thread because
    /// <see cref="IVsOutputWindowPane.OutputStringThreadSafe"/> is thread-safe by contract.
    /// </summary>
    internal static void WriteLine(IVsOutputWindowPane pane, string message)
    {
        pane.OutputStringThreadSafe(message + Environment.NewLine);
    }

    internal static void WriteLineOnUIThread(IServiceProvider serviceProvider, string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        IVsOutputWindowPane pane = GetOrCreatePane(serviceProvider);
        Activate(pane);
        WriteLine(pane, message);
    }

    /// <summary>
    /// Writes a line to the Tlumach output pane without requiring a package reference.
    /// Uses <see cref="TlumachPackage.Instance"/> if available; otherwise resolves
    /// <see cref="SVsOutputWindow"/> from the global service provider. Safe to call
    /// from any VS-invoked method after the package has started loading.
    /// Silently does nothing if the output window is not yet available.
    /// </summary>
    internal static void TryWriteLineOnUIThread(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

#pragma warning disable CA1031
        try
        {
            IServiceProvider? provider = TlumachPackage.Instance;
            if (provider is null)
            {
                // Fall back to the global service provider if the package isn't loaded yet
                var oleProvider = Package.GetGlobalService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider))
                    as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
                if (oleProvider is not null)
                    provider = new ServiceProvider(oleProvider);
            }

            if (provider is null)
                return;

            IVsOutputWindowPane pane = GetOrCreatePane(provider);
            Activate(pane);
            WriteLine(pane, message);
        }
        catch
        {
            // Output window may not be available — absorb silently
        }
#pragma warning restore CA1031
    }
}
