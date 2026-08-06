// <copyright file="Item15_TemplateBuilderTests.cs" company="Allied Bits Ltd.">
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

using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests.Optimizations
{
    /// <summary>
    /// Optimization item 15 — pooling or reusing the <see cref="System.Text.StringBuilder"/> allocated per
    /// template evaluation.
    /// <para>
    /// The hazard is re-entrancy: <c>InternalProcessTemplatedText</c> calls itself for nested placeholder
    /// content, so a single shared builder would interleave two outputs and silently corrupt the result.
    /// These tests cover nesting, long inputs, escaping, and concurrent evaluation, which is where a
    /// naive pool breaks.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "15")]
    [Collection("Optimizations")]
    public class Item15_TemplateBuilderTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [Fact]
        public void ShortTemplate_ProducesExpectedText()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("a{x}b");

            Assert.Equal("aVb", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "V"));
        }

        /// <summary>
        /// A template far larger than any sensible initial builder capacity, so the backing array has to
        /// grow several times.
        /// </summary>
        [Fact]
        public void LongTemplate_ProducesExpectedTextAndLength()
        {
            string filler = new('w', 4000);
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry(filler + "{x}" + filler);

            string result = entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "MIDDLE");

            Assert.Equal((4000 * 2) + "MIDDLE".Length, result.Length);
            Assert.StartsWith(filler, result, StringComparison.Ordinal);
            Assert.EndsWith(filler, result, StringComparison.Ordinal);
            Assert.Contains("MIDDLE", result, StringComparison.Ordinal);
        }

        [Fact]
        public void EmptyTemplate_ProducesEmptyString()
        {
            TranslationEntry entry = new("key", string.Empty) { ContainsPlaceholders = true };

            Assert.Equal(string.Empty, entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "ignored"));
        }

        [Fact]
        public void TemplateWithoutPlaceholders_IsCopiedVerbatim()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("no placeholders here");

            Assert.Equal("no placeholders here", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "ignored"));
        }

        [Theory]
        [InlineData("a\\nb", "a\nb")]
        [InlineData("a\\tb", "a\tb")]
        [InlineData("a\\rb", "a\rb")]
        [InlineData("a\\\\b", "a\\b")]
        [InlineData("a\\\"b", "a\"b")]
        [InlineData("a\\/b", "a/b")]
        [InlineData("a\\bb", "a\bb")]
        [InlineData("a\\fb", "a\fb")]
        [InlineData("\\u0041", "A")]
        [InlineData("\\u00A9", "©")]
        [InlineData("\\u0000", "\0")]
        [InlineData("a\\u0041b", "aAb")]
        public void EscapedText_IsUnescapedDuringProcessing(string escaped, string expected)
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry(escaped + "{0}");

            Assert.Equal(expected + "X", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "X"));
        }

        /// <summary>
        /// Regression guard for a fixed off-by-one. After decoding a <c>\uXXXX</c> escape,
        /// <c>InternalProcessTemplatedText</c> used to advance the read pointer by four instead of five, so
        /// the final hex digit was emitted a second time and <c>A</c> produced <c>"A1"</c>.
        /// </summary>
        [Theory]
        [InlineData("\\u0041", "A")]
        [InlineData("\\u00A9", "©")]
        [InlineData("\\u0031", "1")]
        [InlineData("\\u0041\\u0042", "AB")]
        public void UnicodeEscape_ConsumesAllFourHexDigits(string escaped, string expected)
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry(escaped);

            Assert.Equal(expected, entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, Array.Empty<object?>()));
        }

        /// <summary>
        /// The same decoding through <see cref="Utils.UnescapeString"/>, which was already correct. Both
        /// paths must now agree.
        /// </summary>
        [Theory]
        [InlineData("\\u0041", "A")]
        [InlineData("a\\u0041b", "aAb")]
        [InlineData("\\u0041\\u0042", "AB")]
        public void UnicodeEscape_MatchesUtilsUnescapeString(string escaped, string expected)
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry(escaped);

            Assert.Equal(expected, Utils.UnescapeString(escaped));
            Assert.Equal(expected, entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, Array.Empty<object?>()));
        }

        /// <summary>
        /// An incomplete escape at the very end of the text is emitted literally rather than reading past
        /// the end of the string.
        /// </summary>
        [Fact]
        public void IncompleteUnicodeEscape_IsEmittedLiterally()
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry("ab\\u00");

            Assert.Equal("ab\\u00", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, Array.Empty<object?>()));
        }

        /// <summary>
        /// A malformed escape whose four characters are not hexadecimal is emitted literally, and the read
        /// pointer still advances past all of them.
        /// </summary>
        [Fact]
        public void InvalidUnicodeEscape_IsEmittedLiterally()
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry("\\uZZZZ!");

            Assert.Equal("\\uZZZZ!", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, Array.Empty<object?>()));
        }

        /// <summary>
        /// An unknown escape sequence is passed through with its backslash intact.
        /// </summary>
        [Fact]
        public void UnknownEscapeSequence_IsPreserved()
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry("a\\qb{0}");

            Assert.Equal("a\\qbX", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "X"));
        }

        [Fact]
        public void HangingBackslash_Throws()
        {
            TranslationEntry entry = OptimizationFixtures.EscapedEntry("abc\\");

            Assert.Throws<TemplateParserException>(
                () => entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "X"));
        }

        [Fact]
        public void UnclosedBrace_Throws()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("a{unclosed");

            Assert.ThrowsAny<GenericParserException>(
                () => entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "X"));
        }

        /// <summary>
        /// Nested template processing: an ICU branch body is itself run through the same routine. If a
        /// shared builder were used without accounting for re-entrancy, the inner and outer results would
        /// interleave here.
        /// </summary>
        [Fact]
        public void NestedTemplateProcessing_DoesNotInterleaveOutput()
        {
            const string arb = """
{
    "@@locale": "en",
    "nested": "before {count, plural, =0{ZERO} one{ONE} other{MANY}} after",
    "@nested": {
        "placeholders": {
            "count": { "type": "num", "format": "decimal" }
        }
    }
}
""";
            Translation translation = OptimizationFixtures.ParseArb(arb);
            TranslationEntry entry = translation["nested"];

            Assert.Equal("before ZERO after", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 0))));
            Assert.Equal("before ONE after", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 1))));
            Assert.Equal("before MANY after", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 9))));
        }

        /// <summary>
        /// The same entry evaluated from many threads at once. A thread-static pool is fine here; a
        /// process-wide single builder is not.
        /// </summary>
        [Fact]
        public void ConcurrentEvaluation_ProducesConsistentResults()
        {
            string filler = new('z', 500);
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry(filler + "{x}" + filler);
            string expected = filler + "VALUE" + filler;

            ConcurrentBag<string> results = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, 64, index =>
            {
                try
                {
                    results.Add(entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "VALUE"));
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(64, results.Count);
            Assert.Equal([expected], results.Distinct().ToArray());
        }

        /// <summary>
        /// Concurrent evaluation of nested ICU content, which re-enters the routine on each thread.
        /// </summary>
        [Fact]
        public void ConcurrentNestedEvaluation_ProducesConsistentResults()
        {
            const string arb = """
{
    "@@locale": "en",
    "nested": "start {count, plural, =0{ZERO} one{ONE} other{MANY}} end",
    "@nested": {
        "placeholders": {
            "count": { "type": "num", "format": "decimal" }
        }
    }
}
""";
            Translation translation = OptimizationFixtures.ParseArb(arb);
            TranslationEntry entry = translation["nested"];

            ConcurrentBag<string> results = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, 64, index =>
            {
                try
                {
                    results.Add(entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 1))));
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(["start ONE end"], results.Distinct().ToArray());
        }

        /// <summary>
        /// The <c>params object?[]</c> overload short-circuits for the non-placeholder modes and returns
        /// <see cref="TranslationEntry.Text"/> directly, without consulting
        /// <see cref="TranslationEntry.EscapedText"/>. Documented here because it is easy to "tidy away".
        /// </summary>
        [Theory]
        [InlineData(TextFormat.None)]
        [InlineData(TextFormat.BackslashEscaping)]
        public void ArrayOverload_ForNonPlaceholderModes_ReturnsTextDirectly(TextFormat mode)
        {
            TranslationEntry withEscapedOnly = OptimizationFixtures.EscapedEntry("a\\nb");
            Assert.Equal(string.Empty, withEscapedOnly.ProcessTemplatedValue(Invariant, mode, Array.Empty<object?>()));

            TranslationEntry withText = OptimizationFixtures.TemplatedEntry("plain");
            Assert.Equal("plain", withText.ProcessTemplatedValue(Invariant, mode, Array.Empty<object?>()));
        }
    }
}
