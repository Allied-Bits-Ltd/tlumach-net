// <copyright file="TlumachFluidOptions.cs" company="Allied Bits Ltd.">
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

using System.Text.Encodings.Web;

using Tlumach.Templating;

namespace Tlumach.Fluid;

/// <summary>
/// The options of the Fluid integration, set up by <see cref="TlumachTemplateOptionsExtensions.AddTlumach"/>.
/// </summary>
public sealed class TlumachFluidOptions : TemplateTranslationOptions
{
    /// <summary>
    /// Gets or sets the name of the filter that returns a translation as text. The default is <c>"t"</c>.
    /// </summary>
    public string FilterName { get; set; } = "t";

    /// <summary>
    /// Gets or sets the name of the filter that returns a translation, which contains trusted HTML, with HTML-encoded values. The default is <c>"t_html"</c>.
    /// <see langword="null"/> or an empty string means the filter is not registered.
    /// </summary>
    public string? MarkupFilterName { get; set; } = "t_html";

    /// <summary>
    /// Gets or sets the encoder for the values in the filter named by <see cref="MarkupFilterName"/>. Use the encoder that the templates are rendered with.
    /// The default, <see cref="HtmlEncoder.Default"/>, encodes all characters outside Basic Latin; use <c>HtmlEncoder.Create(UnicodeRanges.All)</c> to keep them.
    /// </summary>
    public HtmlEncoder HtmlEncoder { get; set; } = HtmlEncoder.Default;
}
