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
using System.Runtime.CompilerServices;
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
    private readonly ConcurrentDictionary<TlumachLocalizationOptions, Lazy<ManagerEntry>> _byOptions = new(ReferenceEqualityComparer.Instance);
    private readonly ConcurrentDictionary<Type, Lazy<ManagerEntry>> _byType = new();
    private readonly ConcurrentDictionary<string, Lazy<ManagerEntry>> _byBaseName = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TranslationLookup> _viewLookups = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string Executing, string Main), TranslationLookup> _tagLookups = new();
    private readonly Func<string, TranslationLookup> _createViewLookup;
    private readonly Func<(string Executing, string Main), TranslationLookup> _createTagLookup;

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
        _createViewLookup = CreateViewLookup;
        _createTagLookup = CreateTagLookup;
    }

    internal HtmlEncoder Encoder { get; }

    /// <inheritdoc/>
    public IHtmlLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        TlumachLocalizationOptions options = _settings.GetOptionsFor(resourceSource.FullName ?? resourceSource.Name);
        ManagerEntry entry = TranslationManagerResolver.HasManagerSource(options)
            ? GetEntry(options)
            : GetOrCreate(_byType, resourceSource, static type => new Lazy<ManagerEntry>(() => new ManagerEntry(TranslationManagerResolver.FromGeneratedClass(type), TextFormat.DotNet)));

        return new TlumachHtmlLocalizer(new TranslationLookup(entry, prefix: null, Encoder));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)] // Assembly.GetCallingAssembly() must see the caller of this method.
    public IHtmlLocalizer Create(string baseName, string location)
    {
        ArgumentNullException.ThrowIfNull(baseName);

        string context = string.IsNullOrEmpty(location) ? baseName : location + "." + baseName;
        TlumachLocalizationOptions options = _settings.GetOptionsFor(context);
        if (TranslationManagerResolver.HasManagerSource(options))
            return new TlumachHtmlLocalizer(new TranslationLookup(GetEntry(options), prefix: null, Encoder));

        // As TlumachStringLocalizerFactory does: the default file named baseName, embedded into the calling assembly.
        Assembly assembly = Assembly.GetCallingAssembly();
        ManagerEntry entry = GetOrCreate(
            _byBaseName,
            assembly.FullName + "|" + baseName,
            _ => new Lazy<ManagerEntry>(() => new ManagerEntry(new TranslationManager(new TranslationConfiguration(assembly, baseName, defaultFileLocale: null, TextFormat.DotNet)), TextFormat.DotNet)));

        return new TlumachHtmlLocalizer(new TranslationLookup(entry, prefix: null, Encoder));
    }

    internal ManagerEntry GetEntry(TlumachLocalizationOptions options)
        => GetOrCreate(
            _byOptions,
            options,
            static o => new Lazy<ManagerEntry>(() => new ManagerEntry(
                TranslationManagerResolver.CreateFromOptions(o, Assembly.GetEntryAssembly() ?? typeof(TlumachHtmlLocalizerFactory).Assembly),
                o.TextProcessingMode)));

    /// <summary>
    /// Returns the cached entry. The <see cref="Lazy{T}"/> makes sure that only one manager is created per key; if its creation throws, the failed
    /// <see cref="Lazy{T}"/> is removed, so that a later call (e.g. after the file appears) retries instead of rethrowing the cached exception forever.
    /// </summary>
    private static ManagerEntry GetOrCreate<TKey>(ConcurrentDictionary<TKey, Lazy<ManagerEntry>> cache, TKey key, Func<TKey, Lazy<ManagerEntry>> create)
        where TKey : notnull
    {
        Lazy<ManagerEntry> lazy = cache.GetOrAdd(key, create);
        try
        {
            return lazy.Value;
        }
        catch
        {
            cache.TryRemove(new KeyValuePair<TKey, Lazy<ManagerEntry>>(key, lazy));
            throw;
        }
    }

    internal TranslationLookup GetViewLookup(string? viewPath)
        => _viewLookups.GetOrAdd(viewPath ?? string.Empty, _createViewLookup);

    // The lookup of the tag helpers: the prefix of the file that executes (a section of a view runs in the context of the layout), then the prefix of the main view, then no prefix.
    internal TranslationLookup GetTagLookup(string? executingPath, string? mainViewPath)
        => _tagLookups.GetOrAdd((executingPath ?? string.Empty, mainViewPath ?? string.Empty), _createTagLookup);

    private TranslationLookup CreateViewLookup(string viewPath)
    {
        string? prefix = viewPath.Length == 0 ? null : _options.ViewKeyPrefix(viewPath);
        return CreateLookup(viewPath, string.IsNullOrEmpty(prefix) ? [] : [prefix]);
    }

    private TranslationLookup CreateTagLookup((string Executing, string Main) paths)
    {
        List<string> prefixes = new(2);
        AddPrefix(prefixes, paths.Executing);
        AddPrefix(prefixes, paths.Main);
        return CreateLookup(paths.Executing.Length != 0 ? paths.Executing : paths.Main, [.. prefixes]);
    }

    private void AddPrefix(List<string> prefixes, string viewPath)
    {
        if (viewPath.Length == 0)
            return;

        string? prefix = _options.ViewKeyPrefix(viewPath);
        if (!string.IsNullOrEmpty(prefix) && !prefixes.Contains(prefix, StringComparer.Ordinal))
            prefixes.Add(prefix);
    }

    // The manager is chosen by the context of the first prefix, i.e. the file that is being executed.
    private TranslationLookup CreateLookup(string viewPath, string[] prefixes)
    {
        string context = prefixes.Length == 0 ? string.Empty : prefixes[0].TrimEnd('.');

        TlumachLocalizationOptions options = _settings.GetOptionsFor(context);
        if (!TranslationManagerResolver.HasManagerSource(options))
        {
            throw new InvalidOperationException(
                $"No translation manager is configured for the view '{viewPath}'. Set TranslationManager, Configuration, or DefaultFile in AddTlumachLocalization, or add options for the context '{context}'.");
        }

        return new TranslationLookup(GetEntry(options), prefixes, Encoder);
    }
}
