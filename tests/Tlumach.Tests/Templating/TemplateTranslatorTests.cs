// <copyright file="TemplateTranslatorTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;
using Tlumach.Templating;

namespace Tlumach.Tests.Templating;

public sealed class TemplateTranslatorTests : IDisposable
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");

    private readonly List<TranslationManager> _managers = [];

    public void Dispose()
    {
        foreach (TranslationManager manager in _managers)
            manager.Dispose();

        GC.SuppressFinalize(this);
    }

    private static TemplateArguments Named(params (string Name, object? Value)[] values)
    {
        var arguments = new TemplateArguments();
        foreach ((string name, object? value) in values)
            arguments.AddNamed(name, value);
        return arguments;
    }

    private static TemplateArguments Positional(params object?[] values)
    {
        var arguments = new TemplateArguments();
        foreach (object? value in values)
            arguments.AddPositional(value);
        return arguments;
    }

    private static TranslationUnit Unit(TranslationManager manager, string key)
        => new(manager, manager.DefaultConfiguration!, key, containsPlaceholders: true);

    private TranslationManager CreateManager(string config = "TestData/Templating/Strings.jsoncfg")
    {
        JsonParser.Use();
        var manager = new TranslationManager(typeof(TemplateTranslatorTests).Assembly, config) { LoadFromDisk = false, CurrentCulture = En };
        _managers.Add(manager);
        return manager;
    }

    [Fact]
    public void ExplicitCultureInfoWins()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Same(De, translator.ResolveCulture(De, CultureInfo.GetCultureInfo("uk")));
    }

    [Fact]
    public void ExplicitCultureNameWins()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("de", translator.ResolveCulture("de", CultureInfo.GetCultureInfo("uk")).Name);
    }

    [Fact]
    public void EmptyCultureNameMeansNotGiven()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Same(De, translator.ResolveCulture(string.Empty, De));
    }

    [Fact]
    public void InvalidCultureNameThrows()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Throws<CultureNotFoundException>(() => translator.ResolveCulture("no such culture", renderCulture: null));
    }

    [Fact]
    public void CultureOfUnsupportedTypeThrows()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Throws<ArgumentException>(() => translator.ResolveCulture(42, renderCulture: null));
    }

    [Fact]
    public void RenderCultureIsUsedWhenNoExplicitCulture()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Same(De, translator.ResolveCulture(explicitCulture: null, De));
    }

    [Fact]
    public void InvariantRenderCultureMeansNotSet()
    {
        TranslationManager manager = CreateManager();
        manager.CurrentCulture = De;
        var translator = new TemplateTranslator(manager);

        Assert.Equal("de", translator.ResolveCulture(explicitCulture: null, CultureInfo.InvariantCulture).Name);
    }

    [Fact]
    public void ManagerCultureIsTheLastResort()
    {
        TranslationManager manager = CreateManager();
        manager.CurrentCulture = De;
        var translator = new TemplateTranslator(manager);

        Assert.Equal("de", translator.ResolveCulture(explicitCulture: null, renderCulture: null).Name);
    }

    [Fact]
    public void TranslatesKey()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Willkommen!", translator.Translate("welcome", TemplateArguments.Empty, De));
    }

    [Fact]
    public void AppliesKeyPrefix()
    {
        var translator = new TemplateTranslator(CreateManager(), new TemplateTranslationOptions { KeyPrefix = "email." });

        Assert.Equal("Order 42", translator.Translate("subject", Named(("id", "42")), En));
    }

    [Fact]
    public void UsesConfiguredConfiguration()
    {
        TranslationManager other = CreateManager("TestData/Templating/Other.jsoncfg");
        var translator = new TemplateTranslator(CreateManager(), new TemplateTranslationOptions { Configuration = other.DefaultConfiguration });

        Assert.Equal("Welcome from the other configuration!", translator.Translate("welcome", TemplateArguments.Empty, En));
    }

    [Fact]
    public void FillsNamedPlaceholder()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hallo, Ann!", translator.Translate("greeting", Named(("name", "Ann")), De));
    }

    [Fact]
    public void FillsIndexedPlaceholders()
    {
        var translator = new TemplateTranslator(CreateManager("TestData/Templating/Indexed.jsoncfg"));

        Assert.Equal("Ann and Bob", translator.Translate("pair", Positional("Ann", "Bob"), En));
    }

    [Fact]
    public void NamedValueWinsOverPositionalValue()
    {
        var translator = new TemplateTranslator(CreateManager());
        TemplateArguments arguments = Positional("Positional").AddNamed("name", "Named");

        Assert.Equal("Hello, Named!", translator.Translate("greeting", arguments, En));
    }

    [Fact]
    public void FillsNamedPlaceholderFromPositionalValue()
    {
        var translator = new TemplateTranslator(CreateManager());
        TemplateArguments arguments = Positional("Ann").AddNamed("count", 3);

        Assert.Equal("Ann ordered 3 items.", translator.Translate("order", arguments, En));
    }

    [Theory]
    [InlineData("en", 1, "1 item")]
    [InlineData("en", 3, "3 items")]
    [InlineData("de", 1, "1 Position")]
    [InlineData("de", 3, "3 Positionen")]
    public void FormatsPlural(string culture, int count, string expected)
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal(expected, translator.Translate("items", Named(("count", count)), CultureInfo.GetCultureInfo(culture)));
    }

    [Fact]
    public void FormatsSelect()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("She replied.", translator.Translate("reply", Named(("gender", "female")), En));
    }

    [Fact]
    public void NullValueRendersEmpty()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hello, !", translator.Translate("greeting", Named(("name", null)), En));
    }

    [Fact]
    public void IgnoresWebEncodeValues()
    {
        TranslationManager manager = CreateManager();
        manager.WebEncodeValues = true;
        var translator = new TemplateTranslator(manager);

        Assert.Equal("Hello, <Ann>!", translator.Translate("greeting", Named(("name", "<Ann>")), En));
    }

    [Fact]
    public void MissingKeyReturnsFullKeyByDefault()
    {
        var translator = new TemplateTranslator(CreateManager(), new TemplateTranslationOptions { KeyPrefix = "email." });

        Assert.Equal("email.nope", translator.Translate("nope", TemplateArguments.Empty, En));
    }

    [Fact]
    public void MissingKeyCanReturnEmpty()
    {
        var translator = new TemplateTranslator(CreateManager(), new TemplateTranslationOptions { MissingKey = MissingKeyBehavior.Empty });

        Assert.Equal(string.Empty, translator.Translate("nope", TemplateArguments.Empty, En));
    }

    [Fact]
    public void MissingKeyCanThrow()
    {
        var translator = new TemplateTranslator(CreateManager(), new TemplateTranslationOptions { MissingKey = MissingKeyBehavior.Throw });

        var exception = Assert.Throws<TemplateKeyNotFoundException>(() => translator.Translate("nope", TemplateArguments.Empty, De));

        Assert.Equal("nope", exception.Key);
        Assert.Same(De, exception.Culture);
        Assert.IsAssignableFrom<TlumachException>(exception);
    }

    [Fact]
    public void MissingKeyCallbackWinsOverBehavior()
    {
        var options = new TemplateTranslationOptions
        {
            MissingKey = MissingKeyBehavior.Throw,
            OnMissingKey = (key, culture) => $"[{key}:{culture.Name}]",
        };
        var translator = new TemplateTranslator(CreateManager(), options);

        Assert.Equal("[nope:de]", translator.Translate("nope", TemplateArguments.Empty, De));
    }

    [Fact]
    public void MissingKeyCallbackReturningNullFallsBackToBehavior()
    {
        var options = new TemplateTranslationOptions { MissingKey = MissingKeyBehavior.Empty, OnMissingKey = (_, _) => null };
        var translator = new TemplateTranslator(CreateManager(), options);

        Assert.Equal(string.Empty, translator.Translate("nope", TemplateArguments.Empty, En));
    }

    [Fact]
    public void OptionsAreCopiedAtConstruction()
    {
        var options = new TemplateTranslationOptions { KeyPrefix = "email." };
        var translator = new TemplateTranslator(CreateManager(), options);
        options.KeyPrefix = null;
        options.CultureArgumentName = "lang";

        Assert.Equal("Order 1", translator.Translate("subject", Named(("id", 1)), En));
        Assert.Equal("culture", translator.CultureArgumentName);
    }

    [Fact]
    public void NullKeyThrows()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Throws<ArgumentException>(() => translator.Translate(null, TemplateArguments.Empty, En));
    }

    [Fact]
    public void KeyOfUnsupportedTypeThrows()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Throws<ArgumentException>(() => translator.Translate(42, TemplateArguments.Empty, En));
    }

    [Fact]
    public void TranslatesUnit()
    {
        TranslationManager manager = CreateManager();
        var translator = new TemplateTranslator(manager);

        using TranslationUnit unit = Unit(manager, "greeting");

        Assert.Equal("Hallo, Ann!", translator.Translate(unit, Named(("name", "Ann")), De));
    }

    [Fact]
    public void UnitUsesCachedPlaceholderValue()
    {
        TranslationManager manager = CreateManager();
        using TranslationUnit unit = Unit(manager, "greeting");
        unit.CachePlaceholderValue("name", "Ann");
        var translator = new TemplateTranslator(manager);

        Assert.Equal("Hello, Ann!", translator.Translate(unit, TemplateArguments.Empty, En));
        Assert.Equal("Hello, Bob!", translator.Translate(unit, Named(("name", "Bob")), En));
    }

    [Fact]
    public void UnitUsesPlaceholderValueEvent()
    {
        TranslationManager manager = CreateManager();
        using TranslationUnit unit = Unit(manager, "greeting");
        unit.OnPlaceholderValueNeeded += (_, args) => args.Value = "Eve";
        var translator = new TemplateTranslator(manager);

        Assert.Equal("Hello, Eve!", translator.Translate(unit, TemplateArguments.Empty, En));
    }

    [Fact]
    public void TranslatesUntranslatedUnit()
    {
        TranslationManager manager = CreateManager();
        using var unit = new UntranslatedUnit("Hi, {name}!", manager, manager.DefaultConfiguration!, containsPlaceholders: true);
        var translator = new TemplateTranslator(manager);

        Assert.Equal("Hi, Ann!", translator.Translate(unit, Named(("name", "Ann")), De));
    }

    [Fact]
    public void UnitIgnoresKeyPrefix()
    {
        TranslationManager manager = CreateManager();
        var translator = new TemplateTranslator(manager, new TemplateTranslationOptions { KeyPrefix = "email." });

        using TranslationUnit unit = Unit(manager, "welcome");

        Assert.Equal("Welcome!", translator.Translate(unit, TemplateArguments.Empty, En));
    }

    [Fact]
    public void MissingUnitReturnsItsKey()
    {
        TranslationManager manager = CreateManager();
        var translator = new TemplateTranslator(manager);

        using TranslationUnit unit = Unit(manager, "nope");

        Assert.Equal("nope", translator.Translate(unit, TemplateArguments.Empty, En));
    }

    [Fact]
    public void MarkupEncodesStringValuesOnly()
    {
        var translator = new TemplateTranslator(CreateManager());

        string html = translator.TranslateMarkup("markup", Named(("name", "<Ann>"), ("count", 3)), En, HtmlEncoder.Default.Encode);

        Assert.Equal("<b>&lt;Ann&gt;</b> ordered 3 items.", html);
    }

    [Fact]
    public void MarkupInsertsTemplateMarkupAsIs()
    {
        var translator = new TemplateTranslator(CreateManager());

        string html = translator.TranslateMarkup("markup", Named(("name", new TemplateMarkup("<i>Ann</i>")), ("count", 1)), En, HtmlEncoder.Default.Encode);

        Assert.Equal("<b><i>Ann</i></b> ordered 1 item.", html);
    }

    [Fact]
    public void MarkupEncodesOtherObjectsAsText()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hello, &lt;x&gt;!", translator.TranslateMarkup("greeting", Named(("name", new Tagged())), En, HtmlEncoder.Default.Encode));
    }

    [Fact]
    public void MarkupEncodesUri()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hello, https://example.com/?a=1&amp;b=2!", translator.TranslateMarkup("greeting", Named(("name", new Uri("https://example.com/?a=1&b=2"))), En, HtmlEncoder.Default.Encode));
    }

    [Fact]
    public void MarkupEncodesCustomFormattable()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hello, &lt;x&gt;!", translator.TranslateMarkup("greeting", Named(("name", new TaggedFormattable())), En, HtmlEncoder.Default.Encode));
    }

    [Fact]
    public void PlainTextInsertsTemplateMarkupText()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hello, <i>Ann</i>!", translator.Translate("greeting", Named(("name", new TemplateMarkup("<i>Ann</i>"))), En));
    }

    [Fact]
    public void MarkupEncodesMissingKey()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("&lt;nope&gt;", translator.TranslateMarkup("<nope>", TemplateArguments.Empty, En, HtmlEncoder.Default.Encode));
    }

    [Fact]
    public void MarkupFormatsOtherFormattableValuesForTheCallCulture()
    {
        var translator = new TemplateTranslator(CreateManager());

        Assert.Equal("Hallo, &lt;de&gt;!", translator.TranslateMarkup("greeting", Named(("name", new CultureTagged())), De, HtmlEncoder.Default.Encode));
        Assert.Equal("Hello, &lt;en&gt;!", translator.TranslateMarkup("greeting", Named(("name", new CultureTagged())), En, HtmlEncoder.Default.Encode));
    }

    [Fact]
    public void MarkupFormatsNumbersAndDatesForTheCallCultureWithoutEncoding()
    {
        var translator = new TemplateTranslator(CreateManager());
        var date = new DateTime(2025, 3, 14, 15, 9, 26, DateTimeKind.Unspecified);

        string number = translator.TranslateMarkup("greeting", Named(("name", 1.5m)), De, HtmlEncoder.Default.Encode);
        string moment = translator.TranslateMarkup("greeting", Named(("name", date)), De, HtmlEncoder.Default.Encode);

        Assert.Equal(translator.Translate("greeting", Named(("name", 1.5m)), De), number);
        Assert.Contains("1,5", number, StringComparison.Ordinal);
        Assert.Equal(translator.Translate("greeting", Named(("name", date)), De), moment);
    }

    [Fact]
    public void MarkupEncodesValuesSuppliedByTheUnit()
    {
        TranslationManager manager = CreateManager();
        using TranslationUnit unit = Unit(manager, "greeting");
        unit.CachePlaceholderValue("name", "<Ann>");
        var translator = new TemplateTranslator(manager);

        Assert.Equal("Hello, &lt;Ann&gt;!", translator.TranslateMarkup(unit, TemplateArguments.Empty, En, HtmlEncoder.Default.Encode));
    }

    [Fact]
    public void MarkupDoesNotEncodeTwiceWithWebEncodeValues()
    {
        TranslationManager manager = CreateManager();
        manager.WebEncodeValues = true;
        var translator = new TemplateTranslator(manager);

        string html = translator.TranslateMarkup("markup", Named(("name", "<Ann>"), ("count", 3)), En, HtmlEncoder.Default.Encode);

        Assert.Equal("<b>&lt;Ann&gt;</b> ordered 3 items.", html);
    }

    private sealed class CultureTagged : IFormattable
    {
        public override string ToString() => "<invariant>";

        public string ToString(string? format, IFormatProvider? formatProvider) => "<" + ((formatProvider as CultureInfo)?.Name ?? "other") + ">";
    }

    private sealed class TaggedFormattable : IFormattable
    {
        public override string ToString() => "<x>";

        public string ToString(string? format, IFormatProvider? formatProvider) => "<x>";
    }

    private sealed class Tagged
    {
        public override string ToString() => "<x>";
    }
}
