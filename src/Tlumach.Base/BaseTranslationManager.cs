using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator;
#else
namespace Tlumach.Base;
#endif

/// <summary>
/// The base class shared between the main class hierarchy and Generator.
/// </summary>
public class BaseTranslationManager
{
    /// <summary>
    /// A container for all translations managed by this class.
    /// <para>The map is concurrent so that reading an already-loaded translation needs no lock at all, and
    /// so that two threads asking for two different cultures do not serialize behind each other.</para>
    /// </summary>
    private readonly ConcurrentDictionary<string, Translation> _translations = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// One gate object per culture name, used to make sure a given culture is loaded exactly once even
    /// when several threads ask for it at the same time. Loading a culture holds only its own gate, so
    /// loads of different cultures still run in parallel.
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _loadGates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Guards the one-time initialization of <see cref="_defaultTranslation"/>. A dedicated private object
    /// is used rather than the manager instance so that application code cannot deadlock against it.
    /// </summary>
    private readonly object _defaultTranslationLock = new();

#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable SA1401 // Fields should be private
    /// <summary>
    /// The configuration to use for loading translations.
    /// </summary>
    protected TranslationConfiguration? _defaultConfig;

    /// <summary>
    /// The default translation that is used as a fallback.
    /// <para>Declared volatile so that the common "already loaded" case is a plain read with no lock.</para>
    /// </summary>
    protected volatile Translation? _defaultTranslation;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore CA1051 // Do not declare visible instance fields

    protected ConcurrentDictionary<string, Translation> Translations => _translations;

    /// <summary>
    /// Returns the translation for the given culture, loading it if necessary.
    /// <para>The loading itself happens outside the shared map, under a gate specific to this culture, so
    /// that a slow load blocks only the callers who want that same culture.</para>
    /// </summary>
    /// <param name="cultureName">The culture name used as the map key.</param>
    /// <param name="config">The configuration to load from.</param>
    /// <param name="culture">The culture to load.</param>
    /// <returns>The translation. Never <see langword="null"/>: when no file is found, the value produced by
    /// <see cref="TranslationFileNotFound"/> is stored so that the search is not repeated.</returns>
    protected Translation GetOrLoadTranslation(string cultureName, TranslationConfiguration config, CultureInfo culture)
    {
        if (_translations.TryGetValue(cultureName, out Translation? existing))
            return existing;

        object gate = _loadGates.GetOrAdd(cultureName, static _ => new object());

        lock (gate)
        {
            // Re-check: another thread may have completed the load while this one waited for the gate.
            if (_translations.TryGetValue(cultureName, out existing))
                return existing;

            Translation loaded = InternalLoadTranslation(config, culture, tryLoadDefault: false)
                ?? TranslationFileNotFound(culture);

            _translations[cultureName] = loaded;
            return loaded;
        }
    }

    /// <summary>
    /// Returns the default translation, loading it on first use.
    /// </summary>
    /// <param name="config">The configuration to load from.</param>
    /// <returns>The default translation, or <see langword="null"/> if it could not be loaded.</returns>
    protected Translation? GetOrLoadDefaultTranslation(TranslationConfiguration config)
    {
        Translation? translation = _defaultTranslation;
        if (translation is not null)
            return translation;

        lock (_defaultTranslationLock)
        {
            translation = _defaultTranslation;
            if (translation is not null)
                return translation;

            // When the configuration declares the locale of the default file, the default translation is loaded for that locale.
            // Formats that keep several locales in one file (String Catalog, XLIFF, CSV, TSV) need it to pick the right one;
            // without it, they fall back to whichever locale comes first in the file.
            CultureInfo defaultCulture = CultureInfo.InvariantCulture;

            if (!string.IsNullOrEmpty(config.DefaultFileLocale))
            {
                try
                {
                    defaultCulture = new CultureInfo(config.DefaultFileLocale!);
                }
                catch (CultureNotFoundException)
                {
                    defaultCulture = CultureInfo.InvariantCulture; // an invalid locale in the configuration must not prevent the default file from being loaded
                }
            }

            translation = InternalLoadTranslation(config, defaultCulture, tryLoadDefault: true);
            _defaultTranslation = translation;

            // If we loaded a translation with a locale specified, we can store it for the future.
            if (translation is not null && !string.IsNullOrEmpty(translation.Locale))
                _translations[translation.Locale!] = translation;

            return translation;
        }
    }

    /// <summary>
    /// Gets the configuration used by this Translation Manager. May be empty if it was not set explicitly or by the generated class (when the Generator is used).
    /// </summary>
    public TranslationConfiguration? DefaultConfiguration => _defaultConfig;

    /// <summary>
    /// Gets or sets the indicator that tells TranslationManager to attempt to locate translation files on the disk.
    /// </summary>
    public bool LoadFromDisk { get; set; }

    /// <summary>
    /// Gets or sets the directory in which translations files are looked for.
    /// <para>When <see cref="LoadFromDisk"/> is disabled, this value is used when trying to load the translations from the assembly. When <see cref="LoadFromDisk"/> is enabled, this value is also used when trying to locate translation files on the disk.</para>
    /// <para>This property is a hint for the manager, which affects loading of secondary translation files.
    /// When a configuration is loaded via the <seealso cref="BaseTranslationManager"/> constructor, specify the directory in the name of the configuration file.</para>
    /// </summary>
    public string TranslationsDirectory { get; set; } = string.Empty;

    protected BaseTranslationManager()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseTranslationManager"/> class.
    /// <para>
    /// This constructor creates a translation manager based on the specified configuration.
    /// Such translation manager can be used to simplify access to translations when translation units are not used - an application simply calls the GetValue method and species the key and, optionally, the culture.
    /// </para>
    /// </summary>
    /// <param name="translationConfiguration">The configuration that specifies where to load translations from.</param>
    public BaseTranslationManager(TranslationConfiguration translationConfiguration)
    {
        _defaultConfig = translationConfiguration;
    }

    /// <summary>
    /// Returns the translation object for the given culture if one exists. Optionally tries to load a missing translation.
    /// </summary>
    /// <param name="culture">The culture to retrieve the translation for.</param>
    /// <param name="tryLoadMissing">When set to <see langword="true"/>, specifies that if a translation is not loaded, it should be search for and loaded.</param>
    /// <returns>The <seealso cref="Translation"/> instance if one was found and <see langword="null"/> otherwise or if <paramref name="culture"/> was <see langword="null"/>.</returns>
    public Translation? GetTranslation(CultureInfo culture, bool tryLoadMissing = false)
    {
        if (culture is null)
            return null;

        if (culture.Name.Length == 0) // Invariant culture
        {
            return _defaultTranslation ?? (tryLoadMissing ? LoadTranslation(culture) : null);
        }

        // The map compares its keys case-insensitively, so the culture name is used as-is.
        if (_translations.TryGetValue(culture.Name, out Translation? translation))
            return translation;

        return tryLoadMissing ? LoadTranslation(culture) : null;
    }

    /// <summary>
    /// Locates and loads the translation.
    /// </summary>
    /// <param name="culture">The culture, for which the translation is needed.</param>
    /// <returns>A <seealso cref="Translation"/> instance or <see langword="null"/> if a translation could not be loaded.</returns>
    public Translation? LoadTranslation(CultureInfo culture)
    #pragma warning restore MA0051 // Method is too long
    {
        if (DefaultConfiguration is null)
            throw new TlumachException("Cannot load a translation: the configuration is not available in the translation manager.");

        if (string.IsNullOrEmpty(DefaultConfiguration.DefaultFile))
            throw new TlumachException("Cannot load a translation: the configuration doe not indicate a default file.");

#pragma warning disable CA1510 // Use ArgumentNullException throw helper
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));
#pragma warning restore CA1510 // Use ArgumentNullException throw helper

        // If requesting text for a non-default culture, deal with the culture-specific translation
        if (culture.Name.Length > 0 && !culture.Name.Equals(DefaultConfiguration.DefaultFileLocale ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            return GetOrLoadTranslation(culture.Name, DefaultConfiguration, culture);
        }

        // At this point, we need a default translation
        return GetOrLoadDefaultTranslation(DefaultConfiguration);
    }

    /// <summary>
    /// Loads a translation from a file.
    /// </summary>
    /// <param name="config">Configuration information to use (contains an optional reference to the assembly and the filename(s).</param>
    /// <param name="culture">The desired locale for which the file is needed.</param>
    /// <param name="tryLoadDefault">Whether the default file should be tried.</param>
    /// <returns>A translation if one was found and loaded and <see langword="null"/> otherwise.</returns>
    protected Translation? InternalLoadTranslation(TranslationConfiguration config, CultureInfo culture, bool tryLoadDefault)
    {
        string? translationContent = null;
        string? usedFileName = null;

        bool cultureNamePresent = !string.IsNullOrEmpty(culture.Name);

        // Fire an event if a handler is assigned - maybe, it provides the file content
        translationContent = GetContent(config.Assembly, config.DefaultFile, culture);

        // Look for translations in the config - maybe, one is present there.
        // The candidate references are read out of the map first, and the file access happens afterwards:
        // holding a lock across disk and resource I/O would serialize the loading of unrelated cultures.
        string? cultureRef = null;
        string? languageRef = null;
        string? otherRef = null;

        lock (config.Translations)
        {
            if (!tryLoadDefault && cultureNamePresent)
            {
                // The map compares its keys case-insensitively, so the names are used as-is.
                config.Translations.TryGetValue(culture.Name, out cultureRef);
                config.Translations.TryGetValue(culture.TwoLetterISOLanguageName, out languageRef);
            }

            config.Translations.TryGetValue(TranslationConfiguration.KEY_TRANSLATION_OTHER, out otherRef);
        }

        if (string.IsNullOrEmpty(translationContent) && !string.IsNullOrEmpty(cultureRef))
        {
            translationContent = InternalLoadFileContent(config.Assembly, cultureRef!, config.DirectoryHint, ref usedFileName);
        }

        // Try the language name
        if (string.IsNullOrEmpty(translationContent) && !string.IsNullOrEmpty(languageRef))
        {
            translationContent = InternalLoadFileContent(config.Assembly, languageRef!, config.DirectoryHint, ref usedFileName);
        }

        // See maybe the default value is defined
        if (string.IsNullOrEmpty(translationContent) && !string.IsNullOrEmpty(otherRef))
        {
            translationContent = InternalLoadFileContent(config.Assembly, otherRef!, config.DirectoryHint, ref usedFileName);
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
        }

        return result;
    }

        /// <summary>
        /// Loads content of the file.
        /// </summary>
        /// <param name="assembly">An optional assembly where the file should be looked for.</param>
        /// <param name="filename">The filename to load the data from.</param>
        /// <param name="hintPath">An optional path to the file. If set, the value is used only when opening based on the filename alone did not succeed.</param>
        /// <param name="usedFileName">Becomes set to the filename actually used if loading was successful. This filename may contain a path if loading was performed from the disk.</param>
        /// <param name="originalFile">An optional reference to the file, from which the translation was loaded.</param>
        /// <returns>File content if the file was found and loaded and <see langword="null"/> otherwise.</returns>
        protected string? InternalLoadFileContent(Assembly? assembly, string filename, string? hintPath, ref string? usedFileName, string? originalFile = null)
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
                baseDir = Path.GetDirectoryName(System.AppContext.BaseDirectory);
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

            throw;
        }
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
#pragma warning disable CA1510 // Use ArgumentNullException throw helper
        if (parser is null)
            throw new ArgumentNullException(nameof(parser));
#pragma warning restore CA1510 // Use ArgumentNullException throw helper

        return parser.LoadTranslation(translationText, culture, textProcessingMode);
    }

    protected virtual string GetContent(Assembly? assembly, string fileName, CultureInfo culture) => string.Empty;

#pragma warning disable CA1062 // Validate arguments of public methods
    protected virtual Translation TranslationFileNotFound(CultureInfo culture) => new(culture.Name); // we use an empty translation here, but we cannot use a static instance because this particular instance will be filled with entries from the default translations one by one once they are accessed.
#pragma warning restore CA1062 // Validate arguments of public methods
}
