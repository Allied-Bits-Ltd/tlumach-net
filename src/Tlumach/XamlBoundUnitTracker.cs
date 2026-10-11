// <copyright file="XamlBoundUnitTracker.cs" company="Allied Bits Ltd.">
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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Tlumach
{
    /// <summary>
    /// An internal class used in XAML integrations for a translation unit that comes from a binding: the binding of the unit becomes the first child of a multi-binding
    /// of the target property, so the framework resolves it for the target element, and the second child binds to <see cref="Version"/> of this object.
    /// The converter of the multi-binding turns the unit into its text with <see cref="GetText"/>, and this object changes <see cref="Version"/> when the culture
    /// of a converted unit or its placeholders change, so that the framework converts the units again.
    /// <para>The units and their managers reference this object weakly (see <see cref="WeakUnitSubscription{TOwner}"/>); the multi-binding keeps it alive.</para>
    /// </summary>
    internal sealed class XamlBoundUnitTracker : INotifyPropertyChanged
    {
        private readonly Action<Action> _postToUi;

        // One subscription per unit converted for any of the targets of the multi-binding (a template or a style setter can show many units)
        private readonly ConditionalWeakTable<TranslationUnit, WeakUnitSubscription<XamlBoundUnitTracker>> _subscriptions = new();
        private int _updatePending;

        public XamlBoundUnitTracker(Action<Action> postToUi)
        {
            _postToUi = postToUi ?? throw new ArgumentNullException(nameof(postToUi));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets a number that changes when the text of one of the units passed to <see cref="GetText"/> may have changed.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Returns the current text of the unit and starts following its changes.
        /// </summary>
        /// <param name="value">The value of the binding of the unit.</param>
        /// <returns>The current text of the unit, or an empty string if the value is not a <see cref="TranslationUnit"/>.</returns>
        public string GetText(object? value)
        {
            if (value is not TranslationUnit unit)
                return string.Empty;

            if (!_subscriptions.TryGetValue(unit, out _))
                _subscriptions.Add(unit, new WeakUnitSubscription<XamlBoundUnitTracker>(unit, this, static (tracker, _) => tracker.RequestUpdate()));

            return unit.CurrentValue ?? string.Empty;
        }

        private void RequestUpdate()
        {
            // Called in the thread that changed the culture or the placeholders; the targets are updated in the UI thread, once for several changes made in a row
            if (Interlocked.Exchange(ref _updatePending, 1) != 0)
                return;

            _postToUi(() =>
            {
                Volatile.Write(ref _updatePending, 0);

                Version++;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
            });
        }
    }
}
