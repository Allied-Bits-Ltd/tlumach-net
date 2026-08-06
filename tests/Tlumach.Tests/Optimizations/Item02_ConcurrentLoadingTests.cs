// <copyright file="Item02_ConcurrentLoadingTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 2 — narrowing <c>lock(Translations)</c> so it no longer covers file I/O and
    /// parsing.
    /// <para>
    /// Today the lock serializes all loading, which is slow but trivially correct. Narrowing it or moving
    /// to a concurrent map introduces the possibility of two threads loading the same culture at once, of
    /// a torn read, or of one thread observing a partially populated translation. These tests pin the
    /// guarantees that must survive: one translation instance per culture, correct values under
    /// contention, and no exceptions.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "02")]
    [Collection("Optimizations")]
    public class Item02_ConcurrentLoadingTests
    {
        private const int ThreadCount = 32;

        /// <summary>
        /// Many threads racing on a not-yet-loaded culture must all observe the same value, and the
        /// manager must end up holding exactly one translation instance for that culture.
        /// </summary>
        [Fact]
        public void ConcurrentColdLoad_SameCulture_YieldsOneTranslationAndConsistentValues()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            ConcurrentBag<string?> values = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, ThreadCount, index =>
            {
                try
                {
                    values.Add(manager.GetValue(config, "hello", new CultureInfo("fr"), out _).Text);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(ThreadCount, values.Count);
            Assert.Equal(["Salut"], values.Distinct().ToArray());

            // Exactly one instance is registered, and it is reachable under any casing of the name.
            Translation? one = manager.GetTranslation(new CultureInfo("fr"));
            Translation? two = manager.GetTranslation(new CultureInfo("FR"));
            Assert.NotNull(one);
            Assert.Same(one, two);
        }

        /// <summary>
        /// Threads asking for different cultures at the same time do genuinely independent work. This is
        /// the case the lock currently serializes; after the change it must still produce correct results.
        /// </summary>
        [Fact]
        public void ConcurrentColdLoad_DistinctCultures_ResolvesEachCorrectly()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            (string Culture, string Expected)[] expectations =
            [
                ("fr", "Salut"),
                ("de", "Hallo"),
                ("de-DE", "Hallo"),
                ("de-AT", "Servus"),
            ];

            ConcurrentBag<Exception> failures = [];
            ConcurrentBag<(string Culture, string? Value)> observed = [];

            Parallel.For(0, ThreadCount, index =>
            {
                (string culture, string _) = expectations[index % expectations.Length];
                try
                {
                    observed.Add((culture, manager.GetValue(config, "hello", new CultureInfo(culture), out _).Text));
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);

            foreach ((string culture, string expected) in expectations)
            {
                string?[] forCulture = observed.Where(o => o.Culture == culture).Select(o => o.Value).Distinct().ToArray();
                Assert.Equal([expected], forCulture);
            }
        }

        /// <summary>
        /// The basic-culture fallback runs a second, nested load while the first culture is already being
        /// resolved. It must remain correct when several threads do it at once.
        /// </summary>
        [Fact]
        public void ConcurrentColdLoad_WithBasicCultureFallback_IsConsistent()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir, cacheDefaultTranslations: false);

            ConcurrentBag<string?> values = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, ThreadCount, index =>
            {
                try
                {
                    values.Add(manager.GetValue(config, "welcome", new CultureInfo("de-AT"), out _).Text);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(["Willkommen (de-DE)"], values.Distinct().ToArray());
        }

        /// <summary>
        /// Back-filling writes into a translation that other threads may be reading. All observers must
        /// see the same value, and the write must not corrupt the dictionary.
        /// </summary>
        [Fact]
        public void ConcurrentBackFill_DoesNotCorruptCultureLocalTranslation()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);

            ConcurrentBag<string?> values = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, ThreadCount, index =>
            {
                try
                {
                    values.Add(manager.GetValue(config, "welcome", new CultureInfo("de-AT"), out _).Text);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(["Willkommen (de-DE)"], values.Distinct().ToArray());

            Translation? deAt = manager.GetTranslation(new CultureInfo("de-AT"));
            Assert.NotNull(deAt);
            Assert.True(deAt.ContainsKey("welcome"));
            Assert.Equal("Servus", deAt["hello"].Text);
        }

        /// <summary>
        /// Reloading after <c>DropAllTranslations</c> must produce the same values. This guards a
        /// concurrent map against retaining stale entries.
        /// </summary>
        [Fact]
        public void DropAllTranslations_ThenReload_ProducesSameValues()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            string? before = manager.GetValue(config, "hello", new CultureInfo("fr"), out _).Text;

            manager.DropAllTranslations();
            Assert.Null(manager.GetTranslation(new CultureInfo("fr")));

            string? after = manager.GetValue(config, "hello", new CultureInfo("fr"), out _).Text;

            Assert.Equal("Salut", before);
            Assert.Equal(before, after);
        }

        /// <summary>
        /// A cold load that finds no file for the culture must still register a placeholder translation,
        /// so that later lookups fall through to the default translation instead of retrying the disk
        /// every time. The configuration here deliberately omits the catch-all "other" mapping.
        /// </summary>
        [Fact]
        public void ColdLoad_ForUnmappedCulture_FallsThroughToDefaultTranslation()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateConfiguration(dir, ("FR", "OptStrings_fr.arb"));
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir, cacheDefaultTranslations: false);

            TranslationEntry entry = manager.GetValue(config, "hello", new CultureInfo("ja-JP"), out bool found);

            Assert.Equal("Hello", entry.Text);
            Assert.False(found);

            // A translation object is registered for the culture even though no file existed, so the
            // expensive disk probing is not repeated on the next lookup.
            Assert.NotNull(manager.GetTranslation(new CultureInfo("ja-JP")));
        }
    }
}
