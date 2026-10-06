// <copyright file="TlumachScribanOptions.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Scriban;

/// <summary>
/// The options of the Scriban integration, set up by <see cref="TlumachScriptObjectExtensions.ImportTlumach"/>.
/// </summary>
public sealed class TlumachScribanOptions : TemplateTranslationOptions
{
    /// <summary>
    /// Gets or sets the name of the function that returns a translation as text. The default is <c>"t"</c>.
    /// </summary>
    public string FunctionName { get; set; } = "t";

    /// <summary>
    /// Gets or sets the name of the function that returns a translation, which contains trusted HTML, with HTML-encoded values. The default is <c>"t_html"</c>.
    /// <see langword="null"/> or an empty string means the function is not registered.
    /// </summary>
    public string? MarkupFunctionName { get; set; } = "t_html";

    /// <summary>
    /// Gets or sets a value indicating whether the function named by <see cref="FunctionName"/> HTML-encodes its result. The default is <see langword="false"/>, because Scriban
    /// does not encode output and templates encode text with <c>html.escape</c>.
    /// </summary>
    public bool HtmlEncode { get; set; }

    /// <summary>
    /// Gets or sets the encoder used when <see cref="HtmlEncode"/> is set and for the values in the function named by <see cref="MarkupFunctionName"/>.
    /// The default, <see cref="HtmlEncoder.Default"/>, encodes all characters outside Basic Latin; use <c>HtmlEncoder.Create(UnicodeRanges.All)</c> to keep them.
    /// </summary>
    public HtmlEncoder HtmlEncoder { get; set; } = HtmlEncoder.Default;
}
