// <copyright file="Item01_KeyNormalizationTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 1 — removing the redundant <c>ToUpperInvariant()</c> calls from
    /// <c>TranslationManager.GetValue</c>.
    /// <para>
    /// The uppercase conversions are redundant only because both target dictionaries are built with
    /// <see cref="StringComparer.OrdinalIgnoreCase"/>. These tests pin that invariant, plus the
    /// case-insensitive behaviour that callers depend on, so the conversions cannot be removed without
    /// the case-insensitivity surviving.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "01")]
    [Collection("Optimizations")]
    public class Item01_KeyNormalizationTests
    {
        /// <summary>
        /// The invariant that makes the optimization safe: a <see cref="Translation"/> compares its keys
        /// case-insensitively, so pre-uppercasing a key changes nothing.
        /// </summary>
        [Fact]
        public void Translation_UsesOrdinalIgnoreCaseComparer()
        {
            Translation translation = new("en");

            Assert.Same(StringComparer.OrdinalIgnoreCase, translation.Comparer);
        }

        /// <summary>
        /// The same invariant for the manager's culture map, reached through its observable behaviour
        /// rather than through the protected member.
        /// </summary>
        [Fact]
        public void GetTranslation_MatchesCultureNameCaseInsensitively()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Translation? lower = manager.GetTranslation(new CultureInfo("de-DE"), tryLoadMissing: true);
            Translation? mixed = manager.GetTranslation(new CultureInfo("DE-de"), tryLoadMissing: true);

            Assert.NotNull(lower);
            Assert.Same(lower, mixed);
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("HELLO")]
        [InlineData("HeLLo")]
        public void GetValue_MatchesKeyCaseInsensitively(string key)
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, key, new CultureInfo("de-AT"), out bool found);

            Assert.Equal("Servus", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// Non-ASCII keys must keep matching case-insensitively. <c>ToUpperInvariant</c> and
        /// <see cref="StringComparer.OrdinalIgnoreCase"/> both apply simple, one-to-one invariant case
        /// mapping, so this holds before and after the change.
        /// </summary>
        [Theory]
        [InlineData("grusse")]
        [InlineData("GRUSSE")]
        [InlineData("Grusse")]
        public void GetValue_MatchesNonTrivialKeyCasing(string key)
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, key, new CultureInfo(OptimizationFixtures.DefaultLocale), out _);

            Assert.Equal("Greetings", entry.Text);
        }

        /// <summary>
        /// Requesting the default locale skips the culture branch entirely, and <c>foundForCulture</c> is
        /// reported as <see langword="false"/> even though a value was returned. This is load-bearing for
        /// <c>IStringLocalizer</c>, whose <c>ResourceNotFound</c> flag is derived from it.
        /// </summary>
        [Fact]
        public void GetValue_ForDefaultLocale_ReportsNotFoundForCulture()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", new CultureInfo(OptimizationFixtures.DefaultLocale), out bool found);

            Assert.Equal("Hello", entry.Text);
            Assert.False(found);
        }

        [Fact]
        public void GetValue_ForCultureWithOwnFile_ReportsFoundForCulture()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", new CultureInfo("fr"), out bool found);

            Assert.Equal("Salut", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// The basic-culture fallback: <c>de-AT</c> has no <c>welcome</c>, so the lookup resolves it from
        /// <c>de-DE</c>, which <c>CultureInfo.CreateSpecificCulture("de")</c> selects.
        /// </summary>
        [Fact]
        public void GetValue_FallsBackToBasicCulture()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir, cacheDefaultTranslations: false);

            TranslationEntry entry = manager.GetValue(config, "welcome", new CultureInfo("de-AT"), out bool found);

            Assert.Equal("Willkommen (de-DE)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// The fallback must stay stable when repeated, whether or not the resolved value is cached back
        /// into the culture-local translation.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetValue_BasicCultureFallback_IsStableAcrossRepeatedCalls(bool cacheDefaults)
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir, cacheDefaults);

            CultureInfo culture = new("de-AT");
            for (int i = 0; i < 5; i++)
            {
                TranslationEntry entry = manager.GetValue(config, "welcome", culture, out bool found);
                Assert.Equal("Willkommen (de-DE)", entry.Text);
                Assert.True(found);
            }
        }

        /// <summary>
        /// With back-filling enabled, a value resolved through the fallback is copied into the
        /// culture-local translation. This is what makes the second and later lookups direct hits, so it
        /// must survive any change to how keys are normalized.
        /// </summary>
        [Fact]
        public void GetValue_WithCaching_BackFillsCultureLocalTranslation()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);

            CultureInfo culture = new("de-AT");
            Translation? before = manager.GetTranslation(culture, tryLoadMissing: true);
            Assert.NotNull(before);
            Assert.False(before.ContainsKey("welcome"));

            manager.GetValue(config, "welcome", culture, out _);

            Translation? after = manager.GetTranslation(culture);
            Assert.NotNull(after);
            Assert.True(after.ContainsKey("welcome"));

            // The back-filled key must be findable in any casing, which is the whole point of item 1.
            Assert.True(after.ContainsKey("WELCOME"));
            Assert.True(after.ContainsKey("Welcome"));
        }

        /// <summary>
        /// A key that exists nowhere resolves to the shared empty entry. Negative results are not cached,
        /// so this stays true no matter how many times it is asked for.
        /// </summary>
        [Fact]
        public void GetValue_ForMissingKey_ReturnsSharedEmptyEntry()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            TranslationEntry first = manager.GetValue(config, "noSuchKey", new CultureInfo("de-AT"), out bool found);
            TranslationEntry second = manager.GetValue(config, "noSuchKey", new CultureInfo("de-AT"), out _);

            Assert.Same(TranslationEntry.Empty, first);
            Assert.Same(TranslationEntry.Empty, second);
            Assert.Null(first.Text);
            Assert.False(found);
        }

        [Fact]
        public void GetValue_RejectsNullArguments()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Assert.Throws<ArgumentNullException>(() => manager.GetValue(null!, "hello", CultureInfo.InvariantCulture, out _));
            Assert.Throws<ArgumentNullException>(() => manager.GetValue(config, null!, CultureInfo.InvariantCulture, out _));
            Assert.Throws<ArgumentNullException>(() => manager.GetValue(config, "hello", null!, out _));
        }

        /// <summary>
        /// Dropping a translation must be case-insensitive in the culture name too, since
        /// <c>DropTranslation</c> currently uppercases before removing.
        /// </summary>
        [Fact]
        public void DropTranslation_MatchesCultureNameCaseInsensitively()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Assert.NotNull(manager.GetTranslation(new CultureInfo("fr"), tryLoadMissing: true));

            manager.DropTranslation(new CultureInfo("FR"));

            Assert.Null(manager.GetTranslation(new CultureInfo("fr")));
        }
    }
}
