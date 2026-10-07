// <copyright file="TlumachDisplayMetadataProvider.cs" company="Allied Bits Ltd.">
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

using System.Collections.Concurrent;
using System.Globalization;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;

using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Gives properties without a display name a name from Tlumach: "{KeyPrefix}{container key}.{property}", then "{KeyPrefix}{property}".
/// The keys are formed once per property; the lookup runs whenever MVC asks for the name, in the culture of that moment.
/// </summary>
internal sealed class TlumachDisplayMetadataProvider : IDisplayMetadataProvider
{
    private readonly TlumachDisplayNameOptions _options;
    private readonly RetryingLazy<TemplateTranslator> _translator;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<(string Key, string Culture), byte> _reported = new();

    public TlumachDisplayMetadataProvider(TlumachDisplayNameOptions options, RetryingLazy<TemplateTranslator> translator, ILogger<TlumachDisplayMetadataProvider> logger)
    {
        _options = options;
        _translator = translator;
        _logger = logger;
    }

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ModelMetadataIdentity key = context.Key;
        if (key.MetadataKind != ModelMetadataKind.Property || key.ContainerType is null || string.IsNullOrEmpty(key.Name))
            return;

        // The DataAnnotations provider sets the name of [Display] and [DisplayName] whether it runs before or after this provider, so they always win.
        if (context.DisplayMetadata.DisplayName is not null)
            return;

        Type container = key.ContainerType;
        string containerKey = _options.ContainerKey?.Invoke(container)
            ?? TlumachDisplayNameKeys.GetContainerKey(container, _options.KeyStyle, _options.RootNamespace(container.Assembly));
        string specific = _options.KeyPrefix + containerKey + "." + key.Name;
        string shared = _options.KeyPrefix + key.Name;

        context.DisplayMetadata.DisplayName = () => Lookup(specific, shared);
    }

    private string? Lookup(string specific, string shared)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        TemplateTranslator translator = _translator.Value;
        if (translator.TryTranslate(specific, TemplateArguments.Empty, culture, out string text) || translator.TryTranslate(shared, TemplateArguments.Empty, culture, out text))
            return text;

        if (_logger.IsEnabled(LogLevel.Debug) && _reported.TryAdd((specific, culture.Name), 0))
            _logger.LogDebug("No display name was found for the keys '{SpecificKey}' and '{SharedKey}' in the culture '{Culture}'; the property name is used.", specific, shared, culture.Name);

        return null;
    }
}
