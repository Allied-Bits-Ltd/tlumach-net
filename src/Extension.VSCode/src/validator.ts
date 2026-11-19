// <copyright file="validator.ts" company="Allied Bits Ltd.">
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

import * as cp from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';

import { DiagnosticData, ValidationReport, collectDiagnostics, parseReport, summarize } from './report';
import { buildRunnerCommand } from './runnerCommand';

/** The exit codes of the runner (see RunnerCommand.cs). */
const EXIT_SUCCESS = 0;
const EXIT_ERRORS_FOUND = 1;

/**
 * Validates the translations of projects with the .NET runner bundled in the extension and shows the
 * problems in the Problems panel. The diagnostics are kept per project, so that validating one project
 * does not remove the problems found earlier in another one.
 */
export class TranslationValidator implements vscode.Disposable {
    private readonly diagnostics: vscode.DiagnosticCollection;

    /** Project file (normalized) → file (normalized) → diagnostics. */
    private readonly byProject = new Map<string, Map<string, vscode.Diagnostic[]>>();

    /** Normalized file path → the path as reported, used to build URIs. */
    private readonly filePaths = new Map<string, string>();

    private running = false;

    constructor(
        private readonly extensionPath: string,
        private readonly output: vscode.OutputChannel
    ) {
        this.diagnostics = vscode.languages.createDiagnosticCollection('tlumach');
    }

    dispose(): void {
        this.diagnostics.dispose();
    }

    /** Validates the given projects; reports a single project or a set of projects in one run. */
    async validate(projects: string[]): Promise<void> {
        if (projects.length === 0) {
            return;
        }

        if (this.running) {
            vscode.window.showInformationMessage('Tlumach: a validation of translations is already running.');
            return;
        }

        this.running = true;
        try {
            this.output.show(true);
            this.output.appendLine('');

            const title = projects.length === 1
                ? `Tlumach: validating the translations of ${path.basename(projects[0])}`
                : `Tlumach: validating the translations of ${projects.length} projects`;

            await vscode.window.withProgress(
                { location: vscode.ProgressLocation.Notification, title, cancellable: true },
                (_progress, token) => this.run(projects, token)
            );
        } finally {
            this.running = false;
        }
    }

    private async run(projects: string[], token: vscode.CancellationToken): Promise<void> {
        const dotnet = vscode.workspace.getConfiguration('tlumach').get<string>('dotnetPath')?.trim() || 'dotnet';
        const commandLine = buildRunnerCommand(this.extensionPath, dotnet, projects, file => fs.existsSync(file));

        let result: RunResult;
        try {
            result = await runProcess(commandLine.command, commandLine.args, path.dirname(projects[0]), this.output, token);
        } catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            this.output.appendLine(`ERROR: ${message}`);
            vscode.window.showErrorMessage(`Tlumach: the validation could not run. ${message}`);
            return;
        }

        if (result.cancelled) {
            this.output.appendLine('The validation was cancelled.');
            return;
        }

        if (result.exitCode !== EXIT_SUCCESS && result.exitCode !== EXIT_ERRORS_FOUND) {
            const details = lastLines(result.errorText, 3);
            vscode.window.showErrorMessage(`Tlumach: the validation failed (exit code ${String(result.exitCode)}). ${details}`, 'Show Output')
                .then(choice => choice && this.output.show());
            return;
        }

        let report: ValidationReport;
        try {
            report = parseReport(result.outputText);
        } catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            this.output.appendLine(`ERROR: ${message}`);
            vscode.window.showErrorMessage(`Tlumach: ${message}`);
            return;
        }

        this.publish(report);
        this.notify(report);
    }

    /** Replaces the diagnostics of the validated projects. */
    private publish(report: ValidationReport): void {
        const touched = new Set<string>();

        for (const project of report.projects) {
            const projectKey = normalize(project.project);

            for (const file of this.byProject.get(projectKey)?.keys() ?? []) {
                touched.add(file);
            }

            const files = new Map<string, vscode.Diagnostic[]>();
            for (const [file, list] of collectDiagnostics(project)) {
                const key = normalize(file);
                this.filePaths.set(key, file);
                files.set(key, list.map(toDiagnostic));
                touched.add(key);
            }

            this.byProject.set(projectKey, files);
        }

        // A file may belong to several projects: its diagnostics are those of all of them
        for (const key of touched) {
            const merged: vscode.Diagnostic[] = [];
            for (const files of this.byProject.values()) {
                merged.push(...(files.get(key) ?? []));
            }

            const uri = vscode.Uri.file(this.filePaths.get(key) ?? key);
            if (merged.length > 0) {
                this.diagnostics.set(uri, merged);
            } else {
                this.diagnostics.delete(uri);
            }
        }
    }

    private notify(report: ValidationReport): void {
        const summary = summarize(report);

        if (report.errors > 0 || report.warnings > 0) {
            const show = report.errors > 0 ? vscode.window.showErrorMessage : vscode.window.showWarningMessage;
            show(`Tlumach: ${summary}.`, 'Show Problems').then(choice => {
                if (choice) {
                    void vscode.commands.executeCommand('workbench.actions.view.problems');
                }
            });
        } else {
            vscode.window.setStatusBarMessage(`$(check) Tlumach: ${summary}`, 5000);
        }
    }
}

interface RunResult {
    exitCode: number | null;
    outputText: string;
    errorText: string;
    cancelled: boolean;
}

/**
 * Runs the runner: the text report (standard error) is streamed to the output channel, the JSON report
 * (standard output) is collected.
 */
function runProcess(
    command: string,
    args: string[],
    cwd: string,
    output: vscode.OutputChannel,
    token: vscode.CancellationToken
): Promise<RunResult> {
    return new Promise((resolve, reject) => {
        let outputText = '';
        let errorText = '';
        let cancelled = false;

        const child = cp.spawn(command, args, {
            cwd,
            shell: false,
            windowsHide: true,
            env: { ...process.env, DOTNET_CLI_UI_LANGUAGE: 'en', DOTNET_NOLOGO: '1' },
        });

        child.stdout.setEncoding('utf8');
        child.stderr.setEncoding('utf8');

        child.stdout.on('data', (data: string) => {
            outputText += data;
        });

        child.stderr.on('data', (data: string) => {
            errorText += data;
            output.append(data);
        });

        const subscription = token.onCancellationRequested(() => {
            cancelled = true;
            child.kill();
        });

        child.on('error', err => {
            subscription.dispose();
            const missing = (err as NodeJS.ErrnoException).code === 'ENOENT';
            reject(new Error(missing
                ? `'${command}' was not found. Install .NET 8 or newer from https://dotnet.microsoft.com/download, or set the 'tlumach.dotnetPath' setting to the full path of the dotnet executable.`
                : `'${command}' could not be started: ${err.message}`));
        });

        child.on('close', code => {
            subscription.dispose();
            resolve({ exitCode: code, outputText, errorText, cancelled });
        });
    });
}

function toDiagnostic(data: DiagnosticData): vscode.Diagnostic {
    const position = new vscode.Position(data.line, data.column);
    const severity = data.severity === 'error'
        ? vscode.DiagnosticSeverity.Error
        : data.severity === 'warning'
            ? vscode.DiagnosticSeverity.Warning
            : vscode.DiagnosticSeverity.Information;

    const diagnostic = new vscode.Diagnostic(new vscode.Range(position, position), data.message, severity);
    diagnostic.source = 'Tlumach';
    diagnostic.code = data.code;
    return diagnostic;
}

/** File systems on Windows and macOS are case-insensitive; the paths are compared accordingly. */
function normalize(file: string): string {
    const resolved = path.resolve(file);
    return process.platform === 'linux' ? resolved : resolved.toLowerCase();
}

function lastLines(text: string, count: number): string {
    return text.split(/\r?\n/).map(line => line.trim()).filter(line => line.length > 0).slice(-count).join(' ');
}
