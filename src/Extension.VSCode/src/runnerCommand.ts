// <copyright file="runnerCommand.ts" company="Allied Bits Ltd.">
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

// Builds the command line of the .NET validation runner. This module does not import 'vscode', so that
// it can be tested with plain Node.js ('npm test').

import * as path from 'path';

/** The assembly of the runner, as 'npm run build-runner' publishes it into the extension. */
export const RUNNER_ASSEMBLY = path.join('runner', 'bin', 'publish', 'Tlumach.Extension.Runner.dll');

/** The project of the runner, used when the extension runs from its sources without a published runner. */
export const RUNNER_PROJECT = path.join('runner', 'Runner.csproj');

export interface CommandLine {
    command: string;
    args: string[];
}

/**
 * Builds the command line that validates the given projects and writes a JSON report.
 * @param extensionPath The directory of the extension.
 * @param dotnet The .NET host to use ("dotnet" or a full path).
 * @param projects The project files to validate.
 * @param exists Checks whether a file exists (injected for tests).
 */
export function buildRunnerCommand(
    extensionPath: string,
    dotnet: string,
    projects: string[],
    exists: (file: string) => boolean
): CommandLine {
    const runnerArgs = ['verify', ...projects.flatMap(project => ['--project', project]), '--json'];

    const published = path.join(extensionPath, RUNNER_ASSEMBLY);
    if (exists(published)) {
        return { command: dotnet, args: [published, ...runnerArgs] };
    }

    // Development: build and run the runner from its sources
    return {
        command: dotnet,
        args: ['run', '--project', path.join(extensionPath, RUNNER_PROJECT), '--configuration', 'Release', '--verbosity', 'quiet', '--', ...runnerArgs],
    };
}
