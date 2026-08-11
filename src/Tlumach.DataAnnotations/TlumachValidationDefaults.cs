// <copyright file="TlumachValidationDefaults.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.DataAnnotations
{
    /// <summary>
    /// Process-wide settings shared by the localized validation attributes of Tlumach.
    /// </summary>
    public static class TlumachValidationDefaults
    {
        private static TranslationCultureSource _cultureSource = TranslationCultureSource.TranslationManager;

        /// <summary>
        /// Gets or sets the culture source used by the attributes that leave <see cref="ITlumachLocalizedErrorMessage.CultureSource"/> at <see cref="TranslationCultureSource.Default"/>.
        /// <para>The default value is <see cref="TranslationCultureSource.TranslationManager"/>. Set this property once during the startup of the application. A web application that localizes requests
        /// sets it to <see cref="TranslationCultureSource.Ambient"/> so that validation messages follow the culture of the request.</para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is <see cref="TranslationCultureSource.Default"/>, which would be circular, or is not a defined value.</exception>
        public static TranslationCultureSource CultureSource
        {
            get => _cultureSource;

            set
            {
                if (value != TranslationCultureSource.TranslationManager && value != TranslationCultureSource.Ambient)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The default culture source must be either TranslationManager or Ambient.");

                _cultureSource = value;
            }
        }

        /// <summary>
        /// Registers the translation manager to use for the given localization class, so that the resolver does not have to look the manager up through reflection.
        /// <para>Call this method for a trimmed or NativeAOT application that refers to a translation key rather than to a translation unit member, and for a nested group class. Registrations are consulted
        /// before any reflection takes place.</para>
        /// </summary>
        /// <param name="translationClass">The class created by Tlumach Generator, or a nested group class inside one, as it appears in the annotation.</param>
        /// <param name="manager">The translation manager that provides the messages of that class.</param>
        /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null"/>.</exception>
        public static void RegisterTranslationManager(Type translationClass, TranslationManager manager)
        {
            if (translationClass is null)
                throw new ArgumentNullException(nameof(translationClass));

            if (manager is null)
                throw new ArgumentNullException(nameof(manager));

            TlumachMessageResolver.Managers[translationClass] = manager;
        }

        /// <summary>
        /// Removes the registration made by <see cref="RegisterTranslationManager(Type, TranslationManager)"/>.
        /// </summary>
        /// <param name="translationClass">The class, whose registration is to be removed.</param>
        /// <returns><see langword="true"/> when a registration was removed; <see langword="false"/> when there was none.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="translationClass"/> is <see langword="null"/>.</exception>
        public static bool UnregisterTranslationManager(Type translationClass)
        {
            if (translationClass is null)
                throw new ArgumentNullException(nameof(translationClass));

            return TlumachMessageResolver.Managers.TryRemove(translationClass, out _);
        }

        /// <summary>
        /// Drops every cached message binding.
        /// <para>An application does not need to call this method: a binding refers to a translation unit or to a translation manager and never holds text, so a change of the culture or a reload of a
        /// translation is picked up without it. It exists for tests and for hosts that unload assemblies with localization classes.</para>
        /// </summary>
        public static void ResetResolutionCache()
        {
            TlumachMessageResolver.Bindings.Clear();
        }

        /// <summary>
        /// Tells whether a binding for the given localization class and key is present in the cache. Intended for tests that verify that repeated validation does not resolve the annotation again.
        /// </summary>
        /// <param name="translationClass">The class as it appears in the annotation.</param>
        /// <param name="translationKey">The key as it appears in the annotation.</param>
        /// <returns><see langword="true"/> when the annotation has already been resolved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null"/>.</exception>
        public static bool IsResolutionCached(Type translationClass, string translationKey)
        {
            if (translationClass is null)
                throw new ArgumentNullException(nameof(translationClass));

            if (translationKey is null)
                throw new ArgumentNullException(nameof(translationKey));

            return TlumachMessageResolver.Bindings.ContainsKey((translationClass, translationKey));
        }
    }
}
