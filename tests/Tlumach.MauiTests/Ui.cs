// <copyright file="Ui.cs" company="Allied Bits Ltd.">
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
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Dispatching;

using Tlumach.XamlTests;

namespace Tlumach.MauiTests
{
    /// <summary>
    /// Runs test code on a thread that stands in for the UI thread of a MAUI application: MAUI gets a dispatcher for it, whose queued actions run only when
    /// the test calls <see cref="DoEvents"/>.
    /// </summary>
    internal static class Ui
    {
        /// <summary>
        /// The XML namespace declarations that the XAML snippets of the tests use.
        /// </summary>
        public const string Namespaces =
            "xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2009/xaml\" " +
            "xmlns:tlumach=\"clr-namespace:Tlumach.MAUI;assembly=Tlumach.MAUI\" " +
            "xmlns:t=\"clr-namespace:Tlumach.XamlTests;assembly=Tlumach.MauiTests\"";

        [ThreadStatic]
        private static QueueDispatcher? _dispatcher;

        static Ui()
        {
            DispatcherProvider.SetCurrent(new Provider());
        }

        public static void Run(Action action)
        {
            ExceptionDispatchInfo? failure = null;

            Thread thread = new(() =>
            {
                _dispatcher = new QueueDispatcher();
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
                    _dispatcher = null;
                }
            });

            thread.Start();
            thread.Join();

            failure?.Throw();
        }

        /// <summary>
        /// Loads a XAML snippet into a new instance of <typeparamref name="T"/>; the namespaces of <see cref="Namespaces"/> are added to its root element
        /// (the snippet starts with <c>&lt;RootElement {0}</c>).
        /// </summary>
        public static T Load<T>(string xaml)
            where T : BindableObject, new()
            => new T().LoadFromXaml(string.Format(CultureInfo.InvariantCulture, xaml, Namespaces));

        /// <summary>
        /// Runs the actions that were dispatched to the UI thread, including those dispatched from other threads.
        /// </summary>
        public static void DoEvents() => _dispatcher?.RunPending();

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

        private sealed class Provider : IDispatcherProvider
        {
            public IDispatcher? GetForCurrentThread() => _dispatcher;
        }

        private sealed class QueueDispatcher : IDispatcher
        {
            private readonly Thread _thread = Thread.CurrentThread;
            private readonly ConcurrentQueue<Action> _queue = new();

            public bool IsDispatchRequired => Thread.CurrentThread != _thread;

            public bool Dispatch(Action action)
            {
                _queue.Enqueue(action);
                return true;
            }

            public bool DispatchDelayed(TimeSpan delay, Action action) => Dispatch(action);

            public IDispatcherTimer CreateTimer() => throw new NotSupportedException();

            public void RunPending()
            {
                while (_queue.TryDequeue(out Action? action))
                    action();
            }
        }
    }
}
