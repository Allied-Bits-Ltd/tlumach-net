// <copyright file="SampleCultures.cs" company="Allied Bits Ltd.">
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

using Tlumach.Blazor;

namespace Tlumach.Sample.Blazor.Client;

/// <summary>
/// The configuration shared by the server and the client, so both sides agree on the cultures and on where the choice is stored.
/// </summary>
#pragma warning disable CA1515 // Consider making public types internal: the type is shared with the server project and must be public.
public static class SampleCultures
#pragma warning restore CA1515
{
    public static IReadOnlyList<CultureInfo> All { get; } =
    [
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.GetCultureInfo("uk-UA"),
    ];

    public static void Configure(TlumachBlazorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SupportedCultures = All;
        options.DefaultCulture = All[0];

        // Web App: the server stores the culture in a cookie and renders <html lang>, from which the WebAssembly client reads it on startup.
        options.Persistence = TlumachCulturePersistence.Cookie;
    }
}
