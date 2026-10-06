// <copyright file="TlumachScriptFunction.cs" company="Allied Bits Ltd.">
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
using System.Text.Encodings.Web;

using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

using Tlumach.Templating;

namespace Tlumach.Scriban;

/// <summary>
/// The Scriban function that returns a translation. It takes the key or the unit as the first argument, positional values after it, and named values
/// (<c>name: value</c>), which Scriban passes in <c>ScriptArray.ScriptObject</c> because <see cref="VarParamKind"/> is <see cref="ScriptVarParamKind.Direct"/>.
/// </summary>
internal sealed class TlumachScriptFunction : IScriptCustomFunction
{
    private static readonly ScriptParameterInfo KeyParameter = new(typeof(object), "key");
    private static readonly ScriptParameterInfo ValueParameter = new(typeof(object), "values");

    private readonly TemplateTranslator _translator;
    private readonly bool _markup;
    private readonly bool _htmlEncode;
    private readonly HtmlEncoder _encoder;

    public TlumachScriptFunction(TemplateTranslator translator, bool markup, bool htmlEncode, HtmlEncoder encoder)
    {
        _translator = translator;
        _markup = markup;
        _htmlEncode = htmlEncode;
        _encoder = encoder;
    }

    public int RequiredParameterCount => 1;

    public int ParameterCount => 1;

    public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;

    public Type ReturnType => typeof(string);

    public ScriptParameterInfo GetParameterInfo(int index) => index == 0 ? KeyParameter : ValueParameter;

    public object? Invoke(TemplateContext context, ScriptNode? callerContext, ScriptArray arguments, ScriptBlockStatement? blockStatement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(arguments);

        var values = new TemplateArguments();
        for (int i = 1; i < arguments.Count; i++)
            values.AddPositional(arguments[i]);

        object? explicitCulture = null;
        foreach (var pair in arguments.ScriptObject)
        {
            if (string.Equals(pair.Key, _translator.CultureArgumentName, StringComparison.OrdinalIgnoreCase))
                explicitCulture = pair.Value;
            else
                values.AddNamed(pair.Key, pair.Value);
        }

        // Scriban's culture of the render (PushCulture) is the invariant culture until a template or the application pushes one.
        CultureInfo culture = _translator.ResolveCulture(explicitCulture, context.CurrentCulture);
        object? keyOrUnit = arguments.Count > 0 ? arguments[0] : null;

        if (_markup)
            return _translator.TranslateMarkup(keyOrUnit, values, culture, _encoder.Encode);

        string text = _translator.Translate(keyOrUnit, values, culture);
        return _htmlEncode ? _encoder.Encode(text) : text;
    }

    public ValueTask<object?> InvokeAsync(TemplateContext context, ScriptNode? callerContext, ScriptArray arguments, ScriptBlockStatement? blockStatement)
        => new(Invoke(context, callerContext, arguments, blockStatement));
}
