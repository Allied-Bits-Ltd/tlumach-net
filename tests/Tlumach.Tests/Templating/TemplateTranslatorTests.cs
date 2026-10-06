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

using Tlumach.Base;
using Tlumach.Templating;

namespace Tlumach.Tests.Templating;

public class TemplateTranslatorTests
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de");

    internal static TranslationManager CreateManager(string config = "TestData/Templating/Strings.jsoncfg")
    {
        JsonParser.Use();
        return new TranslationManager(typeof(TemplateTranslatorTests).Assembly, config) { LoadFromDisk = false, CurrentCulture = En };
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
}
