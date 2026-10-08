// <copyright file="ITlumachSettingsProvider.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// An interface that defines the GetOptionsFor method of the settings provider.
    /// </summary>
    public interface ITlumachSettingsProvider
    {
        /// <summary>
        /// Returns the options set for the given context.
        /// <para>This method is called every time a localizer is created, which may happen on every request. An implementation may return a new options object on every call,
        /// but it should put the same <see cref="TlumachLocalizationOptions.TranslationManager"/> or <see cref="TlumachLocalizationOptions.Configuration"/> instance into it every time:
        /// the string localizers of <see cref="TlumachStringLocalizerFactory"/> and the HTML localizers of Tlumach.AspNetCore.Mvc reuse a translation manager created from options while the options carry the same <c>TranslationManager</c> instance, the same
        /// <c>Configuration</c> instance, or the same <c>DefaultFile</c>, <c>Assembly</c>, and <c>DefaultFileLocale</c> values (with the same <c>TextProcessingMode</c>).
        /// A new configuration on every call creates a new translation manager on every call, and every manager stays in <see cref="TranslationManager.TranslationManagers"/> until it is disposed.</para>
        /// </summary>
        /// <param name="context">The context to retrieve the options for.</param>
        /// <returns>An instance of options or the default value if the options specific for the context were not found.</returns>
        TlumachLocalizationOptions GetOptionsFor(string context);
    }
}
