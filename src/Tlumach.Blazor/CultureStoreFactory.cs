// <copyright file="CultureStoreFactory.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Tlumach.Blazor;

/// <summary>
/// Creates the store that <see cref="TlumachBlazorOptions.Persistence"/> asks for.
/// </summary>
internal static class CultureStoreFactory
{
    internal static ITlumachCultureStore Create(IServiceProvider services, TlumachBlazorOptions options)
    {
        TlumachCulturePersistence persistence = options.EffectivePersistence;
        List<ITlumachCultureStore> stores = [];

        if ((persistence & TlumachCulturePersistence.Cookie) != TlumachCulturePersistence.None)
            stores.Add(new CookieCultureStore(services.GetRequiredService<IJSRuntime>(), services.GetRequiredService<NavigationManager>(), options));

        if ((persistence & TlumachCulturePersistence.LocalStorage) != TlumachCulturePersistence.None)
            stores.Add(new LocalStorageCultureStore(services.GetRequiredService<IJSRuntime>(), options));

        return stores.Count == 1 ? stores[0] : new CompositeCultureStore([.. stores]);
    }
}
