// <copyright file="report.test.ts" company="Allied Bits Ltd.">
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

import * as assert from 'node:assert/strict';
import * as path from 'node:path';
import { describe, it } from 'node:test';

import { REPORT_VERSION, ValidationReport, collectDiagnostics, parseReport, summarize } from '../report';
import { RUNNER_ASSEMBLY, RUNNER_PROJECT, buildRunnerCommand } from '../runnerCommand';

const sample: ValidationReport = {
    version: REPORT_VERSION,
    errors: 1,
    warnings: 1,
    infos: 0,
    projects: [
        {
            project: '/work/App/App.csproj',
            note: null,
            errors: 1,
            warnings: 1,
            infos: 0,
            ignoredFiles: [],
            configurations: [
                {
                    configuration: '/work/App/Strings.jsoncfg',
                    generatorInput: true,
                    defaultFile: '/work/App/Strings.json',
                    files: [
                        { path: '/work/App/Strings.json', origin: 'defaultFile', locale: '', checkedLocales: [''], entries: 2, errors: 0, warnings: 0, infos: 0 },
                        { path: '/work/App/Strings_de.json', origin: 'namingConvention', locale: 'de', checkedLocales: ['de'], entries: 2, errors: 1, warnings: 1, infos: 0 },
                    ],
                    messages: [
                        { severity: 'error', code: 'TLUMACHVAL005', message: 'Broken.', file: '/work/App/Strings_de.json', line: 3, column: 7 },
                        { severity: 'warning', code: 'TLUMACHVAL007', message: 'Extra key.', file: '/work/App/Strings_de.json', line: 0, column: 0 },
                        { severity: 'error', code: 'TLUMACHVAL003', message: 'No class.', file: null, line: 0, column: 0 },
                    ],
                },
            ],
        },
    ],
};

describe('parseReport', () => {
    it('reads a report', () => {
        const report = parseReport(JSON.stringify(sample));
        assert.deepEqual(report, sample);
    });

    it('skips text before the report', () => {
        const report = parseReport('Restoring...\n' + JSON.stringify(sample) + '\n');
        assert.equal(report.projects.length, 1);
    });

    it('rejects an empty output', () => {
        assert.throws(() => parseReport(''), /did not produce a report/);
    });

    it('rejects invalid JSON', () => {
        assert.throws(() => parseReport('{ "version": '), /cannot be read/);
    });

    it('rejects another version', () => {
        assert.throws(() => parseReport(JSON.stringify({ ...sample, version: REPORT_VERSION + 1 })), /version/);
    });

    it('rejects a report without projects', () => {
        assert.throws(() => parseReport(JSON.stringify({ version: REPORT_VERSION })), /no projects/);
    });
});

describe('collectDiagnostics', () => {
    it('groups the messages by file with 0-based positions', () => {
        const diagnostics = collectDiagnostics(sample.projects[0]);

        const german = diagnostics.get('/work/App/Strings_de.json');
        assert.ok(german);
        assert.deepEqual(german, [
            { severity: 'error', code: 'TLUMACHVAL005', message: 'Broken.', line: 2, column: 6 },
            { severity: 'warning', code: 'TLUMACHVAL007', message: 'Extra key.', line: 0, column: 0 },
        ]);
    });

    it('attaches a message without a file to the configuration', () => {
        const diagnostics = collectDiagnostics(sample.projects[0]);
        assert.deepEqual(diagnostics.get('/work/App/Strings.jsoncfg')?.map(d => d.code), ['TLUMACHVAL003']);
    });
});

describe('summarize', () => {
    it('counts files, errors, and warnings', () => {
        assert.equal(summarize(sample), '2 files checked, 1 error, 1 warning');
    });

    it('tells when there are no configurations', () => {
        assert.equal(summarize({ version: REPORT_VERSION, errors: 0, warnings: 0, infos: 0, projects: [] }), 'no Tlumach configuration files found');
    });

    it('counts info messages when there are any', () => {
        assert.equal(summarize({ ...sample, infos: 2 }), '2 files checked, 1 error, 1 warning, 2 info messages');
    });
});

describe('buildRunnerCommand', () => {
    const extension = path.join(path.sep, 'ext');

    it('runs the published runner', () => {
        const commandLine = buildRunnerCommand(extension, 'dotnet', ['/a/A.csproj', '/b/B.csproj'], () => true);
        assert.equal(commandLine.command, 'dotnet');
        assert.deepEqual(commandLine.args, [
            path.join(extension, RUNNER_ASSEMBLY),
            'verify', '--project', '/a/A.csproj', '--project', '/b/B.csproj', '--json',
        ]);
    });

    it('runs the runner from its sources when it is not published', () => {
        const commandLine = buildRunnerCommand(extension, '/usr/bin/dotnet', ['/a/A.csproj'], () => false);
        assert.equal(commandLine.command, '/usr/bin/dotnet');
        assert.deepEqual(commandLine.args, [
            'run', '--project', path.join(extension, RUNNER_PROJECT), '--configuration', 'Release', '--verbosity', 'quiet', '--',
            'verify', '--project', '/a/A.csproj', '--json',
        ]);
    });
});
