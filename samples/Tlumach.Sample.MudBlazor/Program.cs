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

using MudBlazor.Services;

using Tlumach;
using Tlumach.AspNetCore;
using Tlumach.Base;
using Tlumach.Blazor;
using Tlumach.MudBlazor;
using Tlumach.Sample.MudBlazor;
using Tlumach.Sample.MudBlazor.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddTlumachBlazor(options =>
{
    options.SupportedCultures = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("uk-UA")];
    options.DefaultCulture = options.SupportedCultures[0];
    options.DefaultManager = Strings.TranslationManager;
    options.Persistence = TlumachCulturePersistence.Cookie;
});

// The texts of MudBlazor live in their own translation set made of .resx files (see Translations/MudBlazor.cfg), with the keys at the root.
// The parsers are registered explicitly, because no class created by Generator loads this set.
IniParser.Use();
ResxParser.Use();
TranslationManager mudBlazorTexts = new(typeof(Program).Assembly, "Translations/MudBlazor.cfg");
builder.Services.AddSingleton(mudBlazorTexts);
builder.Services.AddTlumachMudBlazor(options =>
{
    options.TranslationManager = mudBlazorTexts;
    options.Group = null;
});

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapTlumachCultureEndpoint();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync().ConfigureAwait(false);
