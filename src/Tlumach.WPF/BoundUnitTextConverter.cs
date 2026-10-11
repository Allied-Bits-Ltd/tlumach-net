// <copyright file="BoundUnitTextConverter.cs" company="Allied Bits Ltd.">
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;

namespace Tlumach.WPF
{
    /// <summary>
    /// Converts the unit provided by the binding of <see cref="TranslateExtension.Unit"/> to its current text, and changes <see cref="Version"/> (the source of the second binding
    /// of the multi-binding) when the culture of a converted unit or its placeholders change, so that WPF converts the units again.
    /// <para>The units and their managers reference the converter weakly; the multi-binding keeps it alive.</para>
    /// </summary>
    internal sealed class BoundUnitTextConverter : IMultiValueConverter, INotifyPropertyChanged
    {
        private readonly Action<Action> _postToUi;

        // One subscription per unit converted for any of the targets of the multi-binding (a template or a style setter can show many units)
        private readonly ConditionalWeakTable<TranslationUnit, WeakUnitSubscription<BoundUnitTextConverter>> _subscriptions = new();
        private int _updatePending;

        public BoundUnitTextConverter(Action<Action> postToUi)
        {
            _postToUi = postToUi;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Version { get; private set; }

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Length == 0 || values[0] is not TranslationUnit unit)
                return string.Empty;

            if (!_subscriptions.TryGetValue(unit, out _))
                _subscriptions.Add(unit, new WeakUnitSubscription<BoundUnitTextConverter>(unit, this, static (converter, _) => converter.RequestUpdate()));

            return unit.CurrentValue ?? string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

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
