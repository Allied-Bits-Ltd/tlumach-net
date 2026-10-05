// <copyright file="ITlumachCultureStore.cs" company="Allied Bits Ltd.">
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
/// Stores the culture that the user has chosen so that the next visit or the next circuit starts with it.
/// <para>Register an own implementation before calling <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/> to replace the built-in stores.</para>
/// </summary>
public interface ITlumachCultureStore
{
    /// <summary>
    /// Loads the name of the stored culture.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The culture name, or <see langword="null"/> if nothing is stored.</returns>
    ValueTask<string?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the culture.
    /// </summary>
    /// <param name="culture">The culture to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the culture is stored.</returns>
    ValueTask SaveAsync(CultureInfo culture, CancellationToken cancellationToken = default);
}
