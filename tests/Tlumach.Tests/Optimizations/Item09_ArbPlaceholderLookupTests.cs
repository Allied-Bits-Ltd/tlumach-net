// <copyright file="Item09_ArbPlaceholderLookupTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 9 — replacing the <c>Placeholders.FirstOrDefault(lambda)</c> metadata lookup with
    /// an allocation-free scan or a dictionary.
    /// <para>
    /// The lookup drives real behaviour, not just performance: whether a placeholder is declared
    /// determines whether it is substituted at all, and the declared type determines which formatter runs.
    /// These tests pin the matching rules — case-insensitive by name, first declaration wins, undeclared
    /// placeholders emitted as literals — so a dictionary-based replacement is forced to reproduce them.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "09")]
    [Collection("Optimizations")]
    public class Item09_ArbPlaceholderLookupTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private const string EightPlaceholderArb = """
{
    "@@locale": "en",
    "many": "{a} {b} {c} {d} {e} {f} {g} {h}",
    "@many": {
        "placeholders": {
            "a": { "type": "String" },
            "b": { "type": "String" },
            "c": { "type": "String" },
            "d": { "type": "String" },
            "e": { "type": "String" },
            "f": { "type": "String" },
            "g": { "type": "String" },
            "h": { "type": "String" }
        }
    }
}
""";

        private const string PartiallyDeclaredArb = """
{
    "@@locale": "en",
    "partial": "Hi {who}, meet {name}!",
    "@partial": {
        "placeholders": {
            "name": { "type": "String" }
        }
    }
}
""";

        private const string TypedArb = """
{
    "@@locale": "en",
    "typed": "{count} items",
    "@typed": {
        "placeholders": {
            "count": { "type": "num" }
        }
    }
}
""";

        [Fact]
        public void DeclaredPlaceholders_AreAllParsed()
        {
            Translation translation = OptimizationFixtures.ParseArb(EightPlaceholderArb);
            TranslationEntry entry = translation["many"];

            Assert.NotNull(entry.Placeholders);
            Assert.Equal(8, entry.Placeholders.Count);
            Assert.True(entry.ContainsPlaceholders);
        }

        [Fact]
        public void DeclaredPlaceholders_ResolveFromDictionary()
        {
            Translation translation = OptimizationFixtures.ParseArb(EightPlaceholderArb);
            TranslationEntry entry = translation["many"];

            Dictionary<string, object?> values = OptimizationFixtures.Values(
                ("a", 1), ("b", 2), ("c", 3), ("d", 4), ("e", 5), ("f", 6), ("g", 7), ("h", 8));

            Assert.Equal("1 2 3 4 5 6 7 8", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, values));
        }

        [Fact]
        public void DeclaredPlaceholders_ResolveFromPositionalArray()
        {
            Translation translation = OptimizationFixtures.ParseArb(EightPlaceholderArb);
            TranslationEntry entry = translation["many"];

            Assert.Equal("1 2 3 4 5 6 7 8", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, 1, 2, 3, 4, 5, 6, 7, 8));
        }

        /// <summary>
        /// The declaration lookup is case-insensitive on the placeholder name.
        /// </summary>
        [Fact]
        public void DeclarationLookup_IsCaseInsensitive()
        {
            const string arb = """
{
    "@@locale": "en",
    "mixed": "{UserName}",
    "@mixed": {
        "placeholders": {
            "username": { "type": "String" }
        }
    }
}
""";
            Translation translation = OptimizationFixtures.ParseArb(arb);

            Assert.Equal(
                "Ann",
                translation["mixed"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("UserName", "Ann"))));
        }

        /// <summary>
        /// Arb rules: when an entry declares placeholders, a placeholder that is NOT declared is not a
        /// placeholder at all and is emitted literally, braces included.
        /// </summary>
        [Fact]
        public void UndeclaredPlaceholder_InEntryWithDeclarations_IsEmittedWithBraces()
        {
            Translation translation = OptimizationFixtures.ParseArb(PartiallyDeclaredArb);

            string result = translation["partial"].ProcessTemplatedValue(
                Invariant,
                TextFormat.Arb,
                OptimizationFixtures.Values(("who", "Ann"), ("name", "Bob")));

            Assert.Equal("Hi {who}, meet Bob!", result);
        }

        /// <summary>
        /// An entry with NO declarations at all takes a different branch: an unresolved placeholder is
        /// emitted as its bare name, without braces. The two behaviours must not be unified by accident.
        /// </summary>
        [Fact]
        public void UnresolvedPlaceholder_InEntryWithoutDeclarations_IsEmittedWithoutBraces()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("Hi {who}!");

            Assert.Null(entry.Placeholders);
            Assert.Equal("Hi who!", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, Array.Empty<object?>()));
        }

        /// <summary>
        /// A declared placeholder whose value is missing is also emitted as its bare name.
        /// </summary>
        [Fact]
        public void DeclaredPlaceholder_WithNoValue_IsEmittedWithoutBraces()
        {
            Translation translation = OptimizationFixtures.ParseArb(PartiallyDeclaredArb);

            string result = translation["partial"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values());

            Assert.Equal("Hi {who}, meet name!", result);
        }

        /// <summary>
        /// A placeholder declared as numeric but given a string falls back to string formatting rather
        /// than throwing. The declared type is read from the metadata found by the lookup, so a
        /// replacement lookup must return the same declaration.
        /// </summary>
        [Fact]
        public void NumericDeclaration_WithNonNumericValue_FallsBackToStringFormatting()
        {
            Translation translation = OptimizationFixtures.ParseArb(TypedArb);

            Assert.Equal(
                "not-a-number items",
                translation["typed"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("count", "not-a-number"))));
        }

        [Fact]
        public void NumericDeclaration_WithNumericValue_FormatsWithCulture()
        {
            Translation translation = OptimizationFixtures.ParseArb(TypedArb);
            CultureInfo german = new("de-DE");

            Assert.Equal(
                string.Format(german, "{0}", 1234.5) + " items",
                translation["typed"].ProcessTemplatedValue(german, TextFormat.Arb, OptimizationFixtures.Values(("count", 1234.5))));
        }

        /// <summary>
        /// The same placeholder used twice must resolve twice, which exercises the lookup once per
        /// occurrence rather than once per declaration.
        /// </summary>
        [Fact]
        public void RepeatedPlaceholder_ResolvesEveryOccurrence()
        {
            const string arb = """
{
    "@@locale": "en",
    "twice": "{name} and {name}",
    "@twice": {
        "placeholders": {
            "name": { "type": "String" }
        }
    }
}
""";
            Translation translation = OptimizationFixtures.ParseArb(arb);

            Assert.Equal(
                "Ann and Ann",
                translation["twice"].ProcessTemplatedValue(Invariant, TextFormat.Arb, OptimizationFixtures.Values(("name", "Ann"))));
        }
    }
}
