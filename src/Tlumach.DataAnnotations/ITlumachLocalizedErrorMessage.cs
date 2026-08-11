// <copyright file="ITlumachLocalizedErrorMessage.cs" company="Allied Bits Ltd.">
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
    /// Implemented by validation attributes that take their error message from a Tlumach translation.
    /// <para>The localized validation attributes of Tlumach derive from different attributes of <see cref="System.ComponentModel.DataAnnotations"/> and cannot share a common base class, so this interface is what
    /// <see cref="TlumachMessageResolver"/> talks to. The <see cref="ErrorMessage"/> and <see cref="ErrorMessageResourceName"/> members are satisfied by the properties that every implementer inherits from
    /// <see cref="ValidationAttribute"/>; an implementer does not write any code for them.</para>
    /// </summary>
    public interface ITlumachLocalizedErrorMessage
    {
        /// <summary>
        /// Gets the class created by Tlumach Generator, or a nested group class inside one, that provides the message.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type? TranslationClass { get; }

        /// <summary>
        /// Gets the name of the translation unit member in <see cref="TranslationClass"/>, or the key of a translation entry.
        /// </summary>
        string? TranslationKey { get; }

        /// <summary>
        /// Gets the culture, for which the message is read.
        /// </summary>
        TranslationCultureSource CultureSource { get; }

        /// <summary>
        /// Gets the literal error message. Mirrors <see cref="ValidationAttribute.ErrorMessage"/> and is used to detect the invalid combination of a literal message and a Tlumach translation key.
        /// </summary>
        string? ErrorMessage { get; }

        /// <summary>
        /// Gets the name of the resource that provides the error message. Mirrors <see cref="ValidationAttribute.ErrorMessageResourceName"/> and is used to detect the invalid combination of a resource and a Tlumach translation key.
        /// </summary>
        string? ErrorMessageResourceName { get; }
    }
}
