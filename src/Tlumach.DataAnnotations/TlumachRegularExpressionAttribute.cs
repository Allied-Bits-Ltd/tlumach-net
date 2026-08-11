// <copyright file="TlumachRegularExpressionAttribute.cs" company="Allied Bits Ltd.">
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
    /// A <see cref="RegularExpressionAttribute"/> that takes its error message from a class created by Tlumach Generator.
    /// <para>The message template receives the display name of the member as <c>{0}</c> and <see cref="RegularExpressionAttribute.Pattern"/> as <c>{1}</c>, which is the order that
    /// <see cref="RegularExpressionAttribute"/> itself uses.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class TlumachRegularExpressionAttribute : RegularExpressionAttribute, ITlumachLocalizedErrorMessage
    {
        private readonly TlumachMessageResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRegularExpressionAttribute"/> class. Set <see cref="TranslationClass"/> and <see cref="TranslationKey"/> in the annotation, or leave both
        /// unset to behave exactly like <see cref="RegularExpressionAttribute"/>.
        /// </summary>
        /// <param name="pattern">The regular expression that the value must match.</param>
        public TlumachRegularExpressionAttribute([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
            : base(pattern)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRegularExpressionAttribute"/> class that takes its message from the given translation.
        /// </summary>
        /// <param name="pattern">The regular expression that the value must match.</param>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, that provides the message.</param>
        /// <param name="translationKey">The name of the translation unit member in <paramref name="translationClass"/>, or the key of a translation entry.</param>
        public TlumachRegularExpressionAttribute(
            [StringSyntax(StringSyntaxAttribute.Regex)] string pattern,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass,
            string translationKey)
            : this(pattern)
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
        /// <exception cref="InvalidOperationException">Thrown when the pattern of the attribute is empty, when the Tlumach configuration of the attribute is invalid, or when the message cannot be resolved.</exception>
        public override string FormatErrorMessage(string name)
        {
            if (!_resolver.IsConfigured())
                return base.FormatErrorMessage(name);

            // The base implementation calls the private SetupRegex method, which validates the pattern and which a derived class in another assembly cannot reach. Calling it and discarding the result
            // keeps that validation in place.
            _ = base.FormatErrorMessage(name);

            return _resolver.Format(name, Pattern);
        }
    }
}
