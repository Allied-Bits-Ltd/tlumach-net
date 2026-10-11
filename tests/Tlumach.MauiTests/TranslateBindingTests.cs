// <copyright file="TranslateBindingTests.cs" company="Allied Bits Ltd.">
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

using System.Linq;

using Microsoft.Maui.Controls;

using Tlumach.MAUI;
using Tlumach.XamlTests;

namespace Tlumach.MauiTests
{
    /// <summary>
    /// Checks the <see cref="Translate"/> markup extension with a fixed unit and with a unit that comes from a binding
    /// (<c>Unit="{Binding ...}"</c>) with each kind of binding source.
    /// </summary>
    [Trait("Category", "MAUI")]
    public class TranslateBindingTests
    {
        [Fact]
        public void ShouldShowFixedUnitAndFollowCulture()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="positional" Text="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                        <Label x:Name="named" Text="{{tlumach:Translate Unit={{x:Static t:TestStrings.Farewell}}}}" />
                    </VerticalStackLayout>
                    """);
                Ui.DoEvents();

                Assert.Equal("Hello", Text(layout, "positional"));
                Assert.Equal("Goodbye", Text(layout, "named"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();

                Assert.Equal("Hallo", Text(layout, "positional"));
                Assert.Equal("Auf Wiedersehen", Text(layout, "named"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromBindingContext()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="text" Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                    </VerticalStackLayout>
                    """);
                UnitHolder holder = new(TestStrings.Greeting);
                layout.BindingContext = holder;
                Ui.DoEvents();

                Assert.Equal("Hello", Text(layout, "text"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal("Hallo", Text(layout, "text"));

                holder.Unit = TestStrings.Farewell;
                Ui.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(layout, "text"));

                holder.Unit = null;
                Ui.DoEvents();
                Assert.Equal(string.Empty, Text(layout, "text"));

                layout.BindingContext = new UnitHolder(TestStrings.Greeting);
                Ui.DoEvents();
                Assert.Equal("Hallo", Text(layout, "text"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromBindingPassedAsContentProperty()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="text" Text="{{tlumach:Translate {{Binding Unit}}}}" />
                    </VerticalStackLayout>
                    """);
                UnitHolder holder = new(TestStrings.Greeting);
                layout.BindingContext = holder;
                Ui.DoEvents();

                Assert.Equal("Hello", Text(layout, "text"));

                holder.Unit = TestStrings.Farewell;
                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(layout, "text"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromReferencedElement()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="source" BindingContext="{{x:Static t:TestStrings.FarewellHolder}}" />
                        <Label x:Name="text" Text="{{tlumach:Translate Unit={{Binding BindingContext.Unit, Source={{x:Reference source}}}}}}" />
                    </VerticalStackLayout>
                    """);
                Ui.DoEvents();

                Assert.Equal("Goodbye", Text(layout, "text"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(layout, "text"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromRelativeSource()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <ContentView BindingContext="{{x:Static t:TestStrings.FarewellHolder}}">
                            <Label x:Name="text" Text="{{tlumach:Translate Unit={{Binding BindingContext.Unit, Source={{RelativeSource AncestorType={{x:Type VerticalStackLayout}}}}}}}}" />
                        </ContentView>
                        <Label x:Name="self" BindingContext="{{x:Static t:TestStrings.FarewellHolder}}" Text="{{tlumach:Translate Unit={{Binding BindingContext.Unit, Source={{RelativeSource Self}}}}}}" />
                    </VerticalStackLayout>
                    """);
                layout.BindingContext = new UnitHolder(TestStrings.Greeting);
                Ui.DoEvents();

                Assert.Equal("Hello", Text(layout, "text"));
                Assert.Equal("Goodbye", Text(layout, "self"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal("Hallo", Text(layout, "text"));
                Assert.Equal("Auf Wiedersehen", Text(layout, "self"));
            });
        }

        [Fact]
        public void ShouldResolveUnitFromExplicitSource()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="text" Text="{{tlumach:Translate Unit={{Binding Source={{x:Static t:TestStrings.Greeting}}}}}}" />
                        <Label x:Name="path" Text="{{tlumach:Translate Unit={{Binding Unit, Source={{x:Static t:TestStrings.FarewellHolder}}}}}}" />
                    </VerticalStackLayout>
                    """);
                Ui.DoEvents();

                Assert.Equal("Hello", Text(layout, "text"));
                Assert.Equal("Goodbye", Text(layout, "path"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal("Hallo", Text(layout, "text"));
                Assert.Equal("Auf Wiedersehen", Text(layout, "path"));
            });
        }

        [Fact]
        public void ShouldResolveUnitOfEachItemInDataTemplate()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <BindableLayout.ItemTemplate>
                            <DataTemplate>
                                <Label Text="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                            </DataTemplate>
                        </BindableLayout.ItemTemplate>
                    </VerticalStackLayout>
                    """);
                BindableLayout.SetItemsSource(layout, new[] { new UnitHolder(TestStrings.Greeting), new UnitHolder(TestStrings.Farewell) });
                Ui.DoEvents();

                Assert.Equal(["Hello", "Goodbye"], layout.Children.Cast<Label>().Select(label => label.Text));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal(["Hallo", "Auf Wiedersehen"], layout.Children.Cast<Label>().Select(label => label.Text));
            });
        }

        [Fact]
        public void ShouldResolveUnitInStyleSetter()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <VerticalStackLayout.Resources>
                            <Style x:Key="bound" TargetType="Label">
                                <Setter Property="Text" Value="{{tlumach:Translate Unit={{Binding Unit}}}}" />
                            </Style>
                            <Style x:Key="fixed" TargetType="Label">
                                <Setter Property="Text" Value="{{tlumach:Translate {{x:Static t:TestStrings.Greeting}}}}" />
                            </Style>
                        </VerticalStackLayout.Resources>
                        <Label x:Name="first" Style="{{StaticResource bound}}" BindingContext="{{x:Static t:TestStrings.FarewellHolder}}" />
                        <Label x:Name="second" Style="{{StaticResource bound}}" />
                        <Label x:Name="third" Style="{{StaticResource fixed}}" />
                    </VerticalStackLayout>
                    """);
                layout.BindingContext = new UnitHolder(TestStrings.Greeting);
                Ui.DoEvents();

                Assert.Equal("Goodbye", Text(layout, "first"));
                Assert.Equal("Hello", Text(layout, "second"));
                Assert.Equal("Hello", Text(layout, "third"));

                TestStrings.TranslationManager.CurrentCulture = TestStrings.German;
                Ui.DoEvents();
                Assert.Equal("Auf Wiedersehen", Text(layout, "first"));
                Assert.Equal("Hallo", Text(layout, "second"));
                Assert.Equal("Hallo", Text(layout, "third"));
            });
        }

        [Fact]
        public void ShouldShowEmptyTextForValueThatIsNotUnit()
        {
            Ui.Run(() =>
            {
                VerticalStackLayout layout = Ui.Load<VerticalStackLayout>("""
                    <VerticalStackLayout {0}>
                        <Label x:Name="notUnit" Text="{{tlumach:Translate Unit={{Binding Unit.Key}}}}" />
                        <Label x:Name="missing" Text="{{tlumach:Translate Unit={{Binding NoSuchProperty}}}}" />
                    </VerticalStackLayout>
                    """);
                layout.BindingContext = new UnitHolder(TestStrings.Greeting);
                Ui.DoEvents();

                Assert.Equal(string.Empty, Text(layout, "notUnit"));
                Assert.Equal(string.Empty, Text(layout, "missing"));
            });
        }

        private static string Text(Element root, string name) => root.FindByName<Label>(name).Text;
    }
}
