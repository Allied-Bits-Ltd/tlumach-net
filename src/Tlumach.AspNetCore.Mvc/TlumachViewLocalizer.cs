// <copyright file="TlumachViewLocalizer.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// An <see cref="IViewLocalizer"/> backed by Tlumach. A key is looked up with the prefix of the view (<see cref="TlumachViewLocalizationOptions.ViewKeyPrefix"/>,
/// e.g. "Views.Home.Index.Title") and then without it ("Title"), so that views can share texts. Partial views and layouts use their own prefix.
/// The encoding rules are those of <see cref="TlumachHtmlLocalizer"/>.
/// </summary>
public sealed class TlumachViewLocalizer : IViewLocalizer, IViewContextAware
{
    private readonly TlumachHtmlLocalizerFactory _factory;
    private TranslationLookup? _lookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachViewLocalizer"/> class.
    /// </summary>
    /// <param name="factory">The factory registered by <see cref="TlumachMvcBuilderExtensions.AddTlumachViewLocalization"/>.</param>
    public TlumachViewLocalizer(IHtmlLocalizerFactory factory)
    {
        _factory = factory as TlumachHtmlLocalizerFactory
            ?? throw new InvalidOperationException("TlumachViewLocalizer requires TlumachHtmlLocalizerFactory. Register both with AddTlumachViewLocalization().");
    }

    private TranslationLookup Lookup
        => _lookup ?? throw new InvalidOperationException("The view localizer was used before Contextualize was called. Inject it into a view with @inject IViewLocalizer.");

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name] => Lookup.GetHtml(name, Array.Empty<object?>());

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name, params object[] arguments] => Lookup.GetHtml(name, arguments);

    /// <inheritdoc/>
    public LocalizedString GetString(string name) => Lookup.GetString(name);

    /// <inheritdoc/>
    public LocalizedString GetString(string name, params object[] arguments) => Lookup.GetString(name, arguments);

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Lookup.GetAllStrings(includeParentCultures);

    /// <inheritdoc/>
    public void Contextualize(ViewContext viewContext)
    {
        ArgumentNullException.ThrowIfNull(viewContext);

        string? path = viewContext.ExecutingFilePath;
        if (string.IsNullOrEmpty(path))
            path = viewContext.View?.Path;

        _lookup = _factory.GetViewLookup(path);
    }
}
