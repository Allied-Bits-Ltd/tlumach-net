"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.runForProject = runForProject;
exports.runForAllProjects = runForAllProjects;
const cp = __importStar(require("child_process"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const vscode = __importStar(require("vscode"));
/**
 * Resolves the command-line arguments needed to invoke the .NET runner.
 *
 * Production:  dotnet <extension>/runner/bin/publish/Runner.dll
 * Development: dotnet run --project <extension>/runner/Runner.csproj --
 */
function resolveRunnerArgs(extensionPath) {
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
function spawnRunner(extensionPath, extraArgs, outputChannel) {
    return new Promise((resolve, reject) => {
        const runnerArgs = resolveRunnerArgs(extensionPath);
        const args = [...runnerArgs, ...extraArgs];
        const proc = cp.spawn('dotnet', args, { shell: false });
        proc.stdout.on('data', (data) => {
            outputChannel.append(data.toString());
        });
        proc.stderr.on('data', (data) => {
            outputChannel.append(data.toString());
        });
        proc.on('error', (err) => {
            reject(new Error(`Failed to start dotnet runner: ${err.message}`));
        });
        proc.on('close', (code) => {
            if (code === 0) {
                resolve();
            }
            else {
                reject(new Error(`Runner exited with code ${code}`));
            }
        });
    });
}
/**
 * Runs the Tlumach generator for the given .csproj file.
 */
async function runForProject(extensionPath, projectPath, outputChannel) {
    outputChannel.appendLine(`=== Tlumach Generator: ${path.basename(projectPath)} ===`);
    try {
        await spawnRunner(extensionPath, ['--project', projectPath], outputChannel);
        vscode.window.setStatusBarMessage('$(check) Tlumach Generator: done', 4000);
    }
    catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        outputChannel.appendLine(`ERROR: ${message}`);
        vscode.window.showErrorMessage(`Tlumach Generator failed: ${message}`);
    }
}
/**
 * Runs the Tlumach generator for every project in the list.
 */
async function runForAllProjects(extensionPath, projects, outputChannel) {
    outputChannel.appendLine(`=== Tlumach Generator: ${projects.length} project(s) ===`);
    let successCount = 0;
    let errorCount = 0;
    for (const projectPath of projects) {
        try {
            await spawnRunner(extensionPath, ['--project', projectPath], outputChannel);
            successCount++;
        }
        catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            outputChannel.appendLine(`ERROR: ${path.basename(projectPath)}: ${message}`);
            errorCount++;
        }
    }
    const summary = `=== Done: ${successCount} project(s) succeeded, ${errorCount} failed ===`;
    outputChannel.appendLine(summary);
    if (errorCount > 0) {
        vscode.window.showWarningMessage(`Tlumach Generator: ${errorCount} project(s) had errors. See output for details.`);
    }
    else {
        vscode.window.setStatusBarMessage('$(check) Tlumach Generator: all projects done', 4000);
    }
}
//# sourceMappingURL=runner.js.map