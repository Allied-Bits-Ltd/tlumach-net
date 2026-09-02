// <copyright file="TranslationManagerLangIDsTests.cs" company="Allied Bits Ltd.">
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
    /// Tests of the <c>string[] langIDs</c> overloads of <c>TranslationManager.GetValue(TranslationConfiguration, string, CultureInfo, string[], out bool)</c>
    /// and its siblings: the required lookup order is "all exact matches, in array order" then "all
    /// basic/parent-culture matches, in array order" then "the default translation" - and a value resolved
    /// for one requested language must never be cached into another, unrelated requested language.
    /// </summary>
    [Trait("Category", "TranslationManager")]
    [Trait("Category", "LangIDs")]
    public class TranslationManagerLangIDsTests
    {
        [Fact]
        public void GetValue_FirstLanguageHasExactMatch()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", ["fr", "de"], out bool found);

            Assert.Equal("Salut (fr)", entry.Text);
            Assert.True(found);
        }

        [Fact]
        public void GetValue_FirstLanguageUnavailable_LaterLanguageHasExactMatch()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            // "es" has no translation file at all.
            TranslationEntry entry = manager.GetValue(config, "hello", ["es", "fr"], out bool found);

            Assert.Equal("Salut (fr)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// Both "fr" and "de" have their own exact "welcome" entry; array order alone must decide the winner.
        /// </summary>
        [Theory]
        [InlineData(new[] { "fr", "de" }, "Bienvenue (fr)")]
        [InlineData(new[] { "de", "fr" }, "Willkommen (de)")]
        public void GetValue_MultipleExactMatches_ArrayOrderDeterminesResult(string[] langIDs, string expectedText)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "welcome", langIDs, out bool found);

            Assert.Equal(expectedText, entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// Neither "de-AT" nor "fr-CA" has its own "welcome"; the fallback resolves through de-AT's basic
        /// culture (de-DE), which is tried first because de-AT is first in the array.
        /// </summary>
        [Fact]
        public void GetValue_NoExactMatch_FallsBackToBasicCultureOfFirstApplicableLanguage()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["de-AT", "fr-CA"], out bool found);

            Assert.Equal("Willkommen (de-DE)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// "de-AT" (first in the array) has no exact "welcome", but "de" (second) does. Because every exact
        /// match is tried before any basic-culture match, "de"'s exact text must win over de-AT's basic
        /// culture (de-DE), even though de-AT was requested first.
        /// <para>This also pins down that an exact match for a neutral language ID such as "de" is checked
        /// literally (its own file), and not silently promoted to its default specific culture "de-DE" -
        /// which has different text for the same key in this fixture.</para>
        /// </summary>
        [Fact]
        public void GetValue_LaterExactMatch_WinsOverEarlierLanguagesBasicCultureMatch()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["de-AT", "de"], out bool found);

            Assert.Equal("Willkommen (de)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// An exact match for a neutral language ID must be tried as itself ("de"), not promoted to its
        /// default specific culture ("de-DE") before the exact pass even runs.
        /// </summary>
        [Fact]
        public void GetValue_ExactMatch_UsesLiteralCultureForNeutralLangID()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "welcome", ["de"], out bool found);

            Assert.Equal("Willkommen (de)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// Neither requested language has an exact "welcome", and their basic cultures ("de-DE" and
        /// "fr-FR") both have it with different text - array order must pick the winner.
        /// </summary>
        [Theory]
        [InlineData(new[] { "de-AT", "fr-CA" }, "Willkommen (de-DE)")]
        [InlineData(new[] { "fr-CA", "de-AT" }, "Bienvenue (fr-FR)")]
        public void GetValue_NoExactMatch_MultipleBasicCultures_ArrayOrderDeterminesResult(string[] langIDs, string expectedText)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "welcome", langIDs, out bool found);

            Assert.Equal(expectedText, entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// The requested languages (and their basic cultures) have their own files, but none of them
        /// declares this particular key, so the normal default translation is used.
        /// </summary>
        [Fact]
        public void GetValue_KeyMissingFromRequestedFiles_FallsBackToDefault()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "onlyInDefault", ["de-AT", "fr-CA"], out bool found);

            Assert.Equal("OnlyInDefault", entry.Text);
            Assert.False(found);
        }

        /// <summary>
        /// A language with no translation file at all must not stop the search: it is treated as a miss,
        /// and the next requested language is still tried.
        /// </summary>
        [Fact]
        public void GetValue_LanguageWithNoFileAtAll_DoesNotBreakTraversal()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", ["es", "de"], out bool found);

            Assert.Equal("Hallo (de)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// None of the requested languages, nor their basic cultures, has any file at all: the normal
        /// default translation is used, exactly as it would be for a single unmatched culture.
        /// </summary>
        [Fact]
        public void GetValue_NoneOfRequestedCulturesHasATranslation_ReturnsDefault()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", ["es", "it"], out bool found);

            Assert.Equal("Hello", entry.Text);
            Assert.False(found);
        }

        /// <summary>
        /// An unparseable language ID is silently skipped, exactly like the ones that raise
        /// <see cref="CultureNotFoundException"/> in the pre-existing single-culture fallback code.
        /// </summary>
        [Fact]
        public void GetValue_UnparseableLangID_IsSkippedGracefully()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", ["zz-INVALID", "de"], out bool found);

            Assert.Equal("Hallo (de)", entry.Text);
            Assert.True(found);
        }

        /// <summary>
        /// When none of the language IDs can even be parsed into a <see cref="CultureInfo"/>, the method
        /// must still fall back to the default translation instead of throwing.
        /// </summary>
        [Fact]
        public void GetValue_AllLangIDsUnparseable_FallsBackToDefaultWithoutThrowing()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            TranslationEntry entry = manager.GetValue(config, "hello", ["zz-INVALID", "yy-ALSOINVALID"], out bool found);

            Assert.Equal("Hello", entry.Text);
            Assert.False(found);
        }

        /// <summary>
        /// Regression test for the cache-corruption bug found while implementing this feature: resolving
        /// "welcome" for "de-AT" (via its basic culture "de-DE") must not leak German text into "fr-CA"'s
        /// own cached translation. A later, unrelated lookup for "fr-CA" must still resolve through ITS OWN
        /// basic culture ("fr-FR"), not return the German text cached by the earlier call.
        /// </summary>
        [Fact]
        public void GetValue_DoesNotCorruptAnotherRequestedLanguagesCache()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);

            TranslationEntry first = manager.GetValue(config, "welcome", ["de-AT", "fr-CA"], out _);
            Assert.Equal("Willkommen (de-DE)", first.Text);

            TranslationEntry second = manager.GetValue(config, "welcome", new CultureInfo("fr-CA"), out bool foundSecond);

            Assert.Equal("Bienvenue (fr-FR)", second.Text);
            Assert.True(foundSecond);
        }

        /// <summary>
        /// The value resolved through a basic-culture fallback is backfilled only into the requested
        /// language it actually belongs to ("de-AT"), not into every requested language.
        /// </summary>
        [Fact]
        public void GetValue_BasicCultureHit_BackfillsOnlyItsOwnRequestedLanguage()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);

            manager.GetValue(config, "welcome", ["de-AT", "fr-CA"], out _);

            Translation? deAt = manager.GetTranslation(new CultureInfo("de-AT"));
            Translation? frCa = manager.GetTranslation(new CultureInfo("fr-CA"));

            Assert.NotNull(deAt);
            Assert.True(deAt.ContainsKey("welcome"));

            Assert.NotNull(frCa);
            Assert.False(frCa.ContainsKey("welcome"));
        }

        /// <summary>
        /// When the lookup falls all the way through to the default translation, the resolved value is
        /// backfilled into every requested language that was actually looked at, not just one of them.
        /// </summary>
        [Fact]
        public void GetValue_DefaultFallback_BackfillsEveryRequestedLanguage()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir, cacheDefaultTranslations: true);

            manager.GetValue(config, "hello", ["es", "it"], out _);

            Translation? es = manager.GetTranslation(new CultureInfo("es"));
            Translation? it = manager.GetTranslation(new CultureInfo("it"));

            Assert.NotNull(es);
            Assert.True(es.ContainsKey("hello"));

            Assert.NotNull(it);
            Assert.True(it.ContainsKey("hello"));
        }

        /// <summary>
        /// The 3-argument (no <c>out</c>) and 2-argument (default configuration) convenience overloads must
        /// agree with the full <c>out bool foundForCulture</c> overload.
        /// </summary>
        [Fact]
        public void GetValue_ConvenienceOverloads_AgreeWithFullOverload()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            string[] langIDs = ["fr", "de"];

            TranslationEntry full = manager.GetValue(config, "hello", langIDs, out bool found);
            TranslationEntry noOut = manager.GetValue(config, "hello", langIDs);
            TranslationEntry defaultConfig = manager.GetValue("hello", langIDs);

            Assert.True(found);
            Assert.Equal(full.Text, noOut.Text);
            Assert.Equal(full.Text, defaultConfig.Text);
        }

        [Fact]
        public void GetValue_RejectsNullOrEmptyLangIDs()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            Assert.Throws<ArgumentException>(() => manager.GetValue(config, "hello", (string[])null!, out _));
            Assert.Throws<ArgumentException>(() => manager.GetValue(config, "hello", []));
            Assert.Throws<ArgumentException>(() => manager.GetValue("hello", (string[])null!));
        }

        [Fact]
        public void GetValue_RejectsBothCultureAndLangIDsSpecified()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);

            Assert.Throws<ArgumentException>(() => manager.GetValue(config, "hello", new CultureInfo("de"), ["fr"], out _));
        }
    }
}
