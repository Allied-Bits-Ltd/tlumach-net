// <copyright file="MessageCultureSource.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.FluentValidation;

/// <summary>
/// Selects the culture, for which a validation message is read when neither the caller nor <c>LanguageManager.Culture</c> specifies one.
/// </summary>
public enum MessageCultureSource
{
    /// <summary>
    /// Use <see cref="CultureInfo.CurrentUICulture"/>, which is what the built-in language manager of FluentValidation uses and what the request localization of ASP.NET Core and
    /// <c>TlumachCultureState</c> of Blazor set.
    /// </summary>
    CurrentUICulture = 0,

    /// <summary>
    /// Use <see cref="Tlumach.TranslationManager.CurrentCulture"/> of the translation manager, which a desktop application switches to change the language. It returns
    /// <see cref="CultureInfo.CurrentCulture"/> when <see cref="Tlumach.TranslationManager.UseContextCulture"/> is set.
    /// </summary>
    TranslationManager = 1,
}
