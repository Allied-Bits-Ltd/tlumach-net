// <copyright file="TlumachRuleBuilderExtensions.cs" company="Allied Bits Ltd.">
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

using FluentValidation;
using FluentValidation.Internal;

namespace Tlumach.FluentValidation;

/// <summary>
/// Sets the message and the display name of a rule from a Tlumach translation. The text is read when the rule is validated, so it follows the culture of that moment.
/// </summary>
/// <remarks>
/// <para>Import the namespace <c>Tlumach.FluentValidation</c> wherever these methods are used. <see cref="TranslationUnit"/> converts implicitly to <see cref="string"/>, so without
/// the import, <c>WithMessage(Strings.X)</c> compiles against <c>WithMessage(string)</c> of FluentValidation and keeps the text of the culture that was current when the
/// validator was created.</para>
/// <para>The text is read as a template, without the placeholder engine of Tlumach, and FluentValidation then replaces its placeholders, such as <c>{PropertyName}</c>.
/// A key without text for the culture raises an <see cref="InvalidOperationException"/> during validation.</para>
/// </remarks>
public static class TlumachRuleBuilderExtensions
{
    /// <summary>
    /// Takes the message of the rule from a translation unit, typically one created by Generator.
    /// </summary>
    /// <remarks>
    /// The method is generic on purpose. Every unit class, including the platform ones (Avalonia, WinUI, UWP) that declare their own implicit conversion to <see cref="string"/>,
    /// binds to it with an identity conversion of the argument, which beats the user-defined conversion that would select <c>WithMessage(string)</c> of FluentValidation
    /// or make the call ambiguous. A <see cref="string"/> argument does not satisfy the constraint, so this overload leaves it to FluentValidation.
    /// </remarks>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <typeparam name="TUnit">The class of the translation unit.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="unit">The translation unit with the message template.</param>
    /// <returns>The rule, with the message taken from the unit.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty, TUnit>(this IRuleBuilderOptions<T, TProperty> rule, TUnit unit)
        where TUnit : BaseTranslationUnit
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(unit);

        return DefaultValidatorOptions.WithMessage(rule, (T _) => ValidationTemplates.Read(unit));
    }

    /// <summary>
    /// Takes the message of the rule from a translation unit and fills placeholders of its own before FluentValidation fills the standard ones.
    /// <para>Values are inserted before FluentValidation formats the message, so a value that itself contains a placeholder of FluentValidation, such as
    /// <c>{PropertyName}</c>, has that placeholder replaced too.</para>
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="unit">The translation unit with the message template.</param>
    /// <param name="placeholders">A callback that adds values with <see cref="MessageFormatter.AppendArgument(string, object)"/>. Format specifiers, as in <c>{Limit:N0}</c>,
    /// are applied with <see cref="System.Globalization.CultureInfo.CurrentCulture"/> (FluentValidation formats without a provider), which may differ from the culture of the message.</param>
    /// <returns>The rule, with the message taken from the unit and filled with the values of the callback.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, BaseTranslationUnit unit, Action<T, TProperty, MessageFormatter> placeholders)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(placeholders);

        return DefaultValidatorOptions.WithMessage(rule, (T instance, TProperty value) => Fill(ValidationTemplates.Read(unit), instance, value, placeholders));
    }

    /// <summary>
    /// Takes the message of the rule from a translation manager by the key of the translation entry, which is the route for a class generated with <c>onlyDeclareKeys</c>.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="manager">The translation manager.</param>
    /// <param name="key">The full key, including its groups, for example <c>Messages.NameTooLong</c>.</param>
    /// <returns>The rule, with the message taken from the manager.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);

        return DefaultValidatorOptions.WithMessage(rule, (T _) => ValidationTemplates.Read(manager, key));
    }

    /// <summary>
    /// Takes the message of the rule from a translation manager by key and fills placeholders of its own before FluentValidation fills the standard ones.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="manager">The translation manager.</param>
    /// <param name="key">The full key, including its groups.</param>
    /// <param name="placeholders">A callback that adds values with <see cref="MessageFormatter.AppendArgument(string, object)"/>. Format specifiers are applied with
    /// <see cref="System.Globalization.CultureInfo.CurrentCulture"/>, which may differ from the culture of the message.</param>
    /// <returns>The rule, with the message taken from the manager and filled with the values of the callback.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key, Action<T, TProperty, MessageFormatter> placeholders)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(placeholders);

        return DefaultValidatorOptions.WithMessage(rule, (T instance, TProperty value) => Fill(ValidationTemplates.Read(manager, key), instance, value, placeholders));
    }

    /// <summary>
    /// Takes the display name of the property, which becomes <c>{PropertyName}</c> in the message, from a translation unit.
    /// </summary>
    /// <remarks>
    /// The method is generic for the same reason as <c>WithMessage</c>: every unit class binds to it with an identity conversion, which beats the implicit conversion
    /// to <see cref="string"/> that unit classes declare. A <see cref="string"/> argument does not satisfy the constraint, so this overload leaves it to FluentValidation.
    /// </remarks>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <typeparam name="TUnit">The class of the translation unit.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="unit">The translation unit with the display name.</param>
    /// <returns>The rule, with the display name taken from the unit.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithName<T, TProperty, TUnit>(this IRuleBuilderOptions<T, TProperty> rule, TUnit unit)
        where TUnit : BaseTranslationUnit
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(unit);

        return DefaultValidatorOptions.WithName(rule, (T _) => ValidationTemplates.Read(unit));
    }

    /// <summary>
    /// Takes the display name of the property from a translation manager by the key of the translation entry.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <param name="manager">The translation manager.</param>
    /// <param name="key">The full key, including its groups.</param>
    /// <returns>The rule, with the display name taken from the manager.</returns>
    public static IRuleBuilderOptions<T, TProperty> WithName<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, TranslationManager manager, string key)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(key);

        return DefaultValidatorOptions.WithName(rule, (T _) => ValidationTemplates.Read(manager, key));
    }

    private static string Fill<T, TProperty>(string template, T instance, TProperty value, Action<T, TProperty, MessageFormatter> placeholders)
    {
        // A fresh formatter knows only the values that the callback adds. BuildMessage leaves every other placeholder as it is, and FluentValidation fills those afterwards.
        var formatter = new MessageFormatter();
        placeholders(instance, value, formatter);
        return formatter.BuildMessage(template);
    }
}
