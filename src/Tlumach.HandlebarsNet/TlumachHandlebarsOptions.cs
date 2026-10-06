// <copyright file="TlumachHandlebarsOptions.cs" company="Allied Bits Ltd.">
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

using Tlumach.Templating;

namespace Tlumach.HandlebarsNet;

/// <summary>
/// The options of the Handlebars.Net integration, set up by <see cref="TlumachHandlebarsExtensions.RegisterTlumach"/>.
/// </summary>
public sealed class TlumachHandlebarsOptions : TemplateTranslationOptions
{
    /// <summary>
    /// Gets or sets the name of the helper that returns a translation as text. The default is <c>"t"</c>.
    /// </summary>
    public string HelperName { get; set; } = "t";

    /// <summary>
    /// Gets or sets the name of the helper that returns a translation, which contains trusted HTML, with HTML-encoded values. The default is <c>"t_html"</c>.
    /// <see langword="null"/> or an empty string means the helper is not registered.
    /// </summary>
    public string? MarkupHelperName { get; set; } = "t_html";

    /// <summary>
    /// Gets or sets the name of the <c>@data</c> variable that holds the culture of a render (a <c>CultureInfo</c> or a culture name),
    /// passed as the data of the template, e.g. <c>template(model, new { culture = "de" })</c>. The default is <c>"culture"</c>.
    /// </summary>
    public string CultureDataName { get; set; } = "culture";
}
