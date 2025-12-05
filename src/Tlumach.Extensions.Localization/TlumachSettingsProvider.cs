// <copyright file="TlumachSettingsProvider.cs" company="Allied Bits Ltd.">
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

using System;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// This class keeps settings for various localizer instances bound to specific contents.
    /// </summary>
    public sealed class TlumachSettingsProvider : ITlumachSettingsProvider
    {
        private readonly Dictionary<string, TlumachLocalizationOptions> _perContext =
            new(StringComparer.Ordinal);

        private readonly TlumachLocalizationOptions _defaultOptions;

        internal TlumachSettingsProvider(TlumachLocalizationOptions defaultOptions)
        {
            _defaultOptions = defaultOptions;
        }

        /// <summary>
        /// Use this method to add localizer options for the specified context.
        /// </summary>
        /// <param name="context">The context to add the options for.</param>
        /// <param name="options">The options to add.</param>
        public void AddContext(string context, TlumachLocalizationOptions options)
            => _perContext[context] = options;

        /// <summary>
        /// Returns the options set for the given context.
        /// </summary>
        /// <param name="context">The context to retrieve the options for.</param>
        /// <returns>An instance of options or the default value if the options specific for the context were not found.</returns>
        public TlumachLocalizationOptions GetOptionsFor(string context)
            => _perContext.TryGetValue(context, out var opt) ? opt : _defaultOptions;
    }
}
