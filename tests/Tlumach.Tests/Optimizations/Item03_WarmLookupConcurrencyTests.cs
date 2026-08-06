// <copyright file="Item03_WarmLookupConcurrencyTests.cs" company="Allied Bits Ltd.">
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
    /// Optimization item 3 — removing <c>lock(translation)</c> and the <c>Monitor.Enter(this)</c> pairs
    /// from the warm read path.
    /// <para>
    /// These locks are what currently makes concurrent reads safe. Replacing them with lock-free reads and
    /// a volatile <c>_defaultTranslation</c> must not change any observable result. These tests hammer the
    /// warm paths from many threads and assert that every observer agrees.
    /// </para>
    /// </summary>
    [Trait("Category", "Optimization")]
    [Trait("Item", "03")]
    [Collection("Optimizations")]
    public class Item03_WarmLookupConcurrencyTests
    {
        private const int ThreadCount = 16;
        private const int IterationsPerThread = 200;

        [Fact]
        public void WarmReads_FromManyThreads_AgreeOnCultureLocalValue()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            CultureInfo culture = new("fr");
            manager.GetValue(config, "hello", culture, out _);

            ConcurrentBag<string?> values = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, ThreadCount, index =>
            {
                try
                {
                    for (int i = 0; i < IterationsPerThread; i++)
                        values.Add(manager.GetValue(config, "hello", culture, out _).Text);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(ThreadCount * IterationsPerThread, values.Count);
            Assert.Equal(["Salut"], values.Distinct().ToArray());
        }

        /// <summary>
        /// The default-translation path takes the additional <c>Monitor.Enter(this)</c> pairs, including
        /// the lazy initialization of <c>_defaultTranslation</c>. Racing many threads through it from cold
        /// is the case a volatile-plus-CompareExchange rewrite must get right.
        /// </summary>
        [Fact]
        public void WarmReads_ThroughDefaultTranslation_AgreeOnValue()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            CultureInfo culture = new(OptimizationFixtures.DefaultLocale);

            ConcurrentBag<string?> values = [];
            ConcurrentBag<Exception> failures = [];

            Parallel.For(0, ThreadCount, index =>
            {
                try
                {
                    for (int i = 0; i < IterationsPerThread; i++)
                        values.Add(manager.GetValue(config, "hello", culture, out _).Text);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);
            Assert.Equal(["Hello"], values.Distinct().ToArray());
        }

        /// <summary>
        /// Several cultures read at once through one manager. Each thread must see its own culture's value
        /// and never another's.
        /// </summary>
        [Fact]
        public void WarmReads_AcrossCultures_DoNotCrossOver()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            (string Culture, string Expected)[] expectations =
            [
                ("fr", "Salut"),
                ("de", "Hallo"),
                ("de-AT", "Servus"),
                (OptimizationFixtures.DefaultLocale, "Hello"),
            ];

            foreach ((string culture, string _) in expectations)
                manager.GetValue(config, "hello", new CultureInfo(culture), out _);

            ConcurrentBag<Exception> failures = [];
            ConcurrentBag<(string Culture, string? Value)> observed = [];

            Parallel.For(0, ThreadCount * 4, index =>
            {
                (string culture, string _) = expectations[index % expectations.Length];
                CultureInfo cultureInfo = new(culture);
                try
                {
                    for (int i = 0; i < 50; i++)
                        observed.Add((culture, manager.GetValue(config, "hello", cultureInfo, out _).Text));
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
        /// Reads racing against <c>DropAllTranslations</c>. A reader may legitimately observe either the
        /// loaded value or a reloaded one, but it must never throw and must never observe a value from a
        /// different culture. After the storm settles, a fresh read must be correct again.
        /// </summary>
        [Fact]
        public void WarmReads_RacingWithDropAllTranslations_DoNotThrowOrCorrupt()
        {
            string dir = OptimizationFixtures.CreateStandardDirectory();
            TranslationConfiguration config = OptimizationFixtures.CreateStandardConfiguration(dir);
            using TranslationManager manager = OptimizationFixtures.CreateManager(config, dir);

            CultureInfo culture = new("fr");
            manager.GetValue(config, "hello", culture, out _);

            ConcurrentBag<Exception> failures = [];
            ConcurrentBag<string?> values = [];

            Parallel.For(0, ThreadCount, index =>
            {
                try
                {
                    if (index % 8 == 0)
                    {
                        for (int i = 0; i < 10; i++)
                            manager.DropAllTranslations();
                    }
                    else
                    {
                        for (int i = 0; i < IterationsPerThread; i++)
                            values.Add(manager.GetValue(config, "hello", culture, out _).Text);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            });

            Assert.Empty(failures);

            // Every observed value must be a legitimate result for this culture: either the French text or
            // the default-translation fallback seen while the French translation was momentarily absent.
            foreach (string? value in values.Distinct())
                Assert.True(value is "Salut" or "Hello", $"Unexpected value observed under contention: '{value ?? "<null>"}'.");

            Assert.Equal("Salut", manager.GetValue(config, "hello", culture, out _).Text);
        }
    }
}
