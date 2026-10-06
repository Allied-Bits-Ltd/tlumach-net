// <copyright file="TlumachDisplayNameResolver.cs" company="Allied Bits Ltd.">
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
using System.Linq.Expressions;
using System.Reflection;

namespace Tlumach.FluentValidation;

/// <summary>
/// Provides the display names of properties from a Tlumach translation, for <c>ValidatorOptions.Global.DisplayNameResolver</c>.
/// <para>For the property <c>Name</c> of the class <c>Customer</c>, the resolver looks up <c>{DisplayNamesGroup}.Customer.Name</c> and then <c>{DisplayNamesGroup}.Name</c>. When
/// neither has text, it returns <see langword="null"/>, and FluentValidation uses its default name. A rule that calls <c>WithName</c> is not affected.</para>
/// <para>The name is read when the rule is validated, for the same culture as the messages of the language manager.</para>
/// </summary>
public sealed class TlumachDisplayNameResolver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TlumachDisplayNameResolver"/> class.
    /// </summary>
    /// <param name="translationManager">The translation manager that provides the names.</param>
    /// <param name="displayNamesGroup">The group, in which the names are stored. <see langword="null"/> or empty for root keys.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="translationManager"/> is <see langword="null"/>.</exception>
    public TlumachDisplayNameResolver(TranslationManager translationManager, string? displayNamesGroup = "DisplayNames")
    {
        TranslationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));
        DisplayNamesGroup = displayNamesGroup;
    }

    /// <summary>
    /// Gets the translation manager that provides the names.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the group, in which the names are stored.
    /// </summary>
    public string? DisplayNamesGroup { get; }

    /// <summary>
    /// Returns the display name of the member. The signature matches <c>ValidatorOptions.Global.DisplayNameResolver</c>.
    /// </summary>
    /// <param name="type">The type of the validated object.</param>
    /// <param name="member">The member being validated. May be <see langword="null"/> for rules that do not target a member.</param>
    /// <param name="expression">The expression of the rule. Not used.</param>
    /// <returns>The display name, or <see langword="null"/> when the translation has none.</returns>
    public string? Resolve(Type type, MemberInfo member, LambdaExpression expression)
    {
        if (member is null)
            return null;

        CultureInfo culture = ValidationTemplates.CurrentCulture();
        string prefix = string.IsNullOrEmpty(DisplayNamesGroup) ? string.Empty : DisplayNamesGroup + ".";

        return (type is null ? null : Lookup(prefix + type.Name + "." + member.Name, culture))
            ?? Lookup(prefix + member.Name, culture);
    }

    private string? Lookup(string key, CultureInfo culture)
    {
        string? text = TranslationManager.GetValue(key, culture).Text;
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
