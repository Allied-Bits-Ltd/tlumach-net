// <copyright file="WeakUnitSubscription.cs" company="Allied Bits Ltd.">
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

namespace Tlumach
{
    /// <summary>
    /// An internal class used in XAML integrations: subscribes to <see cref="TranslationUnit.OnChange"/> of a unit and to <see cref="TranslationManager.OnCultureChanged"/> of its manager
    /// on behalf of an owner that it references weakly.
    /// <para>The generated classes keep their translation manager and units in static properties, so a direct subscription would keep the owner (and whatever it references)
    /// alive for the lifetime of the application. With this class, the events reference only the subscription; once the owner is collected, the subscription removes itself
    /// from the events the next time one of them fires. The owner must therefore be kept alive by whoever uses it, typically by the binding of the target property.</para>
    /// </summary>
    /// <typeparam name="TOwner">The type of the owner.</typeparam>
    internal sealed class WeakUnitSubscription<TOwner>
        where TOwner : class
    {
        private readonly WeakReference<TOwner> _owner;
        private readonly Action<TOwner, bool> _callback;
        private readonly TranslationManager _translationManager;

        // The unit is referenced weakly, so that the subscription (kept by the manager) does not keep alive a unit that the application has released.
        private readonly WeakReference<TranslationUnit> _unit;
        private int _unsubscribed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakUnitSubscription{TOwner}"/> class and subscribes to the events.
        /// </summary>
        /// <param name="unit">The unit whose changes are reported.</param>
        /// <param name="owner">The owner, referenced weakly.</param>
        /// <param name="callback">The method called when the unit changes; it receives the owner and an indicator of whether the change was a change of the culture.
        /// It must not capture the owner (use a static lambda), or the owner will never be collected.</param>
        public WeakUnitSubscription(TranslationUnit unit, TOwner owner, Action<TOwner, bool> callback)
        {
            if (unit is null)
                throw new ArgumentNullException(nameof(unit));

            _owner = new WeakReference<TOwner>(owner ?? throw new ArgumentNullException(nameof(owner)));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _unit = new WeakReference<TranslationUnit>(unit);
            _translationManager = unit.TranslationManager;

            unit.OnChange += Unit_OnChange;
            _translationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
        }

        /// <summary>
        /// Unsubscribes from the events.
        /// </summary>
        public void Unsubscribe()
        {
            if (Interlocked.Exchange(ref _unsubscribed, 1) != 0)
                return;

            _translationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
            if (_unit.TryGetTarget(out TranslationUnit? unit))
                unit.OnChange -= Unit_OnChange;
        }

        private void Unit_OnChange(object? sender, EventArgs e) => Notify(cultureChanged: false);

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs e) => Notify(cultureChanged: true);

        private void Notify(bool cultureChanged)
        {
            // An event that was being raised while the subscription was removed may still call it
            if (Volatile.Read(ref _unsubscribed) != 0)
                return;

            if (_owner.TryGetTarget(out TOwner? owner) && _unit.TryGetTarget(out _))
                _callback(owner, cultureChanged);
            else
                Unsubscribe();
        }
    }
}
