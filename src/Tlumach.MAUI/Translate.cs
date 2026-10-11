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

using System;
using System.Globalization;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

using Tlumach;

namespace Tlumach.MAUI
{
    /// <summary>
    /// A XAML markup extension that binds a bindable property to the text of a translation unit and updates the property when the culture of the unit's translation manager changes or when <see cref="BaseTranslationUnit.NotifyPlaceholdersUpdated"/> is called.
    /// <para>Usage: <c>Text="{tlumach:Translate {x:Static translations:Strings.Hello}}"</c>, or, with a unit that comes from a binding, <c>Text="{tlumach:Translate {Binding MyTranslationUnit}}"</c>
    /// or <c>Text="{tlumach:Translate Unit={Binding MyTranslationUnit}}"</c>.</para>
    /// <para>The extension does not keep the target alive: the translation manager and the units, which the generated classes keep in static properties, reference it weakly.</para>
    /// </summary>
    [ContentProperty(nameof(Unit))]
    [AcceptEmptyServiceProvider]
    public sealed class Translate : IMarkupExtension<BindingBase>
    {
        private readonly Action<Action> _postToUi = CreatePostToUi();
        private XamlTranslateCore? _core;

        /// <summary>
        /// Gets or sets the source of the text: either a <see cref="TranslationUnit"/> or a binding (<see cref="BindingBase"/>) whose value is a <see cref="TranslationUnit"/>.
        /// <para>A binding is resolved for the target element, so it can take the unit from the <c>BindingContext</c>, from a relative source, from a referenced element,
        /// or from an explicit source, also in data templates and style setters.</para>
        /// <para>A value of any other type is treated as no unit, and the target property receives an empty string.</para>
        /// </summary>
        public object? Unit { get; set; }

        /// <summary>
        /// Creates a one-way binding of the target property to the current text of <see cref="Unit"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider of the XAML parser. It is not used.</param>
        /// <returns>The binding to apply to the target property.</returns>
        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            // A unit that comes from a binding: the binding becomes a part of a multi-binding of the target, so MAUI resolves it for the target element
            // (and for each element styled by a style setter, as MAUI applies a copy of the multi-binding to each of them)
            if (Unit is BindingBase unitBinding)
            {
                XamlBoundUnitTracker tracker = new(_postToUi);
                MultiBinding multiBinding = new()
                {
                    Mode = BindingMode.OneWay,
                    Converter = new BoundUnitTextConverter(tracker),
                };
                multiBinding.Bindings.Add(unitBinding);

                // The second binding makes MAUI convert the unit again when the culture or the placeholders change
                multiBinding.Bindings.Add(BindingBase.Create<XamlBoundUnitTracker, int>(static tracker => tracker.Version, mode: BindingMode.OneWay, source: tracker));
                return multiBinding;
            }

            // The binding keeps _core alive as long as the target
            _core ??= new XamlTranslateCore(_postToUi);
            _core.Unit = Unit as TranslationUnit;
            return BindingBase.Create<XamlTranslateCore, string>(
                   static core => core.Value,
                   mode: BindingMode.OneWay,
                   source: _core);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
            => ProvideValue(serviceProvider);

        // The dispatcher of the thread that loads the XAML (the UI thread); Dispatcher.GetForCurrentThread() would not work in the thread that changes the culture
        private static Action<Action> CreatePostToUi()
        {
            IDispatcher? dispatcher = Dispatcher.GetForCurrentThread() ?? Application.Current?.Dispatcher;
            if (dispatcher is null)
                return action => MainThread.BeginInvokeOnMainThread(action);

            return action =>
            {
                if (dispatcher.IsDispatchRequired)
                    dispatcher.Dispatch(action);
                else
                    action();
            };
        }
    }
}
