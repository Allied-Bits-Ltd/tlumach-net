// <copyright file="ScribanScenarios.cs" company="Allied Bits Ltd.">
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
using System.Text.Unicode;

using Scriban;
using Scriban.Runtime;

using Tlumach.Scriban;

namespace Tlumach.TemplateEngineTests;

public sealed class ScribanScenarios : TemplateEngineScenarios
{
    [Fact]
    public void AcceptsPipedKey()
    {
        Assert.Equal("Welcome!", Render(new EngineSetup(CreateManager()), "{{ \"welcome\" | t }}", new Dictionary<string, object?>(StringComparer.Ordinal), renderCulture: null));
    }

    [Fact]
    public void HtmlEscapePipeEncodesOnce()
    {
        // HtmlEncode is off by default, so the usual Scriban idiom of piping into html.escape is not encoded twice.
        string template = "{{ (t \"greeting\" name: user.name) | html.escape }}";

        Assert.Equal("Hello, &lt;Ann&gt;!", Render(new EngineSetup(CreateManager()), template, User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void UsesConfiguredHtmlEncoder()
    {
        var options = new TlumachScribanOptions { HtmlEncode = true, HtmlEncoder = HtmlEncoder.Create(UnicodeRanges.All) };

        string output = Render(CreateManager(), options, "{{ t \"greeting\" name: user.name }}", User("<Ann>"), CultureInfo.GetCultureInfo("uk"));

        Assert.Equal("Привіт, &lt;Ann&gt;!", output);
    }

    protected override string Call(string function, string keyExpression, IReadOnlyList<string> positional, IReadOnlyList<(string Name, string Expression)> named)
    {
        var builder = new StringBuilder("{{ ").Append(function).Append(' ').Append(keyExpression);
        foreach (string expression in positional)
            builder.Append(' ').Append(expression);
        foreach ((string name, string expression) in named)
            builder.Append(' ').Append(name).Append(": ").Append(expression);
        return builder.Append(" }}").ToString();
    }

    protected override string Render(EngineSetup setup, string template, IReadOnlyDictionary<string, object?> model, CultureInfo? renderCulture)
    {
        var options = new TlumachScribanOptions
        {
            KeyPrefix = setup.KeyPrefix,
            MissingKey = setup.MissingKey,
            OnMissingKey = setup.OnMissingKey,
            FunctionName = setup.FunctionName,
            MarkupFunctionName = setup.MarkupFunctionName,
            HtmlEncode = setup.HtmlOutput,
        };
        return Render(setup.Manager, options, template, model, renderCulture);
    }

    private static string Render(TranslationManager manager, TlumachScribanOptions options, string template, IReadOnlyDictionary<string, object?> model, CultureInfo? renderCulture)
    {
        Template parsed = Template.Parse(template);
        if (parsed.HasErrors)
            throw new InvalidOperationException(string.Join(Environment.NewLine, parsed.Messages));

        var context = new TemplateContext(StringComparer.Ordinal);
        context.PushGlobal(new ScriptObject(StringComparer.Ordinal).ImportTlumach(manager, options));
        context.PushGlobal(ToScriptObject(model));
        if (renderCulture is not null)
            context.PushCulture(renderCulture);

        return parsed.Render(context);
    }

    private static ScriptObject ToScriptObject(IReadOnlyDictionary<string, object?> values)
    {
        var scriptObject = new ScriptObject(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> pair in values)
            scriptObject.SetValue(pair.Key, pair.Value is IReadOnlyDictionary<string, object?> nested ? ToScriptObject(nested) : pair.Value, readOnly: false);
        return scriptObject;
    }
}
