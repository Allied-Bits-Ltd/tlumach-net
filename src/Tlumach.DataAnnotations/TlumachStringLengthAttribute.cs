// <copyright file="TlumachStringLengthAttribute.cs" company="Allied Bits Ltd.">
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

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Tlumach.DataAnnotations
{
    /// <summary>
    /// A <see cref="StringLengthAttribute"/> that takes its error message from a class created by Tlumach Generator.
    /// <para>The message template receives the display name of the member as <c>{0}</c>, <see cref="StringLengthAttribute.MaximumLength"/> as <c>{1}</c> and
    /// <see cref="StringLengthAttribute.MinimumLength"/> as <c>{2}</c>, which is the order that <see cref="StringLengthAttribute"/> itself uses.</para>
    /// </summary>
    /// <example>
    /// With <c>passwordLength</c> translated as <c>The {0} must be at least {2} and at max {1} characters long.</c>:
    /// <code>
    /// [TlumachStringLength(100, typeof(Strings), Strings.passwordLengthKey, MinimumLength = 6)]
    /// public string? Password { get; set; }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class TlumachStringLengthAttribute : StringLengthAttribute, ITlumachLocalizedErrorMessage
    {
        private readonly TlumachMessageResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachStringLengthAttribute"/> class. Set <see cref="TranslationClass"/> and <see cref="TranslationKey"/> in the annotation, or leave both unset
        /// to behave exactly like <see cref="StringLengthAttribute"/>.
        /// </summary>
        /// <param name="maximumLength">The maximum allowed length of the string.</param>
        public TlumachStringLengthAttribute(int maximumLength)
            : base(maximumLength)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachStringLengthAttribute"/> class that takes its message from the given translation.
        /// </summary>
        /// <param name="maximumLength">The maximum allowed length of the string.</param>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, that provides the message.</param>
        /// <param name="translationKey">The name of the translation unit member in <paramref name="translationClass"/>, or the key of a translation entry.</param>
        public TlumachStringLengthAttribute(
            int maximumLength,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass,
            string translationKey)
            : this(maximumLength)
        {
            TranslationClass = translationClass;
            TranslationKey = translationKey;
        }

        /// <inheritdoc/>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public Type? TranslationClass { get; set; }

        /// <inheritdoc/>
        public string? TranslationKey { get; set; }

        /// <inheritdoc/>
        public TranslationCultureSource CultureSource { get; set; }

        /// <summary>
        /// Formats the error message, taking it from the configured translation when there is one.
        /// </summary>
        /// <param name="name">The display name of the member being validated. It becomes the <c>{0}</c> argument of the template.</param>
        /// <returns>The formatted message.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the lengths of the attribute are illegal, when the Tlumach configuration of the attribute is invalid, or when the message cannot be resolved.</exception>
        public override string FormatErrorMessage(string name)
        {
            if (!_resolver.IsConfigured())
                return base.FormatErrorMessage(name);

            // The base implementation calls the private EnsureLegalLengths method, which is the only place that validates the lengths of the attribute and which a derived class in another assembly
            // cannot reach. Calling it and discarding the result keeps that validation in place; it costs one formatting operation on a path that runs only after validation has already failed.
            _ = base.FormatErrorMessage(name);

            return _resolver.Format(name, MaximumLength, MinimumLength);
        }
    }
}
