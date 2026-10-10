// <copyright file="TestDirectories.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
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

using System.Reflection;

namespace Tlumach.TestUtilities
{
    /// <summary>
    /// The directories of the test projects, so that the tests find their data wherever the build puts the test assembly (e.g. with --artifacts-path).
    /// <para>A test project that links this file records its directory in the assembly: <c>&lt;AssemblyMetadata Include="TestProjectDirectory" Value="$(MSBuildProjectDirectory)" /&gt;</c>.</para>
    /// </summary>
    internal static class TestDirectories
    {
        /// <summary>
        /// Gets the directory of the test project.
        /// </summary>
        public static string Project { get; } = FindProjectDirectory();

        /// <summary>
        /// Gets the directory with the test projects (tests/).
        /// </summary>
        public static string Tests => Path.GetDirectoryName(Project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? Project;

        private static string FindProjectDirectory()
        {
            foreach (AssemblyMetadataAttribute attribute in typeof(TestDirectories).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (string.Equals(attribute.Key, "TestProjectDirectory", StringComparison.Ordinal) && !string.IsNullOrEmpty(attribute.Value))
                    return attribute.Value!;
            }

            throw new InvalidOperationException("The test assembly does not record its project directory (AssemblyMetadata TestProjectDirectory).");
        }
    }
}
