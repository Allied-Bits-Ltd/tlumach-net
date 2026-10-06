// <copyright file="TlumachHelperDescriptor.cs" company="Allied Bits Ltd.">
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

using HandlebarsDotNet;
using HandlebarsDotNet.Compiler;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.PathStructure;

using Tlumach.Templating;

namespace Tlumach.HandlebarsNet;

/// <summary>
/// The Handlebars helper that returns a translation. The first argument is the key or the unit, the next ones are positional values, and the hash arguments (<c>name=value</c>) are named values.
/// </summary>
internal sealed class TlumachHelperDescriptor : IHelperDescriptor<HelperOptions>
{
    private readonly TemplateTranslator _translator;
    private readonly HandlebarsConfiguration _configuration;
    private readonly bool _markup;
    private readonly ChainSegment _cultureData;

    public TlumachHelperDescriptor(string name, TemplateTranslator translator, HandlebarsConfiguration configuration, bool markup, string cultureDataName)
    {
        Name = name;
        _translator = translator;
        _configuration = configuration;
        _markup = markup;
        _cultureData = ChainSegment.Create(cultureDataName);
    }

    public PathInfo Name { get; }

    /// <summary>
    /// The subexpression path, e.g. <c>(t "key")</c>: the text is returned, and the statement that uses it escapes it.
    /// </summary>
    /// <param name="options">The options of the helper call, including the data of the render.</param>
    /// <param name="context">The context of the call.</param>
    /// <param name="arguments">The key or unit, the positional values and the hash arguments.</param>
    /// <returns>The translated text.</returns>
    public object? Invoke(in HelperOptions options, in Context context, in Arguments arguments)
        => Translate(options, arguments, suppressEncoding: false);

    /// <summary>
    /// The output path: <c>t</c> writes text that Handlebars escapes in <c>{{ }}</c> and not in <c>{{{ }}}</c>; <c>t_html</c> writes HTML whose values are encoded unless encoding is suppressed.
    /// </summary>
    /// <param name="output">The writer of the output.</param>
    /// <param name="options">The options of the helper call, including the data of the render.</param>
    /// <param name="context">The context of the call.</param>
    /// <param name="arguments">The key or unit, the positional values and the hash arguments.</param>
    public void Invoke(in EncodedTextWriter output, in HelperOptions options, in Context context, in Arguments arguments)
    {
        if (_markup)
            output.WriteSafeString(Translate(options, arguments, output.SuppressEncoding));
        else
            output.Write(Translate(options, arguments, output.SuppressEncoding));
    }

    private static object? Normalize(object? value) => value is UndefinedBindingResult ? null : value;

    private static string Encode(ITextEncoder encoder, string value)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        encoder.Encode(value, writer);
        return writer.ToString();
    }

    private string Translate(in HelperOptions options, in Arguments arguments, bool suppressEncoding)
    {
        var values = new TemplateArguments();
        for (int i = 1; i < arguments.Length; i++)
        {
            object? value = arguments[i];
            if (value is not HashParameterDictionary)
                values.AddPositional(Normalize(value));
        }

        object? explicitCulture = null;
        foreach (KeyValuePair<string, object?> pair in arguments.Hash)
        {
            if (string.Equals(pair.Key, _translator.CultureArgumentName, StringComparison.OrdinalIgnoreCase))
                explicitCulture = Normalize(pair.Value);
            else
                values.AddNamed(pair.Key, Normalize(pair.Value));
        }

        // Handlebars has no culture per render, so it is passed as @data.
        CultureInfo? renderCulture = TemplateTranslator.ToCulture(Normalize(options.Data[_cultureData]));
        CultureInfo culture = _translator.ResolveCulture(explicitCulture, renderCulture);
        object? keyOrUnit = arguments.Length > 0 ? Normalize(arguments[0]) : null;

        if (!_markup)
            return _translator.Translate(keyOrUnit, values, culture);

        ITextEncoder? encoder = _configuration.TextEncoder;
        return suppressEncoding || _configuration.NoEscape || encoder is null
            ? _translator.TranslateMarkup(keyOrUnit, values, culture, static value => value)
            : _translator.TranslateMarkup(keyOrUnit, values, culture, value => Encode(encoder, value));
    }
}
