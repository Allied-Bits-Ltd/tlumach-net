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

namespace Tlumach.MAUI
{
    [ContentProperty(nameof(Unit))]
    [AcceptEmptyServiceProvider]
    public sealed class Translate : BindableObject, IMarkupExtension<BindingBase>
    {
        public static readonly BindableProperty UnitProperty =
            BindableProperty.Create(nameof(Unit), typeof(TranslationUnit), typeof(Translate), null, propertyChanged: OnUnitChanged);

        public TranslationUnit Unit
        {
            get => (TranslationUnit)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(string), typeof(Translate), string.Empty);

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // Constructor
        public Translate()
        {
            // Subscribe to the external service culture change event.
            // This subscription should be here to ensure it happens for every instance.
            TranslationManager.OnCultureChanged += TranslationProvider_OnCultureChanged;
        }

        // The callback for when the Unit property changes
        private static void OnUnitChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is Translate extension && newValue is TranslationUnit newUnit)
            {
                // Initialize the Value property with the current translated string.
                extension.Value = newUnit.CurrentValue;
            }
        }

        BindingBase IMarkupExtension<BindingBase>.ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(nameof(Value), source: this);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding(nameof(Value), source: this);
        }


        // Event handler for the external service
        private void TranslationProvider_OnCultureChanged(object? sender, CultureChangedEventArgs args)
        {
            // Update the Value property on the UI thread.
            // This is the key to triggering the binding update.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Unit != null)
                    Value = Unit.CurrentValue;
            });
        }

    }
}
