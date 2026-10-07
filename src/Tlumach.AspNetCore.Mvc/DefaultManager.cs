// <copyright file="DefaultManager.cs" company="Allied Bits Ltd.">
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

using System.Reflection;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;

using Tlumach.Extensions.Localization;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Finds the translation manager of the model binding messages and display names: the manager set in their options, else the manager of the default
/// options of <c>AddTlumachLocalization</c> (shared with the HTML localizers when they are registered).
/// </summary>
internal static class DefaultManager
{
    internal static TranslationManager Resolve(IServiceProvider services, TranslationManager? explicitManager)
    {
        if (explicitManager is not null)
            return explicitManager;

        ITlumachSettingsProvider settings = services.GetService<ITlumachSettingsProvider>()
            ?? throw new InvalidOperationException(TlumachHtmlLocalizerFactory.MissingLocalizationMessage);

        TlumachLocalizationOptions options = settings.GetOptionsFor(string.Empty);
        if (!TranslationManagerResolver.HasManagerSource(options))
            throw new InvalidOperationException("No translation manager is configured. Set TranslationManager, Configuration, or DefaultFile in AddTlumachLocalization, or set TranslationManager in the options of this registration.");

        if (services.GetService<IHtmlLocalizerFactory>() is TlumachHtmlLocalizerFactory factory)
            return factory.GetEntry(options).Manager;

        return TranslationManagerResolver.CreateFromOptions(options, Assembly.GetEntryAssembly() ?? typeof(DefaultManager).Assembly);
    }
}
