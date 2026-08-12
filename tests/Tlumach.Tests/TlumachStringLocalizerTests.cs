// <copyright file="TlumachStringLocalizerTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Tlumach.Base;
using Tlumach.Extensions.Localization;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of <see cref="TlumachStringLocalizer"/> against the contract that the consumers of <see cref="IStringLocalizer"/> rely on.
    /// <para>The reference implementation of the framework returns the resource as it stands, falls back to the key itself when a resource is missing, and reports each key of the culture chain once.
    /// These tests pin that behaviour, which is what lets the localization of data annotations of ASP.NET work on top of Tlumach.</para>
    /// </summary>
    [Trait("Category", "Localization")]
    [Trait("Category", "IStringLocalizer")]
    public class TlumachStringLocalizerTests
    {
        private const string TestFilesPath = "../../../TestData/Localization";

        static TlumachStringLocalizerTests()
        {
            IniParser.Use();
            TomlParser.Use();
        }

        [Fact]
        public void ShouldFollowTheAmbientCultureBetweenCalls()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                // The localizer is created once, as a container would create it while the application starts. The culture must be read at the moment of the call and not at the moment of creation.
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal("Hello", localizer["hello"].Value);

                CultureInfo.CurrentCulture = new CultureInfo("de");
                Assert.Equal("Hallo", localizer["hello"].Value);

                CultureInfo.CurrentCulture = new CultureInfo("de-AT");
                Assert.Equal("Grüß Gott", localizer["hello"].Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldNotDisturbTheOriginalLocalizerInWithCulture()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            IStringLocalizer german = localizer.WithCulture(new CultureInfo("de"));

            Assert.NotSame(localizer, german);

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                Assert.Equal("Hallo", german["hello"].Value);
                Assert.Equal("Hello", localizer["hello"].Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldKeepAnExplicitCultureWhenTheAmbientOneChanges()
        {
            IStringLocalizer german = CreateLocalizer().WithCulture(new CultureInfo("de"));

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-AT");

                Assert.Equal("Hallo", german["hello"].Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldReturnTheKeyWhenNothingWasFound()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            LocalizedString result = localizer["thereIsNoSuchKey"];

            Assert.Equal("thereIsNoSuchKey", result.Name);
            Assert.Equal("thereIsNoSuchKey", result.Value);
            Assert.True(result.ResourceNotFound);
        }

        [Fact]
        public void ShouldNotReportNotFoundForATextOfTheDefaultTranslation()
        {
            IStringLocalizer localizer = CreateLocalizer().WithCulture(new CultureInfo("de"));

            // The key is present in the default translation only. The text is real, so the resource was found; only a key that exists nowhere is reported as missing.
            LocalizedString result = localizer["onlyDefault"];

            Assert.Equal("Only in the default translation", result.Value);
            Assert.False(result.ResourceNotFound);
        }

        [Fact]
        public void ShouldReturnTheTemplateUntouchedWithoutArguments()
        {
            IStringLocalizer localizer = CreateLocalizer().WithCulture(CultureInfo.InvariantCulture);

            // The value has to remain a usable format string: this is what ValidationAttribute, IViewLocalizer and the model binding of ASP.NET expect.
            Assert.Equal("Hello {userName}", localizer["greeting"].Value);
            Assert.Equal("Hello {0}", localizer["positional"].Value);
        }

        [Fact]
        public void ShouldSubstituteTheArgumentsThatWerePassed()
        {
            IStringLocalizer localizer = CreateLocalizer().WithCulture(CultureInfo.InvariantCulture);

            // The indexer that takes arguments is the one that the validation adapters of ASP.NET call, with the positional placeholders of a validation message.
            Assert.Equal("Hello Alice", localizer["positional", "Alice"].Value);
        }

        [Fact]
        public void ShouldReturnEveryKeyOnceAndTerminate()
        {
            IStringLocalizer localizer = CreateLocalizer().WithCulture(new CultureInfo("de-AT"));

            // Before the fix this call never returned: the parent of the culture was recomputed on every pass, so the keys of the German translation were appended without end.
            List<LocalizedString> all = localizer.GetAllStrings(includeParentCultures: true).ToList();

            Assert.Equal(all.Count, all.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());

            // The most specific culture wins for a key that several translations of the chain carry.
            Assert.Equal("Grüß Gott", all.Single(s => string.Equals(s.Name, "hello", StringComparison.OrdinalIgnoreCase)).Value);

            // The parent culture and the default translation contribute the keys that the Austrian translation does not carry.
            Assert.Equal("Hallo {userName}", all.Single(s => string.Equals(s.Name, "greeting", StringComparison.OrdinalIgnoreCase)).Value);
            Assert.Contains("onlyDefault", all.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldReturnOnlyTheCurrentCultureWhenParentsAreNotAsked()
        {
            IStringLocalizer localizer = CreateLocalizer().WithCulture(new CultureInfo("de-AT"));

            List<LocalizedString> all = localizer.GetAllStrings(includeParentCultures: false).ToList();

            Assert.Single(all);
            Assert.Equal("hello", all[0].Name, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ShouldReportTheSearchedLocation()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            Assert.Equal("Localizer.toml", localizer["hello"].SearchedLocation);
            Assert.Equal("Localizer.toml", localizer["thereIsNoSuchKey"].SearchedLocation);
        }

        private static TlumachStringLocalizer CreateLocalizer()
        {
            TranslationManager manager = new(Path.Combine(TestFilesPath, "Localizer.cfg"))
            {
                LoadFromDisk = true,
                TranslationsDirectory = TestFilesPath,
            };

            ServiceCollection services = new();
            services.AddTlumachLocalization(options => options.TranslationManager = manager);

            // WithCulture and WithTextProcessingMode are methods of Tlumach and not of the interface, which no longer declares them.
            return (TlumachStringLocalizer)services.BuildServiceProvider().GetRequiredService<IStringLocalizer>();
        }
    }
}
