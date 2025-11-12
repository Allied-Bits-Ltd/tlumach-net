// UWP namespaces
using System;

using Tlumach;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Tlumach.UWP
{
    public static class Translate
    {
        // Bindable attached property: Unit
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.RegisterAttached(
                "Unit",
                typeof(TranslationUnit),
                typeof(Translate),
                new PropertyMetadata(null, OnUnitChanged));

        public static void SetUnit(DependencyObject obj, TranslationUnit value)
        {
            if (obj is not null) obj.SetValue(UnitProperty, value);
        }

        public static TranslationUnit? GetUnit(DependencyObject obj) =>
            obj?.GetValue(UnitProperty) as TranslationUnit;

        // Per-target TranslateCore storage
        private static readonly DependencyProperty CoreProperty =
            DependencyProperty.RegisterAttached(
                "Core",
                typeof(XamlTranslateCore),
                typeof(Translate),
                new PropertyMetadata(null));

        private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Require FrameworkElement for Dispatcher + binding
            var fe = d as FrameworkElement;
            if (fe == null)
                return;

            // Create (or get) a TranslateCore bound to the target UI thread
            var core = (XamlTranslateCore)fe.GetValue(CoreProperty);
            if (core == null)
            {
                CoreDispatcher dispatcher = fe.Dispatcher;
                core = new XamlTranslateCore(a => _ = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => a()));
                fe.SetValue(CoreProperty, core);

                // Wire up a one-way binding from core.Value to a sensible target prop
                if (d is TextBlock tb)
                {
                    tb.SetBinding(TextBlock.TextProperty, new Binding
                    {
                        Source = core,
                        Path = new PropertyPath(nameof(XamlTranslateCore.Value)),
                        Mode = BindingMode.OneWay,
                    });
                }
                else
                if (d is ContentControl cc)
                {
                    cc.SetBinding(ContentControl.ContentProperty, new Binding
                    {
                        Source = core,
                        Path = new PropertyPath(nameof(XamlTranslateCore.Value)),
                        Mode = BindingMode.OneWay,
                    });
                }

                // Add other target types as needed (e.g., HeaderedContentControl.HeaderProperty, etc.)
            }

            // Push the new Unit into the core (this also refreshes Value)
            core.Unit = e.NewValue as TranslationUnit;
        }
    }
}
