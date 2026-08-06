// <copyright file="Item16_PlaceholderValueCacheTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 16 — replacing <see cref="StringComparer.InvariantCulture"/> in the placeholder
    /// value cache with an ordinal comparer.
    /// <para>
    /// The observed behaviour is case-SENSITIVE, so <see cref="StringComparer.Ordinal"/> is the
    /// behaviour-preserving replacement and <see cref="StringComparer.OrdinalIgnoreCase"/> is NOT — it
    /// would make a value cached as <c>"NAME"</c> start satisfying a <c>{name}</c> placeholder. These
    /// tests pin the case sensitivity so that choice has to be made deliberately.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "16")]
    [Collection("Optimizations")]
    public class Item16_PlaceholderValueCacheTests : IDisposable
    {
        private readonly string _dir;
        private readonly TranslationConfiguration _config;
        private readonly TranslationManager _manager;
        private readonly CultureInfo _culture;
        private readonly bool _originalCacheValues;

        public Item16_PlaceholderValueCacheTests()
        {
            _originalCacheValues = TranslationUnit.CacheValues;
            TranslationUnit.CacheValues = false;

            _dir = OptimizationFixtures.CreateStandardDirectory();
            _config = OptimizationFixtures.CreateStandardConfiguration(_dir);
            _manager = OptimizationFixtures.CreateManager(_config, _dir);
            _culture = new CultureInfo(OptimizationFixtures.DefaultLocale);
            _manager.CurrentCulture = _culture;
        }

        public void Dispose()
        {
            TranslationUnit.CacheValues = _originalCacheValues;
            _manager.Dispose();
            GC.SuppressFinalize(this);
        }

        private TranslationUnit CreateUnit() => new(_manager, _config, "greeting", containsPlaceholders: true);

        [Fact]
        public void CachedValue_WithExactName_IsUsed()
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue("name", "Ann");

            Assert.Equal("Hello, Ann!", unit.GetValue(_culture));
        }

        /// <summary>
        /// The behaviour that constrains the comparer choice: a differently cased cache key does NOT
        /// satisfy the placeholder. Switching to <see cref="StringComparer.OrdinalIgnoreCase"/> would
        /// break this.
        /// </summary>
        [Theory]
        [InlineData("NAME")]
        [InlineData("Name")]
        [InlineData("nAmE")]
        public void CachedValue_WithDifferentCasing_IsNotUsed(string cacheKey)
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue(cacheKey, "ShouldNotBeUsed");

            Assert.Equal("Hello, name!", unit.GetValue(_culture));
        }

        /// <summary>
        /// With both casings cached, the exact one is the one that is found.
        /// </summary>
        [Fact]
        public void CachedValues_WithBothCasings_UseTheExactMatch()
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue("NAME", "Upper");
            unit.CachePlaceholderValue("name", "Exact");

            Assert.Equal("Hello, Exact!", unit.GetValue(_culture));
        }

        [Fact]
        public void ForgetPlaceholderValue_RemovesOnlyTheExactKey()
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue("NAME", "Upper");
            unit.CachePlaceholderValue("name", "Exact");

            unit.ForgetPlaceholderValue("name");

            Assert.Equal("Hello, name!", unit.GetValue(_culture));
        }

        [Fact]
        public void ForgetPlaceholderValue_ForAbsentKey_IsHarmless()
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue("name", "Ann");

            unit.ForgetPlaceholderValue("never-cached");

            Assert.Equal("Hello, Ann!", unit.GetValue(_culture));
        }

        [Fact]
        public void CachePlaceholderValue_OverwritesExistingValue()
        {
            using TranslationUnit unit = CreateUnit();

            unit.CachePlaceholderValue("name", "First");
            Assert.Equal("Hello, First!", unit.GetValue(_culture));

            unit.CachePlaceholderValue("name", "Second");
            Assert.Equal("Hello, Second!", unit.GetValue(_culture));
        }

        /// <summary>
        /// A cached <see langword="null"/> is indistinguishable from an absent entry, because the resolver
        /// returns the value as-is and null means "unresolved" further up.
        /// </summary>
        [Fact]
        public void CachedNull_BehavesAsUnresolved()
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue("name", null);

            Assert.Equal("Hello, name!", unit.GetValue(_culture));
        }

        /// <summary>
        /// Non-string values are cached and formatted like any other placeholder value.
        /// </summary>
        [Fact]
        public void CachedNonStringValue_IsFormatted()
        {
            using TranslationUnit unit = CreateUnit();
            unit.CachePlaceholderValue("name", 42);

            Assert.Equal("Hello, 42!", unit.GetValue(_culture));
        }

        /// <summary>
        /// Caches are per-unit, not shared. A comparer change must not accidentally introduce a shared
        /// static cache.
        /// </summary>
        [Fact]
        public void PlaceholderCaches_AreIndependentPerUnit()
        {
            using TranslationUnit first = CreateUnit();
            using TranslationUnit second = CreateUnit();

            first.CachePlaceholderValue("name", "First");

            Assert.Equal("Hello, First!", first.GetValue(_culture));
            Assert.Equal("Hello, name!", second.GetValue(_culture));
        }

        /// <summary>
        /// Keys containing characters where linguistic and ordinal comparison genuinely differ. Ordinal
        /// treats them as distinct; the current invariant-culture comparer may or may not. This test
        /// records the current answer so the switch is evaluated against it rather than assumed.
        /// </summary>
        [Fact]
        public void DistinctKeys_RemainDistinct()
        {
            using TranslationUnit unit = CreateUnit();

            unit.CachePlaceholderValue("name", "plain");
            unit.CachePlaceholderValue("namé", "accented");

            Assert.Equal("Hello, plain!", unit.GetValue(_culture));
        }
    }
}
