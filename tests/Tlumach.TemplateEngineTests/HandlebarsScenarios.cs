// <copyright file="HandlebarsScenarios.cs" company="Allied Bits Ltd.">
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

using HandlebarsDotNet;

using Tlumach.HandlebarsNet;

namespace Tlumach.TemplateEngineTests;

public sealed class HandlebarsScenarios : TemplateEngineScenarios
{
    [Fact]
    public void TripleStashWritesRawText()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = true };

        Assert.Equal("Hello, <Ann>!", Render(setup, "{{{t \"greeting\" name=user.name}}}", User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void TripleStashMarkupStillEncodesValues()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = true };

        Assert.Equal("<b>&lt;Ann&gt;</b> ordered 1 item.", Render(setup, "{{{t_html \"markup\" name=user.name count=1}}}", User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void NoEscapeMarkupStillEncodesValues()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = false };

        Assert.Equal("<b>&lt;Ann&gt;</b> ordered 1 item.", Render(setup, "{{t_html \"markup\" name=user.name count=1}}", User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void SubexpressionReturnsPlainText()
    {
        Assert.Equal("Hello, Welcome!!", Render(new EngineSetup(CreateManager()), "{{t \"greeting\" name=(t \"welcome\")}}", new Dictionary<string, object?>(StringComparer.Ordinal), renderCulture: null));
    }

    protected override string Call(string function, string keyExpression, IReadOnlyList<string> positional, IReadOnlyList<(string Name, string Expression)> named)
    {
        var builder = new StringBuilder("{{").Append(function).Append(' ').Append(keyExpression);
        foreach (string expression in positional)
            builder.Append(' ').Append(expression);
        foreach ((string name, string expression) in named)
            builder.Append(' ').Append(name).Append('=').Append(expression);
        return builder.Append("}}").ToString();
    }

    protected override Func<IReadOnlyDictionary<string, object?>, CultureInfo?, string> CreateRenderer(EngineSetup setup, string templateText)
    {
        IHandlebars handlebars = Handlebars.Create(new HandlebarsConfiguration { NoEscape = !setup.HtmlOutput });
        handlebars.RegisterTlumach(setup.Manager, new TlumachHandlebarsOptions
        {
            KeyPrefix = setup.KeyPrefix,
            MissingKey = setup.MissingKey,
            OnMissingKey = setup.OnMissingKey,
            HelperName = setup.FunctionName,
            MarkupHelperName = setup.MarkupFunctionName,
        });

        HandlebarsTemplate<object, object> compiled = handlebars.Compile(templateText);

        // The environment and the compiled template are shared by all renders; the culture of a render is passed as @culture data.
        return (model, renderCulture) => compiled(model, renderCulture is null ? new object() : new { culture = renderCulture });
    }
}
