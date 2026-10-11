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
        private static readonly string TestFilesPath = Path.Combine(Tlumach.TestUtilities.TestDirectories.Project, "TestData", "Localization");

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
        public void ShouldFormatTheKeyWithTheArgumentsWhenNothingWasFound()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            // The data annotations localization of ASP.NET passes the default message of an attribute without ErrorMessage, such as that of [EmailAddress], as the key. Like the reference
            // implementation, the localizer formats the key with the arguments, so that the user sees the name of the field and not "{0}".
            LocalizedString result = localizer["The {0} field is not a valid e-mail address.", "Email"];

            Assert.Equal("The {0} field is not a valid e-mail address.", result.Name);
            Assert.Equal("The Email field is not a valid e-mail address.", result.Value);
            Assert.True(result.ResourceNotFound);
            Assert.Equal("Localizer.toml", result.SearchedLocation);
        }

        [Fact]
        public void ShouldFormatAMissingKeyInTheCultureOfTheLocalizer()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de");
                Assert.Equal("Total 1.234,50", localizer["Total {0:N2}", 1234.5].Value);

                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.Equal("Total 1,234.50", localizer["Total {0:N2}", 1234.5].Value);
                Assert.Equal("Total 1.234,50", localizer.WithCulture(new CultureInfo("de"))["Total {0:N2}", 1234.5].Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Theory]
        [InlineData("Unbalanced {0 brace")]
        [InlineData("Missing {1} argument")]
        [InlineData("Named {field} placeholder")]
        public void ShouldReturnAMissingKeyThatIsNotACompositeFormatAsItStands(string name)
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            // The reference implementation throws a FormatException here. A missing translation must not break the page, so the key is returned as it stands, as it is without arguments.
            LocalizedString result = localizer[name, "x"];

            Assert.Equal(name, result.Value);
            Assert.True(result.ResourceNotFound);
        }

        [Fact]
        public void ShouldReturnAMissingKeyAsItStandsForANullArgumentArray()
        {
            TlumachStringLocalizer localizer = CreateLocalizer();

            LocalizedString result = localizer["The {0} field is required.", null!];

            Assert.Equal("The {0} field is required.", result.Value);
            Assert.True(result.ResourceNotFound);
        }

        [Fact]
        public void ShouldFormatAMissingKeyThroughTheGenericLocalizer()
        {
            using ServiceProvider provider = CreateServices().BuildServiceProvider();
            IStringLocalizer<TlumachStringLocalizerTests> localizer = provider.GetRequiredService<IStringLocalizer<TlumachStringLocalizerTests>>();

            LocalizedString result = localizer["The {0} field is not a valid e-mail address.", "Email"];

            Assert.Equal("The Email field is not a valid e-mail address.", result.Value);
            Assert.True(result.ResourceNotFound);
        }

        [Fact]
        public void ShouldEncodeAFormattedMissingKeyLikeAFoundText()
        {
            IStringLocalizer localizer = CreateLocalizer(webEncodeValues: true).WithCulture(CultureInfo.InvariantCulture);

            // With WebEncodeValues, the value of the indexer is ready for HTML output. The arguments may come from the user, so a formatted key is encoded just as the text of a found key is.
            LocalizedString found = localizer["positional", "<i>Alice</i>"];
            LocalizedString missing = localizer["<b>{0}</b>", "<i>Alice</i>"];

            Assert.Equal("Hello &lt;i&gt;Alice&lt;/i&gt;", found.Value);
            Assert.False(found.ResourceNotFound);
            Assert.Equal("&lt;b&gt;&lt;i&gt;Alice&lt;/i&gt;&lt;/b&gt;", missing.Value);
            Assert.True(missing.ResourceNotFound);
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
            LocalizedString result = localizer["positional", "Alice"];

            Assert.Equal("Hello Alice", result.Value);
            Assert.False(result.ResourceNotFound);
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

        [Fact]
        public void AddTlumachLocalization_KeepsLocalizersRegisteredEarlier()
        {
            TranslationManager manager = new(Path.Combine(TestFilesPath, "Localizer.cfg"))
            {
                LoadFromDisk = true,
                TranslationsDirectory = TestFilesPath,
            };

            ServiceCollection services = new();
            services.AddTransient(typeof(IStringLocalizer<>), typeof(MarkerLocalizer<>));
            services.AddTlumachLocalization(options => options.TranslationManager = manager);

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.IsType<MarkerLocalizer<TlumachStringLocalizerTests>>(provider.GetRequiredService<IStringLocalizer<TlumachStringLocalizerTests>>());
        }

        [Fact]
        public void AddTlumachLocalization_CalledTwice_ConfiguresTheOptionsOfTheFirstCall()
        {
            TranslationManager first = CreateManager();
            TranslationManager second = CreateManager();
            TlumachLocalizationOptions? seenByTheSecondCall = null;

            ServiceCollection services = new();
            services.AddTlumachLocalization(options => options.TranslationManager = first);
            services.AddTlumachLocalization(
                options =>
                {
                    seenByTheSecondCall = options;
                    Assert.Same(first, options.TranslationManager);
                    options.TranslationManager = second;
                },
                provider => provider.AddContext("Other", new TlumachLocalizationOptions { TranslationManager = first }));

            // Nothing is registered twice, so the last registration does not silently win.
            Assert.Single(services, d => d.ServiceType == typeof(ITlumachSettingsProvider));
            Assert.Single(services, d => d.ServiceType == typeof(IStringLocalizerFactory));
            Assert.Single(services, d => d.ServiceType == typeof(IStringLocalizer));
            Assert.Single(services, d => d.ServiceType == typeof(IStringLocalizer<>));

            using ServiceProvider provider = services.BuildServiceProvider();
            ITlumachSettingsProvider settings = provider.GetRequiredService<ITlumachSettingsProvider>();
            Assert.Same(seenByTheSecondCall, settings.GetOptionsFor("Unknown"));
            Assert.Same(second, settings.GetOptionsFor("Unknown").TranslationManager);
            Assert.Same(first, settings.GetOptionsFor("Other").TranslationManager);
        }

        [Fact]
        public void AddContext_CalledWhileOptionsAreRead_IsThreadSafe()
        {
            ServiceCollection services = new();
            services.AddTlumachLocalization(options => options.TranslationManager = CreateManager());
            using ServiceProvider provider = services.BuildServiceProvider();
            TlumachSettingsProvider settings = (TlumachSettingsProvider)provider.GetRequiredService<ITlumachSettingsProvider>();
            TlumachLocalizationOptions defaultOptions = settings.GetOptionsFor("Unknown");

            // A plain Dictionary fails here, or corrupts itself, when it is resized while other threads read it.
            Parallel.For(0, 20_000, i =>
            {
                if (i % 2 == 0)
                    settings.AddContext("Context" + i, new TlumachLocalizationOptions());
                else
                    Assert.Same(defaultOptions, settings.GetOptionsFor("Missing" + i));
            });

            Assert.NotSame(defaultOptions, settings.GetOptionsFor("Context0"));
            Assert.NotSame(defaultOptions, settings.GetOptionsFor("Context19998"));
        }

        [Fact]
        public void Constructor_WithoutAMode_UsesTheModeOfTheManager()
        {
            TranslationManager manager = CreateManager(TextFormat.Arb);

            IStringLocalizer withoutMode = new TlumachStringLocalizer(manager).WithCulture(CultureInfo.InvariantCulture);
            IStringLocalizer arb = new TlumachStringLocalizer(manager, TextFormat.Arb).WithCulture(CultureInfo.InvariantCulture);
            IStringLocalizer dotNet = new TlumachStringLocalizer(manager, TextFormat.DotNet).WithCulture(CultureInfo.InvariantCulture);

            // The ARB format treats two apostrophes as an escaped one, the .NET format does not; the manager was configured for ARB.
            Assert.Equal("It's Alice", arb["apostrophe", "Alice"].Value);
            Assert.Equal("It''s Alice", dotNet["apostrophe", "Alice"].Value);
            Assert.Equal("It's Alice", withoutMode["apostrophe", "Alice"].Value);
        }

        [Fact]
        public void GeneratedClassLocalizer_UsesTheModeOfTheManagerOfTheClass()
        {
            // Without options that name a manager, the factory takes the manager from the TranslationManager property of the class, as for a class created by Generator.
            ServiceCollection services = new();
            services.AddTlumachLocalization(_ => { });
            using ServiceProvider provider = services.BuildServiceProvider();

            IStringLocalizer<ArbStrings> localizer = provider.GetRequiredService<IStringLocalizer<ArbStrings>>();

            Assert.Equal("It's Alice", ((TlumachStringLocalizer<ArbStrings>)localizer).WithCulture(CultureInfo.InvariantCulture)["apostrophe", "Alice"].Value);
        }

        [Fact]
        public void GenericLocalizer_WithAnotherFactory_ThrowsADescriptiveException()
        {
            TlumachStringLocalizer<TlumachStringLocalizerTests> localizer = new(new ForeignFactory());

            InvalidOperationException withCulture = Assert.Throws<InvalidOperationException>(() => localizer.WithCulture(CultureInfo.InvariantCulture));
            InvalidOperationException withMode = Assert.Throws<InvalidOperationException>(() => localizer.WithTextProcessingMode(TextFormat.Arb));

            Assert.Contains(nameof(TlumachStringLocalizerFactory), withCulture.Message, StringComparison.Ordinal);
            Assert.Contains(typeof(MarkerLocalizer<object>).FullName!, withCulture.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(TlumachStringLocalizer.WithTextProcessingMode), withMode.Message, StringComparison.Ordinal);

            // The members of the interface still delegate to the localizer of the other factory.
            Assert.Equal("Key", localizer["Key"].Value);
        }

        [Fact]
        public void GenericLocalizer_WithTheTlumachFactory_DelegatesWithCultureAndWithTextProcessingMode()
        {
            IStringLocalizer<TlumachStringLocalizerTests> localizer = CreateServices().BuildServiceProvider().GetRequiredService<IStringLocalizer<TlumachStringLocalizerTests>>();
            TlumachStringLocalizer<TlumachStringLocalizerTests> tlumach = Assert.IsType<TlumachStringLocalizer<TlumachStringLocalizerTests>>(localizer);

            Assert.Equal("Hallo", tlumach.WithCulture(new CultureInfo("de"))["hello"].Value);
            Assert.IsType<TlumachStringLocalizer>(tlumach.WithTextProcessingMode(TextFormat.Arb));
        }

        // Stands for a class created by Generator: the factory reads its static TranslationManager property.
        private sealed class ArbStrings
        {
            public static TranslationManager TranslationManager { get; } = CreateManager(TextFormat.Arb);
        }

        private sealed class ForeignFactory : IStringLocalizerFactory
        {
            public IStringLocalizer Create(Type resourceSource) => new MarkerLocalizer<object>();

            public IStringLocalizer Create(string baseName, string location) => new MarkerLocalizer<object>();
        }

        private sealed class MarkerLocalizer<T> : IStringLocalizer<T>
        {
            public LocalizedString this[string name] => new(name, name);

            public LocalizedString this[string name, params object[] arguments] => new(name, name);

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        }

        private static TlumachStringLocalizer CreateLocalizer(bool webEncodeValues = false)
        {
            // WithCulture and WithTextProcessingMode are methods of Tlumach and not of the interface, which no longer declares them.
            return (TlumachStringLocalizer)CreateServices(webEncodeValues).BuildServiceProvider().GetRequiredService<IStringLocalizer>();
        }

        private static TranslationManager CreateManager(TextFormat textProcessingMode)
            => new(new TranslationConfiguration(assembly: null, "Localizer.toml", defaultFileLocale: null, textProcessingMode))
            {
                LoadFromDisk = true,
                TranslationsDirectory = TestFilesPath,
            };

        private static TranslationManager CreateManager()
            => new(Path.Combine(TestFilesPath, "Localizer.cfg"))
            {
                LoadFromDisk = true,
                TranslationsDirectory = TestFilesPath,
            };

        private static ServiceCollection CreateServices(bool webEncodeValues = false)
        {
            TranslationManager manager = new(Path.Combine(TestFilesPath, "Localizer.cfg"))
            {
                LoadFromDisk = true,
                TranslationsDirectory = TestFilesPath,
                WebEncodeValues = webEncodeValues,
            };

            ServiceCollection services = new();
            services.AddTlumachLocalization(options => options.TranslationManager = manager);

            return services;
        }
    }
}
