// <copyright file="HostSmokeTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.AspNetCore;
using Tlumach.AspNetCore.Testing;
using Tlumach.Base;
using Tlumach.Web;

namespace Tlumach.MvcTests;

public class HostSmokeTests
{
    [Fact]
    public void NestedObjects_ProduceDottedKeys()
    {
        using TestTranslations translations = new();

        TranslationEntry entry = translations.Manager.GetValue(translations.Configuration, "Views.Home.Index.Title", TestTranslations.De);

        Assert.Equal("Startseite", entry.Text);
    }

    [Fact]
    public async Task CompiledView_RendersInTheCultureOfTheCookie()
    {
        await using WebApplication app = await TestHost.StartAsync(
            builder =>
            {
                builder.Services.AddTlumachCultures(o => o.SupportedCultures = [TestTranslations.En, TestTranslations.De]);
                builder.Services.AddControllersWithViews();
            },
            app =>
            {
                app.UseTlumachRequestLocalization();
                app.MapDefaultControllerRoute();
            });

        string html = await TestHost.GetStringAsync(app, "/Home/Ping", TestTranslations.De);

        Assert.Contains("<p id=\"culture\">de-DE</p>", html, StringComparison.Ordinal);
    }
}
