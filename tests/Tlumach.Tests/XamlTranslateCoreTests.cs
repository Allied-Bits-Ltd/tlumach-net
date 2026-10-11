// <copyright file="XamlTranslateCoreTests.cs" company="Allied Bits Ltd.">
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
using System.Reflection;
using System.Runtime.CompilerServices;

using Tlumach.Base;

namespace Tlumach.Tests
{
    /// <summary>
    /// Tests of <see cref="XamlTranslateCore"/>, the part of the <c>Translate</c> markup extensions of WPF and MAUI that follows a translation unit,
    /// and of <see cref="WeakUnitSubscription{TOwner}"/>, which keeps the translation manager and the unit from keeping it alive.
    /// </summary>
    [Trait("Category", "XAML")]
    public class XamlTranslateCoreTests
    {
        [Fact]
        public void ShouldFollowCultureAndPlaceholderChanges()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            using TranslationUnit unit = new(manager, config, "hello", containsPlaceholders: false);
            List<string> notifications = [];
            XamlTranslateCore core = new(action => action());
            core.PropertyChanged += (_, args) => notifications.Add(args.PropertyName!);

            core.Unit = unit;
            Assert.Equal("Hello", core.Value);

            manager.CurrentCulture = new CultureInfo("de");
            Assert.Equal("Hallo (de)", core.Value);

            unit.NotifyPlaceholdersUpdated();
            Assert.Equal("Hallo (de)", core.Value);
            Assert.Equal(3, notifications.Count);

            core.Unit = null;
            Assert.Equal(string.Empty, core.Value);
        }

        [Fact]
        public void ShouldUnsubscribeFromPreviousUnitWhenUnitChanges()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            using TranslationUnit first = new(manager, config, "hello", containsPlaceholders: false);
            using TranslationUnit second = new(manager, config, "hello", containsPlaceholders: false);
            int cultureHandlers = CultureChangedHandlerCount(manager);
            XamlTranslateCore core = new(action => action());

            core.Unit = first;
            core.Unit = second;
            core.Unit = first;

            Assert.Equal(cultureHandlers + 1, CultureChangedHandlerCount(manager));
            Assert.Equal(1, UnitChangedHandlerCount(first));
            Assert.Equal(0, UnitChangedHandlerCount(second));

            core.Unit = null;
            Assert.Equal(cultureHandlers, CultureChangedHandlerCount(manager));
            Assert.Equal(0, UnitChangedHandlerCount(first));
        }

        [Fact]
        public void ShouldNotBeKeptAliveByManagerOrUnit()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            using TranslationUnit unit = new(manager, config, "hello", containsPlaceholders: false);
            int cultureHandlers = CultureChangedHandlerCount(manager);

            WeakReference core = CreateCore(unit);
            CollectGarbage();

            Assert.False(core.IsAlive, "The core was kept alive by the events of the manager or of the unit.");

            // The subscription of the collected core removes itself when an event fires
            Assert.Equal(cultureHandlers + 1, CultureChangedHandlerCount(manager));
            manager.CurrentCulture = new CultureInfo("de");
            unit.NotifyPlaceholdersUpdated();
            Assert.Equal(cultureHandlers, CultureChangedHandlerCount(manager));
            Assert.Equal(0, UnitChangedHandlerCount(unit));
        }

        [Fact]
        public void ShouldKeepFollowingCultureWhileReferenced()
        {
            using TranslationManager manager = CreateManager(out TranslationConfiguration config);
            using TranslationUnit unit = new(manager, config, "hello", containsPlaceholders: false);
            XamlTranslateCore core = new(action => action()) { Unit = unit };

            CollectGarbage();
            manager.CurrentCulture = new CultureInfo("fr");

            Assert.Equal("Salut (fr)", core.Value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateCore(TranslationUnit unit)
        {
            XamlTranslateCore core = new(action => action()) { Unit = unit };
            Assert.Equal("Hello", core.Value);
            return new WeakReference(core);
        }

        private static TranslationManager CreateManager(out TranslationConfiguration config)
        {
            string dir = LangIDsFixtures.CreateDirectory();
            config = LangIDsFixtures.CreateConfiguration(dir);
            TranslationManager manager = LangIDsFixtures.CreateManager(config, dir);
            manager.CurrentCulture = CultureInfo.InvariantCulture;
            return manager;
        }

        private static void CollectGarbage()
        {
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static int CultureChangedHandlerCount(TranslationManager manager) => HandlerCount(typeof(TranslationManager), manager, nameof(TranslationManager.OnCultureChanged));

        private static int UnitChangedHandlerCount(TranslationUnit unit) => HandlerCount(typeof(TranslationUnit), unit, nameof(TranslationUnit.OnChange));

        private static int HandlerCount(Type type, object instance, string eventName)
        {
            // A field-like event has a backing field with the name of the event.
            FieldInfo? field = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (field.GetValue(instance) as Delegate)?.GetInvocationList().Length ?? 0;
        }
    }
}
