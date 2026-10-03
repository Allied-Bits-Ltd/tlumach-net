// <copyright file="UiInvoker.cs" company="Allied Bits Ltd.">
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
using System.Threading;

namespace Tlumach.WinForms
{
    /// <summary>
    /// Runs actions on the thread that created the invoker, which, for Windows Forms components, is the UI thread.
    /// <para>The invoker captures the synchronization context of that thread (<c>WindowsFormsSynchronizationContext</c> in Windows Forms applications).
    /// An action requested on the same thread runs immediately; an action requested on another thread is posted to the captured context.
    /// When the creating thread has no synchronization context, there is nothing to marshal to, and actions run immediately on the calling thread.</para>
    /// </summary>
    internal sealed class UiInvoker
    {
        private readonly SynchronizationContext? _context;
        private readonly int _threadId;

        public UiInvoker()
        {
            _context = SynchronizationContext.Current;
            _threadId = Environment.CurrentManagedThreadId;
        }

        public void Invoke(Action action)
        {
            // The thread ID, rather than the identity of SynchronizationContext.Current, tells whether we are on the UI thread:
            // code may install a different context on the UI thread, and that must not make us post to ourselves.
            if (_context is null || Environment.CurrentManagedThreadId == _threadId)
                action();
            else
                _context.Post(static state => (state as Action)?.Invoke(), action);
        }
    }
}
