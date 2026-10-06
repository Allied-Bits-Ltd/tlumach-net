// <copyright file="TranslationManagerBorrowedEntryTests.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Tests
{
    /// <summary>
    /// Regression tests of the cache of default translations (<see cref="TranslationManager.CacheDefaultTranslations"/>): an entry that a lookup copies
    /// into the translation of a culture from the basic culture or from the default translation must not be mistaken for the culture's own text later.
    /// </summary>
    [Trait("Category", "TranslationManager")]
    public class TranslationManagerBorrowedEntryTests
    {
        private static TranslationManager CreateManager(out TranslationConfiguration config)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            config = LangIDsFixtures.CreateConfiguration(dir);
            return LangIDsFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);
        }

        [Fact]
        public void CachedDefault_IsNotReportedAsFoundForCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            CultureInfo fr = new("fr");

            manager.GetValue(config, "onlyInDefault", fr, out bool firstFound);
            TranslationEntry second = manager.GetValue(config, "onlyInDefault", fr, out bool secondFound);

            Assert.False(firstFound);
            Assert.False(secondFound);
            Assert.Equal("OnlyInDefault", second.Text);
        }

        [Fact]
        public void CachedDefault_ReportsDefaultTranslationSource()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            CultureInfo fr = new("fr");

            manager.GetValueWithSource(config, "onlyInDefault", fr, out _);
            manager.GetValueWithSource(config, "onlyInDefault", fr, out TranslationEntrySource source);

            Assert.Equal(TranslationEntrySource.DefaultTranslation, source);
        }

        [Fact]
        public void CachedDefault_DoesNotHideLaterLanguagesBasicCultureMatch()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            // "es" has no file, so this caches the default "Welcome" into the translation of "es".
            manager.GetValue(config, "welcome", ["es"], out _);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["es", "fr-CA"], out bool found);

            Assert.Equal("Bienvenue (fr-FR)", entry.Text);
            Assert.True(found);
        }

        [Fact]
        public void CachedDefault_DoesNotHideLaterLanguagesExactMatch()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            manager.GetValue(config, "welcome", ["es"], out _);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["es", "fr"], out bool found);

            Assert.Equal("Bienvenue (fr)", entry.Text);
            Assert.True(found);
        }

        [Fact]
        public void CachedBasicCultureEntry_IsReportedAsBasicCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            CultureInfo deAt = new("de-AT");

            manager.GetValueWithSource(config, "welcome", deAt, out _);
            TranslationEntry second = manager.GetValueWithSource(config, "welcome", deAt, out TranslationEntrySource source);

            Assert.Equal("Willkommen (de-DE)", second.Text);
            Assert.Equal(TranslationEntrySource.BasicCulture, source);
        }

        [Fact]
        public void CachedEntries_StayInTheTranslation()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            manager.GetValue(config, "onlyInDefault", new CultureInfo("fr"), out _);

            Translation? fr = manager.GetTranslation(new CultureInfo("fr"));
            Assert.NotNull(fr);
            Assert.True(fr.ContainsKey("onlyInDefault"));
        }
    }
}
