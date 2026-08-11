// <copyright file="TlumachPhoneAttribute.cs" company="Allied Bits Ltd.">
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
    /// A telephone number validation attribute that takes its error message from a class created by Tlumach Generator.
    /// <para>The message template receives the display name of the member as <c>{0}</c>.</para>
    /// </summary>
    /// <remarks>
    /// <see cref="PhoneAttribute"/> is sealed, so this attribute derives from <see cref="DataTypeAttribute"/> the way the sealed one does and delegates the check itself to an instance of it. The rule is
    /// therefore identical to the one of the framework and follows any future correction of it, and no expression from the framework is copied into Tlumach.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class TlumachPhoneAttribute : DataTypeAttribute, ITlumachLocalizedErrorMessage
    {
        private static readonly PhoneAttribute StockRule = new();

        private readonly TlumachMessageResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachPhoneAttribute"/> class. Set <see cref="TranslationClass"/> and <see cref="TranslationKey"/> in the annotation, or leave both unset to
        /// behave exactly like <see cref="PhoneAttribute"/>.
        /// </summary>
        public TlumachPhoneAttribute()
            : base(DataType.PhoneNumber)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachPhoneAttribute"/> class that takes its message from the given translation.
        /// </summary>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, that provides the message.</param>
        /// <param name="translationKey">The name of the translation unit member in <paramref name="translationClass"/>, or the key of a translation entry.</param>
        public TlumachPhoneAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass,
            string translationKey)
            : this()
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
        /// Tells whether the value is a valid telephone number, using the rule of <see cref="PhoneAttribute"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><see langword="true"/> when the value is <see langword="null"/> or a valid telephone number.</returns>
        public override bool IsValid(object? value) => StockRule.IsValid(value);

        /// <summary>
        /// Formats the error message, taking it from the configured translation when there is one.
        /// </summary>
        /// <param name="name">The display name of the member being validated. It becomes the <c>{0}</c> argument of the template.</param>
        /// <returns>The formatted message.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the Tlumach configuration of the attribute is invalid or the message cannot be resolved.</exception>
        public override string FormatErrorMessage(string name)
        {
            if (_resolver.IsConfigured())
                return _resolver.Format(name);

            // The default message of a DataTypeAttribute is set by the sealed attribute that Tlumach cannot derive from, and the property that holds it is not reachable from another assembly, so the
            // message of the framework is borrowed from the same instance that performs the check. That keeps the text localized by the framework itself rather than hard-coded here.
            return ErrorMessage is null && ErrorMessageResourceName is null
                ? StockRule.FormatErrorMessage(name)
                : base.FormatErrorMessage(name);
        }
    }
}
