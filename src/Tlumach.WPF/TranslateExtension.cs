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
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

using Tlumach;
using Tlumach.Base;

namespace Tlumach.WPF
{
    [MarkupExtensionReturnType(typeof(Binding))]
    [ContentProperty(nameof(Unit))]
    public sealed class TranslateExtension : MarkupExtension
    {
        private readonly XamlTranslateCore _core;
        private readonly Dispatcher _dispatcher;

        // Accept either a TranslationUnit or a BindingBase
        public object? Unit { get; set; }

        public TranslateExtension()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _core = new XamlTranslateCore(a =>
            {
                if (_dispatcher.CheckAccess())
                    a();
                else
#pragma warning disable MA0134 // Observe result of async code
                    _dispatcher.BeginInvoke(a);
#pragma warning restore MA0134 // Observe result of async code
            });
        }

        public TranslateExtension(object unit)
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _core = new XamlTranslateCore(a =>
            {
                if (_dispatcher.CheckAccess())
                    a();
                else
#pragma warning disable MA0134 // Observe result of async code
                    _dispatcher.BeginInvoke(a);
#pragma warning restore MA0134 // Observe result of async code
            });
            Unit = unit;
        }

        private BindingListener? _listener;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // 1) Resolve Unit (static or bound)
            if (Unit is BindingBase unitBinding)
            {
                _listener ??= new BindingListener();
                _listener.Changed -= OnUnitChanged;
                _listener.Changed += OnUnitChanged;
                _listener.Attach(unitBinding);

                _core.Unit = _listener.Value as TranslationUnit;
            }
            else
            {
                _core.Unit = Unit as TranslationUnit;
            }

            // 2) Create binding from target property to _core.Value
            var binding = new Binding(nameof(XamlTranslateCore.Value))
            {
                Source = _core,
                Mode = BindingMode.OneWay,
            };

            // 3) RETURN the binding’s ProvideValue result, NOT the Binding itself
            return binding.ProvideValue(serviceProvider);
        }

        private void OnUnitChanged(object? value)
            => _core.Unit = value as TranslationUnit;
    }

    // Local listener to host the incoming Unit binding
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
