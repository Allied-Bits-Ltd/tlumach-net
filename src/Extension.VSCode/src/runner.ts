// <copyright file="runner.ts" company="Allied Bits Ltd.">
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

import * as path from 'path';
import * as vscode from 'vscode';

/**
 * Tells the active C# language server (C# DevKit / ms-dotnettools.csharp Roslyn LSP,
 * or legacy OmniSharp) to restart. Restarting drops the cached source-generator
 * driver state, so the next compilation re-runs Tlumach.Generator against the
 * project's current AdditionalFiles. This is the only public surface that lets
 * an external extension force the toolchain to refresh its in-memory generated
 * source — the LSP holds the only authoritative copy and there is no API for
 * pushing source into it.
 */
async function restartCSharpLanguageServer(outputChannel: vscode.OutputChannel): Promise<void> {
    const allCommands = await vscode.commands.getCommands(true);

    // C# DevKit / Roslyn LSP — preferred when available.
    if (allCommands.includes('dotnet.restartServer')) {
        outputChannel.appendLine('Restarting C# language server (dotnet.restartServer)…');
        await vscode.commands.executeCommand('dotnet.restartServer');
        return;
    }

    // Legacy OmniSharp.
    if (allCommands.includes('o.restart')) {
        outputChannel.appendLine('Restarting OmniSharp (o.restart)…');
        await vscode.commands.executeCommand('o.restart');
        return;
    }

    throw new Error(
        'No C# language server is active. Install the C# (ms-dotnettools.csharp) ' +
        'or C# Dev Kit extension and ensure the project references AlliedBits.Tlumach ' +
        'so the toolchain loads the Tlumach source generator.'
    );
}

/**
 * Refreshes the toolchain's cached generator output for the given .csproj. Does not
 * write any files — the C# language server holds the authoritative copy of the
 * generated source.
 */
export async function runForProject(
    extensionPath: string,
    projectPath: string,
    outputChannel: vscode.OutputChannel
): Promise<void> {
    void extensionPath; // kept for signature compatibility with callers
    outputChannel.appendLine(`=== Tlumach Generator: ${path.basename(projectPath)} ===`);

    try {
        await restartCSharpLanguageServer(outputChannel);
        outputChannel.appendLine('  Toolchain output refreshed (language server restarted).');
        vscode.window.setStatusBarMessage('$(check) Tlumach Generator: toolchain refreshed', 4000);
    } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        outputChannel.appendLine(`ERROR: ${message}`);
        vscode.window.showErrorMessage(`Tlumach Generator failed: ${message}`);
    }
}

/**
 * Refreshes the toolchain for an entire workspace. Because restarting the C#
 * language server affects every loaded project at once, we restart only once
 * regardless of how many .csproj paths were passed in.
 */
export async function runForAllProjects(
    extensionPath: string,
    projects: string[],
    outputChannel: vscode.OutputChannel
): Promise<void> {
    void extensionPath;
    outputChannel.appendLine(`=== Tlumach Generator: ${projects.length} project(s) ===`);

    try {
        await restartCSharpLanguageServer(outputChannel);
        outputChannel.appendLine('  Toolchain output refreshed for all projects (language server restarted).');
        vscode.window.setStatusBarMessage('$(check) Tlumach Generator: toolchain refreshed', 4000);
    } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        outputChannel.appendLine(`ERROR: ${message}`);
        vscode.window.showErrorMessage(`Tlumach Generator failed: ${message}`);
    }
}
