// <copyright file="TranslationValidator.cs" company="Allied Bits Ltd.">
//
// Copyright 2026 Allied Bits Ltd.
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
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;

using Tlumach.Base;

namespace Tlumach.Validation;

/// <summary>
/// Finds the Tlumach configurations of a project and the translation files that they lead to, and checks that the library can load and process every one of them.
/// <para>The translation files of a configuration are its default file, the files named in its translations section, and the files that <see cref="TranslationManager"/> finds by name next to the default file (<c>{base}{separator}{culture}{extension}</c>), including the files that the project links into that directory.</para>
/// </summary>
public static class TranslationValidator
{
    /// <summary>
    /// The extension of a ResX file that a project embeds as it is. MSBuild treats a file named like "Strings.pl.resx" as a localized resource and moves it into a satellite assembly, so such a translation is kept as "Strings.pl.resx.resx" and embedded with the <c>Non-Resx</c> type, which gives the resource "Strings.pl.resx" in the main assembly (see "File formats" in the documentation).
    /// </summary>
    private const string EmbeddedResxExtension = ".resx.resx";

    /// <summary>
    /// The parsers keep settings in static properties, which loading a file changes (e.g., the text processing mode), so validations run one at a time.
    /// </summary>
    private static readonly object Gate = new();

    private static readonly StringComparer PathComparer =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    static TranslationValidator()
    {
        // Register every built-in parser, as the generator does
        ArbParser.Use();
        IniParser.Use();
        JsonParser.Use();
        ResxParser.Use();
        CsvParser.Use();
        TomlParser.Use();
        TsvParser.Use();
        XliffParser.Use();
        StringCatParser.Use();
    }

    /// <summary>
    /// Determines whether a file has the extension of a Tlumach configuration file.
    /// </summary>
    /// <param name="path">The path or the name of the file.</param>
    /// <returns><see langword="true"/> when a configuration parser is registered for the extension.</returns>
    public static bool IsConfigurationFile(string path)
        => !string.IsNullOrEmpty(path) && FileFormats.HasConfigParser(Extension(path));

    /// <summary>
    /// Validates the translations of a project.
    /// </summary>
    /// <param name="projectPath">The full path of the project file.</param>
    /// <param name="items">The files that the project includes. A file may be listed more than once with different item types.</param>
    /// <param name="cancellationToken">A token that cancels the validation between files.</param>
    /// <returns>The report.</returns>
    public static ProjectValidationReport ValidateProject(string projectPath, IEnumerable<ProjectFile> items, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(projectPath))
            throw new ArgumentException("The path of the project must not be empty.", nameof(projectPath));

        if (items is null)
            throw new ArgumentNullException(nameof(items));

        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? string.Empty;

        // A file can be listed with several item types (an SDK project lists an AdditionalFiles item in None as well); it is a generator input when any of them is AdditionalFiles
        Dictionary<string, bool> files = new(PathComparer);
        foreach (ProjectFile item in items)
        {
            string path = Path.GetFullPath(item.FullPath);
            files[path] = (files.TryGetValue(path, out bool known) && known) || item.IsGeneratorInput;
        }

        List<LinkedFile> linkedFiles = LinkedFiles(items, projectDirectory);
        Dictionary<string, ProjectFile> embeddedFiles = EmbeddedFiles(items);

        ProjectValidationReport report = new(projectPath);

        lock (Gate)
        {
            foreach (KeyValuePair<string, bool> file in files.OrderBy(f => f.Key, PathComparer))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!FileFormats.HasConfigParser(Extension(file.Key)))
                    continue;

                ConfigurationValidationReport? configurationReport = ValidateConfigurationCore(file.Key, file.Value, projectDirectory, linkedFiles, embeddedFiles, cancellationToken);
                if (configurationReport is null)
                    report.IgnoredFiles.Add(file.Key);
                else
                    report.Configurations.Add(configurationReport);
            }
        }

        return report;
    }

    /// <summary>
    /// Validates one configuration and the translation files that it leads to.
    /// </summary>
    /// <param name="configurationPath">The full path of the configuration file.</param>
    /// <param name="isGeneratorInput">Whether the generator processes the configuration; the generator requires the namespace and the class to be set.</param>
    /// <param name="projectDirectory">The directory of the project, used to find the files that the configuration names.</param>
    /// <param name="projectItems">The files that the project includes, if known; used to find the translation files linked into the directory of the default file and to check how ResX translations are embedded.</param>
    /// <param name="cancellationToken">A token that cancels the validation between files.</param>
    /// <returns>The report. When <paramref name="isGeneratorInput"/> is <see langword="false"/> and the file is not a Tlumach configuration, the report contains no files and no messages.</returns>
    public static ConfigurationValidationReport ValidateConfiguration(string configurationPath, bool isGeneratorInput, string projectDirectory, IEnumerable<ProjectFile>? projectItems = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("The path of the configuration must not be empty.", nameof(configurationPath));

        string path = Path.GetFullPath(configurationPath);
        projectDirectory ??= string.Empty;
        List<ProjectFile> items = projectItems?.ToList() ?? [];
        List<LinkedFile> linkedFiles = LinkedFiles(items, projectDirectory);
        Dictionary<string, ProjectFile> embeddedFiles = EmbeddedFiles(items);

        lock (Gate)
        {
            return ValidateConfigurationCore(path, isGeneratorInput, projectDirectory, linkedFiles, embeddedFiles, cancellationToken)
                ?? new ConfigurationValidationReport(path, isGeneratorInput);
        }
    }

    /// <summary>
    /// Collects the files that the project embeds into its assembly, by their full paths. A file listed as an embedded resource and with another item type counts as embedded.
    /// </summary>
    private static Dictionary<string, ProjectFile> EmbeddedFiles(IEnumerable<ProjectFile> items)
    {
        Dictionary<string, ProjectFile> result = new(PathComparer);
        foreach (ProjectFile item in items)
        {
            if (!item.IsEmbeddedResource)
                continue;

            try
            {
                result[Path.GetFullPath(item.FullPath)] = item;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // an invalid path is not a file that can be checked
            }
        }

        return result;
    }

    /// <summary>
    /// Collects the linked items: the files that the project shows in another place than where they are.
    /// </summary>
    private static List<LinkedFile> LinkedFiles(IEnumerable<ProjectFile> items, string projectDirectory)
    {
        List<LinkedFile> result = [];
        if (string.IsNullOrEmpty(projectDirectory))
            return result;

        foreach (ProjectFile item in items)
        {
            if (item.Link is null)
                continue;

            try
            {
                string physical = Path.GetFullPath(item.FullPath);
                string logical = Path.GetFullPath(Path.Combine(projectDirectory, item.Link));
                if (!PathComparer.Equals(physical, logical) && !result.Exists(f => PathComparer.Equals(f.PhysicalPath, physical) && PathComparer.Equals(f.LogicalPath, logical)))
                    result.Add(new LinkedFile(physical, logical));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // a link that is not a valid path cannot place the file anywhere
            }
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Configuration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates one configuration and the translation files that it leads to. The caller holds <see cref="Gate"/>.
    /// </summary>
    /// <returns>The report, or <see langword="null"/> when the file is not a generator input and not a Tlumach configuration.</returns>
    private static ConfigurationValidationReport? ValidateConfigurationCore(string configPath, bool isGeneratorInput, string projectDirectory, IReadOnlyList<LinkedFile> linkedFiles, Dictionary<string, ProjectFile> embeddedFiles, CancellationToken cancellationToken)
    {
        ConfigurationValidationReport report = new(configPath, isGeneratorInput);

        BaseParser? configParser = FileFormats.GetConfigParser(Extension(configPath));
        if (configParser is null)
            return isGeneratorInput ? AddMessage(report, ValidationSeverity.Error, ValidationCodes.ConfigurationUnreadable, $"No parser is available for configuration files with the extension '{Path.GetExtension(configPath)}'.", configPath) : null;

        if (!File.Exists(configPath))
            return isGeneratorInput ? AddMessage(report, ValidationSeverity.Error, ValidationCodes.ConfigurationUnreadable, "The configuration file does not exist.", configPath) : null;

        TranslationConfiguration? configuration;
        try
        {
            configuration = configParser.ParseConfigurationFile(configPath);
        }
        catch (Exception ex) when (IsLoadException(ex))
        {
            if (!isGeneratorInput)
                return null;

            (int line, int column) = ErrorLocation(ex);
            return AddMessage(report, ValidationSeverity.Error, ValidationCodes.ConfigurationUnreadable, $"The configuration cannot be parsed: {Describe(ex)}", configPath, line, column);
        }

        if (configuration is null)
            return isGeneratorInput ? AddMessage(report, ValidationSeverity.Error, ValidationCodes.ConfigurationUnreadable, "The configuration cannot be read.", configPath) : null;

        if (string.IsNullOrWhiteSpace(configuration.DefaultFile))
        {
            return isGeneratorInput
                ? AddMessage(report, ValidationSeverity.Error, ValidationCodes.ConfigurationUnreadable, $"The configuration does not name the default translation file in the '{TranslationConfiguration.KEY_DEFAULT_FILE}' setting.", configPath)
                : null;
        }

        string configDirectory = Path.GetDirectoryName(configPath) ?? string.Empty;
        string defaultFileName = configuration.DefaultFile.Trim();
        string? defaultPath = ResolveFile(defaultFileName, configDirectory, projectDirectory);
        report.DefaultFilePath = defaultPath;

        // The text of the configuration, to point the messages about its settings at the right line
        string configText = ReadTextOrEmpty(configPath);

        bool defaultFileReported = false;

        if (isGeneratorInput)
        {
            CheckGeneratorSettings(report, configuration, configPath);

            // A missing default file is reported below, with the position of its name in the configuration
            if (defaultPath is not null)
                defaultFileReported = CheckAsGenerator(report, configParser, configPath, projectDirectory, defaultPath);
        }

        if (defaultPath is null)
        {
            if (!defaultFileReported)
            {
                (int line, int column) = KeyLocator.FindWord(configText, defaultFileName);
                AddMessage(report, ValidationSeverity.Error, ValidationCodes.DefaultFileUnusable, $"The default translation file '{defaultFileName}' does not exist ({LookedIn(configDirectory, projectDirectory)}).", configPath, line, column);
            }

            defaultFileReported = true;
        }

        TextFormat? textProcessingMode = configuration.TextProcessingMode;

        // The files to check, in the order of the report: the default file first
        List<Candidate> candidates = [];
        if (defaultPath is not null)
            candidates.Add(new Candidate(defaultPath, TranslationFileOrigin.DefaultFile, configuration.DefaultFileLocale ?? string.Empty));

        AddConfiguredFiles(report, configuration, configPath, configText, configDirectory, projectDirectory, candidates);

        // The translation manager looks for files named after the default file in the directory where it found that file
        string defaultDirectory = Path.GetDirectoryName(defaultPath ?? Path.GetFullPath(Path.Combine(configDirectory, defaultFileName))) ?? configDirectory;
        List<Candidate> conventional = FindConventionalFiles(configuration, defaultFileName, defaultDirectory, defaultPath);
        AddDistinct(candidates, conventional);
        AddDistinct(candidates, FindLinkedFiles(defaultFileName, defaultDirectory, defaultPath, linkedFiles));

        ReportShadowedFiles(report, configuration, configDirectory, projectDirectory, defaultFileName, conventional);

        // Load the default translation first: the keys of the other files are compared with its keys
        Dictionary<string, string>? defaultKeys = null;

        foreach (Candidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidatedFile verified = new(candidate.Path, candidate.Origin, candidate.Locale);
            report.Files.Add(verified);

            bool isDefault = candidate.Origin == TranslationFileOrigin.DefaultFile;

            int messagesBefore = report.Messages.Count;
            if (embeddedFiles.TryGetValue(candidate.Path, out ProjectFile? embedded))
                CheckResxEmbedding(report, candidate.Path, embedded, isDefault && isGeneratorInput);

            // The generator check has already reported a problem with the default file; loading it again would repeat the message
            if (isDefault && defaultFileReported)
            {
                verified.ErrorCount++;
                CountMessages(report, verified, messagesBefore);
                continue;
            }

            Dictionary<string, string>? keys = ValidateTranslationFile(report, verified, candidate, textProcessingMode, isDefault ? null : defaultKeys, cancellationToken);
            CountMessages(report, verified, messagesBefore);

            if (isDefault)
                defaultKeys = keys;
        }

        return report;
    }

    /// <summary>
    /// Checks that an embedded ResX translation file reaches the main assembly as text under a name that ends with ".resx", which is where the translation manager looks for it (see "File formats" in the documentation). MSBuild embeds a .resx file as text only with the <c>Non-Resx</c> type, and otherwise compiles it into binary resources; it moves a file named like "Strings.de.resx" into the satellite assembly of the culture unless the item sets <c>WithCulture</c> to false; and it removes the ".resx" extension from the name of the resource, unless the file has a duplicate extension ("Strings.de.resx.resx" gives "Strings.de.resx") or the item sets <c>LogicalName</c>.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <param name="path">The full path of the file.</param>
    /// <param name="item">The project item of the file.</param>
    /// <param name="isGeneratorDefaultFile">Whether the file is the default file of a configuration that the generator processes; the generator reads that file from the disk by the name that the configuration gives, so the file cannot be renamed.</param>
    private static void CheckResxEmbedding(ConfigurationValidationReport report, string path, ProjectFile item, bool isGeneratorDefaultFile)
    {
        string name = Path.GetFileName(path);
        if (!name.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            return;

        // Without the metadata, the type of the item is not known
        if (item.Metadata is null)
            return;

        bool doubled = name.EndsWith(EmbeddedResxExtension, StringComparison.OrdinalIgnoreCase);
        bool nonResx = item.Metadata.TryGetValue("Type", out string? type) && string.Equals(type?.Trim(), "Non-Resx", StringComparison.OrdinalIgnoreCase);
        bool logicalName = item.Metadata.TryGetValue("LogicalName", out string? logical) && !string.IsNullOrWhiteSpace(logical);

        string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
        int lastDot = nameWithoutExtension.LastIndexOf('.');
        // WithCulture="false" stops MSBuild from taking the file for the resources of a culture
        bool withCulture = !(item.Metadata.TryGetValue("WithCulture", out string? withCultureValue) && string.Equals(withCultureValue?.Trim(), "false", StringComparison.OrdinalIgnoreCase));
        CultureInfo? culture = null;
        bool cultureNamed = withCulture && !doubled && lastDot > 0 && CultureNames.TryGetCulture(nameWithoutExtension.Substring(lastDot + 1), out culture);

        string resourceName = ResourceName(name);
        string asText = $"set the Type metadata of its EmbeddedResource item to Non-Resx";
        string keepName = isGeneratorDefaultFile
            ? $"set its LogicalName metadata to the name of the assembly, a dot, and '{resourceName}' (with the folder of the configuration, if any, between them, as in '<assembly>.<folder>.{resourceName}'), because the generator reads the file by the name in the configuration"
            : $"name the file '{name}.resx' or set its LogicalName metadata to the name of the assembly, a dot, and '{resourceName}'";

        string? problem;
        if (cultureNamed)
            problem = $"MSBuild takes the file for the resources of the culture '{culture!.Name}' and moves it into the satellite assembly of that culture, so the translation manager does not find it. Name the file '{name}.resx' and {asText}, or set the WithCulture metadata to false, {asText}, and set LogicalName to the name of the assembly, a dot, and '{resourceName}'";
        else if (!nonResx)
            problem = $"MSBuild compiles a .resx file into binary resources, which the translation manager cannot read. To embed it as text, {asText}" + (doubled || logicalName ? string.Empty : $", and {keepName}");
        else if (!doubled && !logicalName)
            problem = $"MSBuild removes the extension from the name of the resource, so the translation manager does not find it as '{resourceName}'. To keep the name, {keepName}";
        else
            problem = null;

        if (problem is not null)
            AddMessage(report, ValidationSeverity.Warning, ValidationCodes.ResxNotEmbeddedAsText, $"The translation manager cannot read this file from the resources of the assembly: {problem} (see \"File formats\" in the documentation).", path);
    }

    /// <summary>
    /// Reports what the generator would report about a configuration that lacks the namespace or the class to generate.
    /// </summary>
    private static void CheckGeneratorSettings(ConfigurationValidationReport report, TranslationConfiguration configuration, string configPath)
    {
        if (string.IsNullOrEmpty(configuration.Namespace))
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.GeneratorSettingMissing, $"The configuration does not name the namespace of the generated class in the '{TranslationConfiguration.KEY_GENERATED_NAMESPACE}' setting; the generator requires it.", configPath);

        if (string.IsNullOrEmpty(configuration.ClassName))
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.GeneratorSettingMissing, $"The configuration does not name the generated class in the '{TranslationConfiguration.KEY_GENERATED_CLASS}' setting; the generator requires it.", configPath);
    }

    /// <summary>
    /// Loads the configuration and the default file the way the generator does, which builds the tree of keys and so reports the errors that only the generator reports.
    /// </summary>
    /// <returns><see langword="true"/> when a problem with the default file was reported.</returns>
    private static bool CheckAsGenerator(ConfigurationValidationReport report, BaseParser configParser, string configPath, string projectDirectory, string defaultPath)
    {
        bool previous = BaseParser.PopulateKeyLocations;
        try
        {
            // The generator loads the files with the positions of the keys
            BaseParser.PopulateKeyLocations = true;
            configParser.LoadTranslationStructure(configPath, projectDirectory, out _, out _);
            return false;
        }
        catch (ParserConfigException ex)
        {
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.ConfigurationUnreadable, Describe(ex), configPath);
            return true;
        }
        catch (TextParseException ex)
        {
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.DefaultFileUnusable, $"The generator cannot parse the default translation file: {Describe(ex)}", defaultPath, ex.LineNumber, ex.ColumnNumber);
            return true;
        }
        catch (Exception ex) when (IsLoadException(ex))
        {
            // A ParserLoadException names the configuration (e.g., "the default file is empty"); the other exceptions come from parsing the default file
            (int line, int column) = ErrorLocation(ex);
            string file = ex is ParserLoadException ? configPath : defaultPath;
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.DefaultFileUnusable, Describe(ex), file, line, column);
            return true;
        }
        finally
        {
            BaseParser.PopulateKeyLocations = previous;
        }
    }

    // -------------------------------------------------------------------------
    // Discovery
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds the files named in the translations section of the configuration.
    /// </summary>
    private static void AddConfiguredFiles(ConfigurationValidationReport report, TranslationConfiguration configuration, string configPath, string configText, string configDirectory, string projectDirectory, List<Candidate> candidates)
    {
        // The entries are processed in the order of the file
        foreach (KeyValuePair<string, string> translation in configuration.Translations)
        {
            // Configuration parsers store the locale names in upper case; the name of the culture is shown instead when there is one
            string locale = translation.Key.Trim();
            string fileName = translation.Value?.Trim() ?? string.Empty;
            bool isOther = IsOtherLocale(locale);

            // The entry is found by its file name, which is more distinctive than the locale name
            (int line, int column) = KeyLocator.FindWord(configText, fileName.Length > 0 ? fileName : locale);

            if (!isOther)
            {
                if (CultureNames.TryGetCulture(locale, out CultureInfo? culture))
                    locale = culture!.Name;
                else
                    AddMessage(report, ValidationSeverity.Warning, ValidationCodes.UnknownLocale, $"The translations section names the locale '{locale}', which is not a known culture; a translation manager never asks for it.", configPath, line, column);
            }

            if (fileName.Length == 0)
            {
                AddMessage(report, ValidationSeverity.Error, ValidationCodes.ReferencedFileMissing, $"The translations section gives no file for the locale '{locale}'.", configPath, line, column);
                continue;
            }

            if (FileFormats.GetParser(Extension(fileName)) is null)
            {
                AddMessage(report, ValidationSeverity.Error, ValidationCodes.NoParser, $"The file '{fileName}' named for the locale '{locale}' has an extension that no Tlumach parser reads.", configPath, line, column);
                continue;
            }

            string? path = ResolveFile(fileName, configDirectory, projectDirectory);
            if (path is null)
            {
                AddMessage(report, ValidationSeverity.Warning, ValidationCodes.ReferencedFileMissing, $"The file '{fileName}' named for the locale '{locale}' does not exist ({LookedIn(configDirectory, projectDirectory)}).", configPath, line, column);
                continue;
            }

            AddDistinct(candidates, [new Candidate(path, TranslationFileOrigin.Configuration, isOther ? string.Empty : locale)]);
        }
    }

    /// <summary>
    /// Finds the files that the translation manager would load by their names, with <see cref="TranslationManager.ListTranslationFiles"/>.
    /// </summary>
    private static List<Candidate> FindConventionalFiles(TranslationConfiguration configuration, string defaultFileName, string defaultDirectory, string? defaultPath)
    {
        List<Candidate> result = [];
        if (!Directory.Exists(defaultDirectory))
            return result;

        IList<string> names;
        using (TranslationManager manager = new(configuration) { LoadFromDisk = true, TranslationsDirectory = defaultDirectory })
        {
            names = manager.ListTranslationFiles(null, Path.GetFileName(defaultFileName));
        }

        foreach (string name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            string path = Path.Combine(defaultDirectory, name);
            if (defaultPath is not null && PathComparer.Equals(path, defaultPath))
                continue;

            if (TryGetConventionalLocale(defaultFileName, ResourceName(name), out string locale))
                result.Add(new Candidate(path, TranslationFileOrigin.NamingConvention, locale));
        }

        return result;
    }

    /// <summary>
    /// Finds the files that the project links into the directory of the default file under a name that follows the naming convention. An embedded resource is named after the place where the project shows the file, so the translation manager finds such a file as if it were next to the default file.
    /// </summary>
    private static List<Candidate> FindLinkedFiles(string defaultFileName, string defaultDirectory, string? defaultPath, IReadOnlyList<LinkedFile> linkedFiles)
    {
        List<Candidate> result = [];
        string directory = defaultDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (LinkedFile file in linkedFiles.OrderBy(f => f.LogicalPath, PathComparer))
        {
            if (defaultPath is not null && PathComparer.Equals(file.PhysicalPath, defaultPath))
                continue;

            if (!PathComparer.Equals(Path.GetDirectoryName(file.LogicalPath) ?? string.Empty, directory))
                continue;

            if (FileFormats.GetParser(Extension(file.PhysicalPath)) is null || !File.Exists(file.PhysicalPath))
                continue;

            if (TryGetConventionalLocale(defaultFileName, ResourceName(Path.GetFileName(file.LogicalPath)), out string locale))
                result.Add(new Candidate(file.PhysicalPath, TranslationFileOrigin.LinkedItem, locale));
        }

        return result;
    }

    /// <summary>
    /// Checks whether a file name is <c>{base}{separator}{culture}{extension}</c>, where the base is the name of the default file without its extension and the separator is the locale separator of the parser of the extension.
    /// <para><see cref="TranslationManager.ListTranslationFiles"/> accepts any name that starts with the base and the separator; the translation manager loads only the names that are exactly the base, the separator, and the name of a culture, so the others are left out.</para>
    /// </summary>
    private static bool TryGetConventionalLocale(string defaultFileName, string fileName, out string locale)
    {
        locale = string.Empty;

        string extension = Extension(fileName);
        BaseParser? parser = FileFormats.GetParser(extension, getStaticInstance: true);
        if (parser is null)
            return false;

        string baseName = Path.GetFileNameWithoutExtension(defaultFileName);
        string nameOnly = Path.GetFileNameWithoutExtension(fileName);
        char separator = parser.GetLocaleSeparatorChar();

        if (nameOnly.Length <= baseName.Length + 1
            || !nameOnly.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)
            || nameOnly[baseName.Length] != separator)
        {
            return false;
        }

        string suffix = nameOnly.Substring(baseName.Length + 1);
        if (!CultureNames.TryGetCulture(suffix, out _))
            return false;

        locale = suffix;
        return true;
    }

    /// <summary>
    /// Reports the files found by name that the translation manager never loads because another file takes precedence for their locale.
    /// <para>For a culture, the translation manager loads the file named in the configuration for the culture, for its language, or as <c>other</c>; only when the configuration names none of them, it looks for a file by name, trying the extension of the default file first.</para>
    /// </summary>
    private static void ReportShadowedFiles(ConfigurationValidationReport report, TranslationConfiguration configuration, string configDirectory, string projectDirectory, string defaultFileName, List<Candidate> conventional)
    {
        string? otherFile = ConfiguredFile(configuration, TranslationConfiguration.KEY_TRANSLATION_OTHER, configDirectory, projectDirectory)
            ?? ConfiguredFile(configuration, TranslationConfiguration.KEY_TRANSLATION_ASTERISK, configDirectory, projectDirectory);

        string defaultExtension = Extension(defaultFileName);

        foreach (IGrouping<string, Candidate> group in conventional.GroupBy(c => c.Locale, StringComparer.OrdinalIgnoreCase))
        {
            CultureNames.TryGetCulture(group.Key, out CultureInfo? culture);

            string? configured = ConfiguredFile(configuration, group.Key, configDirectory, projectDirectory);
            if (configured is null && culture is not null && !culture.IsNeutralCulture)
                configured = ConfiguredFile(configuration, culture.TwoLetterISOLanguageName, configDirectory, projectDirectory);

            configured ??= otherFile;

            if (configured is not null)
            {
                foreach (Candidate candidate in group.Where(c => !PathComparer.Equals(c.Path, configured)))
                    AddMessage(report, ValidationSeverity.Warning, ValidationCodes.FileShadowed, $"The translation manager does not load this file for the locale '{group.Key}': the configuration names '{Path.GetFileName(configured)}' for it.", candidate.Path);

                continue;
            }

            List<Candidate> files = group.ToList();
            if (files.Count < 2)
                continue;

            Candidate? preferred = files.FirstOrDefault(c => string.Equals(Extension(c.Path), defaultExtension, StringComparison.Ordinal));
            foreach (Candidate candidate in files.Where(c => !ReferenceEquals(c, preferred)))
            {
                string message = preferred is not null
                    ? $"The translation manager does not load this file for the locale '{group.Key}': '{Path.GetFileName(preferred.Path)}' has the extension of the default file and is loaded instead."
                    : $"There are several files for the locale '{group.Key}' ({string.Join(", ", files.Select(f => Path.GetFileName(f.Path)))}); the translation manager loads only one of them.";
                AddMessage(report, ValidationSeverity.Warning, ValidationCodes.FileShadowed, message, candidate.Path);
            }
        }
    }

    private static string? ConfiguredFile(TranslationConfiguration configuration, string locale, string configDirectory, string projectDirectory)
    {
        if (!configuration.Translations.TryGetValue(locale, out string? fileName) || string.IsNullOrWhiteSpace(fileName))
            return null;

        return ResolveFile(fileName.Trim(), configDirectory, projectDirectory);
    }

    // -------------------------------------------------------------------------
    // Translation files
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads a translation file with the parser and the text processing mode that the translation manager uses, checks the templates of its entries, and compares its keys with the keys of the default file.
    /// </summary>
    /// <returns>The keys of the file, as the translation stores them mapped to the keys as the file writes them (for the default file, used to check the other files), or <see langword="null"/> when the file could not be loaded.</returns>
    private static Dictionary<string, string>? ValidateTranslationFile(ConfigurationValidationReport report, ValidatedFile verified, Candidate candidate, TextFormat? textProcessingMode, Dictionary<string, string>? defaultKeys, CancellationToken cancellationToken)
    {
        string path = candidate.Path;

        BaseParser? parser = FileFormats.GetParser(Extension(path));
        if (parser is null)
        {
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.NoParser, $"No Tlumach parser reads files with the extension '{Path.GetExtension(path)}'.", path);
            return null;
        }

        string content;
        try
        {
            content = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AddMessage(report, ValidationSeverity.Error, ValidationCodes.TranslationUnparsable, $"The file cannot be read: {ex.Message}", path);
            return null;
        }

        // The translation manager treats an empty file as a missing one
        if (content.Trim().Length == 0)
        {
            AddMessage(report, ValidationSeverity.Warning, ValidationCodes.TranslationEmpty, "The file is empty; the translation manager treats it as missing.", path);
            return null;
        }

        // A file with several locales is checked for each of them; another file, for the locale for which it is meant
        List<string?> locales = [];
        if (MultiLocaleInspector.IsMultiLocale(parser))
            locales.AddRange(MultiLocaleInspector.GetLocales(parser, content));

        if (locales.Count == 0)
            locales.Add(candidate.Locale.Length > 0 ? candidate.Locale : null);

        Dictionary<string, string>? allKeys = null;

        foreach (string? locale in locales)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CultureInfo? culture = null;
            if (locale is not null)
                CultureNames.TryGetCulture(locale, out culture);

            Translation? translation;
            try
            {
                translation = parser.LoadTranslation(content, culture, textProcessingMode);
            }
            catch (Exception ex) when (IsLoadException(ex))
            {
                (int line, int column) = ErrorLocation(ex);
                AddMessage(report, ValidationSeverity.Error, ValidationCodes.TranslationUnparsable, $"The file cannot be parsed{ForLocale(locale, locales.Count)}: {Describe(ex)}", path, line, column);

                // A syntax error affects every locale of the file
                return allKeys;
            }

            if (translation is null)
            {
                AddMessage(report, ValidationSeverity.Warning, ValidationCodes.TranslationEmpty, $"The file yields no translation{ForLocale(locale, 2)}.", path);
                continue;
            }

            verified.CheckedLocales.Add(locale ?? string.Empty);
            verified.EntryCount = Math.Max(verified.EntryCount, translation.Count);

            allKeys ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, TranslationEntry> pair in translation)
            {
                if (!allKeys.ContainsKey(pair.Key))
                    allKeys.Add(pair.Key, pair.Value.Key);
            }

            CheckEntries(report, parser, content, path, translation, culture, locale, locales.Count, textProcessingMode, defaultKeys);
        }

        return allKeys;
    }

    /// <summary>
    /// Checks the templates of the entries and, for a file other than the default one, compares its keys with the keys of the default file in both directions.
    /// </summary>
    private static void CheckEntries(ConfigurationValidationReport report, BaseParser parser, string content, string path, Translation translation, CultureInfo? culture, string? locale, int localeCount, TextFormat? textProcessingMode, Dictionary<string, string>? defaultKeys)
    {
        if (defaultKeys is not null)
            ReportKeysMissingFromTranslation(report, path, translation, locale, localeCount, defaultKeys);

        TextFormat mode = textProcessingMode ?? TextFormat.None;
        CultureInfo formattingCulture = culture ?? CultureInfo.InvariantCulture;

        List<(TranslationEntry Entry, TemplateProblem Problem)> templateProblems = [];
        List<TranslationEntry> keysNotInDefault = [];

        foreach (KeyValuePair<string, TranslationEntry> pair in translation)
        {
            TranslationEntry entry = pair.Value;

            TemplateProblem? templateProblem = TemplateChecker.Check(entry, formattingCulture, mode);
            if (templateProblem is not null)
                templateProblems.Add((entry, templateProblem));

            if (defaultKeys is not null && !defaultKeys.ContainsKey(pair.Key))
                keysNotInDefault.Add(entry);
        }

        if (templateProblems.Count == 0 && keysNotInDefault.Count == 0)
            return;

        List<string> keysToLocate = templateProblems.Select(p => p.Entry.Key).Concat(keysNotInDefault.Select(e => e.Key)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Dictionary<string, (int Line, int Column)> positions = KeyLocator.Locate(parser, content, culture, textProcessingMode, keysToLocate);

        (int Line, int Column) PositionOf(TranslationEntry entry) => positions.TryGetValue(entry.Key, out (int Line, int Column) position) ? position : (int.MaxValue, 0);

        foreach ((TranslationEntry entry, TemplateProblem problem) in templateProblems.OrderBy(p => PositionOf(p.Entry)).ThenBy(p => p.Entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            (int line, int column) = positions.TryGetValue(entry.Key, out (int Line, int Column) position) ? position : (0, 0);
            AddMessage(
                report,
                problem.Definite ? ValidationSeverity.Error : ValidationSeverity.Warning,
                ValidationCodes.TemplateInvalid,
                $"The text of the key '{entry.Key}'{ForLocale(locale, localeCount)} cannot be formatted: {problem.Message}",
                path,
                line,
                column);
        }

        if (keysNotInDefault.Count > 0)
        {
            // One message per file: some integrations keep keys only in the translations on purpose, and a message per key would bury the other messages
            List<TranslationEntry> ordered = keysNotInDefault.OrderBy(PositionOf).ThenBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToList();
            (int line, int column) = positions.TryGetValue(ordered[0].Key, out (int Line, int Column) first) ? first : (0, 0);

            string message;
            if (ordered.Count == 1)
                message = $"The key '{ordered[0].Key}'{ForLocale(locale, localeCount)} is not in the default translation file, so the generated class has no member for it; it can be reached only by its name through the translation manager.";
            else
                message = $"{ordered.Count} keys{ForLocale(locale, localeCount)} are not in the default translation file, so the generated class has no members for them; they can be reached only by their names through the translation manager: {KeyList(ordered.Select(e => e.Key))}.";

            AddMessage(report, ValidationSeverity.Info, ValidationCodes.KeyNotInDefault, message, path, line, column);
        }
    }

    /// <summary>
    /// Reports the keys of the default file that a translation lacks; the translation manager shows the text of the basic culture or of the default file for them.
    /// </summary>
    private static void ReportKeysMissingFromTranslation(ConfigurationValidationReport report, string path, Translation translation, string? locale, int localeCount, Dictionary<string, string> defaultKeys)
    {
        List<string> missing = defaultKeys.Where(k => !translation.ContainsKey(k.Key)).Select(k => k.Value).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count == 0)
            return;

        string message = missing.Count == 1
            ? $"The key '{missing[0]}' of the default translation file is missing{ForLocale(locale, localeCount)}; the text of the basic culture or of the default file is shown instead."
            : $"{missing.Count} keys of the default translation file are missing{ForLocale(locale, localeCount)}; the texts of the basic culture or of the default file are shown instead: {KeyList(missing)}.";

        AddMessage(report, ValidationSeverity.Info, ValidationCodes.KeyMissingFromTranslation, message, path);
    }

    /// <summary>
    /// Lists keys for a message, at most ten of them.
    /// </summary>
    private static string KeyList(IEnumerable<string> keys)
    {
        const int Shown = 10;
        List<string> list = keys.ToList();
        string names = string.Join(", ", list.Take(Shown).Select(k => "'" + k + "'"));
        return list.Count > Shown ? names + $", and {list.Count - Shown} more" : names;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Decides whether an exception thrown while a file is loaded is a problem of the file. Besides the exceptions of the parsers, a parser can fail with an unexpected exception on unusual content; the application would fail the same way when it loads the file, so such a failure is reported as a problem of the file instead of stopping the validation of the other files.
    /// </summary>
    private static bool IsLoadException(Exception ex)
        => ex is not (OperationCanceledException or OutOfMemoryException or StackOverflowException or ThreadAbortException or AccessViolationException);

    private static string LookedIn(string configDirectory, string projectDirectory)
    {
        if (string.IsNullOrEmpty(projectDirectory) || PathComparer.Equals(Path.GetFullPath(configDirectory), Path.GetFullPath(projectDirectory)))
            return $"looked for it in '{configDirectory}'";

        return $"looked for it in '{configDirectory}' and '{projectDirectory}'";
    }

    private static string ForLocale(string? locale, int localeCount)
        => localeCount > 1 && !string.IsNullOrEmpty(locale) ? $" for the locale '{locale}'" : string.Empty;

    private static bool IsOtherLocale(string locale)
        => locale.Equals(TranslationConfiguration.KEY_TRANSLATION_OTHER, StringComparison.OrdinalIgnoreCase)
        || locale.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal);

#pragma warning disable CA1308 // The parsers are registered under lowercase extensions
    private static string Extension(string path) => Path.GetExtension(path).ToLowerInvariant();
#pragma warning restore CA1308

    /// <summary>
    /// Returns the name under which the translation manager finds a file among the resources: "Strings.pl.resx" for "Strings.pl.resx.resx", the name itself for other files.
    /// </summary>
    private static string ResourceName(string fileName)
        => fileName.EndsWith(EmbeddedResxExtension, StringComparison.OrdinalIgnoreCase)
            ? fileName.Substring(0, fileName.Length - ".resx".Length)
            : fileName;

    /// <summary>
    /// Finds a file named in a configuration the way the translation manager does when it loads files from the disk: an absolute path as is, a relative one next to the configuration (the directory hint) and in the project directory. A ResX file is also found under its name with the duplicate extension (see <see cref="EmbeddedResxExtension"/>), which the translation manager loads from the resources.
    /// </summary>
    private static string? ResolveFile(string fileName, string configDirectory, string projectDirectory)
    {
        try
        {
            foreach (string name in CandidateNames(fileName))
            {
                if (Path.IsPathRooted(name))
                {
                    if (File.Exists(name))
                        return Path.GetFullPath(name);

                    continue;
                }

                foreach (string directory in new[] { configDirectory, projectDirectory })
                {
                    if (string.IsNullOrEmpty(directory))
                        continue;

                    string path = Path.GetFullPath(Path.Combine(directory, name));
                    if (File.Exists(path))
                        return path;
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // An invalid file name cannot be found
        }

        return null;

        static IEnumerable<string> CandidateNames(string fileName)
        {
            yield return fileName;

            if (fileName.EndsWith(".resx", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(EmbeddedResxExtension, StringComparison.OrdinalIgnoreCase))
                yield return fileName + ".resx";
        }
    }

    private static string ReadTextOrEmpty(string path)
    {
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    private static void AddDistinct(List<Candidate> candidates, IEnumerable<Candidate> additions)
    {
        foreach (Candidate addition in additions)
        {
            if (!candidates.Exists(c => PathComparer.Equals(c.Path, addition.Path)))
                candidates.Add(addition);
        }
    }

    private static ConfigurationValidationReport AddMessage(ConfigurationValidationReport report, ValidationSeverity severity, string code, string message, string? filePath, int line = 0, int column = 0)
    {
        report.Messages.Add(new ValidationMessage(severity, code, message, filePath, line, column));
        return report;
    }

    private static void CountMessages(ConfigurationValidationReport report, ValidatedFile verified, int messagesBefore)
    {
        for (int i = messagesBefore; i < report.Messages.Count; i++)
        {
            if (report.Messages[i].Severity == ValidationSeverity.Error)
                verified.ErrorCount++;
            else if (report.Messages[i].Severity == ValidationSeverity.Warning)
                verified.WarningCount++;
            else
                verified.InfoCount++;
        }
    }

    /// <summary>
    /// Builds the text of an exception and its inner exceptions, without repeating a message that the outer exception already contains.
    /// </summary>
    private static string Describe(Exception ex)
    {
        StringBuilder builder = new(ex.Message.Trim());
        for (Exception? inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            string text = inner.Message.Trim();
            if (text.Length > 0 && builder.ToString().IndexOf(text, StringComparison.Ordinal) < 0)
                builder.Append(" (").Append(text).Append(')');
        }

        return builder.Replace("\r\n", " ").Replace('\n', ' ').ToString();
    }

    /// <summary>
    /// Takes the position of an error from the exception or from one of its inner exceptions.
    /// </summary>
    private static (int Line, int Column) ErrorLocation(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case TextParseException { LineNumber: > 0 } text:
                    return (text.LineNumber, text.ColumnNumber);
                case XmlException { LineNumber: > 0 } xml:
                    return (xml.LineNumber, xml.LinePosition);
                case JsonException { LineNumber: not null } json:
                    // System.Text.Json reports 0-based positions
                    return ((int)json.LineNumber.Value + 1, (int)(json.BytePositionInLine ?? 0) + 1);
            }
        }

        return (0, 0);
    }

    /// <summary>
    /// A file that the project shows in another place than where it is.
    /// </summary>
    private sealed class LinkedFile
    {
        public LinkedFile(string physicalPath, string logicalPath)
        {
            PhysicalPath = physicalPath;
            LogicalPath = logicalPath;
        }

        /// <summary>Gets the full path of the file.</summary>
        public string PhysicalPath { get; }

        /// <summary>Gets the full path of the place where the project shows the file.</summary>
        public string LogicalPath { get; }
    }

    /// <summary>
    /// A translation file to check.
    /// </summary>
    private sealed class Candidate
    {
        public Candidate(string path, TranslationFileOrigin origin, string locale)
        {
            Path = path;
            Origin = origin;
            Locale = locale;
        }

        public string Path { get; }

        public TranslationFileOrigin Origin { get; }

        public string Locale { get; }
    }
}
