// <copyright file="TranslationEntrySourceTests.cs" company="Allied Bits Ltd.">
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
    /// Tests of <see cref="TranslationManager.GetValueWithSource(TranslationConfiguration, string, CultureInfo, out TranslationEntrySource)"/> and its siblings.
    /// Each test performs a single lookup per key, so that the cache of default translations plays no role here; the cache is covered by
    /// <see cref="TranslationManagerBorrowedEntryTests"/>.
    /// </summary>
    [Trait("Category", "TranslationManager")]
    public class TranslationEntrySourceTests
    {
        private static TranslationManager CreateManager(out TranslationConfiguration config)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            config = LangIDsFixtures.CreateConfiguration(dir);
            return LangIDsFixtures.CreateManager(config, dir);
        }

        [Fact]
        public void ExactMatch_ReportsCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "hello", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.Equal("Salut (fr)", entry.Text);
            Assert.Equal(TranslationEntrySource.Culture, source);
        }

        [Fact]
        public void BasicCultureMatch_ReportsBasicCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "welcome", new CultureInfo("de-AT"), out TranslationEntrySource source);

            Assert.Equal("Willkommen (de-DE)", entry.Text);
            Assert.Equal(TranslationEntrySource.BasicCulture, source);
        }

        [Fact]
        public void KeyOnlyInDefaultFile_ReportsDefaultTranslation()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "onlyInDefault", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.Equal("OnlyInDefault", entry.Text);
            Assert.Equal(TranslationEntrySource.DefaultTranslation, source);
        }

        [Fact]
        public void RequestInDefaultLocale_ReportsDefaultTranslation()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "hello", new CultureInfo(LangIDsFixtures.DefaultLocale), out TranslationEntrySource source);

            Assert.Equal("Hello", entry.Text);
            Assert.Equal(TranslationEntrySource.DefaultTranslation, source);
        }

        [Fact]
        public void MissingKey_ReportsNotFound()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "noSuchKey", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.True(string.IsNullOrEmpty(entry.Text));
            Assert.Equal(TranslationEntrySource.NotFound, source);
        }

        [Fact]
        public void TextFromValueNeededHandler_ReportsCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            manager.OnTranslationValueNeeded += (_, args) =>
            {
                if (args.Key == "hello")
                    args.Text = "From the handler";
            };

            TranslationEntry entry = manager.GetValueWithSource(config, "hello", new CultureInfo("fr"), out TranslationEntrySource source);

            Assert.Equal("From the handler", entry.Text);
            Assert.Equal(TranslationEntrySource.Culture, source);
        }

        [Fact]
        public void EntryFromValueNeededHandler_ReportsCultureAndFoundForCulture()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            manager.OnTranslationValueNeeded += (_, args) =>
            {
                if (args.Key == "hello")
                    args.Entry = new TranslationEntry("hello", "Entry from the handler");
            };

            manager.GetValueWithSource(config, "hello", new CultureInfo("fr"), out TranslationEntrySource source);
            manager.GetValue(config, "hello", new CultureInfo("fr"), out bool foundForCulture);

            Assert.Equal(TranslationEntrySource.Culture, source);
            Assert.True(foundForCulture);
        }

        [Fact]
        public void DefaultConfigurationOverload_AgreesWithConfigurationOverload()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry viaDefault = manager.GetValueWithSource("hello", new CultureInfo("fr"), out TranslationEntrySource sourceViaDefault);

            Assert.Equal("Salut (fr)", viaDefault.Text);
            Assert.Equal(TranslationEntrySource.Culture, sourceViaDefault);
        }

        [Fact]
        public void LangIDsOverload_ReportsSourceOfTheLanguageThatAnswered()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);

            TranslationEntry entry = manager.GetValueWithSource(config, "welcome", ["es", "fr-CA"], out TranslationEntrySource source);

            Assert.Equal("Bienvenue (fr-FR)", entry.Text);
            Assert.Equal(TranslationEntrySource.BasicCulture, source);
        }
    }
}
