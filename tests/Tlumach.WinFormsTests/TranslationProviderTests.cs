// <copyright file="TranslationProviderTests.cs" company="Allied Bits Ltd.">
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
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

using Tlumach.WinForms;

#pragma warning disable CA1303 // The tests set literal texts on controls on purpose; they are test data, not user-facing strings that need localization

namespace Tlumach.WinFormsTests
{
    [Trait("Category", "WinForms")]
    public class TranslationProviderTests
    {
        private static readonly CultureInfo German = new("de");

        [Fact]
        public void ShouldApplyTranslationKeyToControlText()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetTranslationKey(label, "greeting");

            Assert.Equal("Hello", label.Text);
            Assert.Equal("greeting", provider.GetTranslationKey(label));
        }

        [Fact]
        public void ShouldApplyTranslationKeyToFormsToolStripItemsAndColumnHeaders()
        {
            using WinFormsFixture fixture = new();
            using Form form = new() { Text = "designed" };
            using ToolStripMenuItem menuItem = new() { Text = "designed" };
            using ColumnHeader column = new() { Text = "designed" };
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetTranslationKey(form, "greeting");
            provider.SetTranslationKey(menuItem, "farewell");
            provider.SetTranslationKey(column, "hint");

            Assert.Equal("Hello", form.Text);
            Assert.Equal("Goodbye", menuItem.Text);
            Assert.Equal("Click here", column.Text);
        }

        [Fact]
        public void ShouldApplyToolTipKeys()
        {
            using WinFormsFixture fixture = new();
            using ToolTip toolTip = new();
            using Button button = new();
            using ToolStripMenuItem menuItem = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager, ToolTip = toolTip };

            provider.SetToolTipKey(button, "hint");
            provider.SetToolTipKey(menuItem, "hint");

            Assert.Equal("Click here", toolTip.GetToolTip(button));
            Assert.Equal("Click here", menuItem.ToolTipText);
            Assert.Equal("hint", provider.GetToolTipKey(button));
        }

        [Fact]
        public void ShouldKeepDesignedTextWhenKeyIsMissing()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetTranslationKey(label, "no.such.key");

            Assert.Equal("designed", label.Text);
        }

        [Fact]
        public void ShouldForgetKeyWhenEmptyKeyIsAssigned()
        {
            using WinFormsFixture fixture = new();
            using Label label = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            provider.SetTranslationKey(label, "greeting");

            provider.SetTranslationKey(label, string.Empty);
            label.Text = "manual";
            fixture.Manager.CurrentCulture = German;

            Assert.Equal(string.Empty, provider.GetTranslationKey(label));
            Assert.Equal("manual", label.Text);
        }

        [Fact]
        public void ShouldDeferApplyingUntilEndInit()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new();

            provider.BeginInit();
            provider.TranslationManager = fixture.Manager;
            provider.SetTranslationKey(label, "greeting");
            Assert.Equal("designed", label.Text);

            provider.EndInit();
            Assert.Equal("Hello", label.Text);
        }

        [Fact]
        public void ShouldUseDefaultTranslationManagerWhenNoneIsAssigned()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            TranslationProvider.DefaultTranslationManager = fixture.Manager;
            try
            {
                using TranslationProvider provider = new();
                provider.BeginInit();
                provider.SetTranslationKey(label, "greeting");
                provider.EndInit();

                Assert.Equal("Hello", label.Text);
            }
            finally
            {
                TranslationProvider.DefaultTranslationManager = null;
            }
        }

        [Fact]
        public void ShouldNotTranslateInDesignMode()
        {
            using WinFormsFixture fixture = new();
            using Label label = new() { Text = "designed" };
            using TranslationProvider provider = new();
            provider.Site = new DesignModeSite(provider);
            provider.TranslationManager = fixture.Manager;

            provider.SetTranslationKey(label, "greeting");
            fixture.Manager.CurrentCulture = German;

            Assert.Equal("designed", label.Text);
            Assert.Equal("greeting", provider.GetTranslationKey(label));

            // A provider that had subscribed to the manager while in design mode would apply the translation when the culture changes now.
            provider.Site = null;
            fixture.Manager.CurrentCulture = CultureInfo.InvariantCulture;

            Assert.Equal("designed", label.Text);
        }

        [Fact]
        public void ShouldReapplyTranslationsWhenCultureChanges()
        {
            using WinFormsFixture fixture = new();
            using Label label = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            provider.SetTranslationKey(label, "greeting");

            fixture.Manager.CurrentCulture = German;

            Assert.Equal("Hallo", label.Text);
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
                using Label label = new();
                using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
                provider.SetTranslationKey(label, "greeting");

                Thread worker = new(() => fixture.Manager.CurrentCulture = German);
                worker.Start();
                worker.Join();

                Assert.Equal("Hello", label.Text);
                Assert.Equal(1, uiContext.PendingCount);

                uiContext.RunPending();

                Assert.Equal("Hallo", label.Text);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void ShouldCaptureTheUiContextWhenItAppearsAfterTheProviderWasCreated()
        {
            SynchronizationContext? original = SynchronizationContext.Current;
            QueueSynchronizationContext uiContext = new();
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                using WinFormsFixture fixture = new();
                using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

                // Creating a control with no current context would install a WindowsFormsSynchronizationContext, so the queue context is set before that.
                SynchronizationContext.SetSynchronizationContext(uiContext);
                using Label label = new();
                provider.SetTranslationKey(label, "greeting");

                Thread worker = new(() => fixture.Manager.CurrentCulture = German);
                worker.Start();
                worker.Join();

                Assert.Equal("Hello", label.Text);
                Assert.Equal(1, uiContext.PendingCount);

                uiContext.RunPending();

                Assert.Equal("Hallo", label.Text);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [Fact]
        public void ShouldStopUpdatingAfterDispose()
        {
            using WinFormsFixture fixture = new();
            using Label label = new();
            int baseline = ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager);
            TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            provider.SetTranslationKey(label, "greeting");
            Assert.Equal(baseline + 1, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));

            provider.Dispose();
            Assert.Equal(baseline, ManagerSubscriptions.CultureChangedHandlerCount(fixture.Manager));
            fixture.Manager.CurrentCulture = German;

            Assert.Equal("Hello", label.Text);
        }

        [Fact]
        public void ShouldUnsubscribeFromThePreviousTranslationManager()
        {
            using WinFormsFixture first = new();
            using WinFormsFixture second = new();
            using Label label = new();
            int firstBaseline = ManagerSubscriptions.CultureChangedHandlerCount(first.Manager);
            int secondBaseline = ManagerSubscriptions.CultureChangedHandlerCount(second.Manager);
            using TranslationProvider provider = new() { TranslationManager = first.Manager };
            provider.SetTranslationKey(label, "greeting");
            Assert.Equal(firstBaseline + 1, ManagerSubscriptions.CultureChangedHandlerCount(first.Manager));

            provider.TranslationManager = second.Manager;
            Assert.Equal(firstBaseline, ManagerSubscriptions.CultureChangedHandlerCount(first.Manager));
            Assert.Equal(secondBaseline + 1, ManagerSubscriptions.CultureChangedHandlerCount(second.Manager));
            label.Text = "manual";
            first.Manager.CurrentCulture = German;
            Assert.Equal("manual", label.Text);

            second.Manager.CurrentCulture = German;
            Assert.Equal("Hallo", label.Text);
        }

        [Fact]
        public void ShouldForgetDisposedComponents()
        {
            using WinFormsFixture fixture = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            Label label = new();
            provider.SetTranslationKey(label, "greeting");

            label.Dispose();

            Assert.Equal(string.Empty, provider.GetTranslationKey(label));
        }

        [Fact]
        public void ShouldForgetDisposedToolStripItems()
        {
            using WinFormsFixture fixture = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };
            ToolStripMenuItem menuItem = new();
            provider.SetTranslationKey(menuItem, "greeting");

            menuItem.Dispose();

            Assert.Equal(string.Empty, provider.GetTranslationKey(menuItem));
        }

        [Fact]
        public void ShouldIgnoreToolTipKeysOfControlsWhenNoToolTipIsSet()
        {
            using WinFormsFixture fixture = new();
            using Button button = new();
            using ToolTip toolTip = new();
            using TranslationProvider provider = new() { TranslationManager = fixture.Manager };

            provider.SetToolTipKey(button, "hint");

            Assert.Equal("hint", provider.GetToolTipKey(button));
            Assert.Equal(string.Empty, toolTip.GetToolTip(button));

            provider.ToolTip = toolTip;

            Assert.Equal("Click here", toolTip.GetToolTip(button));
        }

        [Fact]
        public void ShouldApplyRightToLeftToTheContainerControl()
        {
            using WinFormsFixture fixture = new();
            using Form form = new();
            using TranslationProvider provider = new() { ContainerControl = form, ApplyRightToLeft = true, TranslationManager = fixture.Manager };

            fixture.Manager.CurrentCulture = new CultureInfo("ar");
            Assert.Equal(RightToLeft.Yes, form.RightToLeft);
            Assert.True(form.RightToLeftLayout);

            fixture.Manager.CurrentCulture = German;
            Assert.Equal(RightToLeft.No, form.RightToLeft);
            Assert.False(form.RightToLeftLayout);
        }

        [Fact]
        public void ShouldNotChangeRightToLeftWhenDisabled()
        {
            using WinFormsFixture fixture = new();
            using Form form = new();
            using TranslationProvider provider = new() { ContainerControl = form, TranslationManager = fixture.Manager };

            fixture.Manager.CurrentCulture = new CultureInfo("ar");

            Assert.Equal(RightToLeft.No, form.RightToLeft);
            Assert.False(form.RightToLeftLayout);
        }

        [Fact]
        public void ShouldExtendOnlySupportedComponents()
        {
            using TranslationProvider provider = new();
            using Label label = new();
            using ToolStripMenuItem menuItem = new();
            using ColumnHeader column = new();
            using System.Windows.Forms.Timer timer = new();

            Assert.True(provider.CanExtend(label));
            Assert.True(provider.CanExtend(menuItem));
            Assert.True(provider.CanExtend(column));
            Assert.False(provider.CanExtend(timer));
            Assert.False(provider.CanExtend(provider));
        }

        [Fact]
        public void ShouldExposeExtenderPropertiesToTheDesigner()
        {
            using Container container = new();
#pragma warning disable CA2000 // The container disposes of the components added to it
            TranslationProvider provider = new();
            Label label = new();
#pragma warning restore CA2000
            container.Add(provider);
            container.Add(label);

            PropertyDescriptor? property = TypeDescriptor.GetProperties(label)["TranslationKey"];

            Assert.NotNull(property);
            DefaultValueAttribute? defaultValue = property.Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
            Assert.Equal(string.Empty, defaultValue?.Value);

            property.SetValue(label, "greeting");

            Assert.Equal("greeting", provider.GetTranslationKey(label));
            Assert.Equal("greeting", property.GetValue(label));
        }
    }
}
