// <copyright file="TemplateCheckerTests.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Tlumach.Validation;

using Tlumach.Base;

namespace Tlumach.Tests.Validation;

public class TemplateCheckerTests
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData("Hello {0}")]
    [InlineData("Hello {name}")]
    [InlineData("Total: {0:N2}")]
    [InlineData("Date: {0:yyyy-MM-dd}")]
    [InlineData("{0} {1} {2}")]
    [InlineData("It''s {x}")]
    [InlineData("{}")]
    public void ValidDotNetTemplatesPass(string text)
    {
        Assert.Null(Check(text, TextFormat.DotNet));
    }

    [Theory]
    [InlineData("Hello {name}")]
    [InlineData("{count, plural, one {# item} other {# items}}")]
    [InlineData("{count, plural, =0 {none} one {# item} other {# items}}")]
    [InlineData("{count, plural, offset:1 one {# item} other {# items}}")]
    [InlineData("{count, plural, one {{name} has # item} other {{name} has # items}}")]
    [InlineData("{count,plural,one{x}other{y}}")]
    [InlineData("{gender, select, male {He} female {She} other {They}}")]
    [InlineData("{when, date, short}")]
    [InlineData("{when, time, short}")]
    [InlineData("{n, number, integer}")]
    [InlineData("It''s {x}")]
    public void ValidArbTemplatesPass(string text)
    {
        Assert.Null(Check(text, TextFormat.Arb));
    }

    [Theory]
    [InlineData("{count, plural, one {# item}}", "other")]
    [InlineData("{gender, select, male {He} female {She}}", "other")]
    [InlineData("{count, foo, one {x}}", "foo")]
    [InlineData("{count, plural, one # item other # items}", "{")]
    [InlineData("Hello {0}", "Invalid placeholder name")]
    public void BrokenArbTemplatesAreErrors(string text, string expectedFragment)
    {
        TemplateProblem? problem = Check(text, TextFormat.Arb);

        Assert.NotNull(problem);
        Assert.True(problem.Definite);
        Assert.Contains(expectedFragment, problem.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("in the following text", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IcuMessageInDotNetModeIsAnErrorWithAHint()
    {
        TemplateProblem? problem = Check("{count, plural, one {# item} other {# items}}", TextFormat.DotNet);

        Assert.NotNull(problem);
        Assert.True(problem.Definite);
        Assert.Contains("Arb", problem.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatThatDependsOnTheValueTypeIsNotDefinite()
    {
        // "Q" is not a format of numbers or dates, but .NET ignores the format for strings
        TemplateProblem? problem = Check("Value: {0:Q}", TextFormat.DotNet);

        Assert.NotNull(problem);
        Assert.False(problem.Definite);
    }

    [Theory]
    [InlineData(TextFormat.None)]
    [InlineData(TextFormat.BackslashEscaping)]
    public void ModesWithoutPlaceholdersAreNotChecked(TextFormat mode)
    {
        TranslationEntry entry = new("Key", "{count, plural, one {# item}}", "{count, plural, one {# item}}") { ContainsPlaceholders = true };

        Assert.Null(TemplateChecker.Check(entry, Culture, mode));
    }

    [Fact]
    public void EntryWithoutPlaceholdersIsNotChecked()
    {
        TranslationEntry entry = new("Key", "{count, plural, one {# item}}", "{count, plural, one {# item}}") { ContainsPlaceholders = false };

        Assert.Null(TemplateChecker.Check(entry, Culture, TextFormat.Arb));
    }

    private static TemplateProblem? Check(string text, TextFormat mode)
    {
        TranslationEntry entry = new("Key", text, text) { ContainsPlaceholders = BaseParser.StringHasParameters(text, mode) };
        return TemplateChecker.Check(entry, Culture, mode);
    }
}
