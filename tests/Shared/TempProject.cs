// <copyright file="TempProject.cs" company="Allied Bits Ltd.">
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

using System.Text;

using Tlumach.Validation;

namespace Tlumach.TestUtilities;

/// <summary>
/// A project in a new temporary directory. Each test writes the files it needs, so that the data of a test is next to its assertions.
/// </summary>
internal sealed class TempProject : IDisposable
{
    private readonly List<ProjectFile> _items = [];

    private readonly string _root;

    public TempProject(string projectName = "App")
    {
        // The project is one level below the temporary root, so that files outside the project directory ("../Shared/...") stay inside the root
        _root = Path.Combine(Path.GetTempPath(), "TlumachExtensionTests", Guid.NewGuid().ToString("N"));
        Directory = Path.Combine(_root, projectName);
        System.IO.Directory.CreateDirectory(Directory);
        ProjectPath = Path.Combine(Directory, projectName + ".csproj");
        WriteProjectFile(string.Empty);
    }

    /// <summary>Writes an SDK-style project file with the given content of its item groups.</summary>
    public void WriteProjectFile(string items)
    {
        File.WriteAllText(ProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                {items}
              </ItemGroup>
            </Project>
            """);
    }

    public string Directory { get; }

    public string ProjectPath { get; }

    public IReadOnlyList<ProjectFile> Items => _items;

    /// <summary>Writes a file without adding it to the project.</summary>
    public string Write(string relativePath, string content)
    {
        string path = Path.GetFullPath(Path.Combine(Directory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }

    /// <summary>Writes a file and adds it to the project with the given item type.</summary>
    public string Add(string relativePath, string content, string itemType = "None", string? link = null, IReadOnlyDictionary<string, string>? metadata = null)
    {
        string path = Write(relativePath, content);
        _items.Add(new ProjectFile(path, itemType, link, metadata));
        return path;
    }

    /// <summary>Writes a configuration and adds it as a generator input.</summary>
    public string AddConfig(string relativePath, string content) => Add(relativePath, content, ProjectFile.AdditionalFilesItemType);

    public string PathOf(string relativePath) => Path.Combine(Directory, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public ProjectValidationReport Validate() => TranslationValidator.ValidateProject(ProjectPath, _items);

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // a leftover temporary directory does not affect the result
        }
        catch (UnauthorizedAccessException)
        {
            // a leftover temporary directory does not affect the result
        }
    }

    /// <summary>A configuration in JSON with the settings that the generator requires.</summary>
    public static string JsonConfig(string defaultFile, string? translations = null, string? extra = null)
    {
        StringBuilder builder = new();
        builder.AppendLine("{");
        builder.Append("  \"defaultFile\": \"").Append(defaultFile).AppendLine("\",");
        if (extra is not null)
            builder.Append("  ").Append(extra).AppendLine(",");
        if (translations is not null)
            builder.Append("  \"translations\": { ").Append(translations).AppendLine(" },");
        builder.AppendLine("  \"generatedNamespace\": \"App\",");
        builder.AppendLine("  \"generatedClass\": \"Strings\"");
        builder.AppendLine("}");
        return builder.ToString();
    }
}
