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
}
