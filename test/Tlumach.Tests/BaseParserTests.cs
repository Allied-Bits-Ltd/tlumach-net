// <copyright file="BaseParserTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "Base")]
    public class BaseParserTests
    {
        [Theory]
        [InlineData(TextFormat.BackslashEscaping, "TextFormat.BackslashEscaping")]
        [InlineData(TextFormat.DotNet, "TextFormat.DotNet")]
        public void ShouldConvertEscapeModeToStringRight(TextFormat textProcessingMode, string expected)
        {
            TranslationConfiguration config = new(null, string.Empty, null, null, null, textProcessingMode);
            Assert.Equal(expected, config.GetEscapeModeFullName());
        }

        [Theory]
        [ClassData(typeof(TemplateExpressionTestData))]
        public void ShouldDetectTemplatedString(string input, TextFormat escaping, bool? expected)
        {
            ArbParser.TextProcessingMode = escaping;

            if (expected is null)
                Assert.Throws<GenericParserException>(() => ArbParser.StringHasParameters(input, escaping));
            else
                Assert.Equal(expected, ArbParser.StringHasParameters(input, escaping));
        }
    }

    internal class TemplateExpressionTestData : TheoryData<string, TextFormat, bool?>
    {
        public TemplateExpressionTestData()
        {
            var testConditions = new (string input, TextFormat escaping, bool? expected)[]
            {
                // Basic matches
                ("{}", TextFormat.None, false),
                ("{abc}", TextFormat.None, false),
                ("a { b } c", TextFormat.None, false),
                ("} {", TextFormat.None, false),
                ("{ }", TextFormat.None, false),

                ("{}", TextFormat.BackslashEscaping, false),
                ("{abc}", TextFormat.BackslashEscaping, false),
                ("a { b } c", TextFormat.BackslashEscaping, false),
                ("} {", TextFormat.BackslashEscaping, false),
                ("{ }", TextFormat.BackslashEscaping, false),

                ("{}", TextFormat.Arb, true),
                ("{abc}", TextFormat.Arb, true),
                ("a { b } c", TextFormat.Arb, true),
                ("} {", TextFormat.Arb, null), // Fails because { isn't closed
                ("{ }", TextFormat.Arb, true),

                ("{}", TextFormat.DotNet, true),
                ("{abc}", TextFormat.DotNet, true),
                ("a { b } c", TextFormat.DotNet, true),
                ("} {", TextFormat.DotNet, null), // Fails because { isn't closed
                ("{ }", TextFormat.DotNet, true),

                // Duplicated braces (ignored)
                ("{{}}", TextFormat.None, false),
                ("{{abc}}", TextFormat.None, false),
                ("a {{ b }} c", TextFormat.None, false),
                ("{{ { } }}", TextFormat.None, false),
                ("a { {{b}} } c", TextFormat.None, false),

                ("{{}}", TextFormat.BackslashEscaping, false),
                ("{{abc}}", TextFormat.BackslashEscaping, false),
                ("a {{ b }} c", TextFormat.BackslashEscaping, false),
                ("{{ { } }}", TextFormat.BackslashEscaping, false),
                ("a { {{b}} } c", TextFormat.BackslashEscaping, false),

                ("{{}}", TextFormat.Arb,  true),
                ("{{abc}}", TextFormat.Arb, true),
                ("a {{ b }} c", TextFormat.Arb, true),
                ("{{ { } }}", TextFormat.Arb, true),
                ("a { {{b}} } c", TextFormat.Arb, true),

                ("{{}}", TextFormat.DotNet, false),
                ("{{abc}}", TextFormat.DotNet, false),
                ("a {{ b }} c", TextFormat.DotNet, false),
                ("{{ { } }}", TextFormat.DotNet, true), // The inner { } is valid
                ("a { {{b}} } c", TextFormat.DotNet, true), // The outer { } is valid

                // Quotes (ignore brackets inside)
                ("'{abc}'", TextFormat.None, false),
                ("a '{ b }' c", TextFormat.None, false),
                ("a { 'b' } c", TextFormat.None, false),

                ("'{abc}'", TextFormat.BackslashEscaping, false),
                ("a '{ b }' c", TextFormat.BackslashEscaping, false),
                ("a { 'b' } c", TextFormat.BackslashEscaping, false),

                ("'{abc}'", TextFormat.Arb, false),
                ("a '{ b }' c", TextFormat.Arb, false),
                ("a { 'b' } c", TextFormat.Arb, true), // Brackets are outside quotes

                ("'{abc}'", TextFormat.DotNet, true),
                ("a '{ b }' c", TextFormat.DotNet, true),
                ("a { 'b' } c", TextFormat.DotNet,  true), // Brackets are outside quotes

                // Duplicated quotes (escaped)
                ("''{abc}''", TextFormat.None, false), // '' is not a toggle, so { } is valid
                ("a ''{ b }'' c", TextFormat.None, false),
                ("a '{ '' }' c", TextFormat.None, false),
                ("a { '' } c", TextFormat.None, false),

                ("''{abc}''", TextFormat.BackslashEscaping, false), // '' is not a toggle, so { } is valid
                ("a ''{ b }'' c", TextFormat.BackslashEscaping, false),
                ("a '{ '' }' c", TextFormat.BackslashEscaping, false),
                ("a { '' } c", TextFormat.BackslashEscaping, false),

                ("''{abc}''", TextFormat.Arb, true), // '' is not a toggle, so { } is valid
                ("a ''{ b }'' c", TextFormat.Arb, true),
                ("a '{ '' }' c", TextFormat.Arb, false), // { and } are inside quotes
                ("a { '' } c", TextFormat.Arb, true), // '' is inside valid { }

                ("''{abc}''", TextFormat.DotNet, true),
                ("a ''{ b }'' c", TextFormat.DotNet,  true),
                ("a '{ '' }' c", TextFormat.DotNet, true),
                ("a { '' } c", TextFormat.DotNet, true),

                // Mixed and complex cases
                ("a { '}' } c", TextFormat.None, false),
                ("a { '{' } c", TextFormat.None, false),
                ("a '}' { b } c", TextFormat.None, false),
                ("a '{' { b } c", TextFormat.None, false),
                ("'{ { } }'", TextFormat.None, false),
                ("abc", TextFormat.None, false),
                ("", TextFormat.None, false),
                ("{", TextFormat.None, false),
                ("}", TextFormat.None, false),
                ("{{", TextFormat.None, false),
                ("}}", TextFormat.None, false),

                ("a { '}' } c", TextFormat.BackslashEscaping, false),
                ("a { '{' } c", TextFormat.BackslashEscaping, false),
                ("a '}' { b } c", TextFormat.BackslashEscaping, false),
                ("a '{' { b } c", TextFormat.BackslashEscaping, false),
                ("'{ { } }'", TextFormat.BackslashEscaping, false),
                ("abc", TextFormat.BackslashEscaping, false),
                ("", TextFormat.BackslashEscaping, false),
                ("{", TextFormat.BackslashEscaping, false),
                ("}", TextFormat.BackslashEscaping, false),
                ("{{", TextFormat.BackslashEscaping, false),
                ("}}", TextFormat.BackslashEscaping, false),

                ("a { '}' } c", TextFormat.Arb,  true), // Quoted '}' doesn't count, final '}' does
                ("a { '{' } c", TextFormat.Arb, true), // Quoted '{' doesn't count
                ("a '}' { b } c", TextFormat.Arb, true), // Quoted '}' is fine
                ("a '{' { b } c", TextFormat.Arb, true), // Quoted '{' is fine
                ("'{ { } }'", TextFormat.Arb, false), // Quoted sequence is fine
                ("abc", TextFormat.Arb, false),
                ("", TextFormat.Arb, false),
                ("{", TextFormat.Arb, null),
                ("}", TextFormat.Arb, null),
                ("{{", TextFormat.Arb, null),
                ("}}", TextFormat.Arb, null),

                ("a { '}' } c", TextFormat.DotNet, true),
                ("a { '{' } c", TextFormat.DotNet, true),
                ("a '}' { b } c", TextFormat.DotNet, null),
                ("a '{' { b } c", TextFormat.DotNet, true),
                ("'{ { } }'", TextFormat.DotNet, true),
                ("'{ {a} }'", TextFormat.DotNet, true),
                ("abc", TextFormat.DotNet, false),
                ("", TextFormat.DotNet, false),
                ("{", TextFormat.DotNet, null),
                ("}", TextFormat.DotNet, null),
                ("{{", TextFormat.DotNet, false),
                ("}}", TextFormat.DotNet, false),
            };
            foreach (var testCondition in testConditions)
            {
                Add(testCondition.input, testCondition.escaping, testCondition.expected);
            }
        }
    }
}
