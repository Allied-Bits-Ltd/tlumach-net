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

using Tlumach.AspNetCore;
using Tlumach.Blazor;
using Tlumach.Extensions.Localization;
using Tlumach.Sample.Blazor.Client;
using Tlumach.Sample.Blazor.Components;
using Tlumach.Sample.Blazor.Translation;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachBlazor(SampleCultures.Configure);

WebApplication app = builder.Build();

app.UseTlumachRequestLocalization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapTlumachCultureEndpoint();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SampleCultures).Assembly);

await app.RunAsync().ConfigureAwait(false);
