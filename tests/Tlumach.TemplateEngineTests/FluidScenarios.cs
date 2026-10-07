// <copyright file="FluidScenarios.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Text.Encodings.Web;

using Fluid;

using Tlumach.Fluid;

namespace Tlumach.TemplateEngineTests;

public sealed class FluidScenarios : TemplateEngineScenarios
{
    [Fact]
    public void KeepsRawValueRawInMarkup()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = true };
        string template = "{% assign who = \"<i>Ann</i>\" | raw %}{{ \"markup\" | t_html: name: who, count: 1 }}";

        Assert.Equal("<b><i>Ann</i></b> ordered 1 item.", Render(setup, template, new Dictionary<string, object?>(StringComparer.Ordinal), renderCulture: null));
    }

    [Fact]
    public void PassesIntegralNumbersAsIntegers()
    {
        Assert.Equal("1 item", Render(new EngineSetup(CreateManager()), "{{ \"items\" | t: count: 1 }}", new Dictionary<string, object?>(StringComparer.Ordinal), renderCulture: null));
    }

    protected override string Call(string function, string keyExpression, IReadOnlyList<string> positional, IReadOnlyList<(string Name, string Expression)> named)
    {
        var builder = new StringBuilder("{{ ").Append(keyExpression).Append(" | ").Append(function);
        string separator = ": ";
        foreach (string expression in positional)
        {
            builder.Append(separator).Append(expression);
            separator = ", ";
        }

        foreach ((string name, string expression) in named)
        {
            builder.Append(separator).Append(name).Append(": ").Append(expression);
            separator = ", ";
        }

        return builder.Append(" }}").ToString();
    }

    protected override Func<IReadOnlyDictionary<string, object?>, CultureInfo?, string> CreateRenderer(EngineSetup setup, string templateText)
    {
        var options = new TemplateOptions().AddTlumach(setup.Manager, new TlumachFluidOptions
        {
            KeyPrefix = setup.KeyPrefix,
            MissingKey = setup.MissingKey,
            OnMissingKey = setup.OnMissingKey,
            FilterName = setup.FunctionName,
            MarkupFilterName = setup.MarkupFunctionName,
        });

        var parser = new FluidParser();
        if (!parser.TryParse(templateText, out var fluidTemplate, out var error))
            throw new InvalidOperationException(error);

        // The options and the parsed template are shared by all renders; each render has its own context.
        HtmlEncoder encoder = HtmlEncoder.Default;
        bool html = setup.HtmlOutput;
        return (model, renderCulture) =>
        {
            var context = new TemplateContext(options);
            foreach (KeyValuePair<string, object?> pair in model)
                context.SetValue(pair.Key, pair.Value);
            if (renderCulture is not null)
                context.CultureInfo = renderCulture;

            return fluidTemplate.Render(context, html ? encoder : NullEncoder.Default);
        };
    }
}
