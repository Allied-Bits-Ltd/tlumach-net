// <copyright file="ValidationFileSystem.cs" company="Allied Bits Ltd.">
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

#if GENERATOR
namespace Tlumach.Generator.Validation;
#else
namespace Tlumach.Validation;
#endif

/// <summary>
/// The file access of the validation, in one place.
/// <para>The validation is also compiled into the generator. The analyzer rules ban file access there (RS1035), because the compiler tracks only the files that it provides (AdditionalFiles) for incremental generation.
/// The translation files of a configuration are not among them: the generator reads them from the disk, as it already reads the default file (see <c>Utils.ReadFileFromDisk</c>).
/// So a change of a translation file is seen at the next build or at the next change of the configuration, and not as it is typed.</para>
/// </summary>
#pragma warning disable RS1035 // Do not use APIs banned for analyzers - see the summary
internal static class ValidationFileSystem
{
    public static bool FileExists(string path) => File.Exists(path);

    public static bool DirectoryExists(string path) => Directory.Exists(path);

    public static IEnumerable<string> EnumerateFiles(string directory) => Directory.EnumerateFiles(directory);

    public static string ReadAllText(string path) => File.ReadAllText(path, Encoding.UTF8);
}
#pragma warning restore RS1035
