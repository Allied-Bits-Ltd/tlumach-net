using Windows.UI.Xaml.Controls;

using Tlumach.UWP;
using System.Globalization;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;

namespace Tlumach.Sample.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// This class is used to hold locale information for the language selection dropbox.
        /// </summary>
        private sealed class LanguageItem
        {
            public CultureInfo? Culture { get; }

            public override string ToString()
            {
                if (Culture is null)
                    return $"(system)";
                else
                if (Culture == CultureInfo.InvariantCulture)
                    return "(default)";
                else
                    return $"{Culture.EnglishName} ({Culture.NativeName})";
            }

            public LanguageItem(CultureInfo? culture)
            {
                Culture = culture;
            }
        }

        private LocaleChangeHook _localeChangeHook;

        public MainPage()
        {
            // We set the event handler early because when InitializeComponent() initializes the UI, it requests the value, so we need to have the handler ready by that time.
            Strings.CultureInfo.OnPlaceholderValueNeeded += (sender, args) =>
            {
                if (args.Name.Equals("currentCulture", StringComparison.Ordinal))
                {
                    // we use the invariant culture to tell Tlumach that it should use a default translation file.
                    if (Strings.TranslationManager.CurrentCulture == CultureInfo.InvariantCulture)
                        args.Value = "(default)";
                    else
                        args.Value = Strings.TranslationManager.CurrentCulture.Name;
                }
            };

            InitializeComponent();

            // Populate the Languages dropbox.
            // As we explicitly specified all languages in the configuration, we call ListCulturesInConfiguration.
            // The alternative method is shown below.

            IList<string> culturesInConfig = Strings.TranslationManager.ListCulturesInConfiguration();

            // This is how you can enumerate the files in resources or on the disk and obtain the cultures when the config file does not specify translations explicitly.
            // This approach is useful for files on the disk, when you want to let users add translations for new languages by putting these translations to some disk directory.
            IList<string> filesInResources = Strings.TranslationManager.ListTranslationFiles(typeof(Strings).Assembly, Strings.TranslationManager.DefaultConfiguration?.DefaultFile ?? "strings.arb");
            IList<CultureInfo> culturesInResources = TranslationManager.ListCultures(filesInResources);

            LanguageSelector.Items.Clear();

            // Neither of the above methods include the "default" translation to the list. It is expected that you know what your default translation is.
            ComboBoxItem item = new ComboBoxItem();
            item.Content = new LanguageItem(CultureInfo.InvariantCulture);
            LanguageSelector.Items.Add(item);

            // This adds the "System" item to the dropdown.
            item = new ComboBoxItem();
            item.Content = new LanguageItem(null);
            LanguageSelector.Items.Add(item);

            // This item has no translation so it is added explicitly. This is because we _know_ that our default translation is English, but we want to have "English" listed explicitly too.
            item = new ComboBoxItem();
            item.Content = new LanguageItem(new CultureInfo("en"));
            LanguageSelector.Items.Add(item);

            // Add the names of locales to the dropdown
            foreach (var locale in culturesInConfig)
            {
#pragma warning disable CS0168 // Variable is declared but never used
                try
                {
                    item = new ComboBoxItem();
                    item.Content = new LanguageItem(new CultureInfo(locale));
                    LanguageSelector.Items.Add(item);
                }
                catch (CultureNotFoundException ex)
                {
                    // locale not found, and so be it
                }
#pragma warning restore CS0168 // Variable is declared but never used
            }

            LanguageSelector.SelectedIndex = 0; // default locale

            // Update the control that contains templated text - such controls cannot be bound for dynamic updates
            UpdateControlledUnits(CultureInfo.InvariantCulture);

            // We track the change of a culture to update controls which are updated from code rather than bound for dynamic updates
            Strings.TranslationManager.OnCultureChanged += (sender, e) =>
            {
                UpdateControlledUnits(e.Culture);
                Strings.CultureInfo.NotifyPlaceholdersUpdated();
            };

            _localeChangeHook = new LocaleChangeHook();

            // Register ourselves for system locale change
            _localeChangeHook.SystemLocaleChanged += (sender, e) => UpdateUI();
        }

        private void UpdateUI()
        {
            // The user has changed regional settings / locale
            CultureInfo.CurrentCulture.ClearCachedData();
            CultureInfo.CurrentUICulture.ClearCachedData();

            // As the system culture has been updated, we need to update some translation units
            UpdateControlledUnits(CultureInfo.CurrentCulture);

            // Notifies the translation manager about the change of current locale / culture.
            // If TranslationManager decides that the texts need to change, it will fire the event, to which translation units listen and react.
            Strings.TranslationManager.SystemCultureUpdated();
        }

#pragma warning disable S1172
#pragma warning disable RCS1163 // Unused parameter
        private void UpdateControlledUnits(CultureInfo culture)
#pragma warning restore RCS1163 // Unused parameter
#pragma warning disable S1172
        {
            // This is the illustration of updating the binding made to the templated unit.

            // The calls to ForgetPlaceholderValue are optional here - the CachePlaceholderValue will replace the already cached one
            Strings.CultureInfo.ForgetPlaceholderValue("systemCulture");
            Strings.CultureInfo.CachePlaceholderValue("systemCulture", CultureInfo.CurrentCulture.Name);

            // We set the systemCulture placeholder via the cache and use the event to provide currentCulture value.
            Strings.CultureInfo.NotifyPlaceholdersUpdated();

            // This is an illustration of using the templated item.
            // The copyright text itself is not translated in our example (although it could be), yet the placeholder is passed.

#pragma warning disable MA0011
#pragma warning disable CA1304
            // Any of the below variants will work
            CopyrightLabel.Text = Strings.Copyright.GetValue(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { { "Year", DateTime.Now.Year } });
            CopyrightLabel.Text = Strings.Copyright.GetValue(new OrderedDictionary { { "Year", DateTime.Now.Year } });
            // This is a list of objects - the objects themselves are matched against the placeholders by index (not by name!)
            CopyrightLabel.Text = Strings.Copyright.GetValue(new object[] { DateTime.Now.Year });
            // This variant passes an object which has properties with the names that match template placeholders
            CopyrightLabel.Text = Strings.Copyright.GetValue(new { Year = DateTime.Now.Year, });
#pragma warning restore CA1304
#pragma warning restore MA0011
        }

        // Handles the change of the language by the user in the UI
        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageItem? selected = (LanguageSelector.SelectedItem as ComboBoxItem)?.Content as LanguageItem;
            if (selected is not null)
                Strings.TranslationManager.CurrentCulture = selected.Culture ?? CultureInfo.CurrentCulture;
        }
    }
}
