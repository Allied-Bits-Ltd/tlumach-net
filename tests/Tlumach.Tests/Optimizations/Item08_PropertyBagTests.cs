// <copyright file="Item08_PropertyBagTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 8 — caching the per-type property map used by
    /// <c>Utils.TryGetPropertyValue</c> instead of calling <c>Type.GetProperties</c> per placeholder.
    /// <para>
    /// The current lookup has a specific resolution order that a cache must reproduce: an exact,
    /// case-sensitive match wins over a case-insensitive one, and only public instance properties are
    /// considered. It must also cope with the awkward members a caller's type can carry — indexers,
    /// inherited properties, static properties, and write-only properties.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "08")]
    [Collection("Optimizations")]
    public class Item08_PropertyBagTests
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [Fact]
        public void TryGetPropertyValue_FindsExactMatch()
        {
            Assert.True(Utils.TryGetPropertyValue(typeof(SimpleBag), new SimpleBag(), "Name", out object? value));
            Assert.Equal("Bob", value);
        }

        [Fact]
        public void TryGetPropertyValue_FindsCaseInsensitiveMatch()
        {
            Assert.True(Utils.TryGetPropertyValue(typeof(SimpleBag), new SimpleBag(), "NAME", out object? value));
            Assert.Equal("Bob", value);
        }

        /// <summary>
        /// The documented tie-break: when a type declares two properties differing only by case, the
        /// exact match must win regardless of declaration order.
        /// </summary>
        [Fact]
        public void TryGetPropertyValue_PrefersExactMatchOverCaseInsensitiveOne()
        {
            Assert.True(Utils.TryGetPropertyValue(typeof(CaseCollisionBag), new CaseCollisionBag(), "value", out object? lower));
            Assert.Equal("lower", lower);

            Assert.True(Utils.TryGetPropertyValue(typeof(CaseCollisionBag), new CaseCollisionBag(), "Value", out object? upper));
            Assert.Equal("upper", upper);
        }

        [Fact]
        public void TryGetPropertyValue_ReturnsFalseForMissingProperty()
        {
            Assert.False(Utils.TryGetPropertyValue(typeof(SimpleBag), new SimpleBag(), "nope", out object? value));
            Assert.Null(value);
        }

        [Fact]
        public void TryGetPropertyValue_RejectsNullAndBlankArguments()
        {
            Assert.False(Utils.TryGetPropertyValue(null!, new SimpleBag(), "Name", out _));
            Assert.False(Utils.TryGetPropertyValue(typeof(SimpleBag), null!, "Name", out _));
            Assert.False(Utils.TryGetPropertyValue(typeof(SimpleBag), new SimpleBag(), "   ", out _));
        }

        /// <summary>
        /// Only public instance properties participate. A cache built from the wrong
        /// <see cref="System.Reflection.BindingFlags"/> would start resolving these.
        /// </summary>
        [Fact]
        public void TryGetPropertyValue_IgnoresNonPublicAndStaticProperties()
        {
            Assert.False(Utils.TryGetPropertyValue(typeof(AwkwardBag), new AwkwardBag(), "Hidden", out _));
            Assert.False(Utils.TryGetPropertyValue(typeof(AwkwardBag), new AwkwardBag(), "StaticValue", out _));
        }

        /// <summary>
        /// Inherited public properties must resolve, which they do because <c>GetProperties</c> walks the
        /// hierarchy by default.
        /// </summary>
        [Fact]
        public void TryGetPropertyValue_FindsInheritedProperty()
        {
            Assert.True(Utils.TryGetPropertyValue(typeof(DerivedBag), new DerivedBag(), "Name", out object? value));
            Assert.Equal("Bob", value);
        }

        /// <summary>
        /// An indexer is reported by <c>GetProperties</c> under the name "Item". Reading it without index
        /// arguments throws, so a property cache must not let a placeholder named "Item" reach it. This
        /// test records the current behaviour so that a cached implementation is compared against it.
        /// </summary>
        [Fact]
        public void TryGetPropertyValue_WithIndexer_DoesNotDisturbNormalLookups()
        {
            Assert.True(Utils.TryGetPropertyValue(typeof(IndexerBag), new IndexerBag(), "Name", out object? value));
            Assert.Equal("Bob", value);
        }

        [Fact]
        public void ProcessTemplatedValueFrom_ResolvesFromAnonymousType()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{name} has {count}");

            Assert.Equal("Bob has 3", entry.ProcessTemplatedValueFrom(Invariant, TextFormat.Arb, new { name = "Bob", count = 3 }));
        }

#pragma warning disable IL2026
        [Fact]
        public void ProcessTemplatedValue_WithObjectOverload_ResolvesFromAnonymousType()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{name} has {count}");

            Assert.Equal("Bob has 3", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, (object)new { name = "Bob", count = 3 }));
        }

        [Fact]
        public void ProcessTemplatedValue_MatchesPropertyNameCaseInsensitively()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{NAME}");

            Assert.Equal("Bob", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, (object)new { name = "Bob" }));
        }

        /// <summary>
        /// A property bag that has no matching property leaves the placeholder unresolved rather than
        /// splicing the object's own <c>ToString()</c> into the text.
        /// </summary>
        [Fact]
        public void ProcessTemplatedValue_WithNoMatchingProperty_LeavesPlaceholderUnresolved()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{nope}");

            Assert.Equal("nope", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, (object)new { name = "Bob" }));
        }

        /// <summary>
        /// A lone scalar is used as the value for a single placeholder rather than being treated as a
        /// property bag.
        /// </summary>
        [Fact]
        public void ProcessTemplatedValue_WithScalar_UsesItAsTheValue()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{a}");

            Assert.Equal("42", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, (object)42));
        }

        [Fact]
        public void ProcessTemplatedValue_WithStructBag_ResolvesProperties()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{name}");

            Assert.Equal("Bob", entry.ProcessTemplatedValue(Invariant, TextFormat.Arb, (object)new StructBag()));
        }
#pragma warning restore IL2026

        /// <summary>
        /// The generic overload resolves properties on <c>typeof(T)</c>, not on the runtime type. Declaring
        /// the argument as <see cref="object"/> therefore finds nothing — documented behaviour that a
        /// caching change must not silently "fix".
        /// </summary>
        [Fact]
        public void ProcessTemplatedValueFrom_UsesStaticTypeArgument()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{name}");
            object bag = new SimpleBag();

            Assert.Equal("name", entry.ProcessTemplatedValueFrom(Invariant, TextFormat.Arb, bag));
            Assert.Equal("Bob", entry.ProcessTemplatedValueFrom(Invariant, TextFormat.Arb, (SimpleBag)bag));
        }

        /// <summary>
        /// Repeated lookups against the same type must be stable. A cache that keys on the wrong thing
        /// would show up here first.
        /// </summary>
        [Fact]
        public void RepeatedLookups_AreStableAcrossTypes()
        {
            TranslationEntry entry = OptimizationFixtures.TemplatedEntry("{name}");

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal("Bob", entry.ProcessTemplatedValueFrom(Invariant, TextFormat.Arb, new SimpleBag()));
                Assert.Equal("Ann", entry.ProcessTemplatedValueFrom(Invariant, TextFormat.Arb, new OtherBag()));
            }
        }

        private sealed class SimpleBag
        {
            public string Name => "Bob";

            public int Count => 3;
        }

        private sealed class OtherBag
        {
            public string Name => "Ann";
        }

        private class BaseBag
        {
            public string Name => "Bob";
        }

        private sealed class DerivedBag : BaseBag
        {
            public int Count => 3;
        }

#pragma warning disable SA1300, IDE1006 // Deliberate case collision.
        private sealed class CaseCollisionBag
        {
            public string value => "lower";

            public string Value => "upper";
        }
#pragma warning restore SA1300, IDE1006

        private sealed class AwkwardBag
        {
            public static string StaticValue => "static";

            public string Name => "Bob";

            private string Hidden => "hidden";
        }

        private sealed class IndexerBag
        {
            public string Name => "Bob";

            public string this[int index] => "indexed";
        }

        private readonly struct StructBag
        {
            public string Name => "Bob";
        }
    }
}
