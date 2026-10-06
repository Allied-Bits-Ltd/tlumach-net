// <copyright file="TlumachTemplateOptionsExtensions.cs" company="Allied Bits Ltd.">
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

using Fluid;

using Tlumach.Templating;

namespace Tlumach.Fluid;

/// <summary>
/// Registers the Tlumach filters in Fluid <see cref="TemplateOptions"/>.
/// </summary>
public static class TlumachTemplateOptionsExtensions
{
    /// <summary>
    /// Adds the filters <c>t</c> and <c>t_html</c> (names set by the options) that return translations from <paramref name="manager"/>.
    /// Set <c>TemplateContext.CultureInfo</c> to the culture of each render, and render HTML templates with an HTML encoder.
    /// </summary>
    /// <param name="options">The template options to add the filters to.</param>
    /// <param name="manager">The translation manager to take the translations from.</param>
    /// <param name="tlumachOptions">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns><paramref name="options"/>.</returns>
    public static TemplateOptions AddTlumach(this TemplateOptions options, TranslationManager manager, TlumachFluidOptions? tlumachOptions = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(manager);

        tlumachOptions ??= new TlumachFluidOptions();
        if (string.IsNullOrEmpty(tlumachOptions.FilterName))
            throw new ArgumentException("The name of the filter must not be empty.", nameof(tlumachOptions));

        var translator = new TemplateTranslator(manager, tlumachOptions);
        options.Filters.AddFilter(tlumachOptions.FilterName, new TlumachFilter(translator, markup: false, tlumachOptions.HtmlEncoder).InvokeAsync);
        if (!string.IsNullOrEmpty(tlumachOptions.MarkupFilterName))
            options.Filters.AddFilter(tlumachOptions.MarkupFilterName, new TlumachFilter(translator, markup: true, tlumachOptions.HtmlEncoder).InvokeAsync);

        return options;
    }
}
