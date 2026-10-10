// <copyright file="Translate.cs" company="Allied Bits Ltd.">
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

using System.Runtime.CompilerServices;

using Tlumach;

namespace Tlumach.MAUI
{
    using System;

    using Microsoft.Maui.Controls;

    /// <summary>
    /// A XAML markup extension that binds a bindable property to the text of a translation unit and updates the property when the culture of the unit's translation manager changes or when <see cref="BaseTranslationUnit.NotifyPlaceholdersUpdated"/> is called.
    /// <para>Usage: <c>Text="{tlumach:Translate {x:Static translations:Strings.Hello}}"</c> or <c>Text="{tlumach:Translate Unit={Binding MyTranslationUnit}}"</c>.</para>
    /// </summary>
    [ContentProperty(nameof(Unit))]
    [AcceptEmptyServiceProvider]
    public sealed class Translate : BindableObject, IMarkupExtension<BindingBase>
    {
        private readonly XamlTranslateCore _core =
            new XamlTranslateCore(a => MainThread.BeginInvokeOnMainThread(a));

        // Bindable Unit so XAML can do: Unit="{Binding MyTranslationUnit}"

        /// <summary>
        /// Identifies the <see cref="Unit"/> bindable property.
        /// </summary>
        public static readonly BindableProperty UnitProperty =
            BindableProperty.Create(
                nameof(Unit),
                typeof(TranslationUnit),
                typeof(Translate),
                defaultValue: null,
                propertyChanged: OnUnitChanged);

        /// <summary>
        /// Gets or sets the translation unit whose text the target property receives. This is a bindable property, so it can be set with a binding.
        /// <para>When the value is <see langword="null"/>, the target property receives an empty string.</para>
        /// </summary>
        public TranslationUnit? Unit
        {
            get => (TranslationUnit?)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        private static void OnUnitChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var ext = (Translate)bindable;
            ext._core.Unit = (TranslationUnit?)newValue;
        }

        /*public BindingBase ProvideValue(IServiceProvider serviceProvider)
            => new Binding(nameof(XamlTranslateCore.Value), source: _core, mode: BindingMode.OneWay);*/

        /// <summary>
        /// Creates a one-way binding of the target property to the current text of <see cref="Unit"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider of the XAML parser. It is not used.</param>
        /// <returns>The binding to apply to the target property.</returns>
        public BindingBase ProvideValue(IServiceProvider serviceProvider)
            => BindingBase.Create<XamlTranslateCore, string>(
                   static core => core.Value,
                   mode: BindingMode.OneWay,
                   source: _core);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);
    }
}
