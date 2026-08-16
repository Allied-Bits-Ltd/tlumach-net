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
 * Finds the .csproj that owns a path by walking up the directory tree. Unlike
 * getProjectForUri() this does not require the path to exist yet, which is what the
 * "New Tlumach ... File" commands need when they place a file that is about to be created.
 */
export async function findNearestProject(uri: vscode.Uri): Promise<string | undefined> {
    let directory = path.dirname(uri.fsPath);

    for (; ;) {
        let entries: [string, vscode.FileType][];
        try {
            entries = await vscode.workspace.fs.readDirectory(vscode.Uri.file(directory));
        } catch {
            return undefined;
        }

        const project = entries.find(
            ([name, type]) => type === vscode.FileType.File && name.toLowerCase().endsWith('.csproj')
        );
        if (project) {
            return path.join(directory, project[0]);
        }

        const parent = path.dirname(directory);
        if (parent === directory) {
            return undefined;
        }
        directory = parent;
    }
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
