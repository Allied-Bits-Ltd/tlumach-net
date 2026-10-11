// <copyright file="TranslateExtension.cs" company="Allied Bits Ltd.">
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
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

using Tlumach;
using Tlumach.Base;

namespace Tlumach.WPF
{
    /// <summary>
    /// A XAML markup extension that binds a dependency property to the text of a translation unit and updates the property when the culture of the unit's translation manager changes or when <see cref="BaseTranslationUnit.NotifyPlaceholdersUpdated"/> is called.
    /// <para>Usage: <c>Text="{tlumach:Translate {x:Static translations:Strings.Hello}}"</c>, or, with a unit that comes from a binding, <c>Text="{tlumach:Translate {Binding MyTranslationUnit}}"</c>
    /// or <c>Text="{tlumach:Translate Unit={Binding MyTranslationUnit}}"</c>.</para>
    /// <para>The extension does not keep the target alive: the translation manager and the units, which the generated classes keep in static properties, reference it weakly.</para>
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    [ContentProperty(nameof(Unit))]
    [XamlSetMarkupExtension(nameof(ReceiveMarkupExtension))]
    public sealed class TranslateExtension : MarkupExtension
    {
        private readonly Dispatcher _dispatcher;
        private XamlTranslateCore? _core;
        private BindingListener? _listener;

        /// <summary>
        /// Gets or sets the source of the text: either a <see cref="TranslationUnit"/> or a binding (<see cref="BindingBase"/>) whose value is a <see cref="TranslationUnit"/>.
        /// <para>A <see cref="Binding"/> is resolved for the target element, so it can take the unit from the <c>DataContext</c>, from an element (<c>ElementName</c>), from a relative source,
        /// or from an explicit source, also in data templates and style setters. A <see cref="MultiBinding"/> or a <see cref="PriorityBinding"/> is resolved outside the element tree,
        /// so only the bindings with an explicit <c>Source</c> in it work.</para>
        /// <para>A value of any other type is treated as no unit, and the target property receives an empty string.</para>
        /// </summary>
        public object? Unit { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateExtension"/> class. Set the <see cref="Unit"/> property before the extension provides a value.
        /// </summary>
        public TranslateExtension()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateExtension"/> class with the given unit.
        /// </summary>
        /// <param name="unit">A <see cref="TranslationUnit"/> or a binding (<see cref="BindingBase"/>) whose value is a <see cref="TranslationUnit"/>.</param>
        public TranslateExtension(object unit)
            : this()
        {
            Unit = unit;
        }

        /// <summary>
        /// Creates a one-way binding of the target property to the current text of <see cref="Unit"/> and returns the value that this binding provides for the target.
        /// </summary>
        /// <param name="serviceProvider">The service provider of the XAML parser.</param>
        /// <returns>The result of <see cref="BindingBase.ProvideValue(IServiceProvider)"/> for the created binding.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // A unit that comes from a binding: the binding becomes a part of a multi-binding of the target, so WPF resolves it for the target element
            // (and for each element created from a template or styled by a style setter, as the multi-binding is shared by them)
            if (Unit is Binding unitBinding)
            {
                XamlBoundUnitTracker tracker = new(PostToUi);
                MultiBinding multiBinding = new()
                {
                    Mode = BindingMode.OneWay,
                    Converter = new BoundUnitTextConverter(tracker),
                };
                multiBinding.Bindings.Add(unitBinding);

                // The second binding makes WPF convert the unit again when the culture or the placeholders change
                multiBinding.Bindings.Add(new Binding(nameof(XamlBoundUnitTracker.Version)) { Source = tracker, Mode = BindingMode.OneWay });
                return multiBinding.ProvideValue(serviceProvider);
            }

            // 1) Resolve Unit (static or, for other kinds of bindings, bound outside the element tree)
            _core ??= new XamlTranslateCore(PostToUi);
            if (Unit is BindingBase otherBinding)
            {
                _listener ??= new BindingListener();
                _listener.Changed -= OnUnitChanged;
                _listener.Changed += OnUnitChanged;
                _listener.Attach(otherBinding);

                _core.Unit = _listener.Value as TranslationUnit;
            }
            else
            {
                _core.Unit = Unit as TranslationUnit;
            }

            // 2) Create binding from target property to _core.Value. The binding keeps _core alive as long as the target.
            var binding = new Binding(nameof(XamlTranslateCore.Value))
            {
                Source = _core,
                Mode = BindingMode.OneWay,
            };

            // 3) RETURN the binding’s ProvideValue result, NOT the Binding itself
            return binding.ProvideValue(serviceProvider);
        }

        /// <summary>
        /// Lets XAML assign a binding to <see cref="Unit"/> (<c>Unit="{Binding ...}"</c>), which WPF otherwise allows only for dependency properties.
        /// </summary>
        private static void ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
        {
            if (targetObject is TranslateExtension extension
                && eventArgs.MarkupExtension is BindingBase binding
                && string.Equals(eventArgs.Member?.Name, nameof(Unit), StringComparison.Ordinal))
            {
                extension.Unit = binding;
                eventArgs.Handled = true;
            }
        }

        private void PostToUi(Action action)
        {
            if (_dispatcher.CheckAccess())
                action();
            else
#pragma warning disable MA0134 // Observe result of async code
                _dispatcher.BeginInvoke(action);
#pragma warning restore MA0134 // Observe result of async code
        }

        private void OnUnitChanged(object? value)
            => _core!.Unit = value as TranslationUnit;
    }

    // Local listener to host the incoming Unit binding when it is not a plain Binding
    internal sealed class BindingListener : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(BindingListener),
                new PropertyMetadata(defaultValue: null, OnChanged));

        public object? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

#pragma warning disable MA0046 // The delegate must have 2 parameters
        public event Action<object?>? Changed;
#pragma warning restore MA0046 // The delegate must have 2 parameters

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((BindingListener)d).Changed?.Invoke(e.NewValue);

        /// <summary>
        /// Attaches a binding to this listener’s Value property.
        /// </summary>
        public void Attach(BindingBase binding)
        {
            BindingOperations.SetBinding(this, ValueProperty, binding);
        }
    }
}
