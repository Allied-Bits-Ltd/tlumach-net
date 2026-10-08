// <copyright file="FormHostTests.cs" company="Allied Bits Ltd.">
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
using Microsoft.Extensions.DependencyInjection;

using Tlumach.AspNetCore.Mvc;
using Tlumach.AspNetCore.Testing;

namespace Tlumach.MvcTests;

public sealed class FormHostTests : IDisposable
{
    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    private static Dictionary<string, string> InvalidForm() => new(StringComparer.Ordinal) { ["Email"] = string.Empty, ["Age"] = "abc" };

    [Fact]
    public async Task BindingErrors_AreTranslated_PerRequestCulture()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.AddTlumachModelBindingMessages());

        string german = await (await TestHost.PostFormAsync(app, "/Account/Register", InvalidForm(), TestTranslations.De)).Content.ReadAsStringAsync();
        string ukrainian = await (await TestHost.PostFormAsync(app, "/Account/Register", InvalidForm(), TestTranslations.Uk)).Content.ReadAsStringAsync();

        Assert.Contains("Der Wert 'abc' ist für Age ungültig.", HtmlAssert.Text(german, "errors"), StringComparison.Ordinal);
        Assert.Contains("Значення 'abc' недійсне для Age.", HtmlAssert.Text(ukrainian, "errors"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisplayNames_AppearInLabelsAndMessages()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.AddTlumachModelBindingMessages().AddTlumachDisplayNames());

        string page = await TestHost.GetStringAsync(app, "/Account/Register", TestTranslations.De);
        string posted = await (await TestHost.PostFormAsync(app, "/Account/Register", InvalidForm(), TestTranslations.De)).Content.ReadAsStringAsync();

        Assert.Equal("Ihre E-Mail", HtmlAssert.Text(page, "email-label"));
        Assert.Equal("Ihr Alter", HtmlAssert.Text(page, "age-label"));
        Assert.Equal("Phone", HtmlAssert.Text(page, "phone-label"));
        Assert.Equal("Mail", HtmlAssert.Text(page, "backup-label"));
        string errors = HtmlAssert.Text(posted, "errors");
        Assert.Contains("Der Wert 'abc' ist für Ihr Alter ungültig.", errors, StringComparison.Ordinal);
        Assert.Contains("Ihre E-Mail", errors, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DataAnnotationsLocalization_FillsTheDefaultMessage_OfAnAttributeWithoutErrorMessage()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.AddDataAnnotationsLocalization().AddTlumachDisplayNames());

        // The default message of [EmailAddress] is the key; there is no translation for it, so the localizer formats it with the display name.
        Dictionary<string, string> form = new(StringComparer.Ordinal) { ["Email"] = "not-an-address", ["Age"] = "30" };
        string posted = await (await TestHost.PostFormAsync(app, "/Account/Register", form, TestTranslations.En)).Content.ReadAsStringAsync();

        string errors = HtmlAssert.Text(posted, "errors");
        Assert.Contains("The Your e-mail field is not a valid e-mail address.", errors, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", errors, StringComparison.Ordinal);
    }
}
