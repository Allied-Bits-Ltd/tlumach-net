// <copyright file="Item20_ConfigTranslationsTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 20 — giving <c>TranslationConfiguration.Translations</c> an
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> comparer so the manager stops uppercasing culture
    /// names before probing it.
    /// <para>
    /// This is a behaviour change as well as a performance one: today the map is case-sensitive, so a
    /// config file with a lowercase locale key simply does not match, and two keys differing only by case
    /// can coexist. These tests record the current behaviour and the resolution order — exact locale,
    /// then two-letter language, then the catch-all "other" entry — that the change must preserve.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "20")]
    [Collection("Optimizations")]
    public class Item20_ConfigTranslationsTests
    {
        /// <summary>
        /// The map matches its keys case-insensitively.
        /// <para>CHANGED BY OPTIMIZATION ITEM 20. It used to be case-sensitive, which meant the manager had
        /// to uppercase every culture name before probing, and a configuration file written with a
        /// lowercase locale key simply did not match. Both are fixed by the comparer change.</para>
        /// </summary>
        [Fact]
        public void TranslationsMap_MatchesKeysCaseInsensitively()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir, ("DE-DE", "OptStrings_de-DE.arb"));

            Assert.True(config.Translations.ContainsKey("DE-DE"));
            Assert.True(config.Translations.ContainsKey("de-DE"));
            Assert.True(config.Translations.ContainsKey("de-de"));
        }

        /// <summary>
        /// Two keys differing only by case now collide.
        /// <para>CHANGED BY OPTIMIZATION ITEM 20. This is the one behavioural regression risk of that item:
        /// a configuration that declared, say, both "de" and "DE" used to produce two entries and now
        /// throws as a duplicate. For locale names that is the desired reading, but it is a change.</para>
        /// </summary>
        [Fact]
        public void TranslationsMap_TreatsKeysDifferingOnlyByCaseAsDuplicates()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir);

            config.Translations.Add("DE", "OptStrings_de.arb");

            Assert.Throws<ArgumentException>(() => config.Translations.Add("de", "OptStrings_de.arb"));
            Assert.Single(config.Translations);
        }

        /// <summary>
        /// A lowercase locale key in a configuration now resolves. Before item 20 the manager probed the
        /// map with an uppercased culture name, so this mapping was silently ignored.
        /// </summary>
        [Fact]
        public void LowercaseLocaleKey_NowResolvesTheCultureFile()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir, ("fr", "OptStrings_fr.arb"));
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Assert.Equal("Salut", manager.GetValue(config, "hello", new CultureInfo("fr"), out _).Text);
        }

        /// <summary>
        /// An uppercase key matches, because the manager uppercases the culture name before probing.
        /// </summary>
        [Fact]
        public void UppercaseLocaleKey_ResolvesTheCultureFile()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir, ("FR", "OptStrings_fr.arb"));
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Assert.Equal("Salut", manager.GetValue(config, "hello", new CultureInfo("fr"), out _).Text);
        }

        /// <summary>
        /// A culture with no exact entry falls back to the two-letter language key.
        /// </summary>
        [Fact]
        public void UnmappedRegion_FallsBackToTwoLetterLanguageKey()
        {
            string dir = OptimizationFixtures.CreateTranslationDirectory(
                (OptimizationFixtures.DefaultFile, OptimizationFixtures.DefaultArb),
                ("German.arb", OptimizationFixtures.GermanArb));

            // Only the language key is mapped; "de-CH" has no entry and no matching file on disk.
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir, ("DE", "German.arb"));
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Assert.Equal("Hallo", manager.GetValue(config, "hello", new CultureInfo("de-CH"), out bool found).Text);
            Assert.True(found);
        }

        /// <summary>
        /// The exact locale entry wins over the two-letter language entry.
        /// </summary>
        [Fact]
        public void ExactLocaleKey_WinsOverLanguageKey()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(
                dir,
                ("DE", "OptStrings_de.arb"),
                ("DE-DE", "OptStrings_de-DE.arb"));
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            Assert.Equal("Willkommen (de-DE)", manager.GetValue(config, "welcome", new CultureInfo("de-DE"), out _).Text);
            Assert.Equal("Willkommen", manager.GetValue(config, "welcome", new CultureInfo("de"), out _).Text);
        }

        /// <summary>
        /// The catch-all "other" entry is used when nothing else matches. Its key is a literal lowercase
        /// constant, not an uppercased locale name, so it is unaffected by the comparer either way.
        /// </summary>
        [Fact]
        public void OtherKey_IsUsedWhenNothingElseMatches()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(
                dir,
                (TranslationConfiguration.KEY_TRANSLATION_OTHER, "OptStrings_fr.arb"));
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            // No entry and no file for ja-JP, so the "other" mapping supplies the content.
            Assert.Equal("Salut", manager.GetValue(config, "hello", new CultureInfo("ja-JP"), out _).Text);
        }

        [Fact]
        public void OtherKeyConstant_IsLowercase()
        {
            Assert.Equal("other", TranslationConfiguration.KEY_TRANSLATION_OTHER);
        }

        /// <summary>
        /// With no mapping at all, the manager falls back to the file-naming heuristic on disk.
        /// </summary>
        [Fact]
        public void WithNoMapping_FileNamingHeuristicResolvesTheCulture()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            // OptStrings_fr.arb is discovered purely from the base name plus the locale separator.
            Assert.Equal("Salut", manager.GetValue(config, "hello", new CultureInfo("fr"), out bool found).Text);
            Assert.True(found);
        }

        /// <summary>
        /// The manager reads the map under a lock while loading. Repeated loads across cultures must not
        /// disturb the configuration itself.
        /// </summary>
        [Fact]
        public void RepeatedLoads_DoNotMutateTheConfiguration()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            int countBefore = config.Translations.Count;

            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            foreach (string culture in new[] { "fr", "de", "de-DE", "de-AT", "ja-JP" })
                manager.GetValue(config, "hello", new CultureInfo(culture), out _);

            Assert.Equal(countBefore, config.Translations.Count);
        }
    }
}
