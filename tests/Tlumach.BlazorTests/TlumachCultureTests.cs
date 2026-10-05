// <copyright file="TlumachCultureTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public sealed class TlumachCultureTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public void Get_ReturnsTextOfSnapshotCulture()
    {
        Assert.Equal("Hello", new TlumachCulture(TestTranslations.En).Get(_translations.Hello));
        Assert.Equal("Hallo", new TlumachCulture(TestTranslations.De).Get(_translations.Hello));
    }

    [Fact]
    public void Get_NamedArguments_AreSubstituted()
    {
        string text = new TlumachCulture(TestTranslations.De).Get(_translations.Greeting, new Dictionary<string, object?> { ["name"] = "Anna" });

        Assert.Equal("Hallo, Anna!", text);
    }

    [Fact]
    public void Get_IndexedValues_AreSubstituted()
    {
        Assert.Equal("Element 2 von 5", new TlumachCulture(TestTranslations.De).Get(_translations.Position, 2, 5));
    }

    [Fact]
    public void Get_IcuPlural_UsesCount()
    {
        string text = new TlumachCulture(TestTranslations.En).Get(_translations.Items, new Dictionary<string, object?> { ["count"] = 3 });

        Assert.Equal("3 items", text);
    }

    [Fact]
    public void GetFrom_AnonymousObject_SuppliesNamedValues()
    {
        Assert.Equal("Hello, Anna!", new TlumachCulture(TestTranslations.En).GetFrom(_translations.Greeting, new { name = "Anna" }));
    }

    [Fact]
    public void Get_WebEncodeValues_ReturnsDecodedText()
    {
        _translations.Manager.WebEncodeValues = true;

        Assert.Equal("Click <b>here</b>", new TlumachCulture(TestTranslations.En).Get(_translations.Rich));
    }

    [Fact]
    public void Markup_ReturnsRawText()
    {
        Assert.Equal("Click <b>here</b>", new TlumachCulture(TestTranslations.En).Markup(_translations.Rich).Value);
    }

    [Fact]
    public void Markup_EncodesStringArgs()
    {
        MarkupString markup = new TlumachCulture(TestTranslations.En).Markup(
            _translations.RichGreeting,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = "<script>x</script>" });

        Assert.Equal("Welcome, <b>&lt;script&gt;x&lt;/script&gt;</b>", markup.Value);
    }

    [Fact]
    public void Markup_MarkupStringArg_IsInsertedRaw()
    {
        MarkupString markup = new TlumachCulture(TestTranslations.De).Markup(
            _translations.RichGreeting,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = new MarkupString("<i>Anna</i>") });

        Assert.Equal("Willkommen, <b><i>Anna</i></b>", markup.Value);
    }

    [Fact]
    public void Markup_NonStringArgs_AreFormatted()
    {
        MarkupString markup = new TlumachCulture(TestTranslations.En).Markup(
            _translations.Items,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["count"] = 3 });

        Assert.Equal("3 items", markup.Value);
    }

    [Fact]
    public void GetByKey_MissingKey_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, new TlumachCulture(TestTranslations.En).GetByKey(_translations.Manager, "missing", args: null, values: null));
    }

    [Fact]
    public void GetByKey_NamedArguments_AreSubstituted()
    {
        string text = new TlumachCulture(TestTranslations.De).GetByKey(_translations.Manager, "greeting", new Dictionary<string, object?> { ["name"] = "Anna" }, values: null);

        Assert.Equal("Hallo, Anna!", text);
    }
}
