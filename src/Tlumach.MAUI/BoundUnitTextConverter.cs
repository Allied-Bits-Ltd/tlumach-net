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
using System.Globalization;

using Microsoft.Maui.Controls;

namespace Tlumach.MAUI
{
    /// <summary>
    /// The converter of the multi-binding that <see cref="Translate"/> creates for a unit that comes from a binding: converts the unit (the value of the first binding)
    /// to its current text. The second binding of the multi-binding binds to <see cref="XamlBoundUnitTracker.Version"/> of the tracker, which makes MAUI convert the unit again
    /// when the culture or the placeholders change.
    /// </summary>
    internal sealed class BoundUnitTextConverter : IMultiValueConverter
    {
        private readonly XamlBoundUnitTracker _tracker;

        public BoundUnitTextConverter(XamlBoundUnitTracker tracker)
        {
            _tracker = tracker;
        }

        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
            => _tracker.GetText(values is { Length: > 0 } ? values[0] : null);

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}