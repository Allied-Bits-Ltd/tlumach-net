// <copyright file="Item06_ValueToTextTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 6 — replacing <c>string.Format(culture, "{0}", value)</c> with a direct
    /// conversion.
    /// <para>
    /// The replacement is only safe if it produces byte-identical output for every type that can reach it.
    /// These tests cover the cases where <c>string.Format</c> and a naive <c>value.ToString()</c> differ:
    /// null, culture-sensitive numerics and dates, <see cref="IFormattable"/> implementers, and types that
    /// only override <see cref="object.ToString"/>.
    /// </para>
    /// <para>
    /// Culture-sensitive expectations are computed rather than hard-coded, because the exact output
    /// depends on the ICU version shipped with the runtime.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "06")]
    [Collection("Optimizations")]
    public class Item06_ValueToTextTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [Fact]
        public void UndeclaredArbPlaceholder_FormatsMixedTypes_Invariant()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a} {b} {c} {d}");
            DateTime when = new(2024, 3, 7, 13, 45, 0, DateTimeKind.Unspecified);

            string result = entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, "text", 42, 1234.5678, when);

            string expected = string.Join(
                ' ',
                string.Format(Invariant, "{0}", "text"),
                string.Format(Invariant, "{0}", 42),
                string.Format(Invariant, "{0}", 1234.5678),
                string.Format(Invariant, "{0}", when));

            Assert.Equal(expected, result);
        }

        /// <summary>
        /// The same values under a culture with different separators. This is the assertion that catches a
        /// replacement that accidentally drops the culture.
        /// </summary>
        [Fact]
        public void UndeclaredArbPlaceholder_FormatsMixedTypes_German()
        {
            CultureInfo german = new("de-DE");
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a} {b} {c} {d}");
            DateTime when = new(2024, 3, 7, 13, 45, 0, DateTimeKind.Unspecified);

            string result = entry.ProcessTemplatedValue(german, TextFormat.Arb, "text", 42, 1234.5678, when);

            string expected = string.Join(
                ' ',
                string.Format(german, "{0}", "text"),
                string.Format(german, "{0}", 42),
                string.Format(german, "{0}", 1234.5678),
                string.Format(german, "{0}", when));

            Assert.Equal(expected, result);

            // Sanity: the German rendering really does differ from the invariant one, so the assertion
            // above is not vacuous.
            Assert.NotEqual(
                string.Format(Invariant, "{0}", 1234.5678),
                string.Format(german, "{0}", 1234.5678));
        }

        /// <summary>
        /// A null argument is turned into the literal string "null" by the array overload before it ever
        /// reaches the formatter, so the conversion helper never sees a null value from this path.
        /// </summary>
        [Fact]
        public void NullArgument_RendersAsLiteralNull()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("null", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, [null]));
        }

        [Theory]
        [InlineData(true, "True")]
        [InlineData(false, "False")]
        public void BooleanArgument_UsesInvariantBooleanText(bool value, string expected)
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal(expected, entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, value));
        }

        [Fact]
        public void EnumArgument_UsesEnumName()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("Friday", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, DayOfWeek.Friday));
        }

        /// <summary>
        /// Decimal keeps its trailing zeros, which is a property of <see cref="decimal.ToString()"/> and
        /// must not be lost.
        /// </summary>
        [Fact]
        public void DecimalArgument_PreservesScale()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("12.50", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, 12.50m));
        }

        /// <summary>
        /// A custom <see cref="IFormattable"/> must be given the culture and a null format specifier,
        /// exactly as <c>string.Format(culture, "{0}", value)</c> does.
        /// </summary>
        [Fact]
        public void CustomFormattable_ReceivesNullFormatAndTheCulture()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");
            RecordingFormattable value = new();

            // Wrapped in an explicit array: a lone reference-typed argument would bind to the
            // property-bag overload instead of the positional-array one.
            string result = entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, new object?[] { value });

            Assert.Equal("formatted", result);
            Assert.True(value.WasCalled);
            Assert.Null(value.SeenFormat);
            Assert.Same(Invariant, value.SeenProvider);
        }

        /// <summary>
        /// A type that only overrides <see cref="object.ToString"/> must still render through it.
        /// </summary>
        [Fact]
        public void PlainObject_UsesToString()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("plain-to-string", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, new object?[] { new PlainToString() }));
        }

        /// <summary>
        /// Overload selection, recorded because it surprises: a lone reference-typed argument binds to the
        /// property-bag overload, not to the positional-array one. With no matching property the
        /// placeholder is left unresolved rather than being filled with the object's
        /// <see cref="object.ToString"/>.
        /// </summary>
        [Fact]
        public void LoneReferenceTypedArgument_BindsToThePropertyBagOverload()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("a", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, new PlainToString()));
        }

        /// <summary>
        /// A lone scalar, by contrast, is recognised as a value rather than a property bag, so it is used
        /// directly. This is why the boolean, enum and decimal cases above work without an explicit array.
        /// </summary>
        [Fact]
        public void LoneScalarArgument_IsUsedAsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("42", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, 42));
        }

        /// <summary>
        /// <see cref="TextFormat.None"/> short-circuits before any placeholder processing, so the raw text
        /// is returned untouched.
        /// </summary>
        [Fact]
        public void NoneMode_ReturnsRawText()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a} literal");

            Assert.Equal("{a} literal", entry.ProcessTemplatedValue(Invariant, TextFormat.None, "ignored"));
        }

        /// <summary>
        /// An entry with neither text nor escaped text yields an empty string rather than throwing.
        /// </summary>
        [Fact]
        public void EmptyEntry_ProducesEmptyString()
        {
            Assert.Equal(string.Empty, TranslationEntry.Empty.ProcessTemplatedValue(Invariant, TextFormat.Arb, "ignored"));
        }

        private sealed class RecordingFormattable : IFormattable
        {
            public bool WasCalled { get; private set; }

            public string? SeenFormat { get; private set; }

            public IFormatProvider? SeenProvider { get; private set; }

            public string ToString(string? format, IFormatProvider? formatProvider)
            {
                WasCalled = true;
                SeenFormat = format;
                SeenProvider = formatProvider;
                return "formatted";
            }

            public override string ToString() => "should-not-be-used";
        }

        private sealed class PlainToString
        {
            public override string ToString() => "plain-to-string";
        }
    }
}
