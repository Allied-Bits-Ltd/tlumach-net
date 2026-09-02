// <copyright file="BaseTranslationUnitLangIDsTests.cs" company="Allied Bits Ltd.">
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
using System.Collections.Specialized;
using System.Globalization;

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of the <c>string[] langIDs</c> overloads of <see cref="BaseTranslationUnit"/> (exercised
    /// through the concrete <see cref="TranslationUnit"/>), which must delegate to the corresponding
    /// <see cref="TranslationManager"/> overloads following the same patterns as the existing
    /// <see cref="CultureInfo"/>-based overloads.
    /// </summary>
    [Trait("Category", "TranslationUnit")]
    [Trait("Category", "LangIDs")]
    public class BaseTranslationUnitLangIDsTests
    {
        [Fact]
        public void GetValue_WithLangIDs_PlainText_ResolvesFirstExactMatch()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "hello", containsPlaceholders: false);

            Assert.Equal("Salut (fr)", unit.GetValue(["fr", "de"]));
        }

        [Fact]
        public void GetValue_WithLangIDs_FallsBackToBasicCulture()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "welcome", containsPlaceholders: false);

            Assert.Equal("Willkommen (de-DE)", unit.GetValue(["de-AT", "fr-CA"]));
        }

        [Fact]
        public void GetValue_WithLangIDs_ArrayOrderDeterminesExactMatchWinner()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "welcome", containsPlaceholders: false);

            Assert.Equal("Bienvenue (fr)", unit.GetValue(["fr", "de"]));
            Assert.Equal("Willkommen (de)", unit.GetValue(["de", "fr"]));
        }

        [Fact]
        public void GetValue_WithLangIDs_TemplatedEntry_SubstitutesPlaceholders()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);
            unit.CachePlaceholderValue("name", "Ann");

            Assert.Equal("Hello, Ann!", unit.GetValue(["fr", "de"]));
        }

        [Fact]
        public void GetValueAsTemplate_WithLangIDs_ReturnsUnprocessedText()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);

            Assert.Equal("Hello, {name}!", unit.GetValueAsTemplate(["fr", "de"]));
        }

        [Fact]
        public void GetValue_WithLangIDsAndExplicitDictionary_IgnoresUnitPlaceholderCache()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);
            unit.CachePlaceholderValue("name", "FromCache");

            Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase) { ["name"] = "FromDictionary" };

            Assert.Equal("Hello, FromDictionary!", unit.GetValue(["fr", "de"], values));
        }

        [Fact]
        public void GetValue_WithLangIDsAndOrderedDictionary_SubstitutesPlaceholders()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);

            OrderedDictionary values = new() { ["name"] = "Ordered" };

            Assert.Equal("Hello, Ordered!", unit.GetValue(["fr", "de"], values));
        }

        [Fact]
        public void GetValue_WithLangIDsAndObjectArray_SubstitutesIndexedPlaceholders()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);

            Assert.Equal("Hello, Positional!", unit.GetValue(["fr", "de"], "Positional"));
        }

        [Fact]
        public void GetValue_WithLangIDsAndAnonymousObject_SubstitutesPlaceholders()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);

            Assert.Equal("Hello, FromAnon!", unit.GetValueFrom(["fr", "de"], new { name = "FromAnon" }));
        }

        /// <summary>
        /// An unparseable first language ID must not break template formatting: the resolver falls back to
        /// <see cref="TranslationManager.CurrentCulture"/> instead of throwing.
        /// </summary>
        [Fact]
        public void GetValue_WithLangIDs_UnparseableFirstLangID_StillFormatsUsingCurrentCulture()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            manager.CurrentCulture = new CultureInfo(LangIDsFixtures.DefaultLocale);
            using TranslationUnit unit = new(manager, config, "greeting", containsPlaceholders: true);
            unit.CachePlaceholderValue("name", "Ann");

            Assert.Equal("Hello, Ann!", unit.GetValue(["zz-INVALID", "de"]));
        }

        /// <summary>
        /// <see cref="UntranslatedUnit"/> overrides the internal accessors to always return its fixed
        /// source value, ignoring whatever culture or language IDs are requested. The langIDs overloads
        /// must respect that override exactly like the CultureInfo ones do, rather than falling through to
        /// a real translation lookup.
        /// </summary>
        [Fact]
        public void GetValue_WithLangIDs_UntranslatedUnit_ReturnsSourceValue()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using UntranslatedUnit unit = new("Untranslated source", manager, config, containsPlaceholders: false);

            Assert.Equal("Untranslated source", unit.GetValue(["fr", "de"]));
            Assert.Equal("Untranslated source", unit.GetValueAsTemplate(["fr", "de"]));
        }

        [Fact]
        public void GetValue_WithLangIDs_NullOrEmptyArray_PropagatesArgumentException()
        {
            string dir = LangIDsFixtures.CreateDirectory();
            TranslationConfiguration config = LangIDsFixtures.CreateConfiguration(dir);
            using TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            using TranslationUnit unit = new(manager, config, "hello", containsPlaceholders: false);

            Assert.Throws<ArgumentException>(() => unit.GetValue((string[])null!));
            Assert.Throws<ArgumentException>(() => unit.GetValue(Array.Empty<string>()));
        }
    }
}
