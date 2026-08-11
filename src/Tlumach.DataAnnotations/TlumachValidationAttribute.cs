// <copyright file="TlumachValidationAttribute.cs" company="Allied Bits Ltd.">
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
    /// The base class for a validation attribute of an application that takes its error message from a class created by Tlumach Generator.
    /// <para>Derive from this class when none of the localized attributes of Tlumach fits. A derived class obtains the message through <see cref="MessageResolver"/>, which is also the place to get the
    /// raw template from when the message takes more arguments than the display name.</para>
    /// </summary>
    /// <example>
    /// The following attribute validates that a string is not longer than a fixed limit and takes its message from a translation:
    /// <code>
    /// public sealed class ShortTextAttribute : TlumachValidationAttribute
    /// {
    ///     public override bool IsValid(object? value) =&gt; value is not string text || text.Length &lt;= 40;
    /// }
    ///
    /// // [ShortText(TranslationClass = typeof(Strings), TranslationKey = Strings.tooLongKey)]
    /// </code>
    /// </example>
    [SuppressMessage("Design", "CA1813:Avoid unsealed attributes", Justification = "This class exists in order to be derived from by application code.")]
    public abstract class TlumachValidationAttribute : ValidationAttribute, ITlumachLocalizedErrorMessage
    {
        private readonly TlumachMessageResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachValidationAttribute"/> class. Set <see cref="TranslationClass"/> and <see cref="TranslationKey"/> in the annotation.
        /// </summary>
        protected TlumachValidationAttribute()
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachValidationAttribute"/> class with a literal error message, used when the annotation carries no Tlumach translation.
        /// </summary>
        /// <param name="errorMessage">The literal error message.</param>
        protected TlumachValidationAttribute(string errorMessage)
            : base(errorMessage)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachValidationAttribute"/> class with a function that provides the error message, used when the annotation carries no Tlumach translation.
        /// </summary>
        /// <param name="errorMessageAccessor">The function that provides the error message.</param>
        protected TlumachValidationAttribute(Func<string> errorMessageAccessor)
            : base(errorMessageAccessor)
        {
            _resolver = new TlumachMessageResolver(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachValidationAttribute"/> class that takes its message from the given translation.
        /// </summary>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, that provides the message.</param>
        /// <param name="translationKey">The name of the translation unit member in <paramref name="translationClass"/>, or the key of a translation entry.</param>
        protected TlumachValidationAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass,
            string translationKey)
        {
            _resolver = new TlumachMessageResolver(this);
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
        /// Gets the resolver that provides the localized message of this attribute.
        /// </summary>
        protected TlumachMessageResolver MessageResolver => _resolver;

        /// <summary>
        /// Formats the error message, taking it from the configured translation when there is one.
        /// </summary>
        /// <param name="name">The display name of the member being validated.</param>
        /// <returns>The formatted message.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the Tlumach configuration of the attribute is invalid or the message cannot be resolved.</exception>
        public override string FormatErrorMessage(string name)
        {
            // No pre-call of the base implementation here, unlike in the attributes that derive from the stock ones: a derived attribute that carries neither a literal message nor an accessor has no
            // default message either, and the base implementation would throw.
            return _resolver.IsConfigured() ? _resolver.Format(name) : base.FormatErrorMessage(name);
        }
    }
}
