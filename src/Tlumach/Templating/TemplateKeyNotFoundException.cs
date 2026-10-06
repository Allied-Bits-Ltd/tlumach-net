// <copyright file="TemplateKeyNotFoundException.cs" company="Allied Bits Ltd.">
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

using System.Globalization;

using Tlumach.Base;

namespace Tlumach.Templating;

/// <summary>
/// Thrown by <see cref="TemplateTranslator"/> for a key that no translation contains, when <see cref="TemplateTranslationOptions.MissingKey"/> is <see cref="MissingKeyBehavior.Throw"/>.
/// </summary>
public class TemplateKeyNotFoundException : TlumachException
{
    public TemplateKeyNotFoundException()
    {
    }

    public TemplateKeyNotFoundException(string message)
        : base(message)
    {
    }

    public TemplateKeyNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateKeyNotFoundException"/> class for a missing key.
    /// </summary>
    /// <param name="key">The full key, with the key prefix.</param>
    /// <param name="culture">The culture of the call.</param>
    public TemplateKeyNotFoundException(string key, CultureInfo culture)
        : base($"The translation with the key '{key}' was not found for the culture '{culture?.Name}'.")
    {
        Key = key;
        Culture = culture;
    }

    /// <summary>
    /// Gets the full key, with the key prefix.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// Gets the culture of the call.
    /// </summary>
    public CultureInfo? Culture { get; }
}
