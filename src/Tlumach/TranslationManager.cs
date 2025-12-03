// <copyright file="TranslationManager.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach
{
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

    /// <summary>
    /// The core of Tlumach that manages translations, provides functions to enumerate translation files, and controls current language and locale used for translations.
    /// <para>Applications can use it to retrieve translation entries by their string key.</para>
    /// </summary>
    public class TranslationManager
    {
        /// <summary>
        /// A container for all translations managed by this class.
        /// </summary>
        private readonly Dictionary<string, Translation> _translations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The configuration to use for loading translations.
        /// </summary>
        private readonly TranslationConfiguration? _defaultConfig;

        /// <summary>
        /// The default translation that is used as a fallback.
        /// </summary>
        private Translation? _defaultTranslation;

        /*/// <summary>
        /// The translation that corresponds to the current culture.
        /// </summary>
        private Translation? _currentTranslation;*/

        private CultureInfo _culture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Gets or sets the indicator that tells TranslationManager to attempt to locate translation files on the disk.
        /// </summary>
        public bool LoadFromDisk { get; set; }

        /// <summary>
        /// Gets or sets the directory in which translations files are looked for.
        /// <para>When <see cref="LoadFromDisk"/> is disabled, this value is used when trying to load the translations from the assembly. When <see cref="LoadFromDisk"/> is enabled, this value is also used when trying to locate translation files on the disk.</para>
        /// <para>This property is a hint for the manager, which affects loading of secondary translation files.
        /// When a configuration is loaded via the <seealso cref="TranslationManager"/> constructor, specify the directory in the name of the configuration file.</para>
        /// </summary>
        public string TranslationsDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the culture, which will be used by the <see cref="GetValue(string)"/> method as a current culture.
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get => _culture;
            set
            {
#pragma warning disable MA0015
                if (value is null)
                    throw new ArgumentNullException("CurrentCulture");
#pragma warning restore MA0015
                // Update the culture only if current culture is not the same as the one in the argument
                if (!value.Name.Equals(_culture.Name, StringComparison.Ordinal))
                {
                    _culture = value;

                    // Notify listeners about the change
                    OnCultureChanged?.Invoke(this, new CultureChangedEventArgs(_culture));
                }
            }
        }

        /// <summary>
        /// Gets the configuration used by this Translation Manager. May be empty if it was not set explicitly or by the generated class (when the Generator is used).
        /// </summary>
        public TranslationConfiguration? DefaultConfiguration => _defaultConfig;

        /// <summary>
        /// The event is fired when the content of the file is to be loaded. A handler can provide file content from another location.
        /// </summary>
        public event EventHandler<FileContentNeededEventArgs>? OnFileContentNeeded;

        /// <summary>
        /// The event is fired when the translation of a certain key is requested. A handler can provide a different text or even a reference which will be resolved.
        /// If the returned values are valid and accepted (e.g., a reference is properly resolved), the value is returned without firing <seealso cref="OnTranslationValueFound"/>.
        /// </summary>
        public event EventHandler<TranslationValueEventArgs>? OnTranslationValueNeeded;

        /// <summary>
        /// The event is fired after a translation of a certain key has been found in a file.
        /// <para>Should a handler need to provide a different value, it may change the text in the <seealso cref="TranslationValueEventArgs.Text"/> property or replace the reference in the <seealso cref="TranslationValueEventArgs.Entry"/> property of the arguments.</para>
        /// </summary>
        public event EventHandler<TranslationValueEventArgs>? OnTranslationValueFound;

        /// <summary>
        /// The event is fired after a translation of a certain key has not been found in a file.
        /// <para>Should a handler decide to provide some value, it may set the text in the <seealso cref="TranslationValueEventArgs.Text"/> property or place a reference in the <seealso cref="TranslationValueEventArgs.Entry"/> property of the arguments.</para>
        /// </summary>
        public event EventHandler<TranslationValueEventArgs>? OnTranslationValueNotFound;

        /// <summary>
        /// The event is fired when the <see cref="CurrentCulture"/> property is changed by the application.
        /// It is used primarily by the reactive classes in XAML packages (Tlumach.MAUI, Tlumach.Avalonia, Tlumach.WPF, Tlumach.WinUI).
        /// </summary>
        public event EventHandler<CultureChangedEventArgs>? OnCultureChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationManager"/> class.
        /// <para>
        /// This constructor creates a translation manager based on the configuration that is to be loaded from the disk file.
        /// Such translation manager can be used to simplify access to translations when translation units are not used - an application simply calls the GetValue method and species the key and, optionally, the culture.
        /// </para>
        /// </summary>
        /// <param name="configFile">The file with the configuration that specifies where to load translations from.</param>
        /// <exception cref="GenericParserException">Thrown if the parser for the specified configuration file is not found.</exception>
        /// <exception cref="ParserLoadException">Thrown if the parser failed to load the configuration file.</exception>
        public TranslationManager(string configFile)
        {
            if (configFile is null)
                throw new ArgumentNullException(nameof(configFile));

            string filename = configFile.Trim();

            // The config parser will parse configuration and will find the correct parser for the files referenced by the configuration
            BaseParser? parser = FileFormats.GetConfigParser(Path.GetExtension(filename));
            if (parser is null)
                throw new GenericParserException($"Failed to find a parser for the configuration file '{filename}'");

            TranslationConfiguration? configuration = parser.ParseConfigurationFile(filename);

            if (configuration is null)
                throw new ParserLoadException(filename, $"Failed to load the configuration from '{filename}'");

            _defaultConfig = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationManager"/> class.
        /// <para>
        /// This constructor creates a translation manager based on the configuration that is to be loaded from the specified assembly.
        /// Such translation manager can be used to simplify access to translations when translation units are not used - an application simply calls the GetValue method and species the key and, optionally, the culture.
        /// </para>
        /// </summary>
        /// <param name="assembly">The reference to the assembly, from which the configuration file should be loaded.</param>
        /// <param name="configFile">The name of the file to load the configuration from. This name must include a subdirectory (if any) in resource format, such as "Translations.Data" if the original files' subdirectory is "Translations\Data" or "Translations/Data".</param>
        /// <exception cref="GenericParserException">Thrown if the parser for the specified configuration file is not found.</exception>
        /// <exception cref="ParserLoadException">Thrown if the parser failed to load the configuration file.</exception>
        public TranslationManager(Assembly assembly, string configFile)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            if (configFile is null)
                throw new ArgumentNullException(nameof(configFile));

            string filename = configFile.Trim();

            // The config parser will parse configuration and will find the correct parser for the files referenced by the configuration
            BaseParser? parser = FileFormats.GetConfigParser(Path.GetExtension(filename));
            if (parser is null)
                throw new GenericParserException($"Failed to find a parser for the configuration file '{filename}'");

            TranslationConfiguration? configuration = parser.ParseConfigurationFile(assembly, filename);
            if (configuration is null)
                throw new ParserLoadException(filename, $"Failed to load the configuration from '{filename}'");

            _defaultConfig = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationManager"/> class.
        /// <para>
        /// This constructor creates a translation manager based on the specified configuration.
        /// Such translation manager can be used to simplify access to translations when translation units are not used - an application simply calls the GetValue method and species the key and, optionally, the culture.
        /// </para>
        /// </summary>
        /// <param name="translationConfiguration">The configuration that specifies where to load translations from.</param>
        public TranslationManager(TranslationConfiguration translationConfiguration)
        {
            _defaultConfig = translationConfiguration;
        }

        private static CultureInfo? FindBasicCulture(CultureInfo culture)
        {
            CultureInfo? neutral = null;

            if (culture.IsNeutralCulture)
            {
                neutral = culture;
            }
            else
            {
                string cultureName = culture.Name;
                if (cultureName.Length > 2 && cultureName[2] == '-')
                {
                    try
                    {
                        neutral = new CultureInfo(cultureName.Substring(0, 2));
                    }
                    catch (CultureNotFoundException)
                    {
                        // ignore the not found exception - that's ok for us
                    }
                }
            }

            if (neutral is null)
                return null;

            CultureInfo? basic = null;
            try
            {
                basic = CultureInfo.CreateSpecificCulture(neutral.Name);
                if (basic.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
                    return null;
            }
            catch (CultureNotFoundException)
            {
                // ignore the not found exception - that's ok for us
            }

            return basic;
        }

        /// <summary>
        /// From the list of available language files obtained using <see cref="ListTranslationFiles"/>, retrieve culture information (needed for language names and to switch application language).
        /// </summary>
        /// <param name="fileNames">The list of names obtained from <see cref="ListTranslationFiles"/>.</param>
        /// <returns>The list of<see cref="System.Globalization.CultureInfo"/>.</returns>
        public static IList<CultureInfo> ListCultures(IList<string> fileNames)
        {
            if (fileNames is null)
                throw new ArgumentNullException(nameof(fileNames));

            IList<CultureInfo> result = [];
            string cultureName;
            CultureInfo cultureInfo;
            int idx;
            string fileExt;
            string nameOnly;
            foreach (var filename in fileNames)
            {
                fileExt = Path.GetExtension(filename);
                nameOnly = Path.GetFileNameWithoutExtension(filename);
#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                BaseParser? parser = FileFormats.GetParser(fileExt.ToLowerInvariant(), true);
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
                char localeSeparator = '_';

                if (parser is not null)
                    localeSeparator = parser.GetLocaleSeparatorChar();

                idx = nameOnly.LastIndexOf(localeSeparator);
                if (idx >= 0 && idx < filename.Length - 1)
                {
                    cultureName = nameOnly.Substring(idx + 1, nameOnly.Length - (idx + 1));
                    try
                    {
                        // We obtain the culture for the given code.
                        // If it is neutral (no region specified),
                        // we use CreateSpecificCulture method to obtain a culture for the default region.
                        // The mapping to default regions is hardcoded into .NET for all neutral cultures.
                        cultureInfo = new CultureInfo(cultureName);
                        if (cultureInfo.IsNeutralCulture)
                            cultureInfo = CultureInfo.CreateSpecificCulture(cultureName);

                        result.Add(cultureInfo);
                    }
                    catch (CultureNotFoundException)
                    {
                        // ignore
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Loads the translation from the given text, choosing the parser based on the extension.
        /// </summary>
        /// <param name="translationText">The text to load the translation from.</param>
        /// <param name="fileExtension">The extension of the file to use for choosing the parser.</param>
        /// <param name="culture">An optional reference to the locale, whose translation is to be loaded. Makes sense for CSV and TSV formats that may contain multiple translations in one file.</param>
        /// <param name="textProcessingMode">The required text processing mode.</param>
        /// <returns>A <seealso cref="Translation"/> instance or <see langword="null"/> if the parser could not be selected or if the parser failed to load the translation.</returns>
        /// <exception cref="GenericParserException"> and its descendants are thrown if parsing fails due to errors in format of the input.</exception>
        public static Translation? LoadTranslation(string translationText, string fileExtension, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            BaseParser? parser = FileFormats.GetParser(fileExtension);
            if (parser is null)
                return null;

            return parser.LoadTranslation(translationText, culture, textProcessingMode);
        }

        /// <summary>
        /// Loads the translation from the given text using the specified parser.
        /// </summary>
        /// <param name="translationText">The text to load the translation from.</param>
        /// <param name="parser">The parser to use for parsing the <paramref name="translationText"/> text.</param>
        /// <param name="culture">An optional reference to the locale, whose translation is to be loaded. Makes sense for CSV and TSV formats that may contain multiple translations in one file.</param>
        /// <param name="textProcessingMode">The required text processing mode.</param>
        /// <returns>A <seealso cref="Translation"/> instance or <see langword="null"/> if the parser failed to load the translation.</returns>
        /// <exception cref="GenericParserException"> and its descendants are thrown if parsing fails due to errors in format of the input.</exception>
        public static Translation? LoadTranslation(string translationText, BaseParser parser, CultureInfo? culture, TextFormat? textProcessingMode)
        {
            if (parser is null)
                throw new ArgumentNullException(nameof(parser));

            return parser.LoadTranslation(translationText, culture, textProcessingMode);
        }

        /// <summary>
        /// "Forgets" the translation for the given culture so that upon the next attempt to access it, the translation gets loaded again.
        /// </summary>
        /// <param name="culture">The culture, whose translation should be dropped.</param>
        public void DropTranslation(CultureInfo culture)
        {
            if (culture is null)
                throw new ArgumentNullException(nameof(culture));

            lock (_translations)
            {
                _translations.Remove(culture.Name.ToUpperInvariant());
            }
        }

        /// <summary>
        /// <para>"Forgets" all translation so that upon the next attempt to access any translation, they get loaded again.</para>
        /// <para>The method is useful when it is necessary to rescan translations updated on the disk or in another storage.</para>
        /// </summary>
        public void DropAllTranslations()
        {
            lock (_translations)
            {
                _translations.Clear();
            }

            _defaultTranslation = null;
        }

        public Translation? GetTranslation(CultureInfo culture)
        {
            if (culture is null)
                return null;

            Translation? translation = null;

            // Locate the translation set for the specified locale
            lock (_translations)
            {
                if (!_translations.TryGetValue(culture.Name.ToUpperInvariant(), out translation))
                    return null;
            }

            return translation;
        }

        /// <summary>
        /// Lists culture names listed in the configuration file, if one was used.
        /// </summary>
        /// <returns>The list of culture names (every name is contained in uppercase).</returns>
        public IList<string> ListCulturesInConfiguration()
        {
            List<string> result = [];
            if (_defaultConfig is null)
                return result;
            lock (_defaultConfig.Translations)
            {
                foreach (var item in _defaultConfig.Translations.Keys)
                {
                    // We do not include "other" because we return only locale names, explicitly listed in the configuration.
                    if (!TranslationConfiguration.KEY_TRANSLATION_OTHER.Equals(item, StringComparison.Ordinal))
                        result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="key">The key of the translation entry to retrieve.</param>
        /// <returns>The translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(string key)
        {
            if (_defaultConfig is null)
                return TranslationEntry.Empty;

            return GetValue(_defaultConfig, key, _culture);
        }

        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="key">The key of the translation entry to retrieve.</param>
        /// <param name="culture">The culture, for which the entry is needed.</param>
        /// <returns>The translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(string key, CultureInfo culture)
        {
            if (_defaultConfig is null)
                return TranslationEntry.Empty;

            return GetValue(_defaultConfig, key, culture);
        }

        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="config">The configuration that specifies from where to load translations.</param>
        /// <param name="key">The key of the translation entry to retrieve.</param>
        /// <param name="culture">The culture, for which the entry is needed.</param>
        /// <returns>The translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(TranslationConfiguration config, string key, CultureInfo culture)
        {
            return GetValue(config, key, culture, out _);
        }

#pragma warning disable MA0051 // Method is too long
        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="config">The configuration that specifies from where to load translations.</param>
        /// <param name="key">The key of the translation entry to retrieve.</param>
        /// <param name="culture">The culture, for which the entry is needed.</param>
        /// <param name="foundForCulture">Upon return, indicates if the requested entry was found for the speciifed culture or its base culture. <see langword="false" /> will be returned if a value from the default translation was used.</param>
        /// <returns>The translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(TranslationConfiguration config, string key, CultureInfo culture, out bool foundForCulture)
#pragma warning restore MA0051 // Method is too long
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

#pragma warning disable MA0015 // config.DefaultFile is not a valid parameter name
            if (config.DefaultFile is null)
                throw new ArgumentNullException("config.DefaultFile");
#pragma warning restore MA0015

            if (culture is null)
                throw new ArgumentNullException(nameof(culture));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            foundForCulture = false;

            TextFormat textProcessingMode = config.TextProcessingMode ?? TextFormat.None;

            TranslationEntry? result = null;
            string keyUpper = key.ToUpperInvariant();

            Translation? translation = null;
            Translation? cultureLocalTranslation = null;
            Translation? basicCultureLocalTranslation = null;

            // If the OnTranslationValueNeeded event is defined, fire it first and use its result if one is returned
            if (OnTranslationValueNeeded is not null)
            {
                TranslationValueEventArgs args = new(culture, key);
                OnTranslationValueNeeded.Invoke(this, args);
                if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, originalAssembly: null, originalFile: null, config.DirectoryHint))
                    return args.Entry;

                if (args.Text is not null || args.EscapedText is not null)
                {
                    foundForCulture = true;
                    return EntryFromEventArgs(args, textProcessingMode);
                }
            }

            // If requesting text for a non-default culture, deal with the culture-specific translation
            if (!culture.Name.Equals(config.DefaultFileLocale, StringComparison.OrdinalIgnoreCase))
            {
                string? cultureNameUpper = culture.Name.ToUpperInvariant();

                // first, we try to obtain the translation entry from the culture-specific translation
                result = TryGetEntryFromCulture(keyUpper, key, cultureNameUpper, config, culture, false, ref cultureLocalTranslation);

                if (result is null && (cultureLocalTranslation is null || !cultureLocalTranslation.IsBasicCulture))
                {
                    // try to find the basic culture, e.g., for de-AT, it would be "de", and from there, "de-DE", in which we are interested
                    CultureInfo? basicCulture = FindBasicCulture(culture);
                    if (basicCulture is not null)
                    {
                        cultureNameUpper = basicCulture.Name.ToUpperInvariant();

                        if (!cultureNameUpper.Equals(config.DefaultFileLocale, StringComparison.OrdinalIgnoreCase))
                        {
                            // next, try to obtain the translation entry from the basic-culture translation
                            result = TryGetEntryFromCulture(keyUpper, key, cultureNameUpper, config, basicCulture, true, ref basicCultureLocalTranslation);
                            if (result is not null)
                            {
                                // if a locale-specific translation exists, cache the value from the basic-culture translation in the culture-local one so that in the future, no attempt to load or go to the basic-culture translation is needed
                                cultureLocalTranslation?.Add(keyUpper, result);
                                translation = basicCultureLocalTranslation;
                            }
                        }
                    }
                }
                else
                {
                    translation = cultureLocalTranslation;
                }

                if (result is not null)
                {
                    foundForCulture = true;
                    return FireTranslationValueFound(culture, key, result, translation?.OriginalAssembly, translation?.OriginalFile, textProcessingMode);
                }
            }

#pragma warning disable CA2002 // Do not lock on objects with weak identity
            // At this point, we need a default translation
            Monitor.Enter(this);
            if (_defaultTranslation is null)
            {
                Monitor.Exit(this);
                translation = InternalLoadTranslation(config, CultureInfo.InvariantCulture, tryLoadDefault: true);

                Monitor.Enter(this);
                _defaultTranslation = translation;
                Monitor.Exit(this);

                if (translation is not null)
                {
                    string cultureNameUpper;
                    if (!string.IsNullOrEmpty(translation.Locale))
                    {
                        cultureNameUpper = translation.Locale!.ToUpperInvariant();
                    }
                    else
                    {
                        cultureNameUpper = _culture.Name.ToUpperInvariant();
                    }

                    lock (_translations)
                    {
                        _translations[cultureNameUpper] = translation;
                    }
                }
            }
            else
            {
                Monitor.Exit(this);
            }

            // Try loading from the default translation
            Monitor.Enter(this);
            if (_defaultTranslation is not null)
            {
                _defaultTranslation.TryGetValue(keyUpper, out result);
                Monitor.Exit(this);

                if (result is not null && TranslationEntryAcceptable(result, _defaultTranslation.OriginalAssembly, _defaultTranslation.OriginalFile, config.DirectoryHint))
                {
                    if (cultureLocalTranslation is not null)
                    {
                        lock (cultureLocalTranslation)
                        {
                            // if a locale-specific translation exists, cache the value from the default translation in the culture-local one so that in the future, no attempt to load or go to the default translation is needed
                            cultureLocalTranslation.Add(keyUpper, result);
                        }
                    }

                    if (basicCultureLocalTranslation is not null)
                    {
                        lock (basicCultureLocalTranslation)
                        {
                            // if a basic-locale translation exists, cache the value from the default translation in the basic-culture one so that in the future, no attempt to load or go to the default translation is needed
                            basicCultureLocalTranslation.Add(keyUpper, result);
                        }
                    }

                    return FireTranslationValueFound(culture, key, result, _defaultTranslation.OriginalAssembly, _defaultTranslation.OriginalFile, textProcessingMode);
                }
            }
            else
            {
                Monitor.Exit(this);
            }
#pragma warning restore CA2002 // Do not lock on objects with weak identity

            return FireTranslationValueNotFound(culture, key, textProcessingMode);
        }

        private TranslationEntry? TryGetEntryFromCulture(string keyUpper, string key, string cultureNameUpper, TranslationConfiguration config, CultureInfo culture, bool isBasicCulture, ref Translation? cultureLocalTranslation)
        {
            TranslationEntry? result = null;
            Translation? translation = null;

            // Locate the translation set for the specified locale
            lock (_translations)
            {
                bool notInList = true; // we use it to speed up access a bit

                if (!_translations.TryGetValue(cultureNameUpper, out translation))
                    translation = null;
                else
                    notInList = false;

                if (translation is null)
                {
                    translation = InternalLoadTranslation(config, culture, false);
                    if (translation is not null)
                    {
                        if (notInList)
                            _translations.Add(cultureNameUpper, translation);
                    }
                    else
                    {
                        if (notInList)
                        {
                            translation = new Translation(culture.Name); // we use an empty translation here, but we cannot use a static instance because this particular instance will be filled with entries from the default translations one by one once they are accessed.
                            _translations.Add(cultureNameUpper, translation);
                        }
                    }
                }
            }

            // pass the translation up so that if the value is not found, the one from the basic-locale or default translation will be written to this saved one
            cultureLocalTranslation = translation;

            if (translation is null)
                return null;

            if (isBasicCulture)
                translation.IsBasicCulture = true;

            lock (translation)
            {
                // If the translation contains what we need, try using it
                if (translation.TryGetValue(keyUpper, out result)
                    && result is not null
                    && TranslationEntryAcceptable(result, translation.OriginalAssembly, translation.OriginalFile, config.DirectoryHint))
                {
                    return FireTranslationValueFound(culture, key, result, translation.OriginalAssembly, translation.OriginalFile, config.TextProcessingMode ?? TextFormat.None);
                }
            }

            return null;
        }

        /// <summary>
        /// <para>Scans the assembly and optionally a disk directory for translation files.</para>
        /// <para>This method can recognize only the files with names that have the {base_name}_{locale-name}[.{supported_extension}] format, where 'locale-name' may be either language name (e.g., "en") or locale name (e.g., "en-US").</para>
        /// </summary>
        /// <param name="assembly">An optional assembly to look for translations.</param>
        /// <param name="defaultFileName">The base name of the file to look for. If it contains a recognized extension, the extension is stripped.</param>
        /// <returns>The list of filenames of files found in the corresponding directory.</returns>
        public IList<string> ListTranslationFiles(Assembly? assembly, string defaultFileName)
        {
            if (defaultFileName is null)
                throw new ArgumentNullException(nameof(defaultFileName));

            foreach (var extension in FileFormats.GetSupportedExtensions().Where((x) => defaultFileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                defaultFileName = defaultFileName.Substring(0, defaultFileName.Length - extension.Length);
            }

            string fileNameMatch;
            char localeSeparator;
            BaseParser? parser;

            List<string> fileNames = [];

            if (LoadFromDisk)
            {
                string? filePath;
                if (string.IsNullOrEmpty(TranslationsDirectory))
                    filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                else
                    filePath = TranslationsDirectory;

                if (!string.IsNullOrEmpty(filePath))
                {
                    // Enumerate all supported files, strip the extension, check if the file's base name matches "fileName", and add all matching filenames to the list

                    string name;
                    foreach (var extension in FileFormats.GetSupportedExtensions())
                    {
                        parser = FileFormats.GetParser(extension, true);
                        localeSeparator = '_';

                        if (parser is not null)
                            localeSeparator = parser.GetLocaleSeparatorChar();

                        fileNameMatch = defaultFileName + localeSeparator;
#if NET9_0_OR_GREATER
                        var diskFiles = Directory.EnumerateFiles(filePath, "*" + extension, new EnumerationOptions() { RecurseSubdirectories = false, IgnoreInaccessible = true, MatchType = MatchType.Simple });
#else
                        var diskFiles = Directory.EnumerateFiles(filePath, "*" + extension);
#endif
                        foreach (var diskFile in diskFiles)
                        {
                            name = Path.GetFileName(diskFile);

                            // The expected name includes the base name + separator + at least two characters for a language name
                            if (name.StartsWith(fileNameMatch, StringComparison.OrdinalIgnoreCase) && (name.Length >= defaultFileName.Length + 2))
                                fileNames.Add(name);
                        }
                    }
                }
            }

            if (assembly is not null)
            {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                string resourceName = defaultFileName.Replace("/", ".").Replace(@"\", ".").ToUpperInvariant();

                string baseName;
                int idx;

                fileNameMatch = "." + defaultFileName;

                var resourceNames = assembly.GetManifestResourceNames();
#pragma warning disable CA1862 // Prefer the string comparison method overload of '...' that takes a 'StringComparison' enum value to perform a case-insensitive comparison
                var resourcePaths = resourceNames.Where(str => str.ToUpperInvariant().Contains(resourceName));
#pragma warning restore CA1862 // Prefer the string comparison method overload of '...' that takes a 'StringComparison' enum value to perform a case-insensitive comparison
#pragma warning restore CA1307 // Specify StringComparison for clarity

                foreach (var resourcePath in resourcePaths)
                {
                    foreach (var extension in FileFormats.GetSupportedExtensions())
                    {
                        // skip resources with unmatching extensions
                        if (!extension.Equals(Path.GetExtension(resourcePath), StringComparison.OrdinalIgnoreCase))
                            continue;

                        parser = FileFormats.GetParser(extension, true);
                        localeSeparator = '_';

                        if (parser is not null)
                            localeSeparator = parser.GetLocaleSeparatorChar();

                        // we enumerate language-specific files like "Strings_de-AT",
                        // and we have to match the generic name, such as "strings", with the name of the resource.
                        // But then, we add a specific name to the list so that we can turn it to the CultureInfo object

                        // Assuming e.g., Tlumach.Tests.Strings.arb to be the default translation file name,
                        // from Tlumach.Tests.Strings_en.arb, we first get
                        // Tlumach.Tests.Strings_en
                        baseName = resourcePath.Substring(0, resourcePath.Length - extension.Length);

                        // ...then find the '_' separator
                        idx = baseName.LastIndexOf(localeSeparator);

                        if (idx > 0)
                        {
                            // ... then obtain "Tlumach.Tests.Strings"
                            baseName = baseName.Substring(0, idx);

                            // ... check if the name ends with ".Strings"
                            if (baseName.EndsWith(fileNameMatch, StringComparison.OrdinalIgnoreCase))
                            {
                                //...take the position of ".Strings"
                                idx = baseName.LastIndexOf(fileNameMatch, StringComparison.OrdinalIgnoreCase);

                                // ... and from Tlumach.Tests.Strings_en.arb, take the name "Strings_en.arb"
                                if (idx < resourcePath.Length - 1)
                                    fileNames.Add(resourcePath.Substring(idx + 1));
                            }
                        }
                    }
                }
            }

            return fileNames;
        }

        private static TranslationEntry EntryFromEventArgs(TranslationValueEventArgs args, TextFormat textProcessingMode)
        {
            string? text = args.Text;
            string? escapedText = args.EscapedText;

            if (text is null && escapedText is not null)
                text = Utils.UnescapeString(escapedText);

            TranslationEntry entry = new(args.Key, text, escapedText);
            if (escapedText is not null)
                entry.ContainsPlaceholders = IsTemplatedText(escapedText, textProcessingMode);
            else
            if (text is not null)
                entry.ContainsPlaceholders = IsTemplatedText(text, textProcessingMode);

            return entry;
        }

        private static bool IsTemplatedText(string text, TextFormat textProcessingMode)
        {
            return BaseParser.StringHasParameters(text, textProcessingMode);
        }

        /// <summary>
        /// Loads a translation from a file.
        /// </summary>
        /// <param name="config">Configuration information to use (contains an optional reference to the assembly and the filename(s).</param>
        /// <param name="culture">The desired locale for which the file is needed.</param>
        /// <param name="tryLoadDefault">Whether the default file should be tried.</param>
        /// <returns>A translation if one was found and loaded and <see langword="null"/> otherwise.</returns>
        private Translation? InternalLoadTranslation(TranslationConfiguration config, CultureInfo culture, bool tryLoadDefault)
        {
            string? translationContent = null;
            string? usedFileName = null;

            bool cultureNamePresent = !string.IsNullOrEmpty(culture.Name);

            // Fire an event if a handler is assigned - maybe, it provides the file content
            if (OnFileContentNeeded != null)
            {
                FileContentNeededEventArgs args = new(config.Assembly, config.DefaultFile, culture);
                OnFileContentNeeded.Invoke(this, args);
                translationContent = args.Content;
            }

            // Look for translations in the config - maybe, one is present there

            string? configRef = null;

            lock (config.Translations)
            {
                if (!tryLoadDefault && cultureNamePresent)
                {
                    if (string.IsNullOrEmpty(translationContent) && config.Translations.TryGetValue(culture.Name.ToUpperInvariant(), out configRef) && !string.IsNullOrEmpty(configRef))
                    {
                        translationContent = InternalLoadFileContent(config.Assembly, configRef, config.DirectoryHint, ref usedFileName);
                    }

                    // Try the language name
                    if (string.IsNullOrEmpty(translationContent) && config.Translations.TryGetValue(culture.TwoLetterISOLanguageName.ToUpperInvariant(), out configRef) && !string.IsNullOrEmpty(configRef))
                    {
                        translationContent = InternalLoadFileContent(config.Assembly, configRef, config.DirectoryHint, ref usedFileName);
                    }
                }

                // See maybe the default value is defined
                if (string.IsNullOrEmpty(translationContent) && config.Translations.TryGetValue(TranslationConfiguration.KEY_TRANSLATION_OTHER, out configRef) && !string.IsNullOrEmpty(configRef))
                {
                    translationContent = InternalLoadFileContent(config.Assembly, configRef, config.DirectoryHint, ref usedFileName);
                }
            }

            Translation? result = null;

            string? fileExtension = Path.GetExtension((usedFileName is null) ? config.DefaultFile : usedFileName);

            // This call will be used in both cases - when a translation has been already loaded using the config file and also to try loading it from a file with the given extension
            result = InternalTryLoadTranslationWithExtension(fileExtension, translationContent, cultureNamePresent, ref usedFileName, config, culture, tryLoadDefault);
            if (result is not null)
                return result;

            // If nothing was found, try other supported extensions

            IList<string> fileExtensions;

            IList<string> extensions = FileFormats.GetSupportedExtensions();

            if (extensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                fileExtensions = extensions;
            }
            else
            {
                fileExtensions = new List<string>();
                fileExtensions.Add(fileExtension);
                ((List<string>)fileExtensions).AddRange(extensions);
            }

            foreach (var supportedExtension in fileExtensions)
            {
                result = InternalTryLoadTranslationWithExtension(supportedExtension, translationContent, cultureNamePresent, ref usedFileName, config, culture, tryLoadDefault);
                if (result is not null)
                    return result;

                /*

                BaseParser? parser = null;
                bool tryUseDefaultFile;

                parser = FileFormats.GetParser(fileExtension);
                if (parser is null)
                    continue;

                tryUseDefaultFile = parser.UseDefaultFileForTranslations;

                // If the content has not been loaded, try some heuristics
                if (string.IsNullOrEmpty(translationContent) && !string.IsNullOrEmpty(config.DefaultFile))
                {
                    string filename = config.DefaultFile;

                    string fileBase = Path.GetFileNameWithoutExtension(filename);

                    // Here, we attempt to guess the filename and load data from there.

                    if (!tryLoadDefault && cultureNamePresent)
                    {
                        // Try the full culture name first
                        filename = string.Concat(fileBase, parser.GetLocaleSeparatorChar(), culture.Name, supportedExtension);
                        translationContent = InternalLoadFileContent(config.Assembly, filename, config.DirectoryHint, ref usedFileName);

                        // If not loaded, try just the language name
                        if (string.IsNullOrEmpty(translationContent))
                        {
                            filename = string.Concat(fileBase, parser.GetLocaleSeparatorChar(), culture.TwoLetterISOLanguageName, supportedExtension);
                            translationContent = InternalLoadFileContent(config.Assembly, filename, config.DirectoryHint, ref usedFileName);
                        }
                    }

                    // We try loading the data from the default file only for a default culture
                    if (string.IsNullOrEmpty(translationContent) && (tryUseDefaultFile || tryLoadDefault))
                    {
                        translationContent = InternalLoadFileContent(config.Assembly, config.DefaultFile, config.DirectoryHint, ref usedFileName);
                    }
                }

                if (string.IsNullOrEmpty(translationContent))
                    continue;

                // File extension is used to create an appropriate parser
                try
                {
                    result = LoadTranslation(translationContent!, parser, culture)?.SetOrigin(config.Assembly, usedFileName);
                    if (result is not null)
                        return result;

                    // if we got here, this means that translationContent was useless and we may try another file extension
                    translationContent = null;
                }
                catch (TextParseException ex)
                {
                    if (usedFileName is not null)
                        throw new TextFileParseException(usedFileName, $"Failed to load the translation from '{usedFileName}':\n" + ex.Message, ex.StartPosition, ex.EndPosition, ex.LineNumber, ex.ColumnNumber, ex);

                    throw;
                }
                */
            }

            return result;
        }

        private Translation? InternalTryLoadTranslationWithExtension(string fileExtension, string? translationContent, bool cultureNamePresent, ref string? usedFileName, TranslationConfiguration config, CultureInfo culture, bool tryLoadDefault)
        {
            BaseParser? parser = FileFormats.GetParser(fileExtension);
            if (parser is null)
                return null;

            bool tryUseDefaultFile = parser.UseDefaultFileForTranslations;

            // If the content has not been loaded, try some heuristics
            if (string.IsNullOrEmpty(translationContent) && !string.IsNullOrEmpty(config.DefaultFile))
            {
                string filename = config.DefaultFile;

                string fileBase = Path.GetFileNameWithoutExtension(filename);

                // Here, we attempt to guess the filename and load data from there.

                if (!tryLoadDefault && cultureNamePresent)
                {
                    // Try the full culture name first
                    filename = string.Concat(fileBase, parser.GetLocaleSeparatorChar(), culture.Name, fileExtension);
                    translationContent = InternalLoadFileContent(config.Assembly, filename, config.DirectoryHint, ref usedFileName);

                    // If not loaded, try just the language name
                    if (string.IsNullOrEmpty(translationContent))
                    {
                        filename = string.Concat(fileBase, parser.GetLocaleSeparatorChar(), culture.TwoLetterISOLanguageName, fileExtension);
                        translationContent = InternalLoadFileContent(config.Assembly, filename, config.DirectoryHint, ref usedFileName);
                    }
                }

                // We try loading the data from the default file only for a default culture
                if (string.IsNullOrEmpty(translationContent) && (tryUseDefaultFile || tryLoadDefault))
                {
                    translationContent = InternalLoadFileContent(config.Assembly, config.DefaultFile, config.DirectoryHint, ref usedFileName);
                }
            }

            if (string.IsNullOrEmpty(translationContent))
                return null;

            // File extension is used to create an appropriate parser
            try
            {
                return LoadTranslation(translationContent!, parser, culture, config.TextProcessingMode)?.SetOrigin(config.Assembly, usedFileName);
            }
            catch (TextParseException ex)
            {
                if (usedFileName is not null)
                    throw new TextFileParseException(usedFileName, $"Failed to load the translation from '{usedFileName}':\n" + ex.Message, ex.StartPosition, ex.EndPosition, ex.LineNumber, ex.ColumnNumber, ex);
                else
                    throw;
            }
        }

        /// <summary>
        /// Loads content of the file.
        /// </summary>
        /// <param name="assembly">An optional assembly where the file should be looked for.</param>
        /// <param name="filename">The filename to load the data from.</param>
        /// <param name="usedFileName">Becomes set to the filename actually used if loading was successful. This filename may contain a path if loading was performed from the disk.</param>
        /// <param name="originalFile">An optional reference to the file, from which the translation was loaded.</param>
        /// <returns>File content if the file was found and loaded and <see langword="null"/> otherwise.</returns>
        private string? InternalLoadFileContent(Assembly? assembly, string filename, string? hintPath, ref string? usedFileName, string? originalFile = null)
        {
            string? fileContent = null;

            // Try to load the file from the disk.
            // The disk is checked first so that a translation provided on the disk can override the translation from resources (useful for translators to test their work).
            if (LoadFromDisk)
            {
                // If the path is absolute, we only try this file and return
                if (Path.IsPathRooted(filename))
                {
                    usedFileName = filename;
                    return Utils.ReadFileFromDisk(filename);
                }

                string tryFileName;

                // Try the file in the FilesLocation directory if one is specified
                if (!string.IsNullOrEmpty(TranslationsDirectory))
                {
                    tryFileName = Path.Combine(TranslationsDirectory, filename);

                    fileContent = Utils.ReadFileFromDisk(tryFileName);
                    if (fileContent is not null)
                    {
                        usedFileName = tryFileName;
                        return fileContent;
                    }
                }

                // Try the file in the hintPath directory if one is specified
                if (!string.IsNullOrEmpty(hintPath))
                {
                    tryFileName = Path.Combine(hintPath, filename);

                    fileContent = Utils.ReadFileFromDisk(tryFileName);
                    if (fileContent is not null)
                    {
                        usedFileName = tryFileName;
                        return fileContent;
                    }
                }

                string? baseDir;

                // Try the directory of the original file, if it was specified.
                if (!string.IsNullOrEmpty(originalFile))
                {
                    baseDir = Path.GetDirectoryName(originalFile);
                    if (!string.IsNullOrEmpty(baseDir))
                    {
                        tryFileName = Path.Combine(baseDir, filename);

                        fileContent = Utils.ReadFileFromDisk(tryFileName);
                        if (fileContent is not null)
                        {
                            usedFileName = tryFileName;
                            return fileContent;
                        }
                    }
                }

                // Try the directory of the main EXE file
                baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(baseDir))
                {
                    tryFileName = Path.Combine(baseDir, filename);

                    fileContent = Utils.ReadFileFromDisk(tryFileName);
                    if (fileContent is not null)
                    {
                        usedFileName = tryFileName;
                        return fileContent;
                    }
                }

                // The last resort - try to load "as is" (from the current directory)
                fileContent = Utils.ReadFileFromDisk(filename);
                if (fileContent is not null)
                {
                    usedFileName = filename;
                    return fileContent;
                }
            }

            // Try to load the file from the assembly
            if (assembly is not null)
            {
                fileContent = Utils.ReadFileFromResource(assembly, filename, TranslationsDirectory, hintPath);

                if (fileContent is not null)
                    usedFileName = filename;
            }

            return fileContent;
        }

        /// <summary>
        /// Checks whether the entry is usable.
        /// </summary>
        /// <param name="entry">The entry to check.</param>
        /// <param name="originalAssembly">An optional reference to the assembly, from which the translation was loaded.</param>
        /// <param name="originalFile">An optional reference to the file, from which the translation was loaded.</param>
        /// <param name="hintPath">An optional hint path, taken from the configuration.</param>
        /// <returns><see langword="true"/> if the entry is usable and <see langword="false"/> otherwise.</returns>
        private bool TranslationEntryAcceptable(TranslationEntry entry, Assembly? originalAssembly, string? originalFile, string? hintPath)
        {
            // We can use a translation entry when either it has the text specified or when there exists a reference that can be used
            if (entry.Text is not null)
                return true;
            if (entry.Reference is not null)
            {
                string? usedFile = null;
                string? referencedText = InternalLoadFileContent(originalAssembly, entry.Reference, hintPath, ref usedFile, originalFile);
                if (!string.IsNullOrEmpty(referencedText))
                {
                    entry.Text = referencedText;
                    return true;
                }
            }

            return false;
        }

        private TranslationEntry FireTranslationValueFound(CultureInfo culture, string key, TranslationEntry entry, Assembly? originalAssembly, string? originalFile, TextFormat textProcessingMode)
        {
            if (OnTranslationValueFound is not null)
            {
                TranslationValueEventArgs args = new(culture, key);

                try
                {
                    entry.Lock();
                    OnTranslationValueFound.Invoke(this, args);
                }
                finally
                {
                    entry.Unlock();
                }

                // If the handler has provided the entry, validate and return it.
                if (args.Entry == entry)
                    return entry;

                if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, originalAssembly, originalFile, null))
                    return args.Entry;

                // If just a text was provided - great, we create an entry based on this text.
                if (args.Text is not null || args.EscapedText is not null)
                    return EntryFromEventArgs(args, textProcessingMode);
            }

            return entry;
        }

        private TranslationEntry FireTranslationValueNotFound(CultureInfo culture, string key, TextFormat textProcessingMode)
        {
            if (OnTranslationValueNotFound is not null)
            {
                TranslationValueEventArgs args = new(culture, key);
                OnTranslationValueNotFound.Invoke(this, args);

                // If the handler has provided the entry, validate and return it.
                // When a new entry is provided, we accept entries only with the text but not with a reference (a handler should resolve references itself).
                if (args.Entry is not null && (args.Entry.Text is not null) && args.Entry.Reference is null)
                    return args.Entry;

                // If just a text was provided - great, we create an entry based on this text.
                if (args.Text is not null || args.EscapedText is not null)
                    return EntryFromEventArgs(args, textProcessingMode);
            }

            // Nothing was found, return an empty entry.
            return TranslationEntry.Empty;
        }

        /// <summary>
        /// This method should be called when a change of the system culture is detected, so that IF the default culture of the TranslationManager is set to <seealso cref="CultureInfo.CurrentCulture"/>, the manager can notify listeners about the change.
        /// </summary>
        public void SystemCultureUpdated()
        {
            if (_culture == CultureInfo.CurrentCulture || _culture == CultureInfo.CurrentUICulture)
            {
                // Notify listeners about the change
                OnCultureChanged?.Invoke(this, new CultureChangedEventArgs(_culture));
            }
        }
    }
}
