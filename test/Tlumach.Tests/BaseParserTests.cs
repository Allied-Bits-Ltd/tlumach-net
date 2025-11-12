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
        [InlineData(TemplateStringEscaping.Backslash, "TemplateStringEscaping.Backslash")]
        [InlineData(TemplateStringEscaping.DotNet, "TemplateStringEscaping.DotNet")]
        public void ShouldConvertTemplateEscapeModeToStringRight(TemplateStringEscaping templateEscapeMode, string expected)
        {
            TranslationConfiguration config = new(null, string.Empty, null, null, null, templateEscapeMode);
            Assert.Equal(expected, config.GetTemplateEscapeModeFullName());
        }

        [Theory]
        [ClassData(typeof(TemplateExpressionTestData))]
        public void ShouldDetectTemplatedString(string input, TemplateStringEscaping escaping, bool? expected)
        {
            ArbParser.TemplateEscapeMode = escaping;

            if (expected is null)
                Assert.Throws<GenericParserException>(() => ArbParser.StringHasParameters(input, escaping));
            else
                Assert.Equal(expected, ArbParser.StringHasParameters(input, escaping));
        }

    }
}
