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

namespace Tlumach;

#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

/// <summary>
/// The core of Tlumach that manages translations, provides functions to enumerate translation files, and controls current language and locale used for translations.
/// <para>Applications can use it to retrieve translation entries by their string key.</para>
/// </summary>
public class TranslationManager : BaseTranslationManager, IDisposable
{
    private static readonly List<TranslationManager> _translationManagers = [];
#if NET
    private static readonly Lock _managerListLock = new();
#else
    private static readonly object _managerListLock = new();
#endif

    /// <summary>
    /// Gets an instance of the class that is empty, not linked to any translations.
    /// </summary>
    public static TranslationManager Empty { get; }

    public static IReadOnlyList<TranslationManager> TranslationManagers => _translationManagers;

    /*/// <summary>
    /// The translation that corresponds to the current culture.
    /// </summary>
    private Translation? _currentTranslation;*/

    private CultureInfo _culture = CultureInfo.InvariantCulture;

/*#if NET
    private Lock _lock = new();
#else
    private object _lock;
#endif
*/
    private bool disposedValue;

    /// <summary>
    /// Gets or sets the culture, which will be used by the <see cref="GetValue(string)"/> method as a current culture.
    /// </summary>
    public CultureInfo CurrentCulture
    {
        get
        {
            if (this == TranslationManager.Empty)
                return CultureInfo.CurrentCulture;
            return _culture;
        }

        set
        {
            if (this == TranslationManager.Empty)
                return;

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
    /// Gets or sets the flag that specifies whether the translation manager should store values from the basic culture or default translation in locale-specific translations (in memory) for optimization as described further.
    /// <para>If a translation text is requested but not found in a locale-specific translation, it is searched in a basic culture translation and then in default culture. If an entry is found there, it may be stored in the locale-specific and basic culture translations; this is done depending on this property.</para>
    /// <para>Caching makes sense in most cases unless you expect that the missing value may become available in the future (e.g., when files are loaded from the disk and you are translating your text to a new language and placing a file to the translation directory for verification in your application).</para>
    /// </summary>
    public bool CacheDefaultTranslations { get; set; } = true;

    /// <summary>
    /// The event is fired when the content of the file is to be loaded. A handler can provide file content from another location.
    /// </summary>
    public event EventHandler<FileContentNeededEventArgs>? OnFileContentNeeded;

    /// <summary>
    /// The event is fired when the translation file for a given locale was looked for but not found. A handler can use a translation from a different non-default locale or otherwise substitute the translation.
    /// </summary>
    public event EventHandler<TranslationFileNotFoundEventArgs>? OnTranslationFileNotFound;

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
    /// The event is fired when a translation entry contains a reference to an external file that could not be resolved.
    /// <para>Should a handler decide to take the text from another place or provide an error message in place of the translation text, it may set the text in the <seealso cref="ReferenceNotResolvedEventArgs.Text"/> property.</para>
    /// </summary>
    public event EventHandler<ReferenceNotResolvedEventArgs>? OnReferenceNotResolved;

    /// <summary>
    /// The event is fired when the <see cref="CurrentCulture"/> property is changed by the application.
    /// It is used primarily by the reactive classes in XAML packages (Tlumach.MAUI, Tlumach.Avalonia, Tlumach.WPF, Tlumach.WinUI).
    /// </summary>
    public event EventHandler<CultureChangedEventArgs>? OnCultureChanged;

    static TranslationManager()
    {
        Empty = new TranslationManager();
    }

    private TranslationManager()
        :base()
    {
        lock (_managerListLock)
        {
            _translationManagers.Add(this);
        }
        /*#if !NET
                    _lock = this;
        #endif */
    }

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
        : this()
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
        : this()
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
        : base(translationConfiguration)
    {
        lock (_managerListLock)
        {
            _translationManagers.Add(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                lock (_managerListLock)
                {
                    _translationManagers.Remove(this);
                }
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
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
                cultureName = nameOnly.Substring(idx + 1);
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
    /// Locates and loads the default translation.<para>This method is intended to be used when working with the writer classes.</para>
    /// </summary>
    /// <param name="fileName">The filename from which the translation should be loaded.</param>
    /// <returns>A <seealso cref="Translation"/> instance or <see langword="null"/> if a translation could not be loaded.</returns>
    public Translation? LoadDefaultTranslation(string fileName)
    {
        var parser = FileFormats.GetParser(Path.GetExtension(fileName));
        if (parser is null)
            return null;

        _defaultConfig ??= new TranslationConfiguration(assembly: null, defaultFile: fileName, @namespace: null, className: null, defaultFileLocale: string.Empty, textProcessingMode: TextFormat.None, delayedUnitCreation: false, onlyDeclareKeys: false);

        return GetTranslation(CultureInfo.InvariantCulture, tryLoadMissing: true);
    }

    /// <summary>
    /// "Forgets" the translation for the given culture so that upon the next attempt to access it, the translation gets loaded again.
    /// </summary>
    /// <param name="culture">The culture, whose translation should be dropped.</param>
    public void DropTranslation(CultureInfo culture)
    {
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));

        lock (Translations)
        {
            Translations.Remove(culture.Name.ToUpperInvariant());
        }
    }

    /// <summary>
    /// <para>"Forgets" all translation so that upon the next attempt to access any translation, they get loaded again.</para>
    /// <para>The method is useful when it is necessary to rescan translations updated on the disk or in another storage.</para>
    /// </summary>
    public void DropAllTranslations()
    {
        lock (Translations)
        {
            Translations.Clear();
        }

        _defaultTranslation = null;
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
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
            foreach (var item in _defaultConfig.Translations.Keys)
            {
                // We do not include "other" because we return only locale names, explicitly listed in the configuration.
                if (!TranslationConfiguration.KEY_TRANSLATION_OTHER.Equals(item, StringComparison.Ordinal))
                    result.Add(item);
            }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
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
            if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, culture, key, originalAssembly: null, originalFile: null, config.DirectoryHint))
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
                            if (CacheDefaultTranslations && cultureLocalTranslation is not null)
                            {
                                lock (cultureLocalTranslation)
                                {
                                    // if a locale-specific translation exists, cache the value from the basic-culture translation in the culture-local one so that in the future, no attempt to load or go to the basic-culture translation is needed
                                    if (!cultureLocalTranslation.ContainsKey(keyUpper))
                                        cultureLocalTranslation.Add(keyUpper, result);
                                }
                            }

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

            // If we loaded a translation with a locale specified, we can store it for the future (unless such a translation is already in the list).
            if (translation is not null && !string.IsNullOrEmpty(translation.Locale))
            {
                string cultureNameUpper = translation.Locale!.ToUpperInvariant();
                lock (Translations)
                {
                    //if (!Translations.ContainsKey(cultureNameUpper))
                        Translations[cultureNameUpper] = translation;
                }

                if (DefaultConfiguration is not null && DefaultConfiguration.DefaultFileLocale is null)
                    DefaultConfiguration.DefaultFileLocale = translation.Locale;
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

            if (result is not null && TranslationEntryAcceptable(result, CultureInfo.InvariantCulture, key, _defaultTranslation.OriginalAssembly, _defaultTranslation.OriginalFile, config.DirectoryHint))
            {
                if (CacheDefaultTranslations)
                {
                    if (cultureLocalTranslation is not null)
                    {
                        lock (cultureLocalTranslation)
                        {
                            // if a locale-specific translation exists, cache the value from the default translation in the culture-local one so that in the future, no attempt to load or go to the default translation is needed
                            if (!cultureLocalTranslation.ContainsKey(keyUpper))
                                cultureLocalTranslation.Add(keyUpper, result);
                        }
                    }

                    if (basicCultureLocalTranslation is not null)
                    {
                        lock (basicCultureLocalTranslation)
                        {
                            // if a basic-locale translation exists, cache the value from the default translation in the basic-culture one so that in the future, no attempt to load or go to the default translation is needed
                            if (!basicCultureLocalTranslation.ContainsKey(keyUpper))
                                basicCultureLocalTranslation.Add(keyUpper, result);
                        }
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
        lock (Translations)
        {
            bool notInList = true; // we use it to speed up access a bit

            if (!Translations.TryGetValue(cultureNameUpper, out translation))
                translation = null;
            else
                notInList = false;

            if (translation is null)
            {
                translation = InternalLoadTranslation(config, culture, tryLoadDefault: false);
                if (translation is not null)
                {
                    if (notInList)
                        Translations.Add(cultureNameUpper, translation);
                }
                else
                {
                    if (notInList)
                    {
                        translation = TranslationFileNotFound(culture);
                        Translations.Add(cultureNameUpper, translation);
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
                && TranslationEntryAcceptable(result, culture, key, translation.OriginalAssembly, translation.OriginalFile, config.DirectoryHint))
            {
                return FireTranslationValueFound(culture, key, result, translation.OriginalAssembly, translation.OriginalFile, config.TextProcessingMode ?? TextFormat.None);
            }
        }

        return null;
    }

    /// <summary>
    /// <para>Scans the assembly and optionally a disk directory for translation files.</para>
    /// <para>This method can recognize only the files with names that have the {base_name}_{locale-name}[.{supported_extension}] format, where 'locale-name' may be either language name (e.g., "en") or locale name (e.g., "en-US").</para>
    /// <para>The "_" character (underscore) is defined in the LocaleSeparatorChar property of the parser classes and can be replaced by another character if needed.</para>
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
                filePath = Path.GetDirectoryName(/*Assembly.GetExecutingAssembly().Location*/System.AppContext.BaseDirectory);
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
    /// Checks whether the entry is usable.
    /// </summary>
    /// <param name="entry">The entry to check.</param>
    /// <param name="culture">The culture, for which the entry was found.</param>
    /// <param name="key">The key, for which the entry was found.</param>
    /// <param name="originalAssembly">An optional reference to the assembly, from which the translation was loaded.</param>
    /// <param name="originalFile">An optional reference to the file, from which the translation was loaded.</param>
    /// <param name="hintPath">An optional hint path, taken from the configuration.</param>
    /// <returns><see langword="true"/> if the entry is usable and <see langword="false"/> otherwise.</returns>
    private bool TranslationEntryAcceptable(TranslationEntry entry, CultureInfo culture, string key, Assembly? originalAssembly, string? originalFile, string? hintPath)
    {
        // We can use a translation entry when either it has the text specified or when there exists a reference that can be used

        if (entry.Text is not null)
            return true;

        if (entry.Reference is not null)
        {
            string? usedFile = null;
            string? referencedText;
            try
            {
                referencedText = InternalLoadFileContent(originalAssembly, entry.Reference, hintPath, ref usedFile, originalFile);
            }
            catch(Exception)
            {
                referencedText = null;
            }

            if (referencedText is null)
                referencedText = FireReferenceNotResolved(culture, key, entry.Reference);

            if (!string.IsNullOrEmpty(referencedText))
            {
                entry.Text = referencedText;
                return true;
            }
        }

        return false;
    }

    private string? FireReferenceNotResolved(CultureInfo culture, string key, string reference)
    {
        if (OnReferenceNotResolved is not null)
        {
            ReferenceNotResolvedEventArgs args = new(culture, key, reference);
            OnReferenceNotResolved.Invoke(this, args);
            return args.Text;
        }

        return '@' + reference;
    }

    protected override Translation TranslationFileNotFound(CultureInfo culture)
    {
        Translation? result = null;

        if (OnTranslationFileNotFound is not null)
        {
            TranslationFileNotFoundEventArgs args = new(culture);
            OnTranslationFileNotFound.Invoke(this, args);
            result = args.Translation;
        }

        if (result is not null)
            return result;

        return new Translation(culture.Name); // we use an empty translation here, but we cannot use a static instance because this particular instance will be filled with entries from the default translations one by one once they are accessed.
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

            if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, culture, key, originalAssembly, originalFile, null))
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

    protected override string GetContent(Assembly? assembly, string fileName, CultureInfo culture)
    {
        if (OnFileContentNeeded != null)
        {
            FileContentNeededEventArgs args = new(assembly, fileName, culture);
            OnFileContentNeeded.Invoke(this, args);
            return args.Content;
        }

        return string.Empty;

    }
}
