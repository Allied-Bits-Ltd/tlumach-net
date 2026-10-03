// <copyright file="TranslationBindingTests.cs" company="Allied Bits Ltd.">
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
using System.Threading;
using System.Windows.Forms;

using Tlumach.WinForms;

#pragma warning disable CA1303 // The tests set literal texts on controls on purpose; they are test data, not user-facing strings that need localization

namespace Tlumach.WinFormsTests
{
    [Trait("Category", "WinForms")]
    public class TranslationBindingTests
    {
        private static readonly CultureInfo German = new("de");

        [Fact]
        public void ShouldApplyTextImmediatelyAndWhenCultureChanges()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using Label label = new();

            using TranslationBinding binding = label.BindTranslation(unit);
            Assert.Equal("Hello", label.Text);
            Assert.Same(unit, binding.Unit);

            fixture.Manager.CurrentCulture = German;
            Assert.Equal("Hallo", label.Text);
        }

        [Fact]
        public void ShouldBindToolStripItemsColumnHeadersAndToolTips()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit greeting = fixture.CreateUnit("greeting");
            using TranslationUnit hint = fixture.CreateUnit("hint");
            using ToolStripMenuItem menuItem = new();
            using ColumnHeader column = new();
            using ToolTip toolTip = new();
            using Button button = new();

            using TranslationBinding menuBinding = menuItem.BindTranslation(greeting);
            using TranslationBinding columnBinding = column.BindTranslation(greeting);
            using TranslationBinding toolTipBinding = toolTip.BindTranslation(button, hint);
            fixture.Manager.CurrentCulture = German;

            Assert.Equal("Hallo", menuItem.Text);
            Assert.Equal("Hallo", column.Text);
            Assert.Equal("Hier klicken", toolTip.GetToolTip(button));
        }

        [Fact]
        public void ShouldBindAnyPropertyUsingASetter()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("hint");
            using Button button = new();

            using TranslationBinding binding = button.BindTranslation(unit, static (b, text) => b.AccessibleName = text);

            Assert.Equal("Click here", button.AccessibleName);
        }

        [Fact]
        public void ShouldBindDataGridViewColumnHeaderText()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using DataGridViewTextBoxColumn column = new();

            // DataGridViewColumn implements IComponent but is not a Component.
            using TranslationBinding binding = column.BindTranslation(unit, static (c, text) => c.HeaderText = text);
            Assert.Equal("Hello", column.HeaderText);

            fixture.Manager.CurrentCulture = German;
            Assert.Equal("Hallo", column.HeaderText);
        }

        [Fact]
        public void ShouldNotKeepTheBindingSubscribedWhenTheInitialApplyThrows()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            int baseline = ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager);
            int calls = 0;
            using Label label = new();

            Assert.Throws<InvalidOperationException>(() => label.BindTranslation(unit, (l, text) =>
            {
                calls++;
                throw new InvalidOperationException("apply failed");
            }));
            fixture.Manager.CurrentCulture = German;

            Assert.Equal(1, calls);
            Assert.Equal(baseline, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));
        }

        [Fact]
        public void ShouldUpdateWhenPlaceholdersAreUpdated()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("named", containsPlaceholders: true);
            using Label label = new();
            unit.CachePlaceholderValue("name", "Ann");

            using TranslationBinding binding = label.BindTranslation(unit);
            Assert.Equal("Hello, Ann", label.Text);

            unit.CachePlaceholderValue("name", "Bob");
            unit.NotifyPlaceholdersUpdated();
            Assert.Equal("Hello, Bob", label.Text);
        }

        [Fact]
        public void ShouldStopUpdatingAfterDispose()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using Label label = new();
            int baseline = ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager);
            TranslationBinding binding = label.BindTranslation(unit);
            Assert.Equal(baseline + 1, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));

            binding.Dispose();
            binding.Dispose();
            Assert.Equal(baseline, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));
            fixture.Manager.CurrentCulture = German;

            Assert.True(binding.IsDisposed);
            Assert.Equal("Hello", label.Text);
        }

        [Fact]
        public void ShouldDisposeTogetherWithTheTarget()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            int baseline = ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager);
            Label label = new();
            TranslationBinding binding = label.BindTranslation(unit);
            Assert.Equal(baseline + 1, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));

            label.Dispose();

            Assert.True(binding.IsDisposed);
            Assert.Equal(baseline, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));
        }

        [Fact]
        public void ShouldMarshalCultureChangesFromOtherThreadsToTheUiThread()
        {
            SynchronizationContext? original = SynchronizationContext.Current;
            QueueSynchronizationContext uiContext = new();
            SynchronizationContext.SetSynchronizationContext(uiContext);
            try
            {
                using WinFormsFixture fixture = new();
                using TranslationUnit unit = fixture.CreateUnit("greeting");
                using Label label = new();
                using TranslationBinding binding = label.BindTranslation(unit);

                Thread worker = new(() => fixture.Manager.CurrentCulture = German);
                worker.Start();
                worker.Join();

                Assert.Equal("Hello", label.Text);

                uiContext.RunPending();

                Assert.Equal("Hallo", label.Text);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void ShouldCaptureTheUiContextWhenItAppearsAfterTheBindingWasCreated()
        {
            SynchronizationContext? original = SynchronizationContext.Current;
            QueueSynchronizationContext uiContext = new();
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                using WinFormsFixture fixture = new();
                using TranslationUnit unit = fixture.CreateUnit("greeting");

                // ToolStripMenuItem is not a Control, so creating it does not install a WindowsFormsSynchronizationContext.
                using ToolStripMenuItem menuItem = new();
                using TranslationBinding binding = menuItem.BindTranslation(unit);

                SynchronizationContext.SetSynchronizationContext(uiContext);
                binding.Update();

                Thread worker = new(() => fixture.Manager.CurrentCulture = German);
                worker.Start();
                worker.Join();

                Assert.Equal("Hello", menuItem.Text);
                Assert.Equal(1, uiContext.PendingCount);

                uiContext.RunPending();

                Assert.Equal("Hallo", menuItem.Text);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void ShouldValidateArguments()
        {
            using WinFormsFixture fixture = new();
            using TranslationUnit unit = fixture.CreateUnit("greeting");
            using Label label = new();

            Assert.Throws<ArgumentNullException>(() => ((Label)null!).BindTranslation(unit));
            Assert.Throws<ArgumentNullException>(() => label.BindTranslation(null!));
            Assert.Throws<ArgumentNullException>(() => label.BindTranslation<Label>(unit, null!));
            Assert.Throws<ArgumentNullException>(() => ((ToolTip)null!).BindTranslation(label, unit));
        }
    }
}
