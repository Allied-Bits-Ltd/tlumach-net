import * as vscode from 'vscode';
import { findCsharpProjects, getProjectForUri } from './scanner';
import { runForProject, runForAllProjects } from './runner';

export function activate(context: vscode.ExtensionContext): void {
    const outputChannel = vscode.window.createOutputChannel('Tlumach Generator');
    context.subscriptions.push(outputChannel);

    context.subscriptions.push(
        vscode.commands.registerCommand(
            'tlumach.runGenerator',
            async (uri?: vscode.Uri) => {
                const projectPath = uri
                    ? uri.fsPath
                    : await getProjectForUri(vscode.window.activeTextEditor?.document.uri);

                if (!projectPath) {
                    vscode.window.showWarningMessage(
                        'Tlumach Generator: select or open a .csproj file first.'
                    );
                    return;
                }

                outputChannel.show(true);
                await runForProject(context.extensionPath, projectPath, outputChannel);
            }
        ),

        vscode.commands.registerCommand(
            'tlumach.runGeneratorAllProjects',
            async () => {
                const projects = await findCsharpProjects();
                if (projects.length === 0) {
                    vscode.window.showInformationMessage(
                        'Tlumach Generator: no .csproj files found in the workspace.'
                    );
                    return;
                }

                outputChannel.show(true);
                await runForAllProjects(context.extensionPath, projects, outputChannel);
            }
        )
    );
}

export function deactivate(): void {
    // nothing to clean up
}
