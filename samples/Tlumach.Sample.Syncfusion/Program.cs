// <copyright file="Program.cs" company="Allied Bits Ltd.">
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

using Syncfusion.Blazor;
using Syncfusion.Licensing;

using Tlumach;
using Tlumach.AspNetCore;
using Tlumach.Base;
using Tlumach.Blazor;
using Tlumach.Sample.Syncfusion;
using Tlumach.Sample.Syncfusion.Components;
using Tlumach.Syncfusion.Blazor;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// A Syncfusion license key from user secrets or the environment variable Syncfusion__LicenseKey. Without one, the components show a trial banner.
string? licenseKey = builder.Configuration["Syncfusion:LicenseKey"];
if (!string.IsNullOrWhiteSpace(licenseKey))
    SyncfusionLicenseProvider.RegisterLicense(licenseKey);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSyncfusionBlazor();

builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("uk-UA")];
    options.DefaultCulture = options.SupportedCultures[0];
    options.DefaultManager = Strings.TranslationManager;
    options.Persistence = TlumachCulturePersistence.Cookie;
});

// The texts of Syncfusion components come from the Syncfusion group of Strings*.json. The official files of Syncfusion, when downloaded into SyncfusionLocale
// (see SyncfusionLocale/README.md), provide the texts that the group lacks.
TranslationManager? officialTexts = null;
string localeDirectory = Path.Combine(builder.Environment.ContentRootPath, "SyncfusionLocale");
if (File.Exists(Path.Combine(localeDirectory, "SfResources.resx")))
{
    IniParser.Use();
    ResxParser.Use();
    officialTexts = new TranslationManager(Path.Combine(localeDirectory, "Syncfusion.cfg")) { LoadFromDisk = true, TranslationsDirectory = localeDirectory };
    builder.Services.AddSingleton(officialTexts);
}

builder.Services.AddTlumachSyncfusionBlazor(options => options.FallbackTranslationManager = officialTexts);

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapTlumachCultureEndpoint();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync().ConfigureAwait(false);
