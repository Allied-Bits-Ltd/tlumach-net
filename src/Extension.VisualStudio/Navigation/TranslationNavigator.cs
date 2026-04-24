// <copyright file="TranslationNavigator.cs" company="Allied Bits Ltd.">
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
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Tlumach.Generator;
using Task = System.Threading.Tasks.Task;

namespace AlliedBits.Tlumach.Extension.VisualStudio.Navigation;

/// <summary>
/// Opens a translation file and moves the editor caret to the location of a key.
/// </summary>
internal static class TranslationNavigator
{
    /// <summary>
    /// Opens <paramref name="location"/>.<see cref="KeyLocation.FilePath"/> in the editor and
    /// navigates to the line/column specified by the <see cref="KeyLocation"/>.
    /// </summary>
    internal static async Task NavigateToAsync(AsyncPackage package, KeyLocation location)
    {
        if (location is null || string.IsNullOrEmpty(location.FilePath) || !File.Exists(location.FilePath))
            return;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var dte = await package.GetServiceAsync(typeof(DTE)).ConfigureAwait(true) as DTE2;
        if (dte is null)
            return;

        try
        {
            dte.ItemOperations.OpenFile(location.FilePath, Constants.vsViewKindCode);

            if (dte.ActiveDocument?.Selection is TextSelection selection)
            {
                int line = Math.Max(1, location.LineNumber);
                int col = Math.Max(1, location.ColumnNumber);
                selection.MoveToLineAndOffset(line, col, Extend: false);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            OutputWindowHelper.WriteLineOnUIThread(
                package,
                $"Tlumach: failed to navigate to '{location.FilePath}': {ex.Message}");
        }
    }
}
