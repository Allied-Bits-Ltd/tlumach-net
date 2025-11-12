// <copyright file="GeneratorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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
}
