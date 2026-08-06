// <copyright file="Item10_ArbNestedClosureTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 10 — hoisting the per-placeholder recursion closure
    /// (<c>internalGetPlaceholderValueFunc</c>) out of <c>GetPlaceholderValue</c>.
    /// <para>
    /// The closure threads <c>placeholderIndex</c> by reference through nested template processing, so
    /// hoisting it is correctness-sensitive rather than mechanical: if the index stops advancing the same
    /// way, positional resolution inside ICU branches silently changes. These tests pin index advancement
    /// and the ICU branch selection that depends on it.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "10")]
    [Collection("Optimizations")]
    public class Item10_ArbNestedClosureTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // The "format" key is required for the ICU tail to be evaluated at all: without it,
        // GetPlaceholderValue short-circuits to plain numeric formatting and the plural expression is
        // never parsed. See PluralWithoutFormat_IgnoresTheIcuExpression below.
        private const string IcuArb = """
{
    "@@locale": "en",
    "plural": "{count, plural, =0{no items} one{one item} other{several items}}",
    "@plural": {
        "placeholders": {
            "count": { "type": "num", "format": "decimal" }
        }
    },
    "pluralNoFormat": "{count, plural, =0{no items} one{one item} other{several items}}",
    "@pluralNoFormat": {
        "placeholders": {
            "count": { "type": "num" }
        }
    },
    "select": "{gender, select, male{He} female{She} other{They}}",
    "@select": {
        "placeholders": {
            "gender": { "type": "String" }
        }
    }
}
""";

        /// <summary>
        /// Positional resolution across several placeholders. The index must advance by exactly one per
        /// placeholder, in source order.
        /// </summary>
        [Fact]
        public void PlaceholderIndex_AdvancesOncePerPlaceholder()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}|{b}|{c}|{d}");

            Assert.Equal("0|1|2|3", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, 0, 1, 2, 3));
        }

        /// <summary>
        /// The index is positional, not name-driven: the same name repeated still advances the index.
        /// </summary>
        [Fact]
        public void PlaceholderIndex_AdvancesForRepeatedNames()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{x}|{x}|{x}");

            Assert.Equal("first|second|third", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "first", "second", "third"));
        }

        [Fact]
        public void ManyPlaceholders_ResolveInOrder()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a} {b} {c} {d} {e} {f} {g} {h} {i} {j} {k} {l} {m} {n} {o} {p}");

            object?[] values = new object?[16];
            for (int i = 0; i < values.Length; i++)
                values[i] = i;

            Assert.Equal("0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, values));
        }

        [Theory]
        [InlineData(0, "no items")]
        [InlineData(1, "one item")]
        [InlineData(5, "several items")]
        [InlineData(2, "several items")]
        public void IcuPlural_SelectsTheExpectedBranch(int count, string expected)
        {
            Translation translation = OptimizationFixtures.ParseArb(IcuArb);

            string result = translation["plural"].ProcessTemplatedValue(
                Invariant,
                TextFormat.Arb,
                OptimizationFixtures.Values(("count", count)));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Documents a pre-existing quirk that the closure change must not disturb: without a declared
        /// <c>format</c>, a numeric placeholder never reaches <c>FormatArbNumber</c>, so the ICU
        /// expression is ignored entirely and only the raw number is emitted.
        /// </summary>
        [Fact]
        public void PluralWithoutFormat_IgnoresTheIcuExpression()
        {
            Translation translation = OptimizationFixtures.ParseArb(IcuArb);

            string result = translation["pluralNoFormat"].ProcessTemplatedValue(
                Invariant,
                TextFormat.Arb,
                OptimizationFixtures.Values(("count", 5)));

            Assert.Equal("5", result);
        }

        [Theory]
        [InlineData("male", "He")]
        [InlineData("female", "She")]
        [InlineData("nonbinary", "They")]
        public void IcuSelect_SelectsTheExpectedBranch(string gender, string expected)
        {
            Translation translation = OptimizationFixtures.ParseArb(IcuArb);

            string result = translation["select"].ProcessTemplatedValue(
                Invariant,
                TextFormat.Arb,
                OptimizationFixtures.Values(("gender", gender)));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// Repeated evaluation of the same ICU entry must be stable. The closure is rebuilt per call
        /// today; after hoisting it is rebuilt per template evaluation, and neither may leak state into
        /// the next call.
        /// </summary>
        [Fact]
        public void IcuEvaluation_IsRepeatable()
        {
            Translation translation = OptimizationFixtures.ParseArb(IcuArb);
            TranslationEntry entry = translation["plural"];

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal("no items", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 0))));
                Assert.Equal("one item", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 1))));
                Assert.Equal("several items", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", 7))));
            }
        }

        /// <summary>
        /// Two ICU expressions in one template. Each must select its own branch, which requires the index
        /// and the recursion closure to stay correctly scoped across both.
        /// </summary>
        [Fact]
        public void TwoIcuExpressionsInOneTemplate_EachSelectsItsOwnBranch()
        {
            const string arb = """
{
    "@@locale": "en",
    "both": "{gender, select, male{He} other{They}} has {count, plural, =0{no items} one{one item} other{several items}}",
    "@both": {
        "placeholders": {
            "gender": { "type": "String" },
            "count": { "type": "num", "format": "decimal" }
        }
    }
}
""";
            Translation translation = OptimizationFixtures.ParseArb(arb);

            Assert.Equal(
                "He has one item",
                translation["both"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("gender", "male"), ("count", 1))));

            Assert.Equal(
                "They has no items",
                translation["both"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("gender", "x"), ("count", 0))));
        }

        /// <summary>
        /// A plain declared placeholder with no ICU tail must produce the same result whether or not the
        /// recursion closure is created for it — which is exactly what the optimization changes.
        /// </summary>
        [Fact]
        public void PlainDeclaredPlaceholder_NeedsNoIcuMachinery()
        {
            const string arb = """
{
    "@@locale": "en",
    "plain": "Hello, {name}!",
    "@plain": {
        "placeholders": {
            "name": { "type": "String" }
        }
    }
}
""";
            Translation translation = OptimizationFixtures.ParseArb(arb);

            Assert.Equal(
                "Hello, Ann!",
                translation["plain"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("name", "Ann"))));
        }
    }
}
