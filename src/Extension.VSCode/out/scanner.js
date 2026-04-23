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
exports.findCsharpProjects = findCsharpProjects;
exports.getProjectForUri = getProjectForUri;
const path = __importStar(require("path"));
const vscode = __importStar(require("vscode"));
/**
 * Finds all *.csproj files across all workspace folders.
 */
async function findCsharpProjects() {
    const folders = vscode.workspace.workspaceFolders;
    if (!folders || folders.length === 0) {
        return [];
    }
    const results = [];
    for (const folder of folders) {
        const files = await vscode.workspace.findFiles(new vscode.RelativePattern(folder, '**/*.csproj'), '{**/node_modules/**,**/bin/**,**/obj/**}');
        results.push(...files.map(f => f.fsPath));
    }
    return results;
}
/**
 * Resolves the .csproj file to use for a given document URI.
 * If the URI is already a .csproj, returns it directly.
 * Otherwise searches the workspace for projects near the file.
 */
async function getProjectForUri(uri) {
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
//# sourceMappingURL=scanner.js.map