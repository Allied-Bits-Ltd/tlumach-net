// <copyright file="WinFormsFixture.cs" company="Allied Bits Ltd.">
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
using System.Text;

using Tlumach.Base;

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// Creates a temporary directory with an English default translation and a German translation, and a translation manager that loads them from the disk.
    /// The manager starts with the invariant culture, which selects the default (English) translation regardless of the culture of the machine.
    /// </summary>
    internal sealed class WinFormsFixture : IDisposable
    {
        public const string DefaultFile = "Strings.json";

        private const string DefaultJson = """
            {
                "greeting": "Hello",
                "farewell": "Goodbye",
                "hint": "Click here",
                "named": "Hello, {name}"
            }
            """;

        private const string GermanJson = """
            {
                "greeting": "Hallo",
                "farewell": "Auf Wiedersehen",
                "hint": "Hier klicken",
                "named": "Hallo, {name}"
            }
            """;

        private static readonly object _registrationLock = new();
        private static bool _registered;

        public WinFormsFixture()
        {
            EnsureParsersRegistered();

            DirectoryPath = Path.Combine(Path.GetTempPath(), "TlumachWinFormsTests", Path.GetRandomFileName());
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, DefaultFile), DefaultJson, Encoding.UTF8);
            File.WriteAllText(Path.Combine(DirectoryPath, "Strings_de.json"), GermanJson, Encoding.UTF8);

            Configuration = new TranslationConfiguration(assembly: null, DefaultFile, "en", TextFormat.DotNet)
            {
                DirectoryHint = DirectoryPath,
            };

            Manager = new TranslationManager(Configuration)
            {
                LoadFromDisk = true,
                TranslationsDirectory = DirectoryPath,
                CurrentCulture = CultureInfo.InvariantCulture,
            };
        }

        public string DirectoryPath { get; }

        public TranslationConfiguration Configuration { get; }

        public TranslationManager Manager { get; }

        public TranslationUnit CreateUnit(string key, bool containsPlaceholders = false)
            => new(Manager, Configuration, key, containsPlaceholders);

        public void Dispose()
        {
            Manager.Dispose();
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary directory does not affect the tests.
            }
        }

        private static void EnsureParsersRegistered()
        {
            lock (_registrationLock)
            {
                if (_registered)
                    return;

                JsonParser.Use();
                _registered = true;
            }
        }
    }
}
