// <copyright file="TlumachStringLocalizerFactoryTests.cs" company="Allied Bits Ltd.">
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

using System.Reflection;

using Tlumach.Base;
using Tlumach.Extensions.Localization;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of the managers that <see cref="TlumachStringLocalizerFactory"/> creates. <c>IStringLocalizer&lt;T&gt;</c> is transient, so the factory is called for every resolution,
    /// and a new manager on every call would load the translations again and stay in <see cref="TranslationManager.TranslationManagers"/> until it is disposed.
    /// <para>Other tests create managers in parallel, so each test names its own default file and counts only the managers of that file.</para>
    /// </summary>
    [Trait("Category", "Localization")]
    [Trait("Category", "IStringLocalizerFactory")]
    public class TlumachStringLocalizerFactoryTests
    {
        [Fact]
        public void Create_Type_NewOptionsWithTheSameConfiguration_CreatesOneManager()
        {
            string file = UniqueFile();
            TranslationConfiguration configuration = new(typeof(TlumachStringLocalizerFactoryTests).Assembly, file, defaultFileLocale: null, TextFormat.DotNet);
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { Configuration = configuration });

            for (int i = 0; i < 5; i++)
                factory.Create(typeof(TlumachStringLocalizerFactoryTests));

            Assert.Equal(1, DisposeManagersOf(file));
        }

        [Fact]
        public void Create_Type_NewOptionsWithTheSameDefaultFile_CreatesOneManager()
        {
            string file = UniqueFile();
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { Assembly = typeof(TlumachStringLocalizerFactoryTests).Assembly, DefaultFile = file, DefaultFileLocale = "en" });

            for (int i = 0; i < 5; i++)
                factory.Create(typeof(TlumachStringLocalizerFactoryTests));

            Assert.Equal(1, DisposeManagersOf(file));
        }

        [Fact]
        public void Create_BaseName_NewOptionsWithTheSameConfiguration_CreatesOneManager()
        {
            string file = UniqueFile();
            TranslationConfiguration configuration = new(typeof(TlumachStringLocalizerFactoryTests).Assembly, file, defaultFileLocale: null, TextFormat.DotNet);
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { Configuration = configuration });

            for (int i = 0; i < 5; i++)
                factory.Create("Strings", "Location");

            Assert.Equal(1, DisposeManagersOf(file));
        }

        [Fact]
        public void Create_BaseName_WithoutAManagerSource_CreatesOneManagerPerBaseName()
        {
            string file = UniqueFile();
            string otherFile = UniqueFile();
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions());

            for (int i = 0; i < 5; i++)
            {
                factory.Create(file, string.Empty);
                factory.Create(otherFile, string.Empty);
            }

            Assert.Equal(1, DisposeManagersOf(file));
            Assert.Equal(1, DisposeManagersOf(otherFile));
        }

        [Fact]
        public void Create_BaseName_WithoutAManagerSource_LoadsTheFileFromTheCallingAssembly()
        {
            string file = UniqueFile();
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions());

            factory.Create(file, string.Empty);

            TranslationManager manager = Assert.Single(ManagersOf(file));
            Assembly? assembly = manager.DefaultConfiguration?.Assembly;
            manager.Dispose();

            Assert.Same(typeof(TlumachStringLocalizerFactoryTests).Assembly, assembly);
        }

        [Fact]
        public void Create_Type_DefaultFileWithoutAssembly_LoadsTheFileFromTheEntryAssembly()
        {
            string file = UniqueFile();
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { DefaultFile = file });

            factory.Create(typeof(TlumachStringLocalizerFactoryTests));

            TranslationManager manager = Assert.Single(ManagersOf(file));
            Assembly? assembly = manager.DefaultConfiguration?.Assembly;
            manager.Dispose();

            Assert.Same(Assembly.GetEntryAssembly(), assembly);
        }

        [Fact]
        public void Create_BaseName_DefaultFileWithoutAssembly_LoadsTheFileFromTheEntryAssembly()
        {
            string file = UniqueFile();
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { DefaultFile = file });

            factory.Create("SomeBaseName", string.Empty);

            TranslationManager manager = Assert.Single(ManagersOf(file));
            Assembly? assembly = manager.DefaultConfiguration?.Assembly;
            manager.Dispose();

            Assert.Same(Assembly.GetEntryAssembly(), assembly);
        }

        [Fact]
        public void CreateFromOptions_DefaultFileWithoutAssembly_LoadsTheFileFromTheEntryAssembly()
        {
            using TranslationManager manager = TranslationManagerResolver.CreateFromOptions(new TlumachLocalizationOptions { DefaultFile = UniqueFile() });

            Assert.Same(Assembly.GetEntryAssembly(), manager.DefaultConfiguration?.Assembly);
        }

        [Fact]
        public void CreateFromOptions_DefaultFileWithAssembly_LoadsTheFileFromThatAssembly()
        {
            Assembly assembly = typeof(TlumachStringLocalizerFactoryTests).Assembly;
            using TranslationManager manager = TranslationManagerResolver.CreateFromOptions(new TlumachLocalizationOptions { Assembly = assembly, DefaultFile = UniqueFile() });

            Assert.Same(assembly, manager.DefaultConfiguration?.Assembly);
        }

        [Fact]
        public void Create_ParallelCallsWithNewOptionsOfTheSameConfiguration_CreateOneManager()
        {
            string file = UniqueFile();
            TranslationConfiguration configuration = new(typeof(TlumachStringLocalizerFactoryTests).Assembly, file, defaultFileLocale: null, TextFormat.DotNet);
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { Configuration = configuration });

            Parallel.For(0, 64, _ => factory.Create(typeof(TlumachStringLocalizerFactoryTests)));

            Assert.Equal(1, DisposeManagersOf(file));
        }

        [Fact]
        public void Create_DifferentDefaultFiles_CreateDifferentManagers()
        {
            string file = UniqueFile();
            string otherFile = UniqueFile();
            string current = file;
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { Assembly = typeof(TlumachStringLocalizerFactoryTests).Assembly, DefaultFile = current });

            factory.Create(typeof(TlumachStringLocalizerFactoryTests));
            current = otherFile;
            factory.Create(typeof(TlumachStringLocalizerFactoryTests));
            current = file;
            factory.Create(typeof(TlumachStringLocalizerFactoryTests));

            // The options are requested for every localizer, so a provider can switch a context to another manager, and the first manager is reused when the provider switches back.
            Assert.Equal(1, DisposeManagersOf(file));
            Assert.Equal(1, DisposeManagersOf(otherFile));
        }

        [Fact]
        public void Create_OptionsWithAManager_UseThatManager()
        {
            string file = UniqueFile();
            using TranslationManager manager = new(new TranslationConfiguration(typeof(TlumachStringLocalizerFactoryTests).Assembly, file, defaultFileLocale: null, TextFormat.DotNet));
            TlumachStringLocalizerFactory factory = CreateFactory(() => new TlumachLocalizationOptions { TranslationManager = manager });

            for (int i = 0; i < 5; i++)
                Assert.IsType<TlumachStringLocalizer>(factory.Create(typeof(TlumachStringLocalizerFactoryTests)));

            Assert.Same(manager, Assert.Single(ManagersOf(file)));
        }

        private static string UniqueFile() => "Factory" + Guid.NewGuid().ToString("N") + ".toml";

        private static TlumachStringLocalizerFactory CreateFactory(Func<TlumachLocalizationOptions> getOptions)
            => new(new NewOptionsProvider(getOptions));

        private static TranslationManager[] ManagersOf(string file)
            => [.. TranslationManager.TranslationManagers.Where(manager => string.Equals(manager.DefaultConfiguration?.DefaultFile, file, StringComparison.Ordinal))];

        private static int DisposeManagersOf(string file)
        {
            TranslationManager[] managers = ManagersOf(file);
            foreach (TranslationManager manager in managers)
                manager.Dispose();

            return managers.Length;
        }

        // A settings provider that builds new options on every call, as a custom provider may do.
        private sealed class NewOptionsProvider : ITlumachSettingsProvider
        {
            private readonly Func<TlumachLocalizationOptions> _getOptions;

            public NewOptionsProvider(Func<TlumachLocalizationOptions> getOptions) => _getOptions = getOptions;

            public TlumachLocalizationOptions GetOptionsFor(string context) => _getOptions();
        }
    }
}
