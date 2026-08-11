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
using System.Text.Encodings.Web;

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
        private readonly TextFormat? _textProcessingMode;
        private readonly CultureInfo? _explicitCulture;

        internal TlumachStringLocalizer(TranslationManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);
            _manager = manager;
            _textProcessingMode = TextFormat.DotNet;
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachStringLocalizer"/> class from an existing one, changing the culture or the text processing mode.
        /// </summary>
        /// <param name="source">The localizer to copy the translation manager from.</param>
        /// <param name="culture">The culture to use, or <see langword="null"/> to follow the culture of the thread.</param>
        /// <param name="textProcessingMode">The text processing mode to use.</param>
        private TlumachStringLocalizer(TlumachStringLocalizer source, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            _manager = source._manager;
            _explicitCulture = culture;
            _textProcessingMode = textProcessingMode;
        }

        /// <summary>
        /// Gets the culture, for which the strings are retrieved.
        /// <para>Unless the application asked for a particular culture through <see cref="WithCulture(CultureInfo)"/>, this is <see cref="CultureInfo.CurrentCulture"/> read at the moment of the call. A
        /// localizer created while the application starts therefore follows the culture of every request of a web application instead of staying with the culture of the startup.</para>
        /// </summary>
        private CultureInfo Culture => _explicitCulture ?? CultureInfo.CurrentCulture;

        /// <summary>
        /// Gets the location that was searched for the strings, reported to the consumers through <see cref="LocalizedString.SearchedLocation"/>.
        /// </summary>
        private string? SearchedLocation => _manager.DefaultConfiguration?.DefaultFile;

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>The value is the text of the translation entry as it stands, with the placeholders it contains left untouched, so that it can be used as a format string. To have the placeholders replaced
        /// with values, use the <see cref="this[string, object[]]"/> property.</para>
        /// <para>When the key is not present in any translation, the value is the key itself and <see cref="LocalizedString.ResourceNotFound"/> is <see langword="true"/>, which is the behaviour that the
        /// consumers of <see cref="IStringLocalizer"/> expect.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <returns>The value of the string with an indicator of whether it was found.</returns>
        public LocalizedString this[string name]
        {
            get
            {
                if (_manager.DefaultConfiguration is null)
                    return NotFound(name);

                TranslationEntry entry = _manager.GetValue(_manager.DefaultConfiguration, name, Culture, out _);

                if (entry.Text is null)
                    return NotFound(name);

                return Found(name, entry.Text);
            }
        }

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>If the string contains placeholders, they are replaced with the placeholder values provided in the <paramref name="arguments"/> parameter.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <param name="arguments">The list of values to use to replace placeholders.</param>
        /// <returns>The value of the string with an indicator of whether it was found.</returns>
        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (_manager.DefaultConfiguration is null)
                    return NotFound(name);

                CultureInfo culture = Culture;

                TranslationEntry entry = _manager.GetValue(_manager.DefaultConfiguration, name, culture, out _);

                if (entry.Text is null)
                    return NotFound(name);

                string text = entry.ContainsPlaceholders
                    ? entry.ProcessTemplatedValue(culture, _textProcessingMode ?? _manager.DefaultConfiguration.TextProcessingMode ?? TextFormat.DotNet, arguments)
                    : entry.Text;

                return Found(name, text);
            }
        }

        /// <summary>
        /// Returns all localized strings contained in the translation for the current culture and, optionally, for its parent cultures and the default translation.
        /// <para>Every key appears once. When a key is present in several translations of the chain, the value of the most specific culture wins, which is the behaviour that the consumers of
        /// <see cref="IStringLocalizer"/> expect.</para>
        /// </summary>
        /// <param name="includeParentCultures">Indicates whether the strings from the parent cultures and from the default translation should be returned as well.</param>
        /// <returns>The list of the localized strings.</returns>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            Dictionary<string, LocalizedString> result = new(StringComparer.OrdinalIgnoreCase);
            string? searchedLocation = SearchedLocation;

            CultureInfo? culture = Culture;
            while (culture is not null)
            {
                CollectStrings(_manager.GetTranslation(culture, tryLoadMissing: true), result, searchedLocation);

                if (!includeParentCultures || culture.Name.Length == 0)
                    break;

                CultureInfo parent = culture.Parent;

                // A culture is its own parent once the chain ends, which is where the loop has to stop.
                culture = parent.Name.Equals(culture.Name, StringComparison.Ordinal) ? null : parent;
            }

            if (includeParentCultures)
            {
                // The default translation is the last fallback. It is only a separate step when the configuration names a locale for the default file; otherwise the invariant culture of the chain above
                // has already covered it.
                string? defaultLocale = _manager.DefaultConfiguration?.DefaultFileLocale;
                if (!string.IsNullOrEmpty(defaultLocale))
                    CollectStrings(_manager.GetTranslation(new CultureInfo(defaultLocale), tryLoadMissing: true), result, searchedLocation);
            }

            return result.Values;
        }

        /// <summary>
        /// Returns a localizer that retrieves the strings for the given culture instead of the culture of the thread.
        /// </summary>
        /// <param name="culture">The culture to use.</param>
        /// <returns>A new localizer. The one whose method was called is left untouched.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return new TlumachStringLocalizer(this, culture, _textProcessingMode);
        }

        /// <summary>
        /// Returns a localizer that uses the given text processing mode when it processes text which contains placeholders.
        /// </summary>
        /// <param name="textProcessingMode">The mode to use.</param>
        /// <returns>A new localizer. The one whose method was called is left untouched.</returns>
        public IStringLocalizer WithTextProcessingMode(TextFormat textProcessingMode)
        {
            return new TlumachStringLocalizer(this, _explicitCulture, textProcessingMode);
        }

        private static void CollectStrings(Translation? translation, Dictionary<string, LocalizedString> result, string? searchedLocation)
        {
            if (translation is null)
                return;

            foreach (var key in translation.Keys)
            {
                // The chain is walked from the most specific culture outwards, so the first value found for a key is the one to keep.
                if (!result.ContainsKey(key))
                    result.Add(key, new LocalizedString(key, translation[key].Text ?? string.Empty, resourceNotFound: false, searchedLocation ?? string.Empty));
            }
        }

        private LocalizedString NotFound(string name)
            => new(name, name, resourceNotFound: true, SearchedLocation ?? string.Empty);

        private LocalizedString Found(string name, string text)
            => new(name, _manager.WebEncodeValues ? HtmlEncoder.Default.Encode(text) : text, resourceNotFound: false, SearchedLocation ?? string.Empty);
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
        /// <para>The value is the text of the translation entry as it stands, with the placeholders it contains left untouched. To have the placeholders replaced with values, use the
        /// <see cref="this[string, object[]]"/> property.</para>
        /// <para>When the key is not present in any translation, the value is the key itself and <see cref="LocalizedString.ResourceNotFound"/> is <see langword="true"/>.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <returns>The value of the string with an indicator of whether it was found.</returns>
        public LocalizedString this[string name]
            => _inner[name];

        /// <summary>
        /// Gets the localized string with the given name (key).
        /// <para>If the string contains placeholders, they are replaced with the placeholder values provided in the <paramref name="arguments"/> parameter.</para>
        /// <para>When the key is not present in any translation, the value is the key itself and <see cref="LocalizedString.ResourceNotFound"/> is <see langword="true"/>.</para>
        /// </summary>
        /// <param name="name">The name (key) of the string to return.</param>
        /// <param name="arguments">The list of values to use to replace placeholders.</param>
        /// <returns>The value of the string with an indicator of whether it was found.</returns>
        public LocalizedString this[string name, params object[] arguments]
            => _inner[name, arguments];

        /// <summary>
        /// Returns all localized strings contained in the translation for the current culture and, optionally, for its parent cultures and the default translation. Every key appears once.
        /// </summary>
        /// <param name="includeParentCultures">Indicates whether the strings from the parent cultures and from the default translation should be returned as well.</param>
        /// <returns>The list of the localized strings.</returns>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => _inner.GetAllStrings(includeParentCultures);

        /// <summary>
        /// Returns a localizer that retrieves the strings for the given culture instead of the culture of the thread.
        /// </summary>
        /// <param name="culture">The culture to use.</param>
        /// <returns>A new localizer. The one whose method was called is left untouched.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
            => ((TlumachStringLocalizer)_inner).WithCulture(culture);

        /// <summary>
        /// Returns a localizer that uses the given text processing mode when it processes text which contains placeholders.
        /// </summary>
        /// <param name="textProcessingMode">The mode to use.</param>
        /// <returns>A new localizer. The one whose method was called is left untouched.</returns>
        public IStringLocalizer WithTextProcessingMode(TextFormat textProcessingMode)
            => ((TlumachStringLocalizer)_inner).WithTextProcessingMode(textProcessingMode);
    }
}
