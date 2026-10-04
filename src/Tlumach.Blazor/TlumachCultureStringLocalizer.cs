// <copyright file="TlumachCultureStringLocalizer.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Microsoft.Extensions.Localization;

using Tlumach.Extensions.Localization;

namespace Tlumach.Blazor;

/// <summary>
/// A localizer that retrieves strings in the culture of the user (<see cref="TlumachCultureState.Culture"/>) instead of the culture of the thread.
/// A localizer that does not come from Tlumach is used unchanged.
/// </summary>
internal sealed class TlumachCultureStringLocalizer : IStringLocalizer
{
    private readonly IStringLocalizer _inner;
    private readonly TlumachCultureState _state;
    private IStringLocalizer? _cached;
    private string? _cachedCultureName;

    public TlumachCultureStringLocalizer(IStringLocalizer inner, TlumachCultureState state)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public LocalizedString this[string name] => Current[name];

    public LocalizedString this[string name, params object[] arguments] => Current[name, arguments];

    private IStringLocalizer Current
    {
        get
        {
            if (_inner is not TlumachStringLocalizer tlumach)
                return _inner;

            CultureInfo culture = _state.Culture;
            if (_cached is null || !culture.Name.Equals(_cachedCultureName, StringComparison.Ordinal))
            {
                _cached = tlumach.WithCulture(culture);
                _cachedCultureName = culture.Name;
            }

            return _cached;
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Current.GetAllStrings(includeParentCultures);
}

/// <summary>
/// The generic counterpart of <see cref="TlumachCultureStringLocalizer"/>, registered for <see cref="IStringLocalizer{T}"/>.
/// </summary>
/// <typeparam name="T">The type whose name selects the strings.</typeparam>
internal sealed class TlumachCultureStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly TlumachCultureStringLocalizer _localizer;

    public TlumachCultureStringLocalizer(IStringLocalizerFactory factory, TlumachCultureState state)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _localizer = new TlumachCultureStringLocalizer(factory.Create(typeof(T)), state);
    }

    public LocalizedString this[string name] => _localizer[name];

    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _localizer.GetAllStrings(includeParentCultures);
}
