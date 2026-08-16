// <copyright file="projectFile.ts" company="Allied Bits Ltd.">
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

import { TlumachFormat } from './templates';

/** SDK-style projects glob loose files into None; legacy projects do not. */
export function isSdkStyleProject(projectText: string): boolean {
    return /<Project[^>]*\sSdk\s*=/.test(projectText) || /Sdk\.props/.test(projectText);
}

/** Reads RootNamespace from a project file, falling back to the project file name. */
export function readRootNamespace(projectPath: string): string {
    try {
        const text = fs.readFileSync(projectPath, 'utf8');
        const match = /<RootNamespace>\s*([^<]+?)\s*<\/RootNamespace>/i.exec(text);
        if (match) {
            return match[1];
        }
    } catch {
        // fall through to the file name
    }
    return path.basename(projectPath, path.extname(projectPath));
}

/**
 * Produces the ItemGroup that adds a created file to a project with the item type Tlumach
 * expects, mirroring what the Visual Studio item templates produce.
 *
 * Translation files become EmbeddedResource items and configuration files become AdditionalFiles
 * items ("C# analyzer additional file"). A ResX translation file is a special case: the SDK
 * already globs *.resx as EmbeddedResource, so the project only needs an Update element marking
 * the item Non-Resx, which stops the toolchain from compiling the file and moving it into a
 * satellite assembly.
 */
export function buildItemGroup(
    relativePath: string,
    itemType: string,
    format: TlumachFormat,
    sdkStyle: boolean,
    newLine: string
): string {
    const metadata = Object.entries(format.itemMetadata ?? {});
    const lines: string[] = ['', '  <ItemGroup>'];

    if (format.duplicateExtension) {
        // The default *.resx glob already created the item; only its metadata has to change.
        lines.push(`    <${itemType} Update="${relativePath}">`);
        for (const [name, value] of metadata) {
            lines.push(`      <${name}>${value}</${name}>`);
        }
        lines.push(`    </${itemType}>`);
    } else {
        if (sdkStyle) {
            lines.push(`    <None Remove="${relativePath}" />`);
        }
        if (metadata.length === 0) {
            lines.push(`    <${itemType} Include="${relativePath}" />`);
        } else {
            lines.push(`    <${itemType} Include="${relativePath}">`);
            for (const [name, value] of metadata) {
                lines.push(`      <${name}>${value}</${name}>`);
            }
            lines.push(`    </${itemType}>`);
        }
    }

    lines.push('  </ItemGroup>', '');
    return lines.join(newLine);
}

/**
 * Adds the created file to the project file. Returns false when the project file already
 * mentions the file, in which case nothing is written.
 */
export function addProjectItem(
    projectPath: string,
    filePath: string,
    itemType: string,
    format: TlumachFormat
): boolean {
    const text = fs.readFileSync(projectPath, 'utf8');
    const relative = path.relative(path.dirname(projectPath), filePath).split('/').join('\\');

    if (text.includes(`"${relative}"`)) {
        return false;
    }

    const closing = text.lastIndexOf('</Project>');
    if (closing < 0) {
        throw new Error('the project file has no closing </Project> element');
    }

    const newLine = text.includes('\r\n') ? '\r\n' : '\n';
    const itemGroup = buildItemGroup(relative, itemType, format, isSdkStyleProject(text), newLine);

    fs.writeFileSync(projectPath, text.slice(0, closing) + itemGroup + text.slice(closing), 'utf8');
    return true;
}
