// <copyright file="Item04_UnitGetValueTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 4 — caching the placeholder-resolver delegate on the translation unit instead of
    /// allocating a new one per <c>GetValue()</c> call.
    /// <para>
    /// The delegate closes over <c>this</c> and reads mutable unit state: the placeholder value cache and
    /// the <c>OnPlaceholderValueNeeded</c> subscription list. Hoisting it into a field is only safe if it
    /// keeps observing that state live rather than capturing a snapshot. These tests mutate the state
    /// between calls and assert the resolver follows.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "04")]
    [Collection("Optimizations")]
    public class Item04_UnitGetValueTests : IDisposable
    {
        private readonly string _dir;
        private readonly TranslationConfiguration _config;
        private readonly TranslationManager _manager;
        private readonly CultureInfo _culture;
        private readonly bool _originalCacheValues;

        public Item04_UnitGetValueTests()
        {
            _originalCacheValues = TranslationUnit.CacheValues;
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

        private TranslationUnit CreateTemplatedUnit() => new(_manager, _config, "greeting", containsPlaceholders: true);

        private TranslationUnit CreatePlainUnit() => new(_manager, _config, "plain", containsPlaceholders: false);

        [Fact]
        public void TemplatedUnit_ResolvesFromPlaceholderCache()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();
            unit.CachePlaceholderValue("name", "Ann");

            Assert.Equal("Hello, Ann!", unit.GetValue(_culture));
        }

        /// <summary>
        /// The cached delegate must observe cache mutations that happen after it was created.
        /// </summary>
        [Fact]
        public void TemplatedUnit_ObservesCacheMutationsBetweenCalls()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();

            unit.CachePlaceholderValue("name", "First");
            Assert.Equal("Hello, First!", unit.GetValue(_culture));

            unit.CachePlaceholderValue("name", "Second");
            Assert.Equal("Hello, Second!", unit.GetValue(_culture));

            unit.ForgetPlaceholderValue("name");

            // An unresolved placeholder in an entry that declares placeholders renders as the placeholder
            // name, without braces.
            Assert.Equal("Hello, name!", unit.GetValue(_culture));
        }

        /// <summary>
        /// A value cached as <see langword="null"/> is indistinguishable from an absent one: the resolver
        /// returns null either way, and the placeholder is left unresolved.
        /// </summary>
        [Fact]
        public void TemplatedUnit_CachedNullBehavesAsAbsent()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();
            unit.CachePlaceholderValue("name", null);

            Assert.Equal("Hello, name!", unit.GetValue(_culture));
        }

        /// <summary>
        /// The event must fire on every call while the value is not cached, and must stop firing once the
        /// handler asks for the value to be cached.
        /// </summary>
        [Fact]
        public void OnPlaceholderValueNeeded_FiresPerCall_UntilTheValueIsCached()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();

            int fired = 0;
            string? seenName = null;
            int seenIndex = -99;

            unit.OnPlaceholderValueNeeded += (sender, args) =>
            {
                fired++;
                seenName = args.Name;
                seenIndex = args.Index;
                args.Value = "FromEvent";
                args.CacheValue = false;
            };

            Assert.Equal("Hello, FromEvent!", unit.GetValue(_culture));
            Assert.Equal(1, fired);
            Assert.Equal("name", seenName);
            Assert.Equal(0, seenIndex);

            Assert.Equal("Hello, FromEvent!", unit.GetValue(_culture));
            Assert.Equal(2, fired);
        }

        [Fact]
        public void OnPlaceholderValueNeeded_WithCacheValue_StopsFiring()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();

            int fired = 0;
            unit.OnPlaceholderValueNeeded += (sender, args) =>
            {
                fired++;
                args.Value = "Cached";
                args.CacheValue = true;
            };

            Assert.Equal("Hello, Cached!", unit.GetValue(_culture));
            Assert.Equal(1, fired);

            Assert.Equal("Hello, Cached!", unit.GetValue(_culture));
            Assert.Equal(1, fired);
        }

        /// <summary>
        /// The explicitly cached value wins over the event, which is only consulted on a cache miss.
        /// </summary>
        [Fact]
        public void CachedValue_TakesPrecedenceOverEvent()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();

            int fired = 0;
            unit.OnPlaceholderValueNeeded += (sender, args) =>
            {
                fired++;
                args.Value = "FromEvent";
            };

            unit.CachePlaceholderValue("name", "FromCache");

            Assert.Equal("Hello, FromCache!", unit.GetValue(_culture));
            Assert.Equal(0, fired);
        }

        /// <summary>
        /// A subscriber added after the first call must still be seen, which a snapshot-capturing
        /// refactor would break.
        /// </summary>
        [Fact]
        public void OnPlaceholderValueNeeded_HandlerAddedLater_IsStillInvoked()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();

            Assert.Equal("Hello, name!", unit.GetValue(_culture));

            unit.OnPlaceholderValueNeeded += (sender, args) => args.Value = "Late";

            Assert.Equal("Hello, Late!", unit.GetValue(_culture));
        }

        [Fact]
        public void PlainUnit_ReturnsTextWithoutTemplateProcessing()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreatePlainUnit();

            Assert.Equal("Plain text", unit.GetValue(_culture));
            Assert.Equal("Plain text", unit.ToString());
            Assert.Equal("Plain text", (string)unit);
        }

        [Fact]
        public void CurrentValue_WithCachingEnabled_IsMemoized()
        {
            TranslationUnit.CacheValues = true;
            using TranslationUnit unit = CreatePlainUnit();

            string first = unit.CurrentValue;
            string second = unit.CurrentValue;

            Assert.Equal("Plain text", first);
            Assert.Same(first, second);
        }

        /// <summary>
        /// A culture change invalidates the memoized value but deliberately does NOT raise
        /// <c>OnChange</c>; that event is reserved for <c>NotifyPlaceholdersUpdated</c>.
        /// </summary>
        [Fact]
        public void CultureChange_InvalidatesCachedValue_ButDoesNotRaiseOnChange()
        {
            TranslationUnit.CacheValues = true;
            using TranslationUnit unit = new(_manager, _config, "hello", containsPlaceholders: false);

            Assert.Equal("Hello", unit.CurrentValue);

            int changed = 0;
            unit.OnChange += (sender, args) => changed++;

            _manager.CurrentCulture = new CultureInfo("fr");

            Assert.Equal("Salut", unit.CurrentValue);
            Assert.Equal(0, changed);
        }

        [Fact]
        public void NotifyPlaceholdersUpdated_RaisesOnChangeAndInvalidatesCachedValue()
        {
            TranslationUnit.CacheValues = true;
            using TranslationUnit unit = CreateTemplatedUnit();
            unit.CachePlaceholderValue("name", "First");

            Assert.Equal("Hello, First!", unit.CurrentValue);

            int changed = 0;
            unit.OnChange += (sender, args) => changed++;

            unit.CachePlaceholderValue("name", "Second");
            unit.NotifyPlaceholdersUpdated();

            Assert.Equal(1, changed);
            Assert.Equal("Hello, Second!", unit.CurrentValue);
        }

        /// <summary>
        /// After disposal the unit must no longer react to culture changes. A cached delegate held in a
        /// field must not resurrect that subscription.
        /// </summary>
        [Fact]
        public void Dispose_UnsubscribesFromCultureChanges()
        {
            TranslationUnit.CacheValues = true;
            TranslationUnit unit = new(_manager, _config, "hello", containsPlaceholders: false);

            Assert.Equal("Hello", unit.CurrentValue);
            unit.Dispose();

            _manager.CurrentCulture = new CultureInfo("fr");

            // The memoized value is retained because the invalidation handler is gone.
            Assert.Equal("Hello", unit.CurrentValue);
        }

        /// <summary>
        /// The explicit-dictionary overloads bypass the unit's own resolver entirely and must be
        /// unaffected by any change to it.
        /// </summary>
        [Fact]
        public void GetValue_WithExplicitDictionary_IgnoresUnitPlaceholderCache()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();
            unit.CachePlaceholderValue("name", "FromCache");

            Dictionary<string, object?> values = OptimizationFixtures.Values(("name", "FromDictionary"));

            Assert.Equal("Hello, FromDictionary!", unit.GetValue(_culture, values));
        }

        /// <summary>
        /// <c>GetValueAsTemplate</c> returns the raw text and must never run template processing.
        /// </summary>
        [Fact]
        public void GetValueAsTemplate_ReturnsUnprocessedText()
        {
            using TranslationUnit unit = CreateTemplatedUnit();

            Assert.Equal("Hello, {name}!", unit.GetValueAsTemplate(_culture));
        }

        /// <summary>
        /// Web encoding is applied after template processing, on the final string.
        /// </summary>
        [Fact]
        public void WebEncodeValues_EncodesTheProcessedResult()
        {
            TranslationUnit.CacheValues = false;
            using TranslationUnit unit = CreateTemplatedUnit();
            unit.CachePlaceholderValue("name", "<b>Ann</b>");

            Assert.Equal("Hello, <b>Ann</b>!", unit.GetValue(_culture));

            _manager.WebEncodeValues = true;
            try
            {
                string encoded = unit.GetValue(_culture);
                Assert.DoesNotContain("<b>", encoded, StringComparison.Ordinal);
                Assert.Contains("Ann", encoded, StringComparison.Ordinal);
            }
            finally
            {
                _manager.WebEncodeValues = false;
            }
        }
    }
}
