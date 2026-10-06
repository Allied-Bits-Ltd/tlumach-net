// <copyright file="TemplateArguments.cs" company="Allied Bits Ltd.">
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
/// The values, which a template passes to one call of <see cref="TemplateTranslator"/> for the placeholders of a translation.
/// <para>Positional values fill indexed placeholders (<c>{0}</c>, <c>{1}</c>) and, by position, named placeholders without a named value. Named values fill named placeholders; their names are case-insensitive.</para>
/// </summary>
public sealed class TemplateArguments
{
    private readonly List<object?> _positional = [];
    private readonly Dictionary<string, object?> _named = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateArguments"/> class without values.
    /// </summary>
    public TemplateArguments()
    {
    }

    private TemplateArguments(bool isReadOnly)
    {
        _isReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets a shared instance without values, which cannot be changed.
    /// </summary>
    public static TemplateArguments Empty { get; } = new(isReadOnly: true);

    /// <summary>
    /// Gets the positional values in the order, in which they were added.
    /// </summary>
    public IReadOnlyList<object?> Positional => _positional;

    /// <summary>
    /// Gets the named values. The names are case-insensitive.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Named => _named;

    /// <summary>
    /// Adds a positional value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>This instance.</returns>
    public TemplateArguments AddPositional(object? value)
    {
        ThrowIfReadOnly();
        _positional.Add(value);
        return this;
    }

    /// <summary>
    /// Adds a named value, replacing an earlier value with the same name.
    /// </summary>
    /// <param name="name">The name of the placeholder.</param>
    /// <param name="value">The value.</param>
    /// <returns>This instance.</returns>
    public TemplateArguments AddNamed(string name, object? value)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowIfReadOnly();
        _named[name] = value;
        return this;
    }

    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
            throw new InvalidOperationException("The shared empty instance of TemplateArguments cannot be changed.");
    }
}
