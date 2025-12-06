// <copyright file="TlumachStringLocalizer.cs" company="Allied Bits Ltd.">
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

using System.Globalization;
using System.Reflection;

using Microsoft.Extensions.Localization;

using Tlumach.Base;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// The class that provides localization functionality.
    /// </summary>
    public class TlumachStringLocalizer : IStringLocalizer
    {
        private readonly TranslationManager _manager;
        private TextFormat? _textProcessingMode;
        private CultureInfo _culture;

        internal TlumachStringLocalizer(TranslationManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);
            _manager = manager;
            _textProcessingMode = TextFormat.DotNet;
            _culture = CultureInfo.CurrentCulture;
        }

        internal TlumachStringLocalizer(TlumachLocalizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.TranslationManager is not null)
                _manager = options.TranslationManager;
            else
            if (options.Configuration is not null)
                _manager = new TranslationManager(options.Configuration);
            else
            if (!string.IsNullOrEmpty(options.DefaultFile))
                _manager = new TranslationManager(new TranslationConfiguration(options.Assembly ?? Assembly.GetCallingAssembly(), options.DefaultFile, options.DefaultFileLocale, options.TextProcessingMode ?? TextFormat.DotNet));
            else
                throw new ArgumentException("Options passed to TlumachStringLocalizer must have either TranslationMAnager, Configuration, or DefaultFile property set.", nameof(options));

            _textProcessingMode = options.TextProcessingMode;
            _culture = CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>If the string contains placeholders, they are replaced with the placeholder names. To provide values for placeholders, use the <see cref="this[string, object[]]"/> property.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <returns>The value of the string with an indicator of whether the localized string was found (if the resource was not found, the value from the default translation is returned).</returns>
        public LocalizedString this[string name]
        {
            get
            {
                string text;
                if (_manager.DefaultConfiguration is null)
                    return new LocalizedString(name, name, true);

                TranslationEntry entry = _manager.GetValue(_manager.DefaultConfiguration, name, _culture, out bool found);

                /*if (!found)
                    return new LocalizedString(name, name, true);*/

                if (!entry.ContainsPlaceholders)
                {
                    text = entry.Text ?? string.Empty;
                }
                else
                {
                    text = entry.ProcessTemplatedValue(_culture, _textProcessingMode ?? _manager.DefaultConfiguration.TextProcessingMode ?? TextFormat.DotNet, static (name, _) => name);
                }

                return new LocalizedString(name, text, !found);
            }
        }

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>If the string contains placeholders, they are replaced with the placeholder values provided in the <paramref name="arguments"/> parameter.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <param name="arguments">The list of values to use to replace placeholders.</param>
        /// <returns>The value of the string with an indicator of whether the localized string was found (if the resource was not found, the value from the default translation is returned).</returns>
        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                string text;
                if (_manager.DefaultConfiguration is null)
                    return new LocalizedString(name, name, true);

                TranslationEntry entry = _manager.GetValue(_manager.DefaultConfiguration, name, _culture, out bool found);

                /*if (!found)
                    return new LocalizedString(name, name, true);*/

                if (!entry.ContainsPlaceholders)
                {
                    text = entry.Text ?? string.Empty;
                }
                else
                {
                    text = entry.ProcessTemplatedValue(_culture, _textProcessingMode ?? _manager.DefaultConfiguration.TextProcessingMode ?? TextFormat.DotNet, arguments);
                }

                return new LocalizedString(name, text, !found);
            }
        }

        /// <summary>
        /// Returns all localized strings contained in the translation for the given culture and, optionally, its parent cultures.
        /// </summary>
        /// <param name="includeParentCultures">Indicates whether the strings from the parent cultures should be returned.</param>
        /// <returns>The list of the localized strings. If the values from multiple cultures are returned, the string keys may overlap (Tlumach does not reduce the list to just unique keys).</returns>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            Translation? translation = _manager.GetTranslation(_culture);

            List<LocalizedString> result = [];
            while (true)
            {
                // retrieve the keys of the translation if one exists.
                if (translation is not null)
                {
                    foreach (var key in translation.Keys)
                    {
                        result.Add(new LocalizedString(key, translation[key].Text ?? string.Empty));
                    }
                }

                if (includeParentCultures)
                {
                    var parentCulture = _culture.Parent;
                    translation = _manager.GetTranslation(parentCulture);

                    // If we got to the invariant culture, there will be no more parent and we may break;
                    if (parentCulture.Name.Length == 0)
                        break;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Switches the culture, used by the localizer object when retrieving localized text.
        /// </summary>
        /// <param name="culture">The new culture to use.</param>
        /// <returns>The object, whose method was called.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            _culture = culture;
            return this;
        }

        /// <summary>
        /// Switches the text processing mode, used by the localizer object when processing text which contains placeholders.
        /// </summary>
        /// <param name="textProcessingMode">The mode to use.</param>
        /// <returns>The object, whose method was called.</returns>
        public IStringLocalizer WithTextProcessingMode(TextFormat textProcessingMode)
        {
            _textProcessingMode = textProcessingMode;
            return this;
        }
    }

    /// <summary>
    /// Represents an IStringLocalizer that provides strings for T.
    /// </summary>
    /// <typeparam name="T">The type that defines the context of localization. Please refer to the topic on Dependency Injection in Tlumach documentation for details.</typeparam>
    public sealed class TlumachStringLocalizer<T> : IStringLocalizer<T>
    {
        private readonly IStringLocalizer _inner;

        public TlumachStringLocalizer(IStringLocalizerFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            _inner = factory.Create(typeof(T));
        }

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>If the string contains placeholders, they are replaced with the placeholder names. To provide values for placeholders, use the <see cref="this[string, object[]]"/> property.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <returns>The value of the string with an indicator of whether the localized string was found (if the resource was not found, the value from the default translation is returned).</returns>
        public LocalizedString this[string name]
            => _inner[name];

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>If the string contains placeholders, they are replaced with the placeholder values provided in the <paramref name="arguments"/> parameter.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <param name="arguments">The list of values to use to replace placeholders.</param>
        /// <returns>The value of the string with an indicator of whether the localized string was found (if the resource was not found, the value from the default translation is returned).</returns>
        public LocalizedString this[string name, params object[] arguments]
            => _inner[name, arguments];

        /// <summary>
        /// Returns all localized strings contained in the translation for the given culture and, optionally, its parent cultures.
        /// </summary>
        /// <param name="includeParentCultures">Indicates whether the strings from the parent cultures should be returned.</param>
        /// <returns>The list of the localized strings. If the values from multiple cultures are returned, the string keys may overlap (Tlumach does not reduce the list to just unique keys).</returns>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => _inner.GetAllStrings(includeParentCultures);

        /// <summary>
        /// Switches the culture, used by the localizer object when retrieving localized text.
        /// </summary>
        /// <param name="culture">The new culture to use.</param>
        /// <returns>The localizer object to be used.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
            => ((TlumachStringLocalizer)_inner).WithCulture(culture);

        /// <summary>
        /// Switches the text processing mode, used by the localizer object when processing text which contains placeholders.
        /// </summary>
        /// <param name="textProcessingMode">The mode to use.</param>
        /// <returns>The localizer object to be used.</returns>
        public IStringLocalizer WithTextProcessingMode(TextFormat textProcessingMode)
            => ((TlumachStringLocalizer)_inner).WithTextProcessingMode(textProcessingMode);
    }
}
