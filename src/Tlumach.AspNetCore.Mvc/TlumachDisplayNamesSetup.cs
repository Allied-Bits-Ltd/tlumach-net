// <copyright file="TlumachDisplayNamesSetup.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Adds <see cref="TlumachDisplayMetadataProvider"/> to <see cref="MvcOptions.ModelMetadataDetailsProviders"/>.
/// </summary>
internal sealed class TlumachDisplayNamesSetup : IConfigureOptions<MvcOptions>
{
    private readonly TlumachDisplayNameOptions _options;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;

    public TlumachDisplayNamesSetup(TlumachDisplayNameOptions options, IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _options = options;
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public void Configure(MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        RetryingLazy<TemplateTranslator> translator = new(() => new TemplateTranslator(DefaultManager.Resolve(_services, _options.TranslationManager)));
        options.ModelMetadataDetailsProviders.Add(new TlumachDisplayMetadataProvider(_options, translator, _loggerFactory.CreateLogger<TlumachDisplayMetadataProvider>()));
    }
}
