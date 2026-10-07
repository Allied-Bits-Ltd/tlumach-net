// <copyright file="RazorPagesTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.AspNetCore.Testing;

namespace Tlumach.RazorPagesTests;

#pragma warning disable CA1724 // The class is named after the feature it tests, which is also the project's namespace.
public sealed class RazorPagesTests : IDisposable
#pragma warning restore CA1724
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    private static Dictionary<string, string> InvalidContact() => new() { ["Input.Email"] = string.Empty, ["Input.Age"] = "abc" };

    [Fact]
    public async Task TheApplicationHasNoControllers()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);
        await TestHost.GetStringAsync(app, "/", TestTranslations.En);

        IActionDescriptorCollectionProvider actions = app.Services.GetRequiredService<IActionDescriptorCollectionProvider>();
        Assert.Empty(actions.ActionDescriptors.Items.OfType<ControllerActionDescriptor>());
    }

    [Fact]
    public async Task Index_UsesPageKeys_LayoutPartialTagHelpersAndSelector()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);

        string html = await TestHost.GetStringAsync(app, "/", TestTranslations.Uk);

        Assert.Equal("Головна сторінка", HtmlAssert.InnerHtml(html, "title"));
        Assert.Equal("Привіт, <b>&lt;Bob&gt;</b>!", HtmlAssert.InnerHtml(html, "intro"));
        Assert.Equal("Ласкаво просимо (спільне)", HtmlAssert.InnerHtml(html, "welcome"));
        Assert.Equal("Нижній колонтитул", HtmlAssert.InnerHtml(html, "footer"));
        Assert.Equal("Заголовок", HtmlAssert.InnerHtml(html, "header"));
        Assert.Contains("action=\"/tlumach/culture\"", HtmlAssert.InnerHtml(html, "sel"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AreaPage_UsesTheAreaPrefix()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);

        Assert.Equal("Benutzer", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Admin/Users/List", TestTranslations.De), "title"));
    }

    [Fact]
    public async Task PageModel_UsesAStringLocalizerWithTheFullKey()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);

        Assert.Equal("Kontakt", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Contact", TestTranslations.De), "title"));
    }

    [Fact]
    public async Task BoundProperty_GetsTranslatedBindingMessagesAndDisplayNames()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);

        using HttpResponseMessage response = await TestHost.PostFormAsync(app, "/Contact", InvalidContact(), TestTranslations.De);
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal("E-Mail-Adresse", HtmlAssert.Text(html, "email-label"));
        Assert.Equal("Ihr Alter", HtmlAssert.Text(html, "age-label"));
        string errors = HtmlAssert.Text(html, "errors");
        Assert.Contains("Der Wert 'abc' ist für Ihr Alter ungültig.", errors, StringComparison.Ordinal);
        Assert.Contains("E-Mail-Adresse", errors, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SameNamedPageModels_GetDistinctDisplayNames()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);

        Assert.Equal("Filmtitel", HtmlAssert.Text(await TestHost.GetStringAsync(app, "/Movies/Create", TestTranslations.De), "title-label"));
        Assert.Equal("Künstlername", HtmlAssert.Text(await TestHost.GetStringAsync(app, "/Actors/Create", TestTranslations.De), "title-label"));
    }

    [Fact]
    public async Task CultureSpecificPartial_IsPicked_ButACultureSpecificPageIsASeparateRoute()
    {
        await using WebApplication app = await PagesHost.StartAsync(_translations);

        Assert.Equal("Kopfzeile (Datei)", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/", TestTranslations.De), "header"));
        Assert.Equal("variant page", HtmlAssert.InnerHtml(await TestHost.GetStringAsync(app, "/Variant.de", TestTranslations.En), "variant"));
    }
}
