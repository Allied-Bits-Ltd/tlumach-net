// <copyright file="copy-templates.js" company="Allied Bits Ltd.">
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

// Copies the shared Tlumach file templates into the extension folder so that they are packaged
// into the .vsix. The same directory feeds the Visual Studio Add New Item templates, which keeps
// the list of supported Tlumach formats in exactly one place.

const fs = require('fs');
const path = require('path');

const source = path.resolve(__dirname, '..', '..', 'Shared', 'FileTemplates');
const destination = path.resolve(__dirname, '..', 'templates');

if (!fs.existsSync(source)) {
    console.error(`copy-templates: the shared template directory '${source}' does not exist.`);
    process.exit(1);
}

fs.rmSync(destination, { recursive: true, force: true });
fs.mkdirSync(destination, { recursive: true });

let count = 0;
for (const entry of fs.readdirSync(source, { withFileTypes: true })) {
    if (entry.isFile()) {
        fs.copyFileSync(path.join(source, entry.name), path.join(destination, entry.name));
        count++;
    }
}

console.log(`copy-templates: copied ${count} file(s) to ${destination}`);
