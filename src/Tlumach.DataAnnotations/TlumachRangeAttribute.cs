// <copyright file="TlumachRangeAttribute.cs" company="Allied Bits Ltd.">
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
    /// A <see cref="RangeAttribute"/> that takes its error message from a class created by Tlumach Generator.
    /// <para>The message template receives the display name of the member as <c>{0}</c>, <see cref="RangeAttribute.Minimum"/> as <c>{1}</c> and <see cref="RangeAttribute.Maximum"/> as <c>{2}</c>, which
    /// is the order that <see cref="RangeAttribute"/> itself uses.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class TlumachRangeAttribute : RangeAttribute, ITlumachLocalizedErrorMessage
    {
        private readonly TlumachMessageResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRangeAttribute"/> class for a range of integers. Set <see cref="TranslationClass"/> and <see cref="TranslationKey"/> in the annotation, or
        /// leave both unset to behave exactly like <see cref="RangeAttribute"/>.
        /// </summary>
        /// <param name="minimum">The lowest allowed value.</param>
        /// <param name="maximum">The highest allowed value.</param>
        public TlumachRangeAttribute(int minimum, int maximum)
            : base(minimum, maximum)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRangeAttribute"/> class for a range of double-precision values. Set <see cref="TranslationClass"/> and <see cref="TranslationKey"/> in the
        /// annotation, or leave both unset to behave exactly like <see cref="RangeAttribute"/>.
        /// </summary>
        /// <param name="minimum">The lowest allowed value.</param>
        /// <param name="maximum">The highest allowed value.</param>
        public TlumachRangeAttribute(double minimum, double maximum)
            : base(minimum, maximum)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRangeAttribute"/> class for a range of integers, taking its message from the given translation.
        /// </summary>
        /// <param name="minimum">The lowest allowed value.</param>
        /// <param name="maximum">The highest allowed value.</param>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, that provides the message.</param>
        /// <param name="translationKey">The name of the translation unit member in <paramref name="translationClass"/>, or the key of a translation entry.</param>
        public TlumachRangeAttribute(
            int minimum,
            int maximum,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass,
            string translationKey)
            : this(minimum, maximum)
        {
            TranslationClass = translationClass;
            TranslationKey = translationKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRangeAttribute"/> class for a range of double-precision values, taking its message from the given translation.
        /// </summary>
        /// <param name="minimum">The lowest allowed value.</param>
        /// <param name="maximum">The highest allowed value.</param>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, that provides the message.</param>
        /// <param name="translationKey">The name of the translation unit member in <paramref name="translationClass"/>, or the key of a translation entry.</param>
        public TlumachRangeAttribute(
            double minimum,
            double maximum,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass,
            string translationKey)
            : this(minimum, maximum)
        {
            TranslationClass = translationClass;
            TranslationKey = translationKey;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachRangeAttribute"/> class for a range of values of an arbitrary comparable type. Set <see cref="TranslationClass"/> and
        /// <see cref="TranslationKey"/> in the annotation as named properties.
        /// </summary>
        /// <param name="operandType">The type of the values being compared.</param>
        /// <param name="minimum">The lowest allowed value, as text.</param>
        /// <param name="maximum">The highest allowed value, as text.</param>
        [RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        public TlumachRangeAttribute(
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                | DynamicallyAccessedMemberTypes.PublicFields
                | DynamicallyAccessedMemberTypes.PublicProperties)]
            Type operandType,
            string minimum,
            string maximum)
            : base(operandType, minimum, maximum)
        {
            _resolver = new TlumachMessageResolver(this);
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
        /// <exception cref="InvalidOperationException">Thrown when the range of the attribute is illegal, when the Tlumach configuration of the attribute is invalid, or when the message cannot be resolved.</exception>
        public override string FormatErrorMessage(string name)
        {
            if (!_resolver.IsConfigured())
                return base.FormatErrorMessage(name);

            // The base implementation calls the private SetupConversion method, which a derived class in another assembly cannot reach. That method validates the range and, when the constructor that
            // takes an operand type was used, converts Minimum and Maximum from text into that type. Calling it before the values are read is therefore required for correctness and not merely for
            // parity of the error behaviour.
            _ = base.FormatErrorMessage(name);

            return _resolver.Format(name, Minimum, Maximum);
        }
    }
}
