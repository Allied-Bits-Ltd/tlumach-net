// <copyright file="TranslateExtensionLifetimeTests.cs" company="Allied Bits Ltd.">
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using Tlumach.WPF;
using Tlumach.XamlTests;

namespace Tlumach.WpfTests
{
    /// <summary>
    /// Checks that the <see cref="TranslateExtension"/> markup extension does not keep the elements, the windows, or itself alive through the events of
    /// the translation manager and of the translation units, which the generated classes keep in static properties, and that the texts are still
    /// updated while the elements are alive.
    /// <para>The objects are created in separate methods that are not inlined, so that no local variable of the test keeps them reachable.</para>
    /// </summary>
    [Trait("Category", "WPF")]
    public class TranslateExtensionLifetimeTests
    {
        private const string WindowXaml = """
            <Window {0} Width="200" Height="100" Left="-10000" Top="-10000" ShowActivated="False" ShowInTaskbar="False" WindowStyle="None">
                <StackPanel>
                    <TextBlock Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                    <TextBlock Text="{{tlumach:Translate Unit={{x:Static t:TestStrings.Farewell}}}}" />
                    <TextBlock Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                </StackPanel>
            </Window>
            """;

        [Fact]
        public void ShouldNotKeepClosedWindowAlive()
        {
            Sta.Run(() =>
            {
                WeakReference window = ShowAndCloseWindow();
                Sta.DoEvents();
                Sta.CollectGarbage();

                Assert.False(window.IsAlive, "The closed window was not collected.");
            });
        }

        [Fact]
        public void ShouldNotKeepMarkupExtensionOrTargetAlive()
        {
            Sta.Run(() =>
            {
                (WeakReference extension, WeakReference target) = ApplyExtensionInCode();
                Sta.DoEvents();
                Sta.CollectGarbage();

                Assert.False(target.IsAlive, "The target element was not collected.");
                Assert.False(extension.IsAlive, "The markup extension was not collected.");
            });
        }

        [Fact]
        public void ShouldNotAccumulateEventHandlersOfReleasedElements()
        {
            Sta.Run(() =>
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
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="fixedUnit" Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                        <TextBlock x:Name="boundUnit" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </StackPanel>
                    """);
                panel.DataContext = new UnitHolder(TestStrings.Farewell);
                Sta.DoEvents();

                Sta.CollectGarbage();
                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();

                Assert.Equal("Hallo", ((TextBlock)panel.FindName("fixedUnit")).Text);
                Assert.Equal("Auf Wiedersehen", ((TextBlock)panel.FindName("boundUnit")).Text);
                GC.KeepAlive(panel);
            });
        }

        [Fact]
        public void ShouldUpdateTextWhenCultureChangesInBackgroundThread()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="fixedUnit" Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                        <TextBlock x:Name="boundUnit" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </StackPanel>
                    """);
                panel.DataContext = new UnitHolder(TestStrings.Farewell);
                Sta.DoEvents();

                Task.Run(() => TestStrings.TranslationManager.CurrentCulture = TestStrings.German).Wait();
                Sta.DoEvents();

                Assert.Equal("Hallo", ((TextBlock)panel.FindName("fixedUnit")).Text);
                Assert.Equal("Auf Wiedersehen", ((TextBlock)panel.FindName("boundUnit")).Text);
            });
        }

        [Fact]
        public void ShouldUpdateTextWhenPlaceholdersAreUpdatedAfterGarbageCollection()
        {
            Sta.Run(() =>
            {
                TestStrings.Named.CachePlaceholderValue("name", "Ann");
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="fixedUnit" Text="{{tlumach:Translate {{x:Static t:TestStrings.Named}}}}" />
                        <TextBlock x:Name="boundUnit" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </StackPanel>
                    """);
                panel.DataContext = new UnitHolder(TestStrings.Named);
                Sta.DoEvents();
                Assert.Equal("Hello, Ann", ((TextBlock)panel.FindName("fixedUnit")).Text);
                Assert.Equal("Hello, Ann", ((TextBlock)panel.FindName("boundUnit")).Text);

                Sta.CollectGarbage();
                TestStrings.Named.CachePlaceholderValue("name", "Bob");
                TestStrings.Named.NotifyPlaceholdersUpdated();
                Sta.DoEvents();

                Assert.Equal("Hello, Bob", ((TextBlock)panel.FindName("fixedUnit")).Text);
                Assert.Equal("Hello, Bob", ((TextBlock)panel.FindName("boundUnit")).Text);
            });
        }

        /// <summary>
        /// Collects the released objects and fires the events, which makes the subscriptions of the collected objects remove themselves.
        /// </summary>
        private static void RemoveSubscriptionsOfCollectedObjects()
        {
            Sta.DoEvents();
            Sta.CollectGarbage();

            TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
            TestStrings.Farewell.NotifyPlaceholdersUpdated();
            TestStrings.Greeting.NotifyPlaceholdersUpdated();
            TestStrings.Reset();
            Sta.DoEvents();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference ShowAndCloseWindow()
        {
            Window window = Sta.Parse<Window>(WindowXaml);
            window.DataContext = new UnitHolder(TestStrings.Greeting);
            window.Show();
            Sta.DoEvents();

            Assert.Equal("Hello", ((TextBlock)((StackPanel)window.Content).Children[0]).Text);
            Assert.Equal("Hello", ((TextBlock)((StackPanel)window.Content).Children[2]).Text);

            window.Close();
            return new WeakReference(window);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference Extension, WeakReference Target) ApplyExtensionInCode()
        {
            TextBlock textBlock = new();
            TranslateExtension extension = new(TestStrings.Greeting);
            object value = extension.ProvideValue(new TargetProvider(textBlock, TextBlock.TextProperty));
            textBlock.SetValue(TextBlock.TextProperty, value);
            Sta.DoEvents();

            Assert.Equal("Hello", textBlock.Text);
            return (new WeakReference(extension), new WeakReference(textBlock));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndReleaseElements(int count)
        {
            List<StackPanel> panels = [];
            for (int i = 0; i < count; i++)
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                        <TextBlock Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </StackPanel>
                    """);
                panel.DataContext = new UnitHolder(TestStrings.Farewell);
                panels.Add(panel);
            }

            Sta.DoEvents();
            Assert.All(panels, panel => Assert.Equal("Goodbye", ((TextBlock)panel.Children[1]).Text));
        }

        private sealed class TargetProvider : IServiceProvider, IProvideValueTarget
        {
            public TargetProvider(object targetObject, object targetProperty)
            {
                TargetObject = targetObject;
                TargetProperty = targetProperty;
            }

            public object TargetObject { get; }

            public object TargetProperty { get; }

            public object? GetService(Type serviceType) => serviceType == typeof(IProvideValueTarget) ? this : null;
        }
    }
}
