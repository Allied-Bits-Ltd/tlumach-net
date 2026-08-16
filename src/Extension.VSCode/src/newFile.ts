// <copyright file="newFile.ts" company="Allied Bits Ltd.">
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

import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';

import { addProjectItem, readRootNamespace } from './projectFile';
import { findNearestProject } from './scanner';
import {
    TlumachFormat,
    TlumachFormatGroup,
    buildFileName,
    configurationFormats,
    fileInputNameOf,
    renderTemplate,
    translationFormats,
} from './templates';

/**
 * VS Code has no "Add New Item" dialog and no template registry that an extension can extend.
 * The idiomatic equivalent is a command that walks the user through a Quick Pick of the
 * available formats and an input box for the file name, which is what this module implements.
 * The two commands share all of their logic; only the format group and the wording differ.
 */

/** Creates a new Tlumach translation file. */
export async function newTranslationFile(
    context: vscode.ExtensionContext,
    target: vscode.Uri | undefined,
    outputChannel: vscode.OutputChannel
): Promise<void> {
    await newTlumachFile(context, target, outputChannel, translationFormats(context.extensionPath), 'translation');
}

/** Creates a new Tlumach configuration file. */
export async function newConfigurationFile(
    context: vscode.ExtensionContext,
    target: vscode.Uri | undefined,
    outputChannel: vscode.OutputChannel
): Promise<void> {
    await newTlumachFile(context, target, outputChannel, configurationFormats(context.extensionPath), 'configuration');
}

async function newTlumachFile(
    context: vscode.ExtensionContext,
    target: vscode.Uri | undefined,
    outputChannel: vscode.OutputChannel,
    group: TlumachFormatGroup,
    kind: string
): Promise<void> {
    const directory = await resolveTargetDirectory(target);
    if (!directory) {
        vscode.window.showWarningMessage(
            `Tlumach: open a folder or select one in the Explorer to create a ${kind} file in.`
        );
        return;
    }

    const picked = await vscode.window.showQuickPick(
        group.formats.map(format => ({
            label: format.name,
            detail: format.description,
            description: format.duplicateExtension
                ? `${format.extension}${format.extension}`
                : format.extension,
            format,
        })),
        { title: `New Tlumach ${kind} file`, placeHolder: 'Select the file format' }
    );

    if (!picked) {
        return;
    }

    const format: TlumachFormat = picked.format;
    const typed = await vscode.window.showInputBox({
        title: `New Tlumach ${kind} file (${format.name})`,
        prompt: 'File name',
        value: format.defaultName,
        valueSelection: [0, format.defaultName.length - format.extension.length],
        validateInput: value =>
            value.trim().length === 0 ? 'Enter a file name.'
                : /[\\/:*?"<>|]/.test(value) ? 'The name contains characters that are not valid in a file name.'
                    : undefined,
    });

    if (!typed) {
        return;
    }

    const fileName = buildFileName(format, typed.trim());
    const filePath = path.join(directory, fileName);

    if (fs.existsSync(filePath)) {
        vscode.window.showErrorMessage(`Tlumach: '${fileName}' already exists.`);
        return;
    }

    const projectPath = await findNearestProject(vscode.Uri.file(filePath));
    const rootNamespace = projectPath
        ? readRootNamespace(projectPath)
        : path.basename(directory);

    const content = renderTemplate(
        context.extensionPath,
        format,
        fileInputNameOf(format, fileName),
        rootNamespace
    );

    await vscode.workspace.fs.writeFile(vscode.Uri.file(filePath), Buffer.from(content, 'utf8'));
    outputChannel.appendLine(`Created ${filePath}`);

    if (projectPath) {
        try {
            const changed = addProjectItem(projectPath, filePath, group.itemType, format);
            outputChannel.appendLine(
                changed
                    ? `Added a ${group.itemType} item for ${fileName} to ${path.basename(projectPath)}`
                    : `${path.basename(projectPath)} already references ${fileName}; left it unchanged`
            );
        } catch (err) {
            const message = err instanceof Error ? err.message : String(err);
            outputChannel.appendLine(`WARNING: could not update ${projectPath}: ${message}`);
            vscode.window.showWarningMessage(
                `Tlumach: '${fileName}' was created, but ${path.basename(projectPath)} could not be updated. ` +
                `Add it manually as a ${group.itemType} item. (${message})`
            );
        }
    } else {
        outputChannel.appendLine(
            `No .csproj found above ${directory}; add ${fileName} as a ${group.itemType} item manually.`
        );
    }

    const document = await vscode.workspace.openTextDocument(vscode.Uri.file(filePath));
    await vscode.window.showTextDocument(document);
}

/**
 * Resolves the folder the new file goes into: the Explorer selection when the command was
 * invoked from the context menu, the folder of the active editor otherwise, and finally the
 * first workspace folder.
 */
async function resolveTargetDirectory(target: vscode.Uri | undefined): Promise<string | undefined> {
    if (target) {
        try {
            const stat = await vscode.workspace.fs.stat(target);
            return stat.type === vscode.FileType.Directory ? target.fsPath : path.dirname(target.fsPath);
        } catch {
            return path.dirname(target.fsPath);
        }
    }

    const active = vscode.window.activeTextEditor?.document.uri;
    if (active && active.scheme === 'file') {
        return path.dirname(active.fsPath);
    }

    return vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
}

