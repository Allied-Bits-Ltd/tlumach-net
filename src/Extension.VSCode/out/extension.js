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
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const scanner_1 = require("./scanner");
const runner_1 = require("./runner");
function activate(context) {
    const outputChannel = vscode.window.createOutputChannel('Tlumach Generator');
    context.subscriptions.push(outputChannel);
    context.subscriptions.push(vscode.commands.registerCommand('tlumach.runGenerator', async (uri) => {
        const projectPath = uri
            ? uri.fsPath
            : await (0, scanner_1.getProjectForUri)(vscode.window.activeTextEditor?.document.uri);
        if (!projectPath) {
            vscode.window.showWarningMessage('Tlumach Generator: select or open a .csproj file first.');
            return;
        }
        outputChannel.show(true);
        await (0, runner_1.runForProject)(context.extensionPath, projectPath, outputChannel);
    }), vscode.commands.registerCommand('tlumach.runGeneratorAllProjects', async () => {
        const projects = await (0, scanner_1.findCsharpProjects)();
        if (projects.length === 0) {
            vscode.window.showInformationMessage('Tlumach Generator: no .csproj files found in the workspace.');
            return;
        }
        outputChannel.show(true);
        await (0, runner_1.runForAllProjects)(context.extensionPath, projects, outputChannel);
    }));
}
function deactivate() {
    // nothing to clean up
}
//# sourceMappingURL=extension.js.map