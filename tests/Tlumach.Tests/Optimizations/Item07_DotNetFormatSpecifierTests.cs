// <copyright file="Item07_DotNetFormatSpecifierTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 7 — the per-placeholder composite-format concatenation in <c>.NET</c>
    /// text-processing mode.
    /// <para>
    /// The second group of tests used to be marked <c>KnownDefect</c>: the format-specifier tail kept its
    /// leading colon, so <c>"{0:N2}"</c> became the composite format <c>"{0::N2}"</c>, the value was
    /// discarded, and the literal <c>":N2"</c> was emitted. The alignment component was swallowed the same
    /// way. That has now been fixed — the tail already carries its own <c>','</c> or <c>':'</c> separator
    /// and is appended straight after the index — so those tests now assert the correct output and are the
    /// regression guard for it.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "07")]
    [Collection("Optimizations")]
    public class Item07_DotNetFormatSpecifierTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // ---------------------------------------------------------------------------------------------
        // Behaviour that must be preserved.
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void PositionalPlaceholder_WithoutSpecifier_FormatsWithCulture()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("value: {0}");

            Assert.Equal("value: 1234.5678", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 1234.5678));
        }

        [Fact]
        public void PositionalPlaceholder_WithoutSpecifier_HonoursCulture()
        {
            CultureInfo german = new("de-DE");
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0}");

            Assert.Equal(
                string.Format(german, "{0}", 1234.5678),
                entry.ProcessTemplatedValue(german, TextFormat.DotNet, 1234.5678));
        }

        [Fact]
        public void NamedPlaceholder_ResolvesFromDictionary()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("Hello, {name}!");
            Dictionary<string, object?> values = OptimizationFixtures.Values(("name", "Ann"));

            Assert.Equal("Hello, Ann!", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, values));
        }

        [Fact]
        public void DoubledBraces_AreEmittedAsLiterals()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{{literal}}");

            Assert.Equal("{literal}", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 1));
        }

        [Fact]
        public void EmptyPlaceholder_ResolvesByPosition()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("a{}b{}c");

            Assert.Equal("a7b8c", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 7, 8));
        }

        /// <summary>
        /// A placeholder with no supplied value yields an empty string in .NET mode, rather than the
        /// placeholder text. This differs from Arb mode and must not be unified by accident.
        /// </summary>
        [Fact]
        public void MissingValue_YieldsEmptyString()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("x{0}y");

            Assert.Equal("xy", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, Array.Empty<object?>()));
        }

        [Fact]
        public void IndexedPlaceholders_ResolveInDeclaredOrder()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{1}={0}");

            Assert.Equal("name=value", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "value", "name"));
        }

        // ---------------------------------------------------------------------------------------------
        // Format specifiers and alignment. These were the KnownDefect group; see the class summary.
        // Every expectation is computed with string.Format, because that is exactly what a .NET
        // placeholder is supposed to mean.
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void StandardNumericSpecifier_FormatsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0:N2}");

            Assert.Equal(
                string.Format(Invariant, "{0:N2}", 1234.5678),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 1234.5678));
        }

        [Fact]
        public void StandardNumericSpecifier_HonoursCulture()
        {
            CultureInfo german = new("de-DE");
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0:N2}");

            Assert.Equal(
                string.Format(german, "{0:N2}", 1234.5678),
                entry.ProcessTemplatedValue(german, TextFormat.DotNet, 1234.5678));
        }

        [Fact]
        public void HexadecimalSpecifier_FormatsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0:X}");

            Assert.Equal("FF", entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 255));
        }

        [Fact]
        public void CustomDatePattern_FormatsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0:yyyy-MM-dd}");

            Assert.Equal(
                "2024-03-07",
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, new DateTime(2024, 3, 7, 0, 0, 0, DateTimeKind.Unspecified)));
        }

        [Fact]
        public void Alignment_PadsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("[{0,10}]");

            Assert.Equal(
                string.Format(Invariant, "[{0,10}]", "ab"),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "ab"));
        }

        [Fact]
        public void NegativeAlignment_PadsTheValueOnTheRight()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("[{0,-10}]");

            Assert.Equal(
                string.Format(Invariant, "[{0,-10}]", "ab"),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, "ab"));
        }

        [Fact]
        public void AlignmentWithSpecifier_AppliesBoth()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("[{0,10:N2}]");

            Assert.Equal(
                string.Format(Invariant, "[{0,10:N2}]", 1234.5678),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 1234.5678));
        }

        /// <summary>
        /// A specifier on a named placeholder, to confirm the fix is not tied to positional ones.
        /// </summary>
        [Fact]
        public void NamedPlaceholder_WithSpecifier_FormatsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{amount:N2}");
            Dictionary<string, object?> values = OptimizationFixtures.Values(("amount", 1234.5678));

            Assert.Equal(
                string.Format(Invariant, "{0:N2}", 1234.5678),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, values));
        }

        /// <summary>
        /// Several placeholders in one template, each with its own specifier, resolved positionally.
        /// </summary>
        [Fact]
        public void MultipleSpecifiers_AreAppliedIndependently()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0:N1}|{1:X}|{2:yyyy}");

            Assert.Equal(
                string.Format(Invariant, "{0:N1}|{1:X}|{2:yyyy}", 12.34, 255, new DateTime(2024, 3, 7, 0, 0, 0, DateTimeKind.Unspecified)),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 12.34, 255, new DateTime(2024, 3, 7, 0, 0, 0, DateTimeKind.Unspecified)));
        }

        /// <summary>
        /// A tail that begins with neither <c>','</c> nor <c>':'</c> is not valid placeholder syntax. It is
        /// treated as a bare format specifier, which is what every tail used to be treated as, so such a
        /// template keeps producing text instead of throwing.
        /// </summary>
        [Fact]
        public void MalformedTail_IsTreatedAsABareFormatSpecifier()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{0-N2}");

            Assert.Equal(
                string.Format(Invariant, "{0:-N2}", 1234.5678),
                entry.ProcessTemplatedValue(Invariant, TextFormat.DotNet, 1234.5678));
        }
    }
}
