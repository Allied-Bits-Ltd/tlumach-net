import * as cp from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';

/**
 * Resolves the command-line arguments needed to invoke the .NET runner.
 *
 * Production:  dotnet <extension>/runner/bin/publish/Runner.dll
 * Development: dotnet run --project <extension>/runner/Runner.csproj --
 */
function resolveRunnerArgs(extensionPath: string): string[] {
    const publishedDll = path.join(extensionPath, 'runner', 'bin', 'publish', 'Runner.dll');
    if (fs.existsSync(publishedDll)) {
        return [publishedDll];
    }

    // Development fallback: run from source via dotnet run
    const runnerCsproj = path.join(extensionPath, 'runner', 'Runner.csproj');
    return ['run', '--project', runnerCsproj, '--'];
}

/**
 * Spawns the .NET runner for a single project and pipes its output to the channel.
 * Resolves when the runner exits; rejects on spawn error or non-zero exit code.
 */
function spawnRunner(
    extensionPath: string,
    extraArgs: string[],
    outputChannel: vscode.OutputChannel
): Promise<void> {
    return new Promise((resolve, reject) => {
        const runnerArgs = resolveRunnerArgs(extensionPath);
        const args = [...runnerArgs, ...extraArgs];

        const proc = cp.spawn('dotnet', args, { shell: false });

        proc.stdout.on('data', (data: Buffer) => {
            outputChannel.append(data.toString());
        });

        proc.stderr.on('data', (data: Buffer) => {
            outputChannel.append(data.toString());
        });

        proc.on('error', (err) => {
            reject(new Error(`Failed to start dotnet runner: ${err.message}`));
        });

        proc.on('close', (code) => {
            if (code === 0) {
                resolve();
            } else {
                reject(new Error(`Runner exited with code ${code}`));
            }
        });
    });
}

/**
 * Runs the Tlumach generator for the given .csproj file.
 */
export async function runForProject(
    extensionPath: string,
    projectPath: string,
    outputChannel: vscode.OutputChannel
): Promise<void> {
    outputChannel.appendLine(`=== Tlumach Generator: ${path.basename(projectPath)} ===`);

    try {
        await spawnRunner(extensionPath, ['--project', projectPath], outputChannel);
        vscode.window.setStatusBarMessage('$(check) Tlumach Generator: done', 4000);
    } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        outputChannel.appendLine(`ERROR: ${message}`);
        vscode.window.showErrorMessage(`Tlumach Generator failed: ${message}`);
    }
}

/**
 * Runs the Tlumach generator for every project in the list.
 */
export async function runForAllProjects(
    extensionPath: string,
    projects: string[],
    outputChannel: vscode.OutputChannel
): Promise<void> {
    outputChannel.appendLine(`=== Tlumach Generator: ${projects.length} project(s) ===`);

    let successCount = 0;
    let errorCount = 0;

    for (const projectPath of projects) {
        try {
            await spawnRunner(extensionPath, ['--project', projectPath], outputChannel);
            successCount++;
        } catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            outputChannel.appendLine(`ERROR: ${path.basename(projectPath)}: ${message}`);
            errorCount++;
        }
    }

    const summary = `=== Done: ${successCount} project(s) succeeded, ${errorCount} failed ===`;
    outputChannel.appendLine(summary);

    if (errorCount > 0) {
        vscode.window.showWarningMessage(`Tlumach Generator: ${errorCount} project(s) had errors. See output for details.`);
    } else {
        vscode.window.setStatusBarMessage('$(check) Tlumach Generator: all projects done', 4000);
    }
}
