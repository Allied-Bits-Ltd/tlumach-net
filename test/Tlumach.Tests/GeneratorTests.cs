// <copyright file="GeneratorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Tlumach.Base;
using Tlumach.Generator;

namespace Tlumach.Tests
{
    public class GeneratorTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\Generator";

        internal class TestGenerator : Tlumach.Generator.Generator
        {
            internal static new string? GenerateClass(string path, string projectDir, string usingNamespace)
            {
                Dictionary<string, string> options = new();
                options.Add("UsingNamespace", usingNamespace);
                return Tlumach.Base.BaseGenerator.GenerateClass(path, projectDir,  options);
            }
        }

        [Fact]
        public void ShouldGenerateClass()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"), TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            var (ok, diags, asm) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(Environment.NewLine,
                    diags.Where(d => d.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }
        }

        [Fact]
        public void ShouldFailOnIncompleteConfig()
        {
            ArbParser.Use();
            Assert.Throws<ParserConfigException>(() => TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigWithoutNamespace.arbcfg"), TestFilesPath, "Tlumach"));
            Assert.Throws<ParserConfigException>(() => TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigWithoutClassName.arbcfg"), TestFilesPath, "Tlumach"));

        }

        [Fact]
        public void ShouldGenerateClassFromSpecifiedDirectory()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass("ValidConfigWithGroups.arbcfg", TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            var (ok, diags, asm) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(Environment.NewLine,
                    diags.Where(d => d.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }
        }
    }

    internal static class RoslynCompileHelper
    {
        /// <summary>
        /// Compiles C# source to an in-memory assembly. Returns (success, diagnostics, assembly).
        /// </summary>
        public static (bool Success, ImmutableArray<Diagnostic> Diagnostics, Assembly? Assembly)
            CompileToAssembly(string source,
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
                options: options ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Release).WithOverflowChecks(true)
            );

            using var peStream = new MemoryStream();
            var emitResult = compilation.Emit(peStream);

            if (!emitResult.Success)
                return (false, emitResult.Diagnostics, null);

            peStream.Position = 0;
            var asm = Assembly.Load(peStream.ToArray());
            return (true, emitResult.Diagnostics, asm);
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

                // You can also filter down to the assemblies you know you need:
                // var file = Path.GetFileName(path);
                // if (!file.StartsWith("System.", StringComparison.Ordinal) &&
                //     file is not "mscorlib.dll" and not "netstandard.dll")
                //     continue;

                MetadataReference? reference = null;
                try { reference = MetadataReference.CreateFromFile(path); }
                catch { /* ignore broken candidates */ }

                if (reference is not null)
                    yield return reference;
            }
        }
    }
}
