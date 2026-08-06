// <copyright file="Item17_LeadingNumberTests.cs" company="Allied Bits Ltd.">
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

using System.Collections.Generic;
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.Tests.Optimizations
{
    /// <summary>
    /// Optimization item 17 — removing the substring allocation from
    /// <c>Utils.GetLeadingNonNegativeNumber</c>.
    /// <para>
    /// The method has a compact but easy-to-get-wrong contract: it returns the leading run of digits and
    /// how many characters that run occupied, and reports <c>-1</c> with <c>charsUsed = 0</c> for no
    /// digits, a non-digit prefix, a sign, leading whitespace, and — importantly — a digit run that
    /// overflows <see cref="int"/>. Every one of those is pinned here.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "17")]
    [Collection("Optimizations")]
    public class Item17_LeadingNumberTests
    {
        [Theory]
        [InlineData("0", 0, 1)]
        [InlineData("7", 7, 1)]
        [InlineData("12", 12, 2)]
        [InlineData("007", 7, 3)]
        [InlineData("2147483647", 2147483647, 10)]
        public void PureDigitRuns_AreParsedInFull(string input, int expectedValue, int expectedCharsUsed)
        {
            int value = Utils.GetLeadingNonNegativeNumber(input, out int charsUsed);

            Assert.Equal(expectedValue, value);
            Assert.Equal(expectedCharsUsed, charsUsed);
        }

        [Theory]
        [InlineData("12abc", 12, 2)]
        [InlineData("0:N2", 0, 1)]
        [InlineData("3,10", 3, 1)]
        public void DigitRunFollowedByText_StopsAtTheFirstNonDigit(string input, int expectedValue, int expectedCharsUsed)
        {
            int value = Utils.GetLeadingNonNegativeNumber(input, out int charsUsed);

            Assert.Equal(expectedValue, value);
            Assert.Equal(expectedCharsUsed, charsUsed);
        }

        /// <summary>
        /// Everything that is not a leading digit run yields the same "no number" answer. Note that a
        /// leading sign and leading whitespace are both rejected, even though
        /// <see cref="NumberStyles.Number"/> would normally accept them — the digit scan runs first.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("name")]
        [InlineData("-5")]
        [InlineData("+5")]
        [InlineData(" 12")]
        [InlineData(".5")]
        public void NonDigitInputs_ReportNoNumber(string input)
        {
            int value = Utils.GetLeadingNonNegativeNumber(input, out int charsUsed);

            Assert.Equal(-1, value);
            Assert.Equal(0, charsUsed);
        }

        /// <summary>
        /// A digit run too large for <see cref="int"/> is reported as "no number", with
        /// <c>charsUsed = 0</c> — not as a truncated or saturated value. Any replacement must reproduce
        /// this, including for very long runs.
        /// </summary>
        [Theory]
        [InlineData("2147483648")]
        [InlineData("99999999999")]
        [InlineData("123456789012345678901234567890")]
        public void OverflowingDigitRuns_ReportNoNumber(string input)
        {
            int value = Utils.GetLeadingNonNegativeNumber(input, out int charsUsed);

            Assert.Equal(-1, value);
            Assert.Equal(0, charsUsed);
        }

        /// <summary>
        /// The parse is culture-independent: only ASCII digits are recognised, and no group separator is
        /// ever consumed.
        /// </summary>
        [Fact]
        public void Parsing_IsCultureIndependent()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.Equal(1234, Utils.GetLeadingNonNegativeNumber("1234", out int charsUsed));
                Assert.Equal(4, charsUsed);

                // A German group separator must not be consumed as part of the number.
                Assert.Equal(1, Utils.GetLeadingNonNegativeNumber("1.234", out int separated));
                Assert.Equal(1, separated);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        /// <summary>
        /// The end-to-end behaviour that depends on this helper: in .NET mode, a numeric placeholder name
        /// is used as a positional index into the supplied values.
        /// </summary>
        [Fact]
        public void PositionalPlaceholders_ResolveThroughTheLeadingNumber()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{1} then {0}");

            Assert.Equal("second then first", entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, "first", "second"));
        }

        /// <summary>
        /// The dictionary overload uses <c>charsUsed</c> to slice a numeric prefix out of the placeholder
        /// name and retry the lookup with it.
        /// </summary>
        [Fact]
        public void DictionaryLookup_RetriesWithTheNumericPrefix()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0}");
            Dictionary<string, object?> values = new(StringComparer.Ordinal) { ["0"] = "byIndexKey" };

            Assert.Equal("byIndexKey", entry.ProcessTemplatedValue(CultureInfo.InvariantCulture, TextFormat.DotNet, values));
        }
    }
}
