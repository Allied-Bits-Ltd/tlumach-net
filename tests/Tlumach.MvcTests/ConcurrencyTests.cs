// <copyright file="ConcurrencyTests.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Builder;

using Tlumach.AspNetCore.Mvc;
using Tlumach.AspNetCore.Testing;

namespace Tlumach.MvcTests;

public sealed class ConcurrencyTests : IDisposable
{
    private static readonly (CultureInfo Culture, string Title, string Footer, string Error)[] Expected =
    [
        (TestTranslations.En, "Home page", "Layout footer", "The value 'abc' is not valid for Your age."),
        (TestTranslations.De, "Startseite", "Fußzeile", "Der Wert 'abc' ist für Ihr Alter ungültig."),
        (TestTranslations.Uk, "Головна сторінка", "Нижній колонтитул", "Значення 'abc' недійсне для Ваш вік."),
    ];

    private readonly TestTranslations _translations = new();

    public void Dispose() => _translations.Dispose();

    [Fact]
    public async Task ParallelRequests_InDifferentCultures_DoNotMix()
    {
        await using WebApplication app = await MvcHost.StartAsync(_translations, mvc => mvc.AddTlumachModelBindingMessages().AddTlumachDisplayNames());

        IEnumerable<Task> requests = Enumerable.Range(0, 200).Select(async i =>
        {
            var expected = Expected[i % Expected.Length];
            if (i % 2 == 0)
            {
                string html = await TestHost.GetStringAsync(app, "/Home/Index", expected.Culture);
                Assert.Equal(expected.Title, HtmlAssert.InnerHtml(html, "title"));
                Assert.Equal(expected.Footer, HtmlAssert.InnerHtml(html, "footer"));
            }
            else
            {
                using HttpResponseMessage response = await TestHost.PostFormAsync(app, "/Account/Register", new Dictionary<string, string>(StringComparer.Ordinal) { ["Email"] = "a@b.c", ["Age"] = "abc" }, expected.Culture);
                Assert.Contains(expected.Error, HtmlAssert.Text(await response.Content.ReadAsStringAsync(), "errors"), StringComparison.Ordinal);
            }
        });

        await Task.WhenAll(requests);
    }
}
