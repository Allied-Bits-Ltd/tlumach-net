// <copyright file="TlumachHtmlLocalizerFactory.cs" company="Allied Bits Ltd.">
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
using System.Reflection;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Mvc.Localization;

using Tlumach.Base;
using Tlumach.Extensions.Localization;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Creates the Tlumach HTML localizers. The translation manager of a localizer is found as for <see cref="TlumachStringLocalizerFactory"/>:
/// the options for the context (the full name of the type, or the location and the base name), then the default options, then the static
/// <c>TranslationManager</c> property of a class created by Tlumach Generator.
/// <para>Managers created from options are cached per options instance, so the transient localizers do not load translations again.</para>
/// </summary>
public sealed class TlumachHtmlLocalizerFactory : IHtmlLocalizerFactory
{
    internal const string MissingLocalizationMessage =
        "Tlumach localization is not registered. Call services.AddTlumachLocalization(...) before using the Tlumach HTML or view localizers, tag helpers, model binding messages, or display names.";

    private readonly ITlumachSettingsProvider _settings;
    private readonly TlumachViewLocalizationOptions _options;
    private readonly ConcurrentDictionary<TlumachLocalizationOptions, ManagerEntry> _byOptions = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<Type, ManagerEntry> _byType = new();
    private readonly ConcurrentDictionary<string, ManagerEntry> _byBaseName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TranslationLookup> _viewLookups = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachHtmlLocalizerFactory"/> class.
    /// </summary>
    /// <param name="settingsProvider">The options registered by <c>AddTlumachLocalization</c>.</param>
    /// <param name="encoder">The encoder of placeholder values.</param>
    /// <param name="options">The options of the view localization.</param>
    public TlumachHtmlLocalizerFactory(ITlumachSettingsProvider settingsProvider, HtmlEncoder encoder, TlumachViewLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(settingsProvider);
        ArgumentNullException.ThrowIfNull(encoder);
        ArgumentNullException.ThrowIfNull(options);

        _settings = settingsProvider;
        Encoder = encoder;
        _options = options;
    }

    internal HtmlEncoder Encoder { get; }

    /// <inheritdoc/>
    public IHtmlLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        TlumachLocalizationOptions options = _settings.GetOptionsFor(resourceSource.FullName ?? resourceSource.Name);
        ManagerEntry entry = TranslationManagerResolver.HasManagerSource(options)
            ? GetEntry(options)
            : _byType.GetOrAdd(resourceSource, static type => new ManagerEntry(TranslationManagerResolver.FromGeneratedClass(type), TextFormat.DotNet));

        return new TlumachHtmlLocalizer(new TranslationLookup(entry, prefix: null, Encoder));
    }

    /// <inheritdoc/>
    public IHtmlLocalizer Create(string baseName, string location)
    {
        ArgumentNullException.ThrowIfNull(baseName);

        string context = string.IsNullOrEmpty(location) ? baseName : location + "." + baseName;
        TlumachLocalizationOptions options = _settings.GetOptionsFor(context);
        if (TranslationManagerResolver.HasManagerSource(options))
            return new TlumachHtmlLocalizer(new TranslationLookup(GetEntry(options), prefix: null, Encoder));

        // As TlumachStringLocalizerFactory does: the default file named baseName, embedded into the calling assembly.
        Assembly assembly = Assembly.GetCallingAssembly();
        ManagerEntry entry = _byBaseName.GetOrAdd(
            assembly.FullName + "|" + baseName,
            static (_, state) => new ManagerEntry(new TranslationManager(new TranslationConfiguration(state.Assembly, state.BaseName, defaultFileLocale: null, TextFormat.DotNet)), TextFormat.DotNet),
            (Assembly: assembly, BaseName: baseName));

        return new TlumachHtmlLocalizer(new TranslationLookup(entry, prefix: null, Encoder));
    }

    internal ManagerEntry GetEntry(TlumachLocalizationOptions options)
        => _byOptions.GetOrAdd(
            options,
            static o => new ManagerEntry(
                TranslationManagerResolver.CreateFromOptions(o, Assembly.GetEntryAssembly() ?? typeof(TlumachHtmlLocalizerFactory).Assembly),
                o.TextProcessingMode));

    internal TranslationLookup GetViewLookup(string? viewPath)
        => _viewLookups.GetOrAdd(viewPath ?? string.Empty, CreateViewLookup);

    private TranslationLookup CreateViewLookup(string viewPath)
    {
        string? prefix = viewPath.Length == 0 ? null : _options.ViewKeyPrefix(viewPath);
        string context = string.IsNullOrEmpty(prefix) ? string.Empty : prefix.TrimEnd('.');

        TlumachLocalizationOptions options = _settings.GetOptionsFor(context);
        if (!TranslationManagerResolver.HasManagerSource(options))
        {
            throw new InvalidOperationException(
                $"No translation manager is configured for the view '{viewPath}'. Set TranslationManager, Configuration, or DefaultFile in AddTlumachLocalization, or add options for the context '{context}'.");
        }

        return new TranslationLookup(GetEntry(options), prefix, Encoder);
    }
}
