// <copyright file="templates.ts" company="Allied Bits Ltd.">
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

/**
 * The catalogue of Tlumach file templates and their payloads is maintained in
 * src/Shared/FileTemplates and copied into the packaged extension by scripts/copy-templates.js.
 * The Visual Studio extension builds its Add New Item templates from the very same directory,
 * so the list of supported formats lives in exactly one place.
 */
export const TEMPLATES_DIR_NAME = 'templates';

/** A single format offered by the "New Tlumach ... File" commands. */
export interface TlumachFormat {
    /** Short identifier of the format, e.g. "json". */
    id: string;
    /** Display name of the format, e.g. "JSON". */
    name: string;
    /** File extension including the leading dot, e.g. ".json". */
    extension: string;
    /** Name of the payload file inside the templates directory. */
    template: string;
    /** Suggested file name, including the extension. */
    defaultName: string;
    /** One-line description shown in the Quick Pick. */
    description: string;
    /**
     * When set, the created file carries the extension twice (the ResX case), because the
     * .NET toolchain would otherwise compile the file and move it into a satellite assembly.
     */
    duplicateExtension?: boolean;
    /** Extra MSBuild metadata that the project item needs, e.g. { "Type": "Non-Resx" }. */
    itemMetadata?: Record<string, string>;
}

/** One of the two kinds of Tlumach files that can be created. */
export interface TlumachFormatGroup {
    /** MSBuild item type the created file must be added with. */
    itemType: string;
    /** The formats belonging to this kind. */
    formats: TlumachFormat[];
}

interface FormatCatalog {
    translations: TlumachFormatGroup;
    configurations: TlumachFormatGroup;
}

let cachedCatalog: FormatCatalog | undefined;

function templatesDir(extensionPath: string): string {
    return path.join(extensionPath, TEMPLATES_DIR_NAME);
}

/** Reads (and caches) the shared format catalogue shipped with the extension. */
export function loadCatalog(extensionPath: string): FormatCatalog {
    if (!cachedCatalog) {
        const catalogPath = path.join(templatesDir(extensionPath), 'formats.json');
        cachedCatalog = JSON.parse(fs.readFileSync(catalogPath, 'utf8')) as FormatCatalog;
    }
    return cachedCatalog;
}

/** Returns the group describing translation files. */
export function translationFormats(extensionPath: string): TlumachFormatGroup {
    return loadCatalog(extensionPath).translations;
}

/** Returns the group describing configuration files. */
export function configurationFormats(extensionPath: string): TlumachFormatGroup {
    return loadCatalog(extensionPath).configurations;
}

/**
 * Reads the payload of a format and substitutes the same tokens that the Visual Studio
 * item templates use, so that both integrations produce identical files.
 */
export function renderTemplate(
    extensionPath: string,
    format: TlumachFormat,
    fileInputName: string,
    rootNamespace: string
): string {
    const payload = fs.readFileSync(path.join(templatesDir(extensionPath), format.template), 'utf8');
    return payload
        .split('$fileinputname$').join(fileInputName)
        .split('$rootnamespace$').join(rootNamespace);
}

/**
 * Builds the final file name for a format from the name the user typed. The user may type
 * the name with or without the extension; formats that need a duplicate extension (ResX)
 * get it appended twice.
 */
export function buildFileName(format: TlumachFormat, typedName: string): string {
    const suffix = format.duplicateExtension
        ? format.extension + format.extension
        : format.extension;

    const lowered = typedName.toLowerCase();
    if (lowered.endsWith(suffix.toLowerCase())) {
        return typedName;
    }
    if (lowered.endsWith(format.extension.toLowerCase())) {
        // "Strings.resx" typed for a format that needs "Strings.resx.resx".
        return format.duplicateExtension ? typedName + format.extension : typedName;
    }
    return typedName + suffix;
}

/** Strips the extension(s) that {@link buildFileName} appends, yielding the $fileinputname$ value. */
export function fileInputNameOf(format: TlumachFormat, fileName: string): string {
    let result = fileName;
    if (result.toLowerCase().endsWith(format.extension.toLowerCase())) {
        result = result.slice(0, result.length - format.extension.length);
    }
    return result;
}
