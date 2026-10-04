// <copyright file="CompositeCultureStore.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Blazor;

/// <summary>
/// Combines several stores: saves to all of them and loads from the first one that has a value. With no stores, nothing is stored.
/// </summary>
internal sealed class CompositeCultureStore : ITlumachCultureStore
{
    private readonly ITlumachCultureStore[] _stores;

    public CompositeCultureStore(params ITlumachCultureStore[] stores)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
    }

    public async ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default)
    {
        foreach (ITlumachCultureStore store in _stores)
        {
            string? value = await store.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    public async ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        foreach (ITlumachCultureStore store in _stores)
            await store.SaveAsync(culture, cancellationToken).ConfigureAwait(false);
    }
}
