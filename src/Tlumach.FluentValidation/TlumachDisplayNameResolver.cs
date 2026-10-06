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
/// <para>For the property <c>Name</c> of the class <c>Customer</c>, the resolver looks up two keys: the type-qualified key <c>{DisplayNamesGroup}.Customer.Name</c> and the
/// member key <c>{DisplayNamesGroup}.Name</c>. The type is the type of the object that FluentValidation validates (the root object). A name in the language of the message
/// is preferred to a name in another language, so the first of these is returned:</para>
/// <list type="number">
/// <item>The text of the type-qualified key, found for the culture or its basic culture.</item>
/// <item>The text of the member key, found for the culture or its basic culture.</item>
/// <item>The text of the type-qualified key from the default file, or a text supplied by an <c>OnTranslationValueNotFound</c> handler.</item>
/// <item>The text of the member key from the default file, or a text supplied by an <c>OnTranslationValueNotFound</c> handler.</item>
/// </list>
/// <para>When neither key has text, the resolver returns <see langword="null"/>, and FluentValidation uses its default name. A rule that calls <c>WithName</c> is not affected.</para>
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
    /// <returns>The display name, chosen as described in the summary of the class, or <see langword="null"/> when the translation has none.</returns>
    public string? Resolve(Type type, MemberInfo member, LambdaExpression expression)
    {
        if (member is null)
            return null;

        CultureInfo culture = ValidationTemplates.CurrentCulture();
        string prefix = string.IsNullOrEmpty(DisplayNamesGroup) ? string.Empty : DisplayNamesGroup + ".";

        TranslationEntrySource typeSource = TranslationEntrySource.NotFound;
        string? typeText = type is null ? null : Lookup(prefix + type.Name + "." + member.Name, culture, out typeSource);
        string? memberText = Lookup(prefix + member.Name, culture, out TranslationEntrySource memberSource);

        // A name found for the culture or its basic culture is in the language of the message, so it is preferred to a name from the default file.
        if (typeText is not null && IsForCulture(typeSource))
            return typeText;

        if (memberText is not null && IsForCulture(memberSource))
            return memberText;

        return typeText ?? memberText;
    }

    private static bool IsForCulture(TranslationEntrySource source)
        => source is TranslationEntrySource.Culture or TranslationEntrySource.BasicCulture;

    private string? Lookup(string key, CultureInfo culture, out TranslationEntrySource source)
    {
        string? text = TranslationManager.GetValueWithSource(key, culture, out source).Text;
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
