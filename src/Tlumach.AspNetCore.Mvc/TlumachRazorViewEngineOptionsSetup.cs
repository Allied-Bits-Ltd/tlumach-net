// <copyright file="TlumachRazorViewEngineOptionsSetup.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Adds <see cref="LanguageViewLocationExpander"/> unless an expander of that type is already present (e.g. from <c>AddViewLocalization</c>).
/// This runs after all <c>Configure</c> calls, because <c>AddViewLocalization</c> adds its expander unconditionally and may be registered after Tlumach.
/// </summary>
internal sealed class TlumachRazorViewEngineOptionsSetup : IPostConfigureOptions<RazorViewEngineOptions>
{
    private readonly TlumachViewLocalizationOptions _options;

    public TlumachRazorViewEngineOptionsSetup(TlumachViewLocalizationOptions options)
    {
        _options = options;
    }

    public void PostConfigure(string? name, RazorViewEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_options.ViewLocationExpanderFormat is { } format && !options.ViewLocationExpanders.OfType<LanguageViewLocationExpander>().Any())
            options.ViewLocationExpanders.Add(new LanguageViewLocationExpander(format));
    }
}
