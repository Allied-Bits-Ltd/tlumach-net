// <copyright file="TemplateMarkup.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Templating;

/// <summary>
/// A trusted HTML fragment passed as a placeholder value. <see cref="TemplateTranslator.TranslateMarkup"/> inserts it without encoding,
/// and <see cref="TemplateTranslator.Translate"/> inserts its text as is.
/// </summary>
public readonly struct TemplateMarkup : IEquatable<TemplateMarkup>
{
    private readonly string? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateMarkup"/> struct.
    /// </summary>
    /// <param name="value">The trusted HTML.</param>
    public TemplateMarkup(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the trusted HTML.
    /// </summary>
    public string Value => _value ?? string.Empty;

    public static bool operator ==(TemplateMarkup left, TemplateMarkup right) => left.Equals(right);

    public static bool operator !=(TemplateMarkup left, TemplateMarkup right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(TemplateMarkup other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TemplateMarkup other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
