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
                return Tlumach.Base.BaseGenerator.GenerateClass(path, projectDir, usingNamespace);
            }
        }

        [Fact]
        public void ShouldGenerateEmptyClass()
        {
            ArbParser.Use();
            string? result = TestGenerator.GenerateClass(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"), TestFilesPath, "Tlumach");
            Assert.NotNull(result);
        }
    }
}
