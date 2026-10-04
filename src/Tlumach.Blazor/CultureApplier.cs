// <copyright file="CultureApplier.cs" company="Allied Bits Ltd.">
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
/// Applies a culture to the whole process. Only for hosts where one user owns the process (Blazor WebAssembly, Blazor Hybrid).
/// </summary>
internal static class CultureApplier
{
    internal static void ApplyGlobally(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // A copy, because the list may change while it is enumerated when a translation manager is created or disposed.
        foreach (TranslationManager manager in TranslationManager.TranslationManagers.ToArray())
        {
            if (!ReferenceEquals(manager, TranslationManager.Empty))
                manager.CurrentCulture = culture;
        }
    }
}
