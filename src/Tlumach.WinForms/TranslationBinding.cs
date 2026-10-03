// <copyright file="TranslationBinding.cs" company="Allied Bits Ltd.">
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
using System.Windows.Forms;

namespace Tlumach.WinForms
{
    /// <summary>
    /// Keeps a property of a Windows Forms component equal to the text of a translation unit.
    /// <para>The binding sets the property when it is created and every time the culture of the unit's translation manager changes
    /// or the unit reports updated placeholder values (see <see cref="BaseTranslationUnit.NotifyPlaceholdersUpdated"/>).
    /// Updates requested on other threads are performed on the thread that created the binding (the UI thread).</para>
    /// <para>Create bindings using the <c>BindTranslation</c> methods of <see cref="TranslationBindingExtensions"/>.
    /// A binding is disposed of automatically when its component is disposed of; dispose of it earlier to stop the updates.</para>
    /// </summary>
    public sealed class TranslationBinding : IDisposable
    {
        private readonly Component _target;
        private readonly Action<string> _apply;
        private readonly UiInvoker _invoker = new();
        private readonly TranslationManager? _translationManager;

        internal TranslationBinding(Component target, TranslationUnit unit, Action<string> apply)
        {
            _target = target;
            _apply = apply;
            Unit = unit;

            // TranslationManager.Empty never changes its culture, so there is nothing to listen to.
            if (unit.TranslationManager != TranslationManager.Empty)
            {
                _translationManager = unit.TranslationManager;
                _translationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
            }

            unit.OnChange += Unit_OnChange;
            target.Disposed += Target_Disposed;

            Update();
        }

        /// <summary>
        /// Gets the translation unit, the text of which the binding applies.
        /// </summary>
        public TranslationUnit Unit { get; }

        /// <summary>
        /// Gets a value indicating whether the binding has been disposed of and no longer updates the component.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Applies the current text of the unit to the component. Must be called on the UI thread.
        /// </summary>
        public void Update()
        {
            if (IsDisposed || _target is Control { IsDisposed: true })
                return;

            _invoker.EnsureContext();
            _apply(Unit.CurrentValue);
        }

        /// <summary>
        /// Stops the updates and releases the event subscriptions. Calling the method more than once has no effect.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            if (_translationManager is not null)
                _translationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;

            Unit.OnChange -= Unit_OnChange;
            _target.Disposed -= Target_Disposed;
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs e) => _invoker.Invoke(Update);

        private void Unit_OnChange(object? sender, EventArgs e) => _invoker.Invoke(Update);

        private void Target_Disposed(object? sender, EventArgs e) => Dispose();
    }
}
