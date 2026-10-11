// <copyright file="TranslateExtensionBindingTests.cs" company="Allied Bits Ltd.">
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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Tlumach.WPF;
using Tlumach.XamlTests;

namespace Tlumach.WpfTests
{
    /// <summary>
    /// Checks the <see cref="TranslateExtension"/> markup extension with a fixed unit and with a unit that comes from a binding
    /// (<c>Unit="{Binding ...}"</c>) with each kind of binding source.
    /// </summary>
    [Trait("Category", "WPF")]
    public class TranslateExtensionBindingTests
    {
        [Fact]
        public void ShouldShowFixedUnitAndFollowCulture()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="positional" Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                        <TextBlock x:Name="named" Text="{{tlumach:Translate Unit={{x:Static t:TestStrings.Farewell}}}}" />
                    </StackPanel>
                    """);
                Sta.DoEvents();

                Assert.Equal("Hello", Text(panel, "positional"));
                Assert.Equal("Goodbye", Text(panel, "named"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();

                Assert.Equal("Hallo", Text(panel, "positional"));
                Assert.Equal("Auf Wiedersehen", Text(panel, "named"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromDataContext()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="text" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </StackPanel>
                    """);
                UnitHolder holder = new(TestStrings.Greeting);
                panel.DataContext = holder;
                Sta.DoEvents();

                Assert.Equal("Hello", Text(panel, "text"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal("Hallo", Text(panel, "text"));

                holder.Unit = TestStrings.Farewell;
                Sta.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(panel, "text"));

                holder.Unit = null;
                Sta.DoEvents();
                Assert.Equal(string.Empty, Text(panel, "text"));

                panel.DataContext = new UnitHolder(TestStrings.Greeting);
                Sta.DoEvents();
                Assert.Equal("Hallo", Text(panel, "text"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromBindingPassedAsConstructorArgument()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="text" Text="{{tlumach:Translate {{Binding Unit}}}}" />
                    </StackPanel>
                    """);
                UnitHolder holder = new(TestStrings.Greeting);
                panel.DataContext = holder;
                Sta.DoEvents();

                Assert.Equal("Hello", Text(panel, "text"));

                holder.Unit = TestStrings.Farewell;
                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(panel, "text"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromElementName()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="source" Tag="{{x:Static t:TestStrings.Greeting}}" />
                        <TextBlock x:Name="text" Text="{{tlumach:Translate Unit={{Binding Tag, ElementName=source}}}}" />
                    </StackPanel>
                    """);
                Sta.DoEvents();

                Assert.Equal("Hello", Text(panel, "text"));

                ((TextBlock)panel.FindName("source")).Tag = TestStrings.Farewell;
                Sta.DoEvents();
                Assert.Equal("Goodbye", Text(panel, "text"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(panel, "text"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromRelativeSource()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0} Tag="{{x:Static t:TestStrings.Greeting}}">
                        <Border>
                            <TextBlock x:Name="text" Text="{{tlumach:Translate Unit={{Binding Tag, RelativeSource={{RelativeSource AncestorType=StackPanel}}}}}}" />
                        </Border>
                        <TextBlock x:Name="self" Tag="{{x:Static t:TestStrings.Farewell}}" Text="{{tlumach:Translate Unit={{Binding Tag, RelativeSource={{RelativeSource Self}}}}}}" />
                    </StackPanel>
                    """);
                Sta.DoEvents();

                Assert.Equal("Hello", Text(panel, "text"));
                Assert.Equal("Goodbye", Text(panel, "self"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal("Hallo", Text(panel, "text"));
                Assert.Equal("Auf Wiedersehen", Text(panel, "self"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromExplicitSource()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <TextBlock x:Name="text" Text="{{tlumach:Translate Unit={{Binding Source={{x:Static t:TestStrings.Greeting}}}}}}" />
                        <TextBlock x:Name="path" Text="{{tlumach:Translate Unit={{Binding Unit, Source={{x:Static t:TestStrings.FarewellHolder}}}}}}" />
                    </StackPanel>
                    """);
                Sta.DoEvents();

                Assert.Equal("Hello", Text(panel, "text"));
                Assert.Equal("Goodbye", Text(panel, "path"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal("Hallo", Text(panel, "text"));
                Assert.Equal("Auf Wiedersehen", Text(panel, "path"));
            });
        }

        [Fact]
        public void ShouldResolveUnitOfEachItemInDataTemplate()
        {
            Sta.Run(() =>
            {
                ItemsControl items = Sta.Parse<ItemsControl>("""
                    <ItemsControl {0}>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <TextBlock Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    """);
                items.ItemsSource = new[] { new UnitHolder(TestStrings.Greeting), new UnitHolder(TestStrings.Farewell) };
                items.Measure(new Size(500, 500));
                items.Arrange(new Rect(0, 0, 500, 500));
                Sta.DoEvents();

                Assert.Equal(["Hello", "Goodbye"], TextBlocks(items).Select(textBlock => textBlock.Text));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal(["Hallo", "Auf Wiedersehen"], TextBlocks(items).Select(textBlock => textBlock.Text));
            });
        }

        [Fact]
        public void ShouldResolveUnitInStyleSetter()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0}>
                        <StackPanel.Resources>
                            <Style x:Key="bound" TargetType="TextBlock">
                                <Setter Property="Text" Value="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                            </Style>
                            <Style x:Key="fixed" TargetType="TextBlock">
                                <Setter Property="Text" Value="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                            </Style>
                        </StackPanel.Resources>
                        <TextBlock x:Name="first" Style="{{StaticResource bound}}" DataContext="{{x:Static t:TestStrings.FarewellHolder}}" />
                        <TextBlock x:Name="second" Style="{{StaticResource bound}}" />
                        <TextBlock x:Name="third" Style="{{StaticResource fixed}}" />
                    </StackPanel>
                    """);
                panel.DataContext = new UnitHolder(TestStrings.Greeting);
                Sta.DoEvents();

                Assert.Equal("Goodbye", Text(panel, "first"));
                Assert.Equal("Hello", Text(panel, "second"));
                Assert.Equal("Hello", Text(panel, "third"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Sta.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(panel, "first"));
                Assert.Equal("Hallo", Text(panel, "second"));
                Assert.Equal("Hallo", Text(panel, "third"));
            });
        }

        [Fact]
        public void ShouldShowEmptyTextForValueThatIsNotUnit()
        {
            Sta.Run(() =>
            {
                StackPanel panel = Sta.Parse<StackPanel>("""
                    <StackPanel {0} Tag="not a unit">
                        <TextBlock x:Name="notUnit" Text="{{tlumach:Translate Unit={{Binding Tag, RelativeSource={{RelativeSource AncestorType=StackPanel}}}}}}" />
                        <TextBlock x:Name="missing" Text="{{tlumach:Translate Unit={{Binding NoSuchProperty}}}}" />
                    </StackPanel>
                    """);
                panel.DataContext = new UnitHolder(TestStrings.Greeting);
                Sta.DoEvents();

                Assert.Equal(string.Empty, Text(panel, "notUnit"));
                Assert.Equal(string.Empty, Text(panel, "missing"));
            });
        }

        private static string Text(FrameworkElement root, string name) => ((TextBlock)root.FindName(name)).Text;

        private static List<TextBlock> TextBlocks(DependencyObject root)
        {
            List<TextBlock> result = [];
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is TextBlock textBlock)
                    result.Add(textBlock);
                else
                    result.AddRange(TextBlocks(child));
            }

            return result;
        }
    }
}
