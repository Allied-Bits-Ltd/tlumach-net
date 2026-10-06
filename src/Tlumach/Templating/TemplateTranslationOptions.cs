// <copyright file="TemplateTranslationOptions.cs" company="Allied Bits Ltd.">
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
/// The options of <see cref="TemplateTranslator"/>, shared by the integrations with template engines, whose options derive from this class.
/// <para><see cref="TemplateTranslator"/> copies the values when it is created, so later changes of an options object do not affect a registered integration.</para>
/// </summary>
public class TemplateTranslationOptions
{
    /// <summary>
    /// The default name of the argument that sets the culture of a call.
    /// </summary>
    public const string DefaultCultureArgumentName = "culture";

    /// <summary>
    /// Gets or sets the configuration, in which keys are looked up. When <see langword="null"/>, the default configuration of the translation manager is used.
    /// Translation units always use their own configuration.
    /// </summary>
    public TranslationConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the text that is prepended to every key, e.g. <c>"email."</c> to look up <c>email.subject</c> for the key <c>subject</c>. It is not applied to translation units.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets what is returned for a key that no translation contains. The default is <see cref="MissingKeyBehavior.ReturnKey"/>.
    /// </summary>
    public MissingKeyBehavior MissingKey { get; set; } = MissingKeyBehavior.ReturnKey;

    /// <summary>
    /// Gets or sets a function that is called with the full key (with the prefix) and the culture of a call when the key is missing.
    /// A non-null return value is used as the text and takes precedence over <see cref="MissingKey"/>. The function may also be used for logging.
    /// </summary>
    public Func<string, CultureInfo, string?>? OnMissingKey { get; set; }

    /// <summary>
    /// Gets or sets the name of the named argument that sets the culture of a call. The argument is never used as a placeholder value. The default is <c>"culture"</c>.
    /// </summary>
    public string CultureArgumentName { get; set; } = DefaultCultureArgumentName;
}
