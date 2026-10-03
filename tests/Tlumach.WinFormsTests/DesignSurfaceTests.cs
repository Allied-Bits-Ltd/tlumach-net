// <copyright file="DesignSurfaceTests.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

using Tlumach.WinForms;

#pragma warning disable CA1303 // The tests set literal texts on controls on purpose; they are test data, not user-facing strings that need localization

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// Tests the provider inside a real designer host (<see cref="DesignSurface"/>), as the Visual Studio Designer hosts it.
    /// </summary>
    [Trait("Category", "WinForms")]
    public class DesignSurfaceTests
    {
        [Fact]
        public void ShouldPickTheRootComponentAndNotTranslateInTheDesigner()
        {
            RunOnStaThread(() =>
            {
                using WinFormsFixture fixture = new();
                using DesignSurface surface = new();
                surface.BeginLoad(typeof(Form));
                Assert.Empty(surface.LoadErrors);

                IDesignerHost host = Assert.IsAssignableFrom<IDesignerHost>(surface.GetService(typeof(IDesignerHost)));
                TranslationProvider provider = (TranslationProvider)host.CreateComponent(typeof(TranslationProvider));
                Label label = (Label)host.CreateComponent(typeof(Label));
                label.Text = "designed";

                Assert.Same(host.RootComponent, provider.ContainerControl);

                provider.TranslationManager = fixture.Manager;
                provider.SetTranslationKey(label, "greeting");
                fixture.Manager.CurrentCulture = new CultureInfo("de");

                Assert.Equal("designed", label.Text);
                Assert.Equal("greeting", provider.GetTranslationKey(label));
            });
        }

        /// <summary>
        /// Runs the action on a dedicated STA thread (the designer host needs one) and rethrows its exception on the calling thread.
        /// </summary>
        private static void RunOnStaThread(Action action)
        {
            Exception? failure = null;

#pragma warning disable CA1031 // The exception is rethrown on the test thread
            Thread thread = new(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
#pragma warning restore CA1031

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure is not null)
                throw new InvalidOperationException("The test failed on the STA thread.", failure);
        }
    }
}
