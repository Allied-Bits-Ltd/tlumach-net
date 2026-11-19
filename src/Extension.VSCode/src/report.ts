// <copyright file="report.ts" company="Allied Bits Ltd.">
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

// The JSON report of the validation runner and its conversion into diagnostics. This module does not
// import 'vscode', so that it can be tested with plain Node.js ('npm test').

/** The version of the report that this extension reads; it matches ReportWriter.JsonVersion of the runner. */
export const REPORT_VERSION = 1;

export type Severity = 'error' | 'warning' | 'info';

export interface ReportMessage {
    severity: Severity;
    code: string;
    message: string;
    file: string | null;
    /** 1-based; 0 when unknown. */
    line: number;
    /** 1-based; 0 when unknown. */
    column: number;
}

export interface ReportFile {
    path: string;
    origin: 'defaultFile' | 'configuration' | 'namingConvention' | 'linkedItem';
    locale: string;
    checkedLocales: string[];
    entries: number;
    errors: number;
    warnings: number;
    infos: number;
}

export interface ReportConfiguration {
    configuration: string;
    generatorInput: boolean;
    defaultFile: string | null;
    files: ReportFile[];
    messages: ReportMessage[];
}

export interface ReportProject {
    project: string;
    note: string | null;
    errors: number;
    warnings: number;
    infos: number;
    ignoredFiles: string[];
    configurations: ReportConfiguration[];
}

export interface ValidationReport {
    version: number;
    errors: number;
    warnings: number;
    infos: number;
    projects: ReportProject[];
}

/** A diagnostic in a file, with 0-based positions as VS Code expects them. */
export interface DiagnosticData {
    severity: Severity;
    code: string;
    message: string;
    line: number;
    column: number;
}

/**
 * Reads the report that the runner writes to the standard output.
 * @throws Error when the output is not a report of the supported version.
 */
export function parseReport(output: string): ValidationReport {
    const start = output.indexOf('{');
    if (start < 0) {
        throw new Error('The validation runner did not produce a report.');
    }

    let value: unknown;
    try {
        value = JSON.parse(output.substring(start));
    } catch (err) {
        const message = err instanceof Error ? err.message : String(err);
        throw new Error(`The report of the validation runner cannot be read: ${message}`);
    }

    if (typeof value !== 'object' || value === null) {
        throw new Error('The report of the validation runner is not an object.');
    }

    const report = value as Partial<ValidationReport>;
    if (report.version !== REPORT_VERSION) {
        throw new Error(
            `The validation runner wrote a report of version ${String(report.version)}, but this extension reads version ${REPORT_VERSION}. Reinstall the extension.`
        );
    }

    if (!Array.isArray(report.projects)) {
        throw new Error('The report of the validation runner lists no projects.');
    }

    return report as ValidationReport;
}

/**
 * Groups the messages about a project by file. A message without a file is attached to the configuration,
 * so that it still appears in the Problems panel.
 */
export function collectDiagnostics(project: ReportProject): Map<string, DiagnosticData[]> {
    const result = new Map<string, DiagnosticData[]>();

    for (const configuration of project.configurations) {
        for (const message of configuration.messages) {
            const file = message.file ?? configuration.configuration;
            const list = result.get(file) ?? [];
            list.push({
                severity: message.severity,
                code: message.code,
                message: message.message,
                line: Math.max(message.line - 1, 0),
                column: Math.max(message.column - 1, 0),
            });
            result.set(file, list);
        }
    }

    return result;
}

/** A one-line summary of the report for notifications and the status bar. */
export function summarize(report: ValidationReport): string {
    const files = report.projects.reduce(
        (sum, project) => sum + project.configurations.reduce((count, configuration) => count + configuration.files.length, 0),
        0
    );
    const configurations = report.projects.reduce((sum, project) => sum + project.configurations.length, 0);

    if (configurations === 0) {
        return 'no Tlumach configuration files found';
    }

    const summary = `${plural(files, 'file')} checked, ${plural(report.errors, 'error')}, ${plural(report.warnings, 'warning')}`;
    return report.infos > 0 ? `${summary}, ${plural(report.infos, 'info message')}` : summary;
}

function plural(count: number, noun: string): string {
    return `${count} ${noun}${count === 1 ? '' : 's'}`;
}
