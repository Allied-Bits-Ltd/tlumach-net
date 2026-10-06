// <copyright file="TemplateEngineScenarios.cs" company="Allied Bits Ltd.">
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

using Tlumach.Templating;

namespace Tlumach.TemplateEngineTests;

/// <summary>
/// The behavior that every template engine integration must have. A subclass per engine supplies the syntax of a call and the rendering, and xUnit runs these tests for each subclass.
/// </summary>
public abstract class TemplateEngineScenarios : IDisposable
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("uk");
    private static readonly Dictionary<string, object?> NoModel = new(StringComparer.Ordinal);

    private readonly List<IDisposable> _disposables = [];

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected static string Literal(string text) => "\"" + text + "\"";

    protected static Dictionary<string, object?> User(object? name)
        => new(StringComparer.Ordinal) { ["user"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = name } };

    /// <summary>
    /// Builds one call of a translation function in the syntax of the engine.
    /// </summary>
    /// <param name="function">The name of the function, filter, or helper.</param>
    /// <param name="keyExpression">A template expression for the key: a quoted literal (see <see cref="Literal"/>) or a variable.</param>
    /// <param name="positional">Template expressions for the positional values.</param>
    /// <param name="named">Names and template expressions for the named values.</param>
    /// <returns>The template text.</returns>
#pragma warning disable CA1716 // The names are the contract between this class and its subclasses, and they are not meant to be consumed from other languages.
    protected abstract string Call(string function, string keyExpression, IReadOnlyList<string> positional, IReadOnlyList<(string Name, string Expression)> named);
#pragma warning restore CA1716

    /// <summary>
    /// Renders the template with the integration registered according to <paramref name="setup"/>.
    /// </summary>
    /// <param name="setup">The configuration of the integration.</param>
    /// <param name="template">The template text.</param>
    /// <param name="model">The variables of the template; a nested dictionary is an object.</param>
    /// <param name="renderCulture">The culture of the render as the engine expresses it, or <see langword="null"/>.</param>
    /// <returns>The rendered text.</returns>
#pragma warning disable CA1716 // See above.
    protected abstract string Render(EngineSetup setup, string template, IReadOnlyDictionary<string, object?> model, CultureInfo? renderCulture);
#pragma warning restore CA1716

    /// <summary>
    /// Creates a translation manager over the embedded test data and disposes it after the test.
    /// </summary>
    /// <param name="config">The embedded configuration file.</param>
    /// <returns>The translation manager.</returns>
    protected TranslationManager CreateManager(string config = "TestData/Strings.jsoncfg")
        => Track(TestTranslations.CreateManager(config));

    /// <summary>
    /// Creates the translation unit of the key and disposes it after the test.
    /// </summary>
    /// <param name="manager">The translation manager that owns the unit.</param>
    /// <param name="key">The key of the translation.</param>
    /// <returns>The translation unit.</returns>
    protected TranslationUnit CreateUnit(TranslationManager manager, string key)
    {
        ArgumentNullException.ThrowIfNull(manager);

        return Track(TestTranslations.Unit(manager, key));
    }

    protected string T(string key, params (string Name, string Expression)[] named) => Call("t", Literal(key), [], named);

    [Fact]
    public void LooksUpKey()
    {
        Assert.Equal("Welcome!", Render(new EngineSetup(CreateManager()), T("welcome"), NoModel, renderCulture: null));
    }

    [Fact]
    public void LooksUpUnit()
    {
        TranslationManager manager = CreateManager();
        var model = new Dictionary<string, object?>(StringComparer.Ordinal) { ["title"] = CreateUnit(manager, "welcome") };

        Assert.Equal("Willkommen!", Render(new EngineSetup(manager), Call("t", "title", [], []), model, De));
    }

    [Fact]
    public void AppliesKeyPrefix()
    {
        var setup = new EngineSetup(CreateManager()) { KeyPrefix = "email." };

        Assert.Equal("Order 42", Render(setup, T("subject", ("id", Literal("42"))), NoModel, renderCulture: null));
    }

    [Fact]
    public void ExplicitCultureWins()
    {
        string template = T("welcome", ("culture", Literal("uk")));

        Assert.Equal("Ласкаво просимо!", Render(new EngineSetup(CreateManager()), template, NoModel, De));
    }

    [Fact]
    public void UsesRenderCulture()
    {
        Assert.Equal("Willkommen!", Render(new EngineSetup(CreateManager()), T("welcome"), NoModel, De));
    }

    [Fact]
    public void UsesManagerCultureWithoutRenderCulture()
    {
        TranslationManager manager = CreateManager();
        manager.CurrentCulture = De;

        Assert.Equal("Willkommen!", Render(new EngineSetup(manager), T("welcome"), NoModel, renderCulture: null));
    }

    [Fact]
    public void TreatsInvariantRenderCultureAsNotSet()
    {
        TranslationManager manager = CreateManager();
        manager.CurrentCulture = De;

        Assert.Equal("Willkommen!", Render(new EngineSetup(manager), T("welcome"), NoModel, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FillsNamedPlaceholder()
    {
        string template = T("greeting", ("name", "user.name"));

        Assert.Equal("Hello, Ann!", Render(new EngineSetup(CreateManager()), template, User("Ann"), renderCulture: null));
    }

    [Fact]
    public void FillsIndexedPlaceholders()
    {
        // Indexed placeholders need the DotNet text processing mode; Arb mode does not accept {0}.
        string template = Call("t", Literal("pair"), ["user.name", Literal("Bob")], []);
        var setup = new EngineSetup(CreateManager("TestData/Indexed.jsoncfg"));

        Assert.Equal("Ann and Bob", Render(setup, template, User("Ann"), renderCulture: null));
    }

    [Fact]
    public void FillsMixedPlaceholders()
    {
        string template = Call("t", Literal("order"), ["user.name"], [("count", "3")]);

        Assert.Equal("Ann ordered 3 items.", Render(new EngineSetup(CreateManager()), template, User("Ann"), renderCulture: null));
    }

    [Theory]
    [InlineData("en", 1, "1 item")]
    [InlineData("en", 3, "3 items")]
    [InlineData("de", 1, "1 Position")]
    [InlineData("de", 3, "3 Positionen")]
    public void FormatsPlural(string culture, int count, string expected)
    {
        var model = new Dictionary<string, object?>(StringComparer.Ordinal) { ["count"] = count };

        string actual = Render(new EngineSetup(CreateManager()), T("items", ("count", "count")), model, CultureInfo.GetCultureInfo(culture));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatsPluralWithDecimalCount()
    {
        var model = new Dictionary<string, object?>(StringComparer.Ordinal) { ["count"] = 3m };

        Assert.Equal("3 items", Render(new EngineSetup(CreateManager()), T("items", ("count", "count")), model, renderCulture: null));
    }

    [Fact]
    public void FormatsSelect()
    {
        var model = new Dictionary<string, object?>(StringComparer.Ordinal) { ["gender"] = "female" };

        Assert.Equal("She replied.", Render(new EngineSetup(CreateManager()), T("reply", ("gender", "gender")), model, renderCulture: null));
    }

    [Fact]
    public void RendersNullValueAsEmpty()
    {
        string template = T("greeting", ("name", "user.name"));

        Assert.Equal("Hello, !", Render(new EngineSetup(CreateManager()), template, User(null), renderCulture: null));
    }

    [Fact]
    public void RendersCulturesConcurrently()
    {
        var setup = new EngineSetup(CreateManager());
        string template = T("greeting", ("name", "user.name"));
        CultureInfo[] cultures = [En, De, Uk];
        string[] expected = ["Hello, Ann!", "Hallo, Ann!", "Привіт, Ann!"];
        var results = new string[150];

        Parallel.For(0, results.Length, i => results[i] = Render(setup, template, User("Ann"), cultures[i % 3]));

        for (int i = 0; i < results.Length; i++)
            Assert.Equal(expected[i % 3], results[i]);
    }

    [Fact]
    public void EscapesTInHtmlOutput()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = true };

        Assert.Equal("Hello, &lt;Ann&gt;!", Render(setup, T("greeting", ("name", "user.name")), User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void DoesNotEscapeTInPlainOutput()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = false };

        Assert.Equal("Hello, <Ann>!", Render(setup, T("greeting", ("name", "user.name")), User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void DoesNotEncodeTwiceWithWebEncodeValues()
    {
        TranslationManager manager = CreateManager();
        manager.WebEncodeValues = true;
        var setup = new EngineSetup(manager) { HtmlOutput = true };

        Assert.Equal("Hello, &lt;Ann&gt;!", Render(setup, T("greeting", ("name", "user.name")), User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void MarkupEncodesValuesButNotTranslation()
    {
        var setup = new EngineSetup(CreateManager()) { HtmlOutput = true };
        string template = Call("t_html", Literal("markup"), [], [("name", "user.name"), ("count", "3")]);

        Assert.Equal("<b>&lt;Ann&gt;</b> ordered 3 items.", Render(setup, template, User("<Ann>"), renderCulture: null));
    }

    [Fact]
    public void MissingKeyReturnsKey()
    {
        var setup = new EngineSetup(CreateManager()) { KeyPrefix = "email." };

        Assert.Equal("email.nope", Render(setup, T("nope"), NoModel, renderCulture: null));
    }

    [Fact]
    public void MissingKeyCanBeEmpty()
    {
        var setup = new EngineSetup(CreateManager()) { MissingKey = MissingKeyBehavior.Empty };

        Assert.Equal(string.Empty, Render(setup, T("nope"), NoModel, renderCulture: null));
    }

    [Fact]
    public void MissingKeyCanThrow()
    {
        var setup = new EngineSetup(CreateManager()) { MissingKey = MissingKeyBehavior.Throw };

        Exception exception = Assert.ThrowsAny<Exception>(() => Render(setup, T("nope"), NoModel, renderCulture: null));

        Assert.Equal("nope", ExceptionChain.Find<TemplateKeyNotFoundException>(exception).Key);
    }

    [Fact]
    public void MissingKeyCallbackSuppliesText()
    {
        var setup = new EngineSetup(CreateManager()) { OnMissingKey = (key, culture) => $"[{key}:{culture.Name}]" };

        Assert.Equal("[nope:de]", Render(setup, T("nope"), NoModel, De));
    }

    [Fact]
    public void UsesCustomFunctionName()
    {
        var setup = new EngineSetup(CreateManager()) { FunctionName = "tr", MarkupFunctionName = null };

        Assert.Equal("Welcome!", Render(setup, Call("tr", Literal("welcome"), [], []), NoModel, renderCulture: null));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        foreach (IDisposable disposable in _disposables)
            disposable.Dispose();

        _disposables.Clear();
    }

    private TDisposable Track<TDisposable>(TDisposable disposable)
        where TDisposable : IDisposable
    {
        lock (_disposables)
            _disposables.Add(disposable);

        return disposable;
    }
}
