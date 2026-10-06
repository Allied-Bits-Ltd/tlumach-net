// <copyright file="TlumachFilter.cs" company="Allied Bits Ltd.">
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

using Fluid;
using Fluid.Values;

using Tlumach.Templating;

namespace Tlumach.Fluid;

/// <summary>
/// The Fluid filter that returns a translation. The input is the key or the unit, and the arguments are the values: positional ones, and named ones (<c>name: value</c>).
/// </summary>
internal sealed class TlumachFilter
{
    private readonly TemplateTranslator _translator;
    private readonly bool _markup;
    private readonly HtmlEncoder _encoder;

    public TlumachFilter(TemplateTranslator translator, bool markup, HtmlEncoder encoder)
    {
        _translator = translator;
        _markup = markup;
        _encoder = encoder;
    }

    public ValueTask<FluidValue> InvokeAsync(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(context);

        var values = new TemplateArguments();
        object? explicitCulture = null;

        // FilterArguments keeps named values in the positional list too, so the positional values are what remains after removing each named value once.
        // The values are matched by reference from the end, which is unambiguous when named arguments follow positional ones.
        var positional = new List<FluidValue>(arguments.Count);
        for (int i = 0; i < arguments.Count; i++)
            positional.Add(arguments.At(i));

        foreach (string name in arguments.Names)
        {
            FluidValue value = arguments[name];
            int index = positional.FindLastIndex(candidate => ReferenceEquals(candidate, value));
            if (index >= 0)
                positional.RemoveAt(index);

            if (string.Equals(name, _translator.CultureArgumentName, StringComparison.OrdinalIgnoreCase))
                explicitCulture = ToClr(value);
            else
                values.AddNamed(name, ToClr(value));
        }

        foreach (FluidValue value in positional)
            values.AddPositional(ToClr(value));

        CultureInfo culture = _translator.ResolveCulture(explicitCulture, context.CultureInfo);
        object? keyOrUnit = input.Type == FluidValues.Nil ? null : input.ToObjectValue();

        FluidValue result = _markup
            ? new StringValue(_translator.TranslateMarkup(keyOrUnit, values, culture, _encoder.Encode), encode: false)
            : new StringValue(_translator.Translate(keyOrUnit, values, culture)); // encoded at write time when the template is rendered with an HTML encoder

        return new ValueTask<FluidValue>(result);
    }

    /// <summary>
    /// Converts a Fluid value to the value of a placeholder.
    /// </summary>
    /// <param name="value">The Fluid value.</param>
    /// <returns><see langword="null"/> for nil, <see cref="TemplateMarkup"/> for a raw string, a <see cref="long"/> for an integral number, and the CLR value otherwise.</returns>
    internal static object? ToClr(FluidValue value)
    {
        if (value.Type == FluidValues.Nil)
            return null;

        switch (value)
        {
            case StringValue { Encode: false } raw:
                return new TemplateMarkup(raw.ToStringValue());
            case NumberValue:
                // Liquid has only decimal numbers; an integral one is passed as an integer, which Tlumach formats as an integer.
                decimal number = value.ToNumberValue();
                return decimal.Truncate(number) == number && number >= long.MinValue && number <= long.MaxValue ? (long)number : number;
            default:
                return value.ToObjectValue();
        }
    }
}
