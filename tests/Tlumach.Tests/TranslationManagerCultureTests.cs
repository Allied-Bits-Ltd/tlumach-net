// <copyright file="TranslationManagerCultureTests.cs" company="Allied Bits Ltd.">
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
    /// Tests of <see cref="TranslationManager.UseContextCulture"/> and its effect on <see cref="TranslationManager.CurrentCulture"/> and on the <see cref="TranslationManager.OnCultureChanged"/> event.
    /// </summary>
    [Trait("Category", "TranslationManager")]
    [Trait("Category", "Culture")]
    public class TranslationManagerCultureTests
    {
        private static TranslationManager CreateManager()
        {
            TranslationConfiguration config = new(null, "dummy.json", null, TextFormat.None);
            return new TranslationManager(config);
        }

        [Fact]
        public void ShouldNotUseContextCultureByDefault()
        {
            using TranslationManager manager = CreateManager();

            Assert.False(manager.UseContextCulture);
        }

        [Fact]
        public void ShouldKeepTheConfiguredCultureWhenContextCultureIsDisabled()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                using TranslationManager manager = CreateManager();
                manager.CurrentCulture = new CultureInfo("de-AT");

                // The manager must keep reporting the culture that was explicitly configured, regardless of the ambient culture of the thread.
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                Assert.Equal("de-AT", manager.CurrentCulture.Name);

                CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
                Assert.Equal("de-AT", manager.CurrentCulture.Name);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldFollowTheAmbientCultureWhenContextCultureIsEnabled()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                using TranslationManager manager = CreateManager();
                manager.CurrentCulture = new CultureInfo("de-AT");
                manager.UseContextCulture = true;

                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                Assert.Equal("fr-FR", manager.CurrentCulture.Name);

                // The read has to be live: a further change of the ambient culture must be visible without touching the manager again.
                CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
                Assert.Equal("ja-JP", manager.CurrentCulture.Name);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldRaiseCultureChangedWhenCurrentCultureIsSetWhileContextCultureIsDisabled()
        {
            using TranslationManager manager = CreateManager();

            int raised = 0;
            CultureInfo? notified = null;
            manager.OnCultureChanged += (_, args) => { raised++; notified = args.Culture; };

            manager.CurrentCulture = new CultureInfo("de-AT");

            Assert.Equal(1, raised);
            Assert.Equal("de-AT", notified?.Name);
        }

        [Fact]
        public void ShouldNotRaiseCultureChangedWhenCurrentCultureIsSetWhileContextCultureIsEnabled()
        {
            using TranslationManager manager = CreateManager();
            manager.UseContextCulture = true;

            int raised = 0;
            manager.OnCultureChanged += (_, _) => raised++;

            // The effective culture keeps following the ambient one, so the change of the backing field must stay silent.
            manager.CurrentCulture = new CultureInfo("de-AT");

            Assert.Equal(0, raised);
        }

        [Fact]
        public void ShouldRaiseCultureChangedWhenContextCultureIsEnabledAndTheAmbientCultureDiffers()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                // The manager's own culture defaults to CultureInfo.InvariantCulture, which differs from the ambient culture set above.
                using TranslationManager manager = CreateManager();

                int raised = 0;
                CultureInfo? notified = null;
                manager.OnCultureChanged += (_, args) => { raised++; notified = args.Culture; };

                manager.UseContextCulture = true;

                Assert.Equal(1, raised);
                Assert.Equal("fr-FR", notified?.Name);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldNotRaiseCultureChangedWhenContextCultureIsEnabledAndTheAmbientCultureAlreadyMatches()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-AT");

                using TranslationManager manager = CreateManager();
                manager.CurrentCulture = new CultureInfo("de-AT");

                int raised = 0;
                manager.OnCultureChanged += (_, _) => raised++;

                // Nothing observable changes: the culture the manager reports is "de-AT" both before and after the switch.
                manager.UseContextCulture = true;

                Assert.Equal(0, raised);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldNotRaiseCultureChangedWhenSettingContextCultureToItsCurrentValue()
        {
            using TranslationManager manager = CreateManager();

            int raised = 0;
            manager.OnCultureChanged += (_, _) => raised++;

            manager.UseContextCulture = false; // already false by default

            Assert.Equal(0, raised);
        }

        [Fact]
        public void ShouldNotNotifyFromSystemCultureUpdatedWhenContextCultureIsEnabled()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo ambient = new("de-AT");
                CultureInfo.CurrentCulture = ambient;

                using TranslationManager manager = CreateManager();
                manager.CurrentCulture = ambient;
                manager.UseContextCulture = true;

                int raised = 0;
                manager.OnCultureChanged += (_, _) => raised++;

                // SystemCultureUpdated exists so that a change of the OS culture can be relayed when the manager tracks it; with the context culture enabled, the manager already tracks it live and must stay silent here.
                manager.SystemCultureUpdated();

                Assert.Equal(0, raised);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ShouldNotifyFromSystemCultureUpdatedWhenContextCultureIsDisabled()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo ambient = new("de-AT");
                CultureInfo.CurrentCulture = ambient;

                using TranslationManager manager = CreateManager();
                manager.CurrentCulture = ambient;

                int raised = 0;
                manager.OnCultureChanged += (_, _) => raised++;

                manager.SystemCultureUpdated();

                Assert.Equal(1, raised);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
