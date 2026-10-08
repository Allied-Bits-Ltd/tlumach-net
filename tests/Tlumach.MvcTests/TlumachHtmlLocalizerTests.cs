// <copyright file="TlumachHtmlLocalizerTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Tlumach.AspNetCore.Mvc;
using Tlumach.Extensions.Localization;

namespace Tlumach.MvcTests;

public sealed class TlumachHtmlLocalizerTests : IDisposable
{
    private readonly TestTranslations _translations = new();
    private readonly CultureInfo _savedCulture = CultureInfo.CurrentCulture;
    private readonly ServiceProvider _services;
    private readonly IHtmlLocalizer _localizer;

    public TlumachHtmlLocalizerTests()
    {
        CultureInfo.CurrentCulture = TestTranslations.En;
        ServiceCollection services = new();
        services.AddTlumachLocalization(o => o.TranslationManager = _translations.Manager);
        _services = services.BuildServiceProvider();
        _localizer = CreateFactory().Create(typeof(TlumachHtmlLocalizerTests));
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _savedCulture;
        _translations.Manager.WebEncodeValues = false;
        _services.Dispose();
        _translations.Dispose();
    }

    internal static string Render(IHtmlContent content)
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private TlumachHtmlLocalizerFactory CreateFactory()
        => new(_services.GetRequiredService<ITlumachSettingsProvider>(), HtmlEncoder.Default, new TlumachViewLocalizationOptions());

    [Fact]
    public void Template_IsInsertedAsHtml()
        => Assert.Equal("Click <b>here</b>", Render(_localizer["rich"]));

    [Fact]
    public void StringArgument_IsEncoded()
        => Assert.Equal("Welcome, <b>&lt;script&gt;</b>", Render(_localizer["richGreeting", "<script>"]));

    [Fact]
    public void NamedArguments_FromDictionary()
        => Assert.Equal("Hello, A&amp;B!", Render(_localizer["greeting", new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "A&B" }]));

    [Fact]
    public void HtmlContentArgument_IsInsertedAsIs()
        => Assert.Equal("Welcome, <b><i>Bob</i></b>", Render(_localizer["richGreeting", new HtmlString("<i>Bob</i>")]));

    [Fact]
    public void LocalizedHtmlStringArgument_IsInsertedAsIs()
        => Assert.Equal("Welcome, <b>Click <b>here</b></b>", Render(_localizer["richGreeting", _localizer["rich"]]));

    [Fact]
    public void IcuPlural_UsesPositionalValue()
        => Assert.Equal("2 items", Render(_localizer["items", 2]));

    [Fact]
    public void Date_IsFormattedForTheCurrentCulture()
    {
        DateTime date = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
        // {date, date} uses the ICU medium style by default, from the CLDR-based table of Tlumach (not from the ICU data of the platform).
        Assert.Equal("Due Jan 2, 2026", Render(_localizer["due", date]));

        CultureInfo.CurrentCulture = TestTranslations.De;
        Assert.Equal("Fällig am 02.01.2026", Render(_localizer["due", date]));
    }

    [Fact]
    public void Lookup_FollowsTheCurrentCulture()
    {
        CultureInfo.CurrentCulture = TestTranslations.Uk;
        Assert.Equal("Привіт", Render(_localizer["hello"]));
    }

    [Fact]
    public void Braces_AreEscapedInValue_AndWrittenExactly()
    {
        LocalizedHtmlString result = _localizer["greeting", "{x}"];

        Assert.Equal("Hello, {{x}}!", result.Value);
        Assert.Equal("Hello, {x}!", Render(result));
    }

    [Fact]
    public void MissingKey_ReturnsEncodedKey_AndIsResourceNotFound()
    {
        LocalizedHtmlString result = _localizer["no.such<key>"];

        Assert.True(result.IsResourceNotFound);
        Assert.Equal("no.such&lt;key&gt;", Render(result));
        Assert.Equal("no.such<key>", result.Name);
    }

    [Fact]
    public void WebEncodeValues_DoesNotChangeTheHtml()
    {
        string off = Render(_localizer["richGreeting", "<x>"]);
        _translations.Manager.WebEncodeValues = true;

        Assert.Equal(off, Render(_localizer["richGreeting", "<x>"]));
    }

    [Fact]
    public void GetString_MatchesTheStringLocalizer()
    {
        var strings = new TlumachStringLocalizerFactory(_services.GetRequiredService<ITlumachSettingsProvider>()).Create(typeof(TlumachHtmlLocalizerTests));

        Assert.Equal(strings["greeting", "<b>"].Value, _localizer.GetString("greeting", "<b>").Value);
        Assert.Equal(strings["rich"].Value, _localizer.GetString("rich").Value);
        Assert.True(_localizer.GetString("missing").ResourceNotFound);
    }

    [Fact]
    public void GetString_FormatsAMissingKey_WithTheArguments()
    {
        // The view lookup tries the prefixed key first ("Views.Home.Index."); the result carries the unprefixed key, formatted as the string localizer formats it.
        LocalizedString fromLocalizer = _localizer.GetString("The {0} field is required.", "Email");
        LocalizedString fromView = CreateFactory().GetViewLookup("/Views/Home/Index.cshtml").GetString("The {0} field is required.", ["Email"]);

        foreach (LocalizedString result in new[] { fromLocalizer, fromView })
        {
            Assert.Equal("The {0} field is required.", result.Name);
            Assert.Equal("The Email field is required.", result.Value);
            Assert.True(result.ResourceNotFound);
        }
    }

    [Fact]
    public void GenericLocalizer_UsesTheFactory()
    {
        IHtmlLocalizer<TlumachHtmlLocalizerTests> localizer = new TlumachHtmlLocalizer<TlumachHtmlLocalizerTests>(CreateFactory());

        Assert.Equal("Click <b>here</b>", Render(localizer["rich"]));
    }
}
