// <copyright file="TlumachScriptObjectExtensions.cs" company="Allied Bits Ltd.">
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

using Scriban.Runtime;

using Tlumach.Templating;

namespace Tlumach.Scriban;

/// <summary>
/// Registers the Tlumach functions in a Scriban <see cref="ScriptObject"/>.
/// </summary>
public static class TlumachScriptObjectExtensions
{
    /// <summary>
    /// Adds the read-only functions <c>t</c> and <c>t_html</c> (names set by the options) that return translations from <paramref name="manager"/>.
    /// Push the script object to a <c>TemplateContext</c> with <c>PushGlobal</c>, and push the culture of each render with <c>PushCulture</c>.
    /// </summary>
    /// <param name="scriptObject">The script object to add the functions to.</param>
    /// <param name="manager">The translation manager to take the translations from.</param>
    /// <param name="options">The options, or <see langword="null"/> for the defaults.</param>
    /// <returns><paramref name="scriptObject"/>.</returns>
    public static ScriptObject ImportTlumach(this ScriptObject scriptObject, TranslationManager manager, TlumachScribanOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(scriptObject);
        ArgumentNullException.ThrowIfNull(manager);

        options ??= new TlumachScribanOptions();
        if (string.IsNullOrEmpty(options.FunctionName))
            throw new ArgumentException("The name of the function must not be empty.", nameof(options));

        var translator = new TemplateTranslator(manager, options);
        scriptObject.SetValue(options.FunctionName, new TlumachScriptFunction(translator, markup: false, options.HtmlEncode, options.HtmlEncoder), readOnly: true);
        if (!string.IsNullOrEmpty(options.MarkupFunctionName))
            scriptObject.SetValue(options.MarkupFunctionName, new TlumachScriptFunction(translator, markup: true, htmlEncode: false, options.HtmlEncoder), readOnly: true);

        return scriptObject;
    }
}
