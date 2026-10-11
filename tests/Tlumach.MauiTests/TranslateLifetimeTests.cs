// <copyright file="TranslateLifetimeTests.cs" company="Allied Bits Ltd.">
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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Maui.Controls;

using Tlumach.MAUI;
using Tlumach.XamlTests;

namespace Tlumach.MauiTests
{
    /// <summary>
    /// Checks that the <see cref="Translate"/> markup extension does not keep the elements or itself alive through the events of the translation manager and of
    /// the translation units, which the generated classes keep in static properties, and that the texts are still updated while the elements are alive.
    /// <para>The objects are created in separate methods that are not inlined, so that no local variable of the test keeps them reachable.</para>
    /// </summary>
    [Trait("Category", "MAUI")]
    public class TranslateLifetimeTests
    {
        private const string LayoutXaml = """
            <VerticalStackLayout {0}>
                <Label x:Name="fixedUnit" Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                <Label x:Name="boundUnit" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
            </VerticalStackLayout>
            """;

        [Fact]
        public void ShouldNotKeepReleasedPageAlive()
        {
            Ui.Run(() =>
            {
                WeakReference page = CreateAndReleasePage();
                Ui.DoEvents();
                Ui.CollectGarbage();

                Assert.False(page.IsAlive, "The released page was not collected.");
            });
        }

        [Fact]
        public void ShouldNotKeepMarkupExtensionOrTargetAlive()
        {
            Ui.Run(() =>
            {
                (WeakReference extension, WeakReference target) = ApplyExtensionInCode();
                Ui.DoEvents();
                Ui.CollectGarbage();

                Assert.False(target.IsAlive, "The target element was not collected.");
                Assert.False(extension.IsAlive, "The markup extension was not collected.");
            });
        }

        [Fact]
        public void ShouldNotAccumulateEventHandlersOfReleasedElements()
        {
            Ui.Run(() =>
            {
                TranslationManager manager = TestStrings.TranslationManager;

                // Earlier tests may have left subscriptions of their collected elements
                RemoveSubscriptionsOfCollectedObjects();
                int cultureHandlers = Subscriptions.CultureChanged(manager);
                int greetingHandlers = Subscriptions.UnitChanged(TestStrings.Greeting);
                int farewellHandlers = Subscriptions.UnitChanged(TestStrings.Farewell);

                CreateAndReleaseElements(count: 20);
                Assert.True(Subscriptions.CultureChanged(manager) > cultureHandlers);

                RemoveSubscriptionsOfCollectedObjects();

                Assert.Equal(cultureHandlers, Subscriptions.CultureChanged(manager));
                Assert.Equal(greetingHandlers, Subscriptions.UnitChanged(TestStrings.Greeting));
                Assert.Equal(farewellHandlers, Subscriptions.UnitChanged(TestStrings.Farewell));
            });
        }

        [Fact]
        public void ShouldUpdateLiveElementsAfterGarbageCollection()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>(LayoutXaml);
                UnitHolder holder = new(TestStrings.Farewell);
                layout.BindingContext = holder;
                Ui.DoEvents();

                Ui.CollectGarbage();
                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();

                Assert.Equal("Hallo", layout.FindByName<Label>("fixedUnit").Text);
                Assert.Equal("Auf Wiedersehen", layout.FindByName<Label>("boundUnit").Text);

                // The binding of the unit still works after a collection (the markup extension is not needed for it)
                Ui.CollectGarbage();
                holder.Unit = TestStrings.Greeting;
                Ui.DoEvents();
                Assert.Equal("Hallo", layout.FindByName<Label>("boundUnit").Text);
            });
        }

        [Fact]
        public void ShouldUpdateTextWhenCultureChangesInBackgroundThread()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>(LayoutXaml);
                layout.BindingContext = new UnitHolder(TestStrings.Farewell);
                Ui.DoEvents();

                Task.Run(() => TestStrings.TranslationManager.CurrentCulture = TestStrings.German).Wait();

                // The change is dispatched to the UI thread
                Assert.Equal("Hello", layout.FindByName<Label>("fixedUnit").Text);
                Ui.DoEvents();

                Assert.Equal("Hallo", layout.FindByName<Label>("fixedUnit").Text);
                Assert.Equal("Auf Wiedersehen", layout.FindByName<Label>("boundUnit").Text);
            });
        }

        [Fact]
        public void ShouldUpdateTextWhenPlaceholdersAreUpdatedAfterGarbageCollection()
        {
            Ui.Run(() =>
            {
                TestStrings.Named.CachePlaceholderValue("name", "Ann");
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="fixedUnit" Text="{{tlumach:Translate {{x:Static t:TestStrings.Named}}}}" />
                        <Label x:Name="boundUnit" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </VerticalStackLayout>
                    """);
                layout.BindingContext = new UnitHolder(TestStrings.Named);
                Ui.DoEvents();
                Assert.Equal("Hello, Ann", layout.FindByName<Label>("fixedUnit").Text);
                Assert.Equal("Hello, Ann", layout.FindByName<Label>("boundUnit").Text);

                Ui.CollectGarbage();
                TestStrings.Named.CachePlaceholderValue("name", "Bob");
                TestStrings.Named.NotifyPlaceholdersUpdated();
                Ui.DoEvents();

                Assert.Equal("Hello, Bob", layout.FindByName<Label>("fixedUnit").Text);
                Assert.Equal("Hello, Bob", layout.FindByName<Label>("boundUnit").Text);
            });
        }

        /// <summary>
        /// Collects the released objects and fires the events, which makes the subscriptions of the collected objects remove themselves.
        /// </summary>
        private static void RemoveSubscriptionsOfCollectedObjects()
        {
            Ui.DoEvents();
            Ui.CollectGarbage();

            TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
            TestStrings.Farewell.NotifyPlaceholdersUpdated();
            TestStrings.Greeting.NotifyPlaceholdersUpdated();
            TestStrings.Reset();
            Ui.DoEvents();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateAndReleasePage()
        {
            ContentPage page = new() { Content = Ui.Load<VerticalStackLayout>(LayoutXaml) };
            page.BindingContext = new UnitHolder(TestStrings.Farewell);
            Ui.DoEvents();

            Assert.Equal("Hello", ((VerticalStackLayout)page.Content).FindByName<Label>("fixedUnit").Text);
            Assert.Equal("Goodbye", ((VerticalStackLayout)page.Content).FindByName<Label>("boundUnit").Text);
            return new WeakReference(page);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference Extension, WeakReference Target) ApplyExtensionInCode()
        {
            Label label = new();
            Translate extension = new() { Unit = TestStrings.Greeting };
            label.SetBinding(Label.TextProperty, extension.ProvideValue(EmptyServiceProvider.Instance));
            Ui.DoEvents();

            Assert.Equal("Hello", label.Text);
            return (new WeakReference(extension), new WeakReference(label));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndReleaseElements(int count)
        {
            List<VerticalStackLayout> layouts = [];
            for (int i = 0; i < count; i++)
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>(LayoutXaml);
                layout.BindingContext = new UnitHolder(TestStrings.Farewell);
                layouts.Add(layout);
            }

            Ui.DoEvents();
            Assert.All(layouts, layout => Assert.Equal("Goodbye", layout.FindByName<Label>("boundUnit").Text));
        }

        private sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new();

            public object? GetService(Type serviceType) => null;
        }
    }
}
