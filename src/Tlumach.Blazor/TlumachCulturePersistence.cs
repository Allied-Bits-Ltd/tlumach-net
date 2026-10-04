// <copyright file="TlumachCulturePersistence.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Blazor;

/// <summary>
/// Specifies where <see cref="TlumachCultureState"/> stores the culture that the user has chosen.
/// </summary>
[Flags]
#pragma warning disable CA1714 // Flags enums should have plural names - "persistence" names the feature; the values are the places where it stores the culture
public enum TlumachCulturePersistence
#pragma warning restore CA1714
{
    /// <summary>
    /// The culture is not stored.
    /// </summary>
    None = 0,

    /// <summary>
    /// The culture is stored in the ASP.NET Core culture cookie by the endpoint that <c>MapTlumachCultureEndpoint</c> maps,
    /// and a WebAssembly client of a Blazor Web App reads it back from the <c>lang</c> attribute of the <c>html</c> element.
    /// </summary>
    Cookie = 1,

    /// <summary>
    /// The culture is stored in the local storage of the browser.
    /// </summary>
    LocalStorage = 2,
}
