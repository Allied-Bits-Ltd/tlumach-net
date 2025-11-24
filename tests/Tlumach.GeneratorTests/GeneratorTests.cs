// <copyright file="GeneratorTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Tlumach.Generator;

namespace Tlumach.Tests
{
    public class GeneratorTests
    {
        private const string TestFilesPath = "..\\..\\..\\TestData\\Generator";

        internal class TestGenerator : Tlumach.Generator.BaseGenerator
        {
            internal static string? GenerateClass(string path, string projectDir, string usingNamespace)
            {
                Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
                options.Add("UsingNamespace", usingNamespace);
                return Tlumach.Generator.BaseGenerator.GenerateClass(path, projectDir,  options);
            }
        }

        [Fact]
        public void ShouldGenerateClass()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"), TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            var (ok, diags) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(
                    Environment.NewLine,
                    diags.Where(d => d.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }
        }

        [Fact]
        public void ShouldGenerateClassWithDelayedUnits()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigDelayedGeneration.arbcfg"), TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            var (ok, diags) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(
                    Environment.NewLine,
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

            var (ok, diags) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(
                    Environment.NewLine,
                    diags.Where(d => d.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }
        }
    }
}
