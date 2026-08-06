// <copyright file="Item18_AppleFormatTests.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Tlumach.Base;

namespace Tlumach.Tests.Optimizations
{
    /// <summary>
    /// Optimization item 18 — removing the per-specifier <c>char.ToString()</c> allocation and replacing
    /// the <c>try</c>/<c>catch</c> type dispatch in the Apple (String Catalog) format path.
    /// <para>
    /// The <c>catch</c> currently swallows every exception type and falls back to plain <c>"{0}"</c>
    /// formatting. Any pattern-matched rewrite must keep that fallback for the same set of inputs, so the
    /// mismatched-type and overflow cases are pinned here alongside the happy path.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "18")]
    [Collection("Optimizations")]
    public class Item18_AppleFormatTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [Fact]
        public void ObjectSpecifier_SubstitutesTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("Hi %@!");

            Assert.Equal("Hi Ann!", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, "Ann"));
        }

        [Theory]
        [InlineData("n=%d", 42, "n=42")]
        [InlineData("n=%i", 42, "n=42")]
        [InlineData("n=%u", 7, "n=7")]
        [InlineData("s=%s", "str", "s=str")]
        public void SimpleSpecifiers_ProduceExpectedText(string template, object value, string expected)
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry(template);

            Assert.Equal(expected, entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, value));
        }

        [Fact]
        public void FloatSpecifier_UsesFixedPointFormatting()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("f=%f");

            Assert.Equal(
                "f=" + string.Format(Invariant, "{0:F}", 1234.5678),
                entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, 1234.5678));
        }

        /// <summary>
        /// The float specifier is culture-sensitive, and the number of decimals comes from the culture's
        /// number format, so the expectation is computed rather than hard-coded.
        /// </summary>
        [Fact]
        public void FloatSpecifier_HonoursCulture()
        {
            CultureInfo german = new("de-DE");
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("f=%f");

            Assert.Equal(
                "f=" + string.Format(german, "{0:F}", 1234.5678),
                entry.ProcessTemplatedValue(german, TextFormat.Apple, 1234.5678));
        }

        [Fact]
        public void HexadecimalSpecifiers_RespectCase()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%x %X");

            Assert.Equal("ff FF", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, 255, 255));
        }

        [Fact]
        public void OctalSpecifier_ConvertsToBaseEight()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%o");

            Assert.Equal("10", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, 8));
        }

        [Fact]
        public void ScientificAndGeneralSpecifiers_ProduceExpectedText()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%e|%g");

            Assert.Equal(
                string.Format(Invariant, "{0:e}", 1234.5) + "|" + string.Format(Invariant, "{0:g}", 1234.5),
                entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, 1234.5, 1234.5));
        }

        [Fact]
        public void DoubledPercent_IsALiteralPercent()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("100%% sure");

            Assert.Equal("100% sure", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, Array.Empty<object?>()));
        }

        [Fact]
        public void PositionalSpecifiers_ReorderTheArguments()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%2$@ %1$@");

            Assert.Equal("two one", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, "one", "two"));
        }

        [Fact]
        public void LengthModifiers_AreSkipped()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%lld");

            Assert.Equal("5", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, 5L));
        }

        /// <summary>
        /// A specifier with no supplied value is emitted verbatim, so the untranslated template is still
        /// readable.
        /// </summary>
        [Fact]
        public void MissingValue_EmitsTheSpecifierVerbatim()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("x%@y");

            Assert.Equal("x%@y", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, Array.Empty<object?>()));
        }

        /// <summary>
        /// String Catalog substitution tokens resolve by name through the resolver function.
        /// </summary>
        [Fact]
        public void SubstitutionToken_ResolvesByName()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("a %#@COUNT@ b");

            string result = entry.ProcessTemplatedValue(
                Invariant,
                TextFormat.Apple,
                (name, index) => string.Equals(name, "COUNT", StringComparison.Ordinal) ? "3 files" : null);

            Assert.Equal("a 3 files b", result);
        }

        [Fact]
        public void SubstitutionToken_WithNoValue_IsEmittedVerbatim()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("a %#@COUNT@ b");

            string result = entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, (name, index) => null);

            Assert.Equal("a %#@COUNT@ b", result);
        }

        /// <summary>
        /// This is the case that currently throws and catches once per specifier. The fallback output must
        /// be preserved exactly by any pattern-matched rewrite.
        /// </summary>
        [Theory]
        [InlineData("n=%d", "n=abc")]
        [InlineData("n=%i", "n=abc")]
        [InlineData("f=%f", "f=abc")]
        [InlineData("x=%x", "x=abc")]
        [InlineData("o=%o", "o=abc")]
        public void MismatchedType_FallsBackToPlainFormatting(string template, string expected)
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry(template);

            Assert.Equal(expected, entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, "abc"));
        }

        /// <summary>
        /// An overflowing value takes the same fallback route as a type mismatch.
        /// </summary>
        [Fact]
        public void OverflowingValue_FallsBackToPlainFormatting()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%d");

            string result = entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, double.MaxValue);

            Assert.Equal(string.Format(Invariant, "{0}", double.MaxValue), result);
        }

        /// <summary>
        /// An unrecognised specifier is emitted verbatim rather than consuming an argument.
        /// </summary>
        [Fact]
        public void UnrecognisedSpecifier_IsEmittedVerbatim()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%q done");

            Assert.Equal("%q done", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, "unused"));
        }

        /// <summary>
        /// Several specifiers in one template consume the arguments in order.
        /// </summary>
        [Fact]
        public void MultipleSpecifiers_ConsumeArgumentsInOrder()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("%@/%d/%@");

            Assert.Equal("a/2/c", entry.ProcessTemplatedValue(Invariant, TextFormat.Apple, "a", 2, "c"));
        }
    }
}
