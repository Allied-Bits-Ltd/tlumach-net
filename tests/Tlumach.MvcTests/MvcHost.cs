// <copyright file="MvcHost.cs" company="Allied Bits Ltd.">
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
using Tlumach.AspNetCore.Mvc;
using Tlumach.AspNetCore.Testing;
using Tlumach.Extensions.Localization;
using Tlumach.Web;

namespace Tlumach.MvcTests;

/// <summary>
/// Starts an MVC application (controllers and views only, no Razor Pages) with Tlumach registered.
/// </summary>
internal static class MvcHost
{
    public static Task<WebApplication> StartAsync(TestTranslations translations, Action<IMvcBuilder>? configureMvc = null, Action<TlumachSettingsProvider>? perContext = null)
        => TestHost.StartAsync(
            builder =>
            {
                builder.Services.AddTlumachLocalization(o => o.TranslationManager = translations.Manager, perContext);
                builder.Services.AddTlumachCultures(o => o.SupportedCultures = [TestTranslations.En, TestTranslations.De, TestTranslations.Uk]);
                IMvcBuilder mvc = builder.Services.AddControllersWithViews().AddTlumachViewLocalization();
                configureMvc?.Invoke(mvc);
            },
            app =>
            {
                app.UseTlumachRequestLocalization();
                app.MapTlumachCultureEndpoint();
                app.MapControllers();
                app.MapDefaultControllerRoute();
            });
}
