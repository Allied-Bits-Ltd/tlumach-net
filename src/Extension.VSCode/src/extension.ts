import * as path from 'path';
import * as vscode from 'vscode';
import { findCsharpProjects, getProjectForUri } from './scanner';
import { runForProject, runForAllProjects } from './runner';
import { newConfigurationFile, newTranslationFile } from './newFile';
import { TranslationValidator } from './validator';

export function activate(context: vscode.ExtensionContext): void {
    const outputChannel = vscode.window.createOutputChannel('Tlumach Generator');
    context.subscriptions.push(outputChannel);

    const validator = new TranslationValidator(context.extensionPath, outputChannel);
    context.subscriptions.push(validator);

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
        ),

        vscode.commands.registerCommand(
            'tlumach.validateTranslations',
            async (uri?: vscode.Uri) => {
                const projectPath = uri
                    ? await getProjectForUri(uri)
                    : await getProjectForUri(vscode.window.activeTextEditor?.document.uri) ?? await pickProject();

                if (!projectPath) {
                    vscode.window.showWarningMessage(
                        'Tlumach: select a .csproj file or open a file of a project first.'
                    );
                    return;
                }

                await validator.validate([projectPath]);
            }
        ),

        vscode.commands.registerCommand(
            'tlumach.validateTranslationsAllProjects',
            async () => {
                const projects = await findCsharpProjects();
                if (projects.length === 0) {
                    vscode.window.showInformationMessage(
                        'Tlumach: no .csproj files found in the workspace.'
                    );
                    return;
                }

                await validator.validate(projects);
            }
        ),

        vscode.commands.registerCommand(
            'tlumach.newTranslationFile',
            async (uri?: vscode.Uri) => {
                await newTranslationFile(context, uri, outputChannel);
            }
        ),

        vscode.commands.registerCommand(
            'tlumach.newConfigurationFile',
            async (uri?: vscode.Uri) => {
                await newConfigurationFile(context, uri, outputChannel);
            }
        )
    );
}

/**
 * Lets the user choose a project when the command is invoked without a selection and the active editor
 * does not belong to a project. A workspace with one project needs no choice.
 */
async function pickProject(): Promise<string | undefined> {
    const projects = await findCsharpProjects();
    if (projects.length <= 1) {
        return projects[0];
    }

    const picked = await vscode.window.showQuickPick(
        projects.map(project => ({
            label: path.basename(project),
            description: vscode.workspace.asRelativePath(path.dirname(project)),
            project,
        })),
        { placeHolder: 'Select the project whose translations to validate' }
    );

    return picked?.project;
}

export function deactivate(): void {
    // nothing to clean up
}
