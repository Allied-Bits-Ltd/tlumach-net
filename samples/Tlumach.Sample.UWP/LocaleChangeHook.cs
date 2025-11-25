using System;
using System.Globalization;
using System.Linq;

using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Tlumach.Sample.UWP
{
    /// <summary>
    /// Watches UWP resource qualifiers for language changes and raises a .NET event.
    /// Intended for use from the UI thread.
    /// </summary>
    internal sealed partial class LocaleChangeHook : IDisposable
    {
        private readonly ResourceContext _context;
        private readonly IObservableMap<string, string> _qualifierValues;

        internal event EventHandler<SystemLocaleChangedEventArgs>? SystemLocaleChanged;

        public LocaleChangeHook()
        {
            // Default context for the current view.
            _context = ResourceContext.GetForCurrentView();
            _qualifierValues = _context.QualifierValues;

            // Will fire when qualifiers such as "Language" change.
            _qualifierValues.MapChanged += QualifierValues_MapChanged;
        }

        private async void QualifierValues_MapChanged(
            IObservableMap<string, string> sender,
            IMapChangedEventArgs<string> args)
        {
            // We only care about language changes.
            if (!string.Equals(args.Key, "Language", StringComparison.OrdinalIgnoreCase))
                return;

            // Marshal back to UI thread if needed.
            var dispatcher = Window.Current?.Dispatcher;
            if (dispatcher == null)
            {
                RaiseCultureChanged();
                return;
            }

            if (dispatcher.HasThreadAccess)
            {
                RaiseCultureChanged();
            }
            else
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, RaiseCultureChanged);
            }
        }

        private void RaiseCultureChanged()
        {
            // Runtime language list, first item is the primary runtime language. :contentReference[oaicite:1]{index=1}
            string? primaryTag = _context.Languages.FirstOrDefault()
                                 ?? ApplicationLanguages.Languages.FirstOrDefault();

            CultureInfo? newCulture = null;
            if (!string.IsNullOrEmpty(primaryTag))
            {
                try
                {
                    newCulture = new CultureInfo(primaryTag);
                }
                catch (CultureNotFoundException)
                {
                    // Fall back to invariant if the tag isn’t a valid .NET culture.
                    newCulture = CultureInfo.InvariantCulture;
                }
            }

            SystemLocaleChanged?.Invoke(
                this,
                new SystemLocaleChangedEventArgs(newCulture ?? CultureInfo.InvariantCulture));
        }

        public void Dispose()
        {
            _qualifierValues.MapChanged -= QualifierValues_MapChanged;
        }
    }

    // Minimal event args; you can reuse your existing one instead.
    internal sealed class SystemLocaleChangedEventArgs : EventArgs
    {
        public CultureInfo NewCulture { get; }

        public SystemLocaleChangedEventArgs(CultureInfo newCulture)
        {
            NewCulture = newCulture;
        }
    }
}
