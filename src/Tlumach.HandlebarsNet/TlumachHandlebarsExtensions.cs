// <copyright file="TlumachHandlebarsExtensions.cs" company="Allied Bits Ltd.">
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

using HandlebarsDotNet;

using Tlumach.Templating;

namespace Tlumach.HandlebarsNet;

/// <summary>
/// Registers the Tlumach helpers in a Handlebars.Net environment.
/// </summary>
public static class TlumachHandlebarsExtensions
{
    /// <summary>
    /// Registers the helpers <c>t</c> and <c>t_html</c> (names set by the options) that return translations from <paramref name="manager"/>.
    /// Pass the culture of each render as data: <c>template(model, new { culture })</c>.
    /// </summary>
    /// <param name="handlebars">The Handlebars environment, e.g. from <c>Handlebars.Create()</c>.</param>
    /// <param name="manager">The translation manager to take the translations from.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns><paramref name="handlebars"/>.</returns>
    public static IHandlebars RegisterTlumach(this IHandlebars handlebars, TranslationManager manager, TlumachHandlebarsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handlebars);
        ArgumentNullException.ThrowIfNull(manager);

        options ??= new TlumachHandlebarsOptions();
        if (string.IsNullOrEmpty(options.HelperName))
            throw new ArgumentException("The name of the helper must not be empty.", nameof(options));

        string cultureDataName = string.IsNullOrEmpty(options.CultureDataName) ? "culture" : options.CultureDataName;
        var translator = new TemplateTranslator(manager, options);
        handlebars.RegisterHelper(new TlumachHelperDescriptor(options.HelperName, translator, handlebars.Configuration, markup: false, cultureDataName));
        if (!string.IsNullOrEmpty(options.MarkupHelperName))
            handlebars.RegisterHelper(new TlumachHelperDescriptor(options.MarkupHelperName, translator, handlebars.Configuration, markup: true, cultureDataName));

        return handlebars;
    }
}
