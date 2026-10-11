// <copyright file="Sta.cs" company="Allied Bits Ltd.">
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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Tlumach.WpfTests
{
    /// <summary>
    /// Runs test code on a single-threaded apartment thread with a WPF dispatcher, which WPF elements require, and pumps the dispatcher queue.
    /// </summary>
    internal static class Sta
    {
        /// <summary>
        /// The XML namespace declarations that the XAML snippets of the tests use.
        /// </summary>
        public const string Namespaces =
            "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:tlumach=\"clr-namespace:Tlumach.WPF;assembly=Tlumach.WPF\" " +
            "xmlns:t=\"clr-namespace:Tlumach.WpfTests;assembly=Tlumach.WpfTests\"";

        public static void Run(Action action)
        {
            ExceptionDispatchInfo? failure = null;

            Thread thread = new(() =>
            {
                try
                {
                    TestStrings.Reset();
                    action();
                }
#pragma warning disable CA1031 // The exception is rethrown on the test thread
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    TestStrings.Reset();
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            failure?.Throw();
        }

        /// <summary>
        /// Parses a XAML snippet; the namespaces of <see cref="Namespaces"/> are added to its root element (the snippet starts with <c>&lt;RootElement {0}</c>).
        /// </summary>
        public static T Parse<T>(string xaml)
            where T : class
            => (T)XamlReader.Parse(string.Format(System.Globalization.CultureInfo.InvariantCulture, xaml, Namespaces));

        /// <summary>
        /// Processes the work queued in the dispatcher of the current thread, including bindings, layout, and the callbacks posted from other threads.
        /// </summary>
        public static void DoEvents()
        {
            DispatcherFrame frame = new();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Runs the garbage collector until the unreachable objects, including those with finalizers, are collected.
        /// </summary>
        public static void CollectGarbage()
        {
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            GC.Collect();
        }
    }
}
