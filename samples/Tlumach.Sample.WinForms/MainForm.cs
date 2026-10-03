// <copyright file="MainForm.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Tlumach.WinForms;

namespace Tlumach.Sample.WinForms
{
    /// <summary>
    /// The main form of the sample.
    /// <para>Most texts are assigned in the Designer: the translationProvider1 component adds the "TranslationKey on translationProvider1"
    /// and "ToolTipKey on translationProvider1" properties to the controls, menu items, and list view columns.
    /// Texts with placeholders are bound in code using the BindTranslation extension methods.</para>
    /// </summary>
    internal sealed partial class MainForm : Form
    {
        public MainForm()
        {
            // The texts assigned in the Designer are applied at the end of InitializeComponent,
            // because Program.Main has set TranslationProvider.DefaultTranslationManager.
            InitializeComponent();

            // Code-first bindings. The values of placeholders are cached in the units; the bindings update the labels when the language changes.
            // The bindings are disposed of automatically together with the labels.
            Strings.HelloName.CachePlaceholderValue("name", nameTextBox.Text);
            helloNameLabel.BindTranslation(Strings.HelloName);

            Strings.Copyright.CachePlaceholderValue("year", DateTime.Now.Year);
            copyrightLabel.BindTranslation(Strings.Copyright);

            FillLanguages();
        }

        private void FillLanguages()
        {
            // Neither ListCulturesInConfiguration nor ListTranslationFiles include the default translation, so it is added explicitly,
            // together with "system" (the culture of the operating system) and English (the language of the default translation).
            languageComboBox.Items.Add(new LanguageItem(CultureInfo.InvariantCulture));
            languageComboBox.Items.Add(new LanguageItem(null));
            languageComboBox.Items.Add(new LanguageItem(new CultureInfo("en")));

            foreach (string locale in Strings.TranslationManager.ListCulturesInConfiguration())
            {
                try
                {
                    languageComboBox.Items.Add(new LanguageItem(new CultureInfo(locale)));
                }
                catch (CultureNotFoundException)
                {
                    // A locale unknown to this system is skipped.
                }
            }

            // Selecting an item raises SelectedIndexChanged, which sets the culture and fills the list.
            languageComboBox.SelectedIndex = 0;
        }

        private void LanguageComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (languageComboBox.SelectedItem is LanguageItem item)
            {
                // The translation manager raises OnCultureChanged; translationProvider1 and the bindings update the controls.
                Strings.TranslationManager.CurrentCulture = item.Culture ?? CultureInfo.CurrentCulture;
                RefreshTranslationsList();
            }
        }

        private void NameTextBox_TextChanged(object? sender, EventArgs e)
        {
            Strings.HelloName.CachePlaceholderValue("name", nameTextBox.Text);

            // The binding of helloNameLabel listens to this notification and re-reads the text.
            Strings.HelloName.NotifyPlaceholdersUpdated();
        }

        private void RefreshTranslationsList()
        {
            // List view items are not components, so the form updates their texts itself.
            translationsListView.BeginUpdate();
            translationsListView.Items.Clear();
            translationsListView.Items.Add(new ListViewItem(new[] { nameof(Strings.Hello), Strings.Hello.CurrentValue }));
            translationsListView.Items.Add(new ListViewItem(new[] { nameof(Strings.Welcome), Strings.Welcome.CurrentValue }));
            translationsListView.EndUpdate();
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e) => Close();

        // An item of the language selection box.
        private sealed class LanguageItem
        {
            public LanguageItem(CultureInfo? culture) => Culture = culture;

            public CultureInfo? Culture { get; }

            public override string ToString()
            {
                if (Culture is null)
                    return "(system)";

                if (Culture.Equals(CultureInfo.InvariantCulture))
                    return "(default)";

                return $"{Culture.EnglishName} ({Culture.NativeName})";
            }
        }
    }
}
