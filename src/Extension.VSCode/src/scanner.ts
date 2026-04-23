import * as path from 'path';
import * as vscode from 'vscode';

/**
 * Finds all *.csproj files across all workspace folders.
 */
export async function findCsharpProjects(): Promise<string[]> {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) {
        return [];
    }

    const results: string[] = [];
    for (const folder of folders) {
        const files = await vscode.workspace.findFiles(
            new vscode.RelativePattern(folder, '**/*.csproj'),
            '{**/node_modules/**,**/bin/**,**/obj/**}'
        );
        results.push(...files.map(f => f.fsPath));
    }
    return results;
}

/**
 * Resolves the .csproj file to use for a given document URI.
 * If the URI is already a .csproj, returns it directly.
 * Otherwise searches the workspace for projects near the file.
 */
export async function getProjectForUri(uri?: vscode.Uri): Promise<string | undefined> {
    if (!uri) {
        return undefined;
    }

    if (uri.fsPath.endsWith('.csproj')) {
        return uri.fsPath;
    }

    // Find the closest .csproj by walking up from the file's directory
    const fileDir = path.dirname(uri.fsPath);
    const allProjects = await findCsharpProjects();

    // Prefer a project in the same directory or a parent directory
    const sorted = allProjects
        .filter(p => fileDir.startsWith(path.dirname(p)))
        .sort((a, b) => b.length - a.length); // longest (most specific) path first

    return sorted[0];
}
