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

using System.Reflection;

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

            internal static new string? GenerateClass(string path, string projectDir, Dictionary<string, string> options)
            {
                return Tlumach.Generator.BaseGenerator.GenerateClass(path, projectDir, options);
            }
        }

        [Fact]
        public void ShouldGenerateClass()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"), TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
        public void ShouldGenerateFilledFunction1()
        {
            IniParser.Use();
            ArbParser.Use();
            Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
            options.Add("UsingNamespace", "Tlumach");
            options.Add("CreateFilledMethods", "true");

            string configFile = Path.Combine(TestFilesPath, "Placeholders1.cfg");

            string? result = TestGenerator.GenerateClass(configFile, TestFilesPath, options);
            Assert.NotNull(result);

            var (ok, diags, assembly) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(
                    Environment.NewLine,
                    diags.Where(d => d.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }
            else
            {
                Assert.NotNull(assembly);

                Tlumach.Base.IniParser.Use();
                Tlumach.Base.ArbParser.Use();

                TranslationManager translationManager = new TranslationManager(configFile);
                translationManager.LoadFromDisk = true;
                translationManager.TranslationsDirectory = TestFilesPath;

                var type = assembly.GetType("Test.Translations.Strings+GreetingMessageTranslationUnit");
                var instance = Activator.CreateInstance(type, new object[] { translationManager, translationManager.DefaultConfiguration!, "greetingMessage", true });
                var method = type.GetMethod("Filled", new[] { typeof(string), typeof(int) });
                var resultObj = method!.Invoke(instance, new object[] { "Alice", 5 });
                Assert.NotNull(resultObj);
                Assert.IsType<string>(resultObj);
                string resultString = (string)resultObj;
                Assert.NotEqual(0, resultString.Length);
                Assert.Equal("Hello Alice, you have 5 unread messages.", resultString);
            }
        }

        [Fact]
        public void ShouldGenerateFilledFunction2()
        {
            IniParser.Use();
            TomlParser.Use();
            Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);
            options.Add("UsingNamespace", "Tlumach");
            options.Add("CreateFilledMethods", "true");

            string configFile = Path.Combine(TestFilesPath, "Placeholders2.cfg");

            string? result = TestGenerator.GenerateClass(configFile, TestFilesPath, options);
            Assert.NotNull(result);

            var (ok, diags, assembly) = RoslynCompileHelper.CompileToAssembly(result);

            if (!ok)
            {
                var msg = string.Join(
                    Environment.NewLine,
                    diags.Where(d => d.Severity >= Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
                         .Select(d => d.ToString()));
                Assert.True(ok, "Compilation failed:" + Environment.NewLine + msg);
            }
            else
            {
                Assert.NotNull(assembly);

                Tlumach.Base.IniParser.Use();
                Tlumach.Base.TomlParser.Use();

                TranslationManager translationManager = new TranslationManager(configFile);
                translationManager.LoadFromDisk = true;
                translationManager.TranslationsDirectory = TestFilesPath;

                var type = assembly.GetType("Test.Translations.Strings+GreetingMessageTranslationUnit");
                var instance = Activator.CreateInstance(type, new object[] { translationManager, translationManager.DefaultConfiguration!, "greetingMessage", true });
                var method = type.GetMethod("Filled", new[] { typeof(string), typeof(int) });
                var resultObj = method!.Invoke(instance, new object[] { "Alice", 5 });
                Assert.NotNull(resultObj);
                Assert.IsType<string>(resultObj);
                string resultString = (string)resultObj;
                Assert.NotEqual(0, resultString.Length);
                Assert.Equal("Hello Alice, you have 5 unread messages.", resultString);
            }
        }

        [Fact]
        public void ShouldGenerateClassWithDelayedUnits()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigDelayedGeneration.arbcfg"), TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
        public void ShouldGenerateClassInSubdirectory()
        {
            IniParser.Use();
            TomlParser.Use();
            string? result = TestGenerator.GenerateClass("Translations\\Strings.cfg", Path.GetFullPath("..\\..\\.."), "Tlumach");
            Assert.NotNull(result);

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
        public void ShouldGenerateClassInSubdirectoryOfProject()
        {
            IniParser.Use();
            TomlParser.Use();
            string? result = TestGenerator.GenerateClass("..\\..\\..\\Translations\\Strings.cfg", Path.GetFullPath("..\\..\\.."), "Tlumach");
            Assert.NotNull(result);

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
        public void ShouldNotGenerateClassWithTemplatedUnits()
        {
            IniParser.Use();
            TomlParser.Use();
            string? result = TestGenerator.GenerateClass("FunctionStrings.cfg", TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            Assert.Equal(-1, result.IndexOf(", true);"));

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
        public void ShouldGenerateClassWithTemplatedUnits()
        {
            IniParser.Use();
            ArbParser.Use();

            string? result = TestGenerator.GenerateClass("ExtraParams.cfg", TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            Assert.Equal(-1, result.IndexOf(", false);"));

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
        public void ShouldGenerateClassWithOnlyKeys()
        {
            IniParser.Use();
            ArbParser.Use();

            string? result = TestGenerator.GenerateClass("NoUnits.cfg", TestFilesPath, "Tlumach");
            Assert.NotNull(result);

            Assert.NotEqual(-1, result.IndexOf("string CultureInfoKey"));
            Assert.Equal(-1, result.IndexOf("TranslationUnit CultureInfo"));

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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

            var (ok, diags, _) = RoslynCompileHelper.CompileToAssembly(result);

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
