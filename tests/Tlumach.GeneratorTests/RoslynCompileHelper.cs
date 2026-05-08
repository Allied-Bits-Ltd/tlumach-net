// <copyright file="RoslynCompileHelper.cs" company="Allied Bits Ltd.">
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

using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tlumach.Tests
{
    internal static class RoslynCompileHelper
    {
        /// <summary>
        /// Compiles C# source to an in-memory assembly. Returns (success, diagnostics, assembly).
        /// </summary>
        public static (bool Success, ImmutableArray<Diagnostic> Diagnostics, Assembly? assembly) CompileToAssembly(
                string source,
                IEnumerable<MetadataReference>? additionalReferences = null,
                CSharpCompilationOptions? options = null,
                LanguageVersion langVersion = LanguageVersion.Latest)
        {
            var parseOptions = new CSharpParseOptions(languageVersion: langVersion);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

            // Get a broad set of framework references from the current runtime (good enough for tests).
            var frameworkRefs = GetTrustedPlatformAssemblyReferences();

            var refs = (additionalReferences is null)
                ? frameworkRefs
                : frameworkRefs.Concat(additionalReferences);

            var compilation = CSharpCompilation.Create(
                                assemblyName: $"GenTest_{Guid.NewGuid():N}",
                                syntaxTrees: new[] { syntaxTree },
                                references: refs,
                                options: options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Release).WithOverflowChecks(true));

            using var peStream = new MemoryStream();
            var emitResult = compilation.Emit(peStream);

            if (!emitResult.Success)
                return (false, emitResult.Diagnostics, null);

            peStream.Position = 0;
            Assembly assembly = Assembly.Load(peStream.ToArray());
            return (true, emitResult.Diagnostics, assembly);
        }

        /// <summary>
        /// Creates MetadataReferences for the “trusted platform assemblies” of the current runtime.
        /// This avoids hunting for targeting packs in tests.
        /// </summary>
        public static IEnumerable<MetadataReference> GetTrustedPlatformAssemblyReferences()
        {
            var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            if (string.IsNullOrEmpty(tpa))
                yield break;

            foreach (var path in tpa.Split(Path.PathSeparator))
            {
                // Optional filter: include only common .dlls to keep the set smaller.
                if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                MetadataReference? reference = null;
                try
                {
                    reference = MetadataReference.CreateFromFile(path);
                }
                catch(IOException)
                {
                    /* ignore broken candidates */
                }

                if (reference is not null)
                    yield return reference;
            }
        }
    }
}
