// <copyright file="TranslationManagerResolver.cs" company="Allied Bits Ltd.">
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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Tlumach.Base;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// Finds the translation manager for a localizer: from the options, or from the class created by Tlumach Generator.
    /// Shared by the string localizer factory and the HTML localizer factory of Tlumach.AspNetCore.Mvc.
    /// </summary>
    public static class TranslationManagerResolver
    {
        /// <summary>
        /// Tells whether the options carry a source of the translation manager.
        /// </summary>
        /// <param name="options">The options to check.</param>
        /// <returns><see langword="true"/> if <see cref="TlumachLocalizationOptions.TranslationManager"/>, <see cref="TlumachLocalizationOptions.Configuration"/>, or <see cref="TlumachLocalizationOptions.DefaultFile"/> is set; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        public static bool HasManagerSource(TlumachLocalizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.TranslationManager is not null || options.Configuration is not null || !string.IsNullOrEmpty(options.DefaultFile);
        }

        /// <summary>
        /// Returns the assembly that contains the <see cref="TlumachLocalizationOptions.DefaultFile"/> of the options: <see cref="TlumachLocalizationOptions.Assembly"/> if it is set,
        /// otherwise the entry assembly of the process (<see cref="Assembly.GetEntryAssembly"/>), i.e. the application.
        /// <para>All localizers of Tlumach (string, HTML, and view localizers, model binding messages, and display names) use this rule, so the same options load the same file everywhere.
        /// A library that embeds its own translation files must set <see cref="TlumachLocalizationOptions.Assembly"/>. When there is no entry assembly (e.g. in some unmanaged hosts), this method returns
        /// <see langword="null"/>, and the file can then only be loaded from the disk.</para>
        /// </summary>
        /// <param name="options">The options to take the assembly from.</param>
        /// <returns>The assembly of the default file, or <see langword="null"/> if the options name no assembly and the process has no entry assembly.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        public static Assembly? GetDefaultFileAssembly(TlumachLocalizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return options.Assembly ?? Assembly.GetEntryAssembly();
        }

        /// <summary>
        /// Returns the translation manager that the options describe: the one set in <see cref="TlumachLocalizationOptions.TranslationManager"/>, a new one for <see cref="TlumachLocalizationOptions.Configuration"/>,
        /// or a new one for the <see cref="TlumachLocalizationOptions.DefaultFile"/> that is loaded from the assembly that <see cref="GetDefaultFileAssembly"/> returns.
        /// </summary>
        /// <param name="options">The options to take the manager from.</param>
        /// <returns>The translation manager.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The options have none of the <c>TranslationManager</c>, <c>Configuration</c>, and <c>DefaultFile</c> properties set.</exception>
        public static TranslationManager CreateFromOptions(TlumachLocalizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.TranslationManager is not null)
                return options.TranslationManager;

            if (options.Configuration is not null)
                return new TranslationManager(options.Configuration);

            if (!string.IsNullOrEmpty(options.DefaultFile))
                return new TranslationManager(new TranslationConfiguration(GetDefaultFileAssembly(options), options.DefaultFile, options.DefaultFileLocale, options.TextProcessingMode ?? TextFormat.DotNet));

            throw new ArgumentException("The options must set TranslationManager, Configuration, or DefaultFile.", nameof(options));
        }

        /// <summary>
        /// Returns the translation manager that the class created by Tlumach Generator exposes in its static <c>TranslationManager</c> property.
        /// <para>This method uses reflection. Its <paramref name="resourceSource"/> parameter is annotated with <see cref="DynamicallyAccessedMembersAttribute"/>, so a caller that passes a type known at compile time keeps the property under trimming.
        /// A caller that receives the type from code without such an annotation (such as <c>IStringLocalizerFactory.Create(Type)</c>) cannot guarantee that, so this reflection is not supported in trimmed or NativeAOT applications there. Such applications should supply the manager through the properties of <see cref="TlumachLocalizationOptions"/> instead.</para>
        /// </summary>
        /// <param name="resourceSource">The type of the class created by Tlumach Generator.</param>
        /// <returns>The translation manager of the class.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resourceSource"/> is <see langword="null"/>.</exception>
        /// <exception cref="TlumachException">The class has no public static <c>TranslationManager</c> property, or the property returns <see langword="null"/>.</exception>
        public static TranslationManager FromGeneratedClass([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type resourceSource)
        {
            ArgumentNullException.ThrowIfNull(resourceSource);

            const BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy;

            var prop = resourceSource.GetProperty("TranslationManager", flags);
            if (prop == null)
                throw new TlumachException("Could not obtain the TranslationManager property from the specified class. Please, double-check that you pass the right class.");

            object? manager = prop.GetValue(null);
            if (manager is null)
                throw new TlumachException("Could not obtain the value of the TranslationManager property from the specified class. Please, double-check that you pass the right class.");

            return (TranslationManager)manager;
        }
    }
}
