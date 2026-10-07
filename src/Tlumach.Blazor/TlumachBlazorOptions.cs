// <copyright file="TlumachBlazorOptions.cs" company="Allied Bits Ltd.">
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

using Tlumach.Web;

namespace Tlumach.Blazor;

/// <summary>
/// The options of the Blazor integration of Tlumach, configured in <see cref="TlumachBlazorServiceCollectionExtensions.AddTlumachBlazor"/>.
/// </summary>
public sealed class TlumachBlazorOptions : TlumachCultureOptions
{
    /// <summary>
    /// The default value of <see cref="LocalStorageKey"/>.
    /// </summary>
    public const string DefaultLocalStorageKey = "tlumach.culture";

    /// <summary>
    /// Gets or sets the translation manager, from which <see cref="TlumachText"/> takes texts by key when its <c>Manager</c> parameter is not set.
    /// </summary>
    public TranslationManager? DefaultManager { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a culture switch also changes the process-wide culture: <see cref="CultureInfo.DefaultThreadCurrentCulture"/>,
    /// <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>, and <see cref="TranslationManager.CurrentCulture"/> of every translation manager.
    /// <para>This is right only when one user owns the process: Blazor WebAssembly and Blazor Hybrid. When <see langword="null"/>, the value is
    /// <see langword="true"/> in the browser and <see langword="false"/> elsewhere, so Blazor Hybrid applications set it to <see langword="true"/> explicitly.</para>
    /// </summary>
    public bool? ApplyCultureGlobally { get; set; }

    /// <summary>
    /// Gets or sets where the chosen culture is stored. When <see langword="null"/>, it is <see cref="TlumachCulturePersistence.LocalStorage"/> in the browser and
    /// <see cref="TlumachCulturePersistence.Cookie"/> elsewhere. A WebAssembly client of a Blazor Web App and a Blazor Hybrid application set it explicitly.
    /// </summary>
    public TlumachCulturePersistence? Persistence { get; set; }

    /// <summary>
    /// Gets or sets the key, under which the culture is kept in the local storage of the browser.
    /// </summary>
    public string LocalStorageKey { get; set; } = DefaultLocalStorageKey;

    internal bool EffectiveApplyCultureGlobally => ApplyCultureGlobally ?? OperatingSystem.IsBrowser();

    internal TlumachCulturePersistence EffectivePersistence
        => Persistence ?? (OperatingSystem.IsBrowser() ? TlumachCulturePersistence.LocalStorage : TlumachCulturePersistence.Cookie);
}
