// <copyright file="TlumachMessageResolver.cs" company="Allied Bits Ltd.">
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

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

using Tlumach.Base;

namespace Tlumach.DataAnnotations
{
    /// <summary>
    /// Resolves the localized message of a validation attribute from a class created by Tlumach Generator.
    /// <para>An instance of this class is composed into every localized validation attribute of Tlumach. It is also available to an attribute that an application derives from
    /// <see cref="TlumachValidationAttribute"/> and that needs the message template itself.</para>
    /// </summary>
    /// <remarks>
    /// A resolved binding refers either to a translation unit or to a translation manager together with a key, and never holds text, so a change of the culture and a reload of a translation are picked
    /// up on the next call. Bindings are cached process-wide, which keeps validation free of reflection after the first message of each annotation has been formatted.
    /// </remarks>
    public sealed class TlumachMessageResolver
    {
        /// <summary>
        /// The resolved bindings, keyed by the localization class and the key as they appear in the annotation.
        /// <para>The default comparers apply: reference equality for the class, which is correct because there is one <see cref="Type"/> instance per class per load context, and ordinal comparison for
        /// the key. Ordinal is deliberate: member names are case-sensitive, and the worst outcome of two spellings of one translation key is two cache entries that both resolve correctly.</para>
        /// </summary>
        internal static readonly ConcurrentDictionary<(Type Class, string Key), ITlumachMessageBinding> Bindings = new();

        /// <summary>
        /// The translation managers registered through <see cref="TlumachValidationDefaults.RegisterTranslationManager(Type, TranslationManager)"/>.
        /// </summary>
        internal static readonly ConcurrentDictionary<Type, TranslationManager> Managers = new();

        private const BindingFlags MemberFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        private const string BothMustBeSetFormat = "Both the TranslationClass and the TranslationKey properties of '{0}' must be set; only {1} was set.";

        private const string ConflictingSourceFormat = "The TranslationKey property of '{0}' cannot be combined with the ErrorMessage or the ErrorMessageResourceName property. Choose one source of the message.";

        private const string NoTextFormat = "The Tlumach translation key '{0}' of the class '{1}' provides no text for the culture '{2}'.";

        private const string WrongMemberTypeFormat = "The member '{0}.{1}' is of the type '{2}' and not a translation unit. Pass the value of the key constant created by Tlumach Generator, such as '{1}Key', rather than its name.";

        private const string NoUnitAndNoManagerFormat = "The class '{0}' has no public static member named '{1}' of a type derived from Tlumach.BaseTranslationUnit, and neither it nor any class that declares it exposes a public static TranslationManager property. Pass the name of a translation unit created by Tlumach Generator, or call TlumachValidationDefaults.RegisterTranslationManager.";

        private const string KeyNotFoundFormat = "The Tlumach translation key '{0}' was not found through the translation manager of the class '{1}'.";

        private const string DeclaringTypeWalkJustification = "Type.DeclaringType carries no DynamicallyAccessedMembers annotation, so the walk towards the declaring class cannot be annotated. A trimmed application that refers to a nested group class by a translation key rather than by the name of a translation unit member must either name the member, which is the annotated path, or register the translation manager through TlumachValidationDefaults.RegisterTranslationManager, which is checked before any reflection at every level of the walk.";

        private readonly ITlumachLocalizedErrorMessage _owner;

        private ITlumachMessageBinding? _binding;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachMessageResolver"/> class for the given attribute.
        /// </summary>
        /// <param name="owner">The attribute that provides the configuration. Its properties are read lazily, so a resolver may be created in the constructor of an attribute, before the named properties of the annotation have been assigned.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is <see langword="null"/>.</exception>
        public TlumachMessageResolver(ITlumachLocalizedErrorMessage owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Gets the attribute that provides the configuration.
        /// </summary>
        public ITlumachLocalizedErrorMessage Owner => _owner;

        /// <summary>
        /// Tells whether the attribute is configured to take its message from a Tlumach translation.
        /// <para>When this method returns <see langword="false"/>, the attribute carries no Tlumach configuration at all and must format its message the way it would without Tlumach. That is not an
        /// error: it makes a localized attribute behave exactly like the attribute it derives from.</para>
        /// </summary>
        /// <returns><see langword="true"/> when both the localization class and the key are set.</returns>
        /// <exception cref="InvalidOperationException">Thrown when only one of the localization class and the key is set, or when a Tlumach key is combined with a literal error message or with a resource name.</exception>
        public bool IsConfigured()
        {
            bool hasClass = _owner.TranslationClass is not null;
            bool hasKey = !string.IsNullOrEmpty(_owner.TranslationKey);

            if (hasClass != hasKey)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BothMustBeSetFormat, _owner.GetType().FullName, hasClass ? "TranslationClass" : "TranslationKey"));

            if (hasKey && (_owner.ErrorMessage is not null || _owner.ErrorMessageResourceName is not null))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ConflictingSourceFormat, _owner.GetType().FullName));

            return hasClass;
        }

        /// <summary>
        /// Returns the message template for the effective culture, without processing the placeholders it contains.
        /// </summary>
        /// <returns>The message template as it appears in the translation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the attribute is not configured for Tlumach, when the configuration is invalid, or when the key provides no text for the effective culture.</exception>
        public string GetTemplate()
        {
            if (!IsConfigured())
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "No Tlumach translation is configured on '{0}'.", _owner.GetType().FullName));

            ITlumachMessageBinding? binding = _binding;
            if (binding is null)
            {
                Type translationClass = _owner.TranslationClass!;
                string key = _owner.TranslationKey!;

                if (!Bindings.TryGetValue((translationClass, key), out binding))
                    binding = ResolveBinding(translationClass, key);

                _binding = binding;
            }

            TranslationCultureSource cultureSource = EffectiveCultureSource();
            string template = binding.GetTemplate(cultureSource);

            if (string.IsNullOrEmpty(template))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, NoTextFormat, _owner.TranslationKey, _owner.TranslationClass!.FullName, binding.GetCulture(cultureSource).Name));

            return template;
        }

        /// <summary>
        /// Formats the localized message the way <see cref="ValidationAttribute.FormatErrorMessage(string)"/> does.
        /// </summary>
        /// <param name="name">The display name of the member being validated. It becomes the <c>{0}</c> argument of the template.</param>
        /// <param name="arguments">The remaining arguments of the template, which become <c>{1}</c>, <c>{2}</c> and so on. Pass them in the same order as the attribute that is being localized.</param>
        /// <returns>The formatted message.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the message cannot be resolved. See <see cref="GetTemplate"/>.</exception>
        /// <exception cref="FormatException">Thrown when the template refers to more arguments than were provided.</exception>
        public string Format(string name, params object?[] arguments)
        {
            string template = GetTemplate();

            object?[] allArguments;
            if (arguments is null || arguments.Length == 0)
            {
                allArguments = new object?[] { name };
            }
            else
            {
                allArguments = new object?[arguments.Length + 1];
                allArguments[0] = name;
                Array.Copy(arguments, 0, allArguments, 1, arguments.Length);
            }

            // CultureInfo.CurrentCulture, always: this is the culture that formats the numbers and the dates of the arguments, exactly as the stock attributes do, and it is independent of the culture
            // for which the template itself was read.
            return string.Format(CultureInfo.CurrentCulture, template, allArguments);
        }

        private static ITlumachMessageBinding ResolveBinding([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type translationClass, string key)
        {
            // Path 1: a translation unit member of the given class. This covers both the field that Generator emits by default and the property it emits when delayedUnitsCreation is set.
            FieldInfo? field = translationClass.GetField(key, MemberFlags);
            if (field is not null)
            {
                if (!typeof(BaseTranslationUnit).IsAssignableFrom(field.FieldType))
                    throw WrongMemberType(translationClass, key, field.FieldType);

                return Cache(translationClass, key, new UnitMessageBinding(UnitOf(field.GetValue(null), translationClass, key)));
            }

            PropertyInfo? property;
            try
            {
                property = translationClass.GetProperty(key, MemberFlags);
            }
            catch (AmbiguousMatchException ex)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The class '{0}' declares more than one member named '{1}'.", translationClass.FullName, key), ex);
            }

            if (property is not null)
            {
                if (!property.CanRead || !typeof(BaseTranslationUnit).IsAssignableFrom(property.PropertyType))
                    throw WrongMemberType(translationClass, key, property.PropertyType);

                return Cache(translationClass, key, new UnitMessageBinding(UnitOf(property.GetValue(null), translationClass, key)));
            }

            // Path 2: the translation manager of the class, or of a class that declares it, together with the key of the translation entry.
            TranslationManager? manager = FindTranslationManager(translationClass, out string groupPath);

            if (manager is null)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, NoUnitAndNoManagerFormat, translationClass.FullName, key));

            // The key constant that Generator emits for a key inside a group holds the own name of the key rather than the whole dotted key, so the group-qualified form is tried first.
            string[] candidates = groupPath.Length == 0 ? new[] { key } : new[] { groupPath + key, key };

            CultureInfo probeCulture = manager.CurrentCulture;
            foreach (string candidate in candidates)
            {
                TranslationEntry entry = manager.GetValue(candidate, probeCulture);
                if (!string.IsNullOrEmpty(entry.Text))
                    return Cache(translationClass, key, new ManagerMessageBinding(manager, candidate));
            }

            // Deliberately not cached: a translation that arrives later, for example through the OnTranslationFileNotFound event, then starts working without a reset of the cache.
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, KeyNotFoundFormat, string.Join("' or '", candidates), translationClass.FullName));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = DeclaringTypeWalkJustification)]
        [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern", Justification = DeclaringTypeWalkJustification)]
        private static TranslationManager? FindTranslationManager(Type translationClass, out string groupPath)
        {
            groupPath = string.Empty;

            Type? walker = translationClass;
            while (walker is not null)
            {
                if (Managers.TryGetValue(walker, out TranslationManager? registered))
                    return registered;

                PropertyInfo? property = walker.GetProperty("TranslationManager", MemberFlags);
                if (property is not null && property.CanRead && typeof(TranslationManager).IsAssignableFrom(property.PropertyType) && property.GetValue(null) is TranslationManager manager)
                    return manager;

                // A group class created by Tlumach Generator is a nested static class and does not inherit the members of the class that declares it, so walking towards the declaring class is required.
                groupPath = walker.Name + "." + groupPath;
                walker = walker.DeclaringType;
            }

            return null;
        }

        private static BaseTranslationUnit UnitOf(object? value, Type translationClass, string key)
        {
            if (value is BaseTranslationUnit unit)
                return unit;

            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The member '{0}.{1}' is null and cannot provide a validation message.", translationClass.FullName, key));
        }

        private static InvalidOperationException WrongMemberType(Type translationClass, string key, Type memberType)
            => new(string.Format(CultureInfo.CurrentCulture, WrongMemberTypeFormat, translationClass.FullName, key, memberType.FullName));

        private static ITlumachMessageBinding Cache(Type translationClass, string key, ITlumachMessageBinding binding)
        {
            // GetOrAdd may run in parallel with another resolution of the same annotation. A binding is immutable and fully built before it is published, so the loser of such a race is equivalent to
            // the winner and is simply collected.
            return Bindings.GetOrAdd((translationClass, key), binding);
        }

        private TranslationCultureSource EffectiveCultureSource()
        {
            TranslationCultureSource source = _owner.CultureSource;

            return source == TranslationCultureSource.Default ? TlumachValidationDefaults.CultureSource : source;
        }
    }
}
