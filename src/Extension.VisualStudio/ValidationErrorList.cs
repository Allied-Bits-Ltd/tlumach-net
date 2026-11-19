// <copyright file="ValidationErrorList.cs" company="Allied Bits Ltd.">
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
using System.Collections.Generic;

using Tlumach.Validation;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AlliedBits.Tlumach.Extension.VisualStudio;

/// <summary>
/// Shows the problems found by the validation of translations in the Error List. The entries are kept per project, so that validating one project does not remove the entries of another one; they stay until the project is validated again or the solution is closed.
/// </summary>
internal static class ValidationErrorList
{
    private static readonly Guid ProviderGuid = new("3E8B6C51-9A2F-4D0E-B7C4-6F1A2D9E5B38");

    private static readonly Dictionary<string, List<ErrorTask>> TasksByProject = new(StringComparer.OrdinalIgnoreCase);

    private static ErrorListProvider? _provider;

    /// <summary>
    /// The events object must be referenced for as long as its handlers are needed; DTE drops the handlers of a collected one.
    /// </summary>
    private static EnvDTE.SolutionEvents? _solutionEvents;

    /// <summary>
    /// Replaces the entries of a project with the given messages.
    /// </summary>
    /// <param name="serviceProvider">The package.</param>
    /// <param name="projectKey">The identity of the project (the path of the project file).</param>
    /// <param name="hierarchy">The hierarchy of the project, which the Error List shows in its Project column; may be <see langword="null"/>.</param>
    /// <param name="messages">The messages of the validation.</param>
    internal static void Replace(IServiceProvider serviceProvider, string projectKey, IVsHierarchy? hierarchy, IEnumerable<ValidationMessage> messages)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        ErrorListProvider provider = GetProvider(serviceProvider);
        provider.SuspendRefresh();
        try
        {
            if (TasksByProject.TryGetValue(projectKey, out List<ErrorTask>? previous))
            {
                foreach (ErrorTask task in previous)
                    provider.Tasks.Remove(task);
            }

            List<ErrorTask> tasks = [];
            foreach (ValidationMessage message in messages)
            {
                ErrorTask task = new()
                {
                    ErrorCategory = message.Severity switch
                    {
                        ValidationSeverity.Error => TaskErrorCategory.Error,
                        ValidationSeverity.Warning => TaskErrorCategory.Warning,
                        _ => TaskErrorCategory.Message,
                    },
                    Category = TaskCategory.BuildCompile,
                    Text = $"{message.Code}: {message.Message}",
                    Document = message.FilePath ?? string.Empty,

                    // The Error List counts from 0 and shows the numbers from 1
                    Line = Math.Max(message.Line - 1, 0),
                    Column = Math.Max(message.Column - 1, 0),
                    HierarchyItem = hierarchy,
                };

                task.Navigate += OnNavigate;
                provider.Tasks.Add(task);
                tasks.Add(task);
            }

            TasksByProject[projectKey] = tasks;
        }
        finally
        {
            provider.ResumeRefresh();
        }
    }

    /// <summary>
    /// Brings the Error List to the front.
    /// </summary>
    internal static void Show()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _provider?.Show();
        _provider?.BringToFront();
    }

    private static ErrorListProvider GetProvider(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_provider is not null)
            return _provider;

        _provider = new ErrorListProvider(serviceProvider)
        {
            ProviderName = "Tlumach Translation Validation",
            ProviderGuid = ProviderGuid,
        };

        // The entries refer to the files of the solution; they go away with it
#pragma warning disable CA1031 // Without the events, the entries stay until the projects are validated again
        try
        {
            if (serviceProvider.GetService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte)
            {
                _solutionEvents = dte.Events.SolutionEvents;
                _solutionEvents.AfterClosing += Clear;
            }
        }
        catch (Exception ex)
        {
            ActivityLog.TryLogWarning(nameof(ValidationErrorList), $"Could not subscribe to the closing of the solution: {ex.Message}");
        }
#pragma warning restore CA1031

        return _provider;
    }

    /// <summary>
    /// Removes all entries.
    /// </summary>
    private static void Clear()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _provider?.Tasks.Clear();
        TasksByProject.Clear();
    }

    private static void OnNavigate(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (sender is ErrorTask task && _provider is not null && !string.IsNullOrEmpty(task.Document))
            _provider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindCode));
    }
}
