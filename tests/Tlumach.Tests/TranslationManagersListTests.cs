// <copyright file="TranslationManagersListTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of <see cref="TranslationManager.TranslationManagers"/>, which is read by code that updates all managers at once while other threads may create or dispose managers.
    /// </summary>
    public class TranslationManagersListTests
    {
        [Fact]
        public void TranslationManagers_ReturnsSnapshotUnaffectedByLaterChanges()
        {
            IReadOnlyList<TranslationManager> before = TranslationManager.TranslationManagers;

            using TranslationManager manager = CreateManager();

            Assert.DoesNotContain(manager, before);
            Assert.Contains(manager, TranslationManager.TranslationManagers);
        }

        [Fact]
        public void TranslationManagers_DoesNotContainDisposedManager()
        {
            TranslationManager manager = CreateManager();
            manager.Dispose();

            Assert.DoesNotContain(manager, TranslationManager.TranslationManagers);
        }

        [Fact]
        public void TranslationManagers_ContainsEmptyManager()
        {
            Assert.Contains(TranslationManager.Empty, TranslationManager.TranslationManagers);
        }

        [Fact]
        public async Task TranslationManagers_CanBeEnumeratedWhileManagersAreCreatedAndDisposed()
        {
            using CancellationTokenSource stop = new();

            Task churn = Task.Run(
                () =>
                {
                    while (!stop.IsCancellationRequested)
                    {
                        using TranslationManager manager = CreateManager();
                    }
                },
                CancellationToken.None);

            try
            {
                for (int i = 0; i < 2000; i++)
                {
                    foreach (TranslationManager manager in TranslationManager.TranslationManagers)
                        Assert.NotNull(manager);
                }
            }
            finally
            {
                await stop.CancelAsync();
                await churn;
            }
        }

        private static TranslationManager CreateManager()
            => new(new TranslationConfiguration(assembly: null, "TranslationManagersListTests.arb", defaultFileLocale: null, TextFormat.Arb));
    }
}
