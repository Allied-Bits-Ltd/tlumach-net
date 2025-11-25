// <copyright file="BaseParser.cs" company="Allied Bits Ltd.">
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
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    public abstract class BaseParser
    {
        /// <summary>
        /// Gets or sets the flag that tells parsers to recognize file references in translation texts.
        /// <para>A file reference is the text that starts with '@' character followed by the file name (with or without a path depending on other settings).
        /// If a reference is used, the text is taken from the referenced file.</para>
        /// </summary>
        public static bool RecognizeFileRefs { get; set; }

        public virtual bool UseDefaultFileForTranslations => false;

        public static bool StringHasParameters(string inputText, TextFormat textProcessingMode)
        {
            if (string.IsNullOrEmpty(inputText))
                return false;

            bool inQuotes = false;
            int openBraceCount = 0;

            int i = 0;
            while (i < inputText.Length)
            {
                char currentChar = inputText[i];

                // Look ahead one character to check for duplicates
                char? nextChar = (i + 1 < inputText.Length) ? inputText[i + 1] : (char?)null;

                if (textProcessingMode == TextFormat.Arb)
                {
                    // --- 1. Handle Duplicated Quote Characters ---
                    // If we see '' (two single quotes), it's an escaped quote.
                    // We skip both characters and stay in the same quote state.
                    if (currentChar == Utils.C_SINGLE_QUOTE && nextChar == Utils.C_SINGLE_QUOTE)
                    {
                        i += 2; // Skip the next character as well
                        continue;
                    }
                }

                if (textProcessingMode == TextFormat.DotNet)
                {
                    // --- 2. Handle Duplicated Braces ---
                    // If we see {{, we skip both characters.
                    // These do not affect the matching logic.
                    if (currentChar == '{' && nextChar == '{')
                    {
                        i += 2; // Skip the next character as well
                        continue;
                    }

                    // If we see }}, we skip both characters unless there was an open brace found earlier.
                    // In the latter case, the brace in currentChar must close the opened brace, and the brace in nextChar will be considered on the next round.
                    if ((currentChar == '}' && nextChar == '}') && (openBraceCount == 0))
                    {
                        i += 2; // Skip the next character as well
                        continue;
                    }
                }

                if (textProcessingMode == TextFormat.Arb)
                {
                    // --- 3. Handle Quote State Toggle ---
                    // If we see a non-duplicated quote, toggle the inQuotes flag.
                    if (currentChar == Utils.C_SINGLE_QUOTE)
                    {
                        inQuotes = !inQuotes;
                        i++;
                        continue;
                    }
                }

                // --- 4. Handle Braces (if not in quotes) ---
                if (!inQuotes)
                {
                    // We found a non-duplicated, non-quoted opening brace.
                    // Mark that we are now looking for a closing brace.
                    if ((currentChar == '{') && ((textProcessingMode == TextFormat.Arb) || (textProcessingMode == TextFormat.ArbNoEscaping) || (textProcessingMode == TextFormat.DotNet)))
                    {
                        openBraceCount++;
                    }
                    else
                    // We found a non-duplicated, non-quoted closing brace.
                    if ((currentChar == '}') && ((textProcessingMode == TextFormat.Arb) || (textProcessingMode == TextFormat.ArbNoEscaping) || (textProcessingMode == TextFormat.DotNet)))
                    {
                        // If we were looking for a closing brace, we found a match!
                        if (openBraceCount > 0)
                        {
                            return true;
                        }

                        // If we found a '}' without a '{' first,
                        // this is an error
                        throw new GenericParserException($"Unmatched closing curly bracket detected in the text '{inputText}'");
                    }
                }

                // else: We are in quotes. All other characters, including single { and }, are ignored.
                i++;
            }

            if (inQuotes)
                throw new TemplateParserException("A hanging open quote detected in the following text:\n" + inputText);

            if (openBraceCount > 0)
                throw new GenericParserException("Unclosed opening curly bracket in the following text:\n" + inputText);

            // If we finished the loop without finding a match, return false.
            return false;
        }

        protected virtual TextFormat GetTextProcessingMode()
        {
            return TextFormat.BackslashEscaping;
        }

        public virtual char GetLocaleSeparatorChar()
        {
            return '_';
        }

        /// <summary>
        /// Parses the specified configuration file, then loads the keys from the specified default translation file and builds a tree of keys.
        /// <para>The files are loaded from the disk - this method is intended to be used by generators and converters.</para>
        /// </summary>
        /// <param name="configFile">The configuration file to read.</param>
        /// <param name="baseDirectory">An optional directory to language files if <paramref name="configFile"/> does not contain a directory.</param>
        /// <param name="configuration">The loaded configuration or <see langword="null"/> if the method does not succeed.</param>
        /// <returns>The constructed <seealso cref="TranslationTree"/> upon success or <see langword="null"/> otherwise.</returns>
        /// <exception cref="ParserLoadException">Gets thrown when loading of a configuration file or a default translation file fails.</exception>
        /// <exception cref="TextFileParseException">Gets thrown when parsing of a default translation file fails.</exception>
        public TranslationTree? LoadTranslationStructure(string configFile, string? baseDirectory, out TranslationConfiguration? configuration)
        {
            if (configFile is null)
                throw new ArgumentNullException(nameof(configFile));

            /*if (!Path.IsPathRooted(configFile))
            {
                string? dir = baseDirectory;

                if (!string.IsNullOrEmpty(dir))
                    configFile = Path.Combine(dir, configFile);
            }
*/
            // First, load the configuration
            string? configContent = null;
            try
            {
                configContent = Utils.ReadFileFromDisk(configFile, baseDirectory, null);
            }
            catch (Exception ex)
            {
                throw new ParserLoadException(configFile, $"Loading of the configuration file '{configFile}' has failed", ex);
            }

            if (configContent is null)
                throw new ParserLoadException(configFile, $"Loading of the configuration file '{configFile}' has failed");

            // parse the configuration
            try
            {
                configuration = ParseConfiguration(configContent, assembly: null);

                // check if configuration was loaded
                if (configuration is null)
                    return null;

                ValidateConfiguration(configuration);
            }
            catch (GenericParserException ex)
            {
                if (ex.InnerException is not null)
                    throw new ParserConfigException(configFile, $"Parsing of the configuration file '{configFile}' has failed with an error: {ex.Message}", ex.InnerException);
                else
                    throw new ParserConfigException(configFile, $"Parsing of the configuration file '{configFile}' has failed with an error: {ex.Message}");
            }

            // Retrieve the name of the default translation file
            string defaultFile = configuration.DefaultFile;
            if (!Path.IsPathRooted(defaultFile))
            {
                string? dir = baseDirectory;
                if (string.IsNullOrEmpty(dir))
                    dir = Path.GetDirectoryName(configFile);

                if (!string.IsNullOrEmpty(dir))
                    defaultFile = Path.Combine(dir, defaultFile);
            }

#pragma warning disable CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            string fileExt = Path.GetExtension(defaultFile)?.ToLowerInvariant() ?? string.Empty;
#pragma warning restore CA1308 // In method '...', replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
            BaseParser? parser;
            if (CanHandleExtension(fileExt))
                parser = this;
            else
                parser = FileFormats.GetParser(fileExt);

            if (parser is null)
                throw new ParserLoadException(configFile, $"No parser found for the '{fileExt}' file extension that the default translation file '{defaultFile}' has");

            // Read the default translation file
            string? defaultContent;
            try
            {
                using Stream stream = new FileStream(defaultFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8, true);
                defaultContent = reader.ReadToEnd();
                //defaultContent = File.ReadAllText(defaultFile, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new ParserLoadException(configFile, $"Loading of the default translation file '{defaultFile}' has failed", ex);
            }

            if (string.IsNullOrEmpty(defaultContent))
                throw new ParserLoadException(configFile, $"Default translation file '{defaultFile}' is empty");

            // Parse the default translation file and return the result
            try
            {
                return parser.InternalLoadTranslationStructure(defaultContent);
            }
            catch (TextParseException ex)
            {
                if (ex.InnerException is not null)
                    throw new TextFileParseException(defaultFile, ex.Message, ex.StartPosition, ex.EndPosition, ex.LineNumber, ex.ColumnNumber, ex.InnerException);
                else
                    throw new TextFileParseException(defaultFile, ex.Message, ex.StartPosition, ex.EndPosition, ex.LineNumber, ex.ColumnNumber);
            }
        }

        /// <summary>
        /// Checks whether this parser can handle a translation file with the given extension.
        /// <para>This method is not used for configuration files.</para>
        /// </summary>
        /// <param name="fileExtension">The extension to check.</param>
        /// <returns><see langword="true"/> if the extension is supported and <see langword="false"/> otherwise.</returns>
        public abstract bool CanHandleExtension(string fileExtension);

        /// <summary>
        /// Loads configuration from the file.
        /// </summary>
        /// <param name="configFile">The name of the file to load the configuration from.</param>
        /// <returns>The loaded configuration or <see langword="null"/> if loading failed.</returns>
        public TranslationConfiguration? ParseConfigurationFile(string configFile)
        {
            if (configFile is null)
                throw new ArgumentNullException(nameof(configFile));

            string? fileContent = null;

            configFile = configFile.Trim();
            if (!string.IsNullOrEmpty(configFile))
                fileContent = Utils.ReadFileFromDisk(configFile.Trim());

            if (fileContent is null)
                return null;

            try
            {
                TranslationConfiguration? result = ParseConfiguration(fileContent, assembly: null);
                if (result is not null)
                {
                    string? dir = Path.GetDirectoryName(configFile);
                    if (!string.IsNullOrEmpty(dir))
                        result.DirectoryHint = dir;
                }

                return result;
            }
            catch (GenericParserException ex)
            {
                throw new ParserFileException(configFile, $"Parsing of the configuration file '{configFile}' has failed with an error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads configuration from the file stored in assembly resource.
        /// </summary>
        /// <param name="assembly">A reference to the assembly, from which the configuration file should be loaded.</param>
        /// <param name="configFile">The name of the file to load the configuration from. This name must include a subdirectory (if any) in resource format, such as "Translations.Data" if the original files' subdirectory is "Translations\Data" or "Translations/Data".</param>
        /// <returns>The loaded configuration or <see langword="null"/> if loading failed.</returns>
        public TranslationConfiguration? ParseConfigurationFile(Assembly assembly, string configFile)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            if (configFile is null)
                throw new ArgumentNullException(nameof(configFile));

            string? fileContent = null;

            configFile = configFile.Trim();
            if (!string.IsNullOrEmpty(configFile))
                fileContent = Utils.ReadFileFromResource(assembly, configFile);

            if (fileContent is null)
                return null;

            try
            {
                TranslationConfiguration? result = ParseConfiguration(fileContent, assembly: assembly);
                if (result is not null)
                {
                    string? dir = Path.GetDirectoryName(configFile);
                    if (!string.IsNullOrEmpty(dir))
                        result.DirectoryHint = dir;
                }

                return result;
            }
            catch (GenericParserException ex)
            {
                throw new ParserFileException(configFile, $"Parsing of the configuration file '{configFile}' in assembly '{assembly.FullName}' has failed with an error: {ex.Message}", ex);
            }
        }

        /*/// <summary>
        /// Checks whether the specified file is a configuration file of the given format.
        /// </summary>
        /// <param name="fileContent">The content of the file.</param>
        /// <param name="configuration">The loaded configuration.</param>
        /// <returns><see langword="true"/> if the config file is recognized and <see langword="false"/> otherwise</returns>
        public abstract bool IsValidConfigFile(string fileContent, out TranslationConfiguration? configuration);
        */

        public abstract TranslationConfiguration? ParseConfiguration(string fileContent, Assembly? assembly);

        /// <summary>
        /// Loads the translation information from the file and returns a translation.
        /// </summary>
        /// <param name="translationText">The text of the file to load.</param>
        /// <param name="culture">An optional reference to the locale, whose translation is to be loaded. Makes sense for CSV and TSV formats that may contain multiple translations in one file.</param>
        /// <returns>The loaded translation or <see langword="null"/> if loading failed.</returns>
        public abstract Translation? LoadTranslation(string translationText, CultureInfo? culture);

        /// <summary>
        /// Loads the keys from the default translation file and builds a tree of keys.
        /// </summary>
        /// <param name="content">The content to parse.</param>
        /// <returns>The constructed <seealso cref="TranslationTree"/> upon success or <see langword="null"/> otherwise. </returns>
        /// <exception cref="TextParseException">Gets thrown when parsing of a default translation file fails.</exception>
        protected abstract TranslationTree? InternalLoadTranslationStructure(string content);

        protected virtual void ValidateConfiguration(TranslationConfiguration configuration)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            // check if the configuration contains a reference to the default file
            if (string.IsNullOrEmpty(configuration.DefaultFile))
                throw new GenericParserException($"No reference to a default translation file is present in the configuration file. The reference must be specified as a '{TranslationConfiguration.KEY_DEFAULT_FILE}' setting.");

            if (!string.IsNullOrEmpty(configuration.DefaultFileLocale))
            {
                try
                {
                    _ = new CultureInfo(configuration.DefaultFileLocale);
                }
                catch
                {
                    throw new GenericParserException($"Unknown locale identifier '{configuration.DefaultFileLocale}' specified as a default locale in the configuration file");
                }
            }

            if (!string.IsNullOrEmpty(configuration.Namespace) && !Utils.IsIdentifierWithDots(configuration.Namespace))
                throw new GenericParserException($"The provided namespace name '{configuration.Namespace}' is not a valid identifier suitable for a namespace name.");

            if (!string.IsNullOrEmpty(configuration.ClassName) && !Utils.IsIdentifier(configuration.ClassName))
                throw new GenericParserException($"The provided class name '{configuration.ClassName}' is not a valid identifier suitable for a class name.");
        }

        /// <summary>
        /// Checks whether the text is templated, i.e. contains placeholders.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns><see langword="true"/> if the text contains placeholders and <see langword="false"/> otherwise.</returns>
        internal virtual bool IsTemplatedText(string text) => false;

        /// <summary>
        /// Checks whether the text is a reference.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns><see langword="true"/> if the text is a reference and <see langword="false"/> otherwise.</returns>
        internal virtual bool IsReference(string text) => RecognizeFileRefs && text.Length > 0 && text[0] == '@';

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

        protected static int GetAbsolutePosition(string text, int lineNumber, int linePosition)
        {
            // LineNumber and LinePosition are 1-based
            int currentLine = 1;
            int index = 0;

            while (currentLine < lineNumber && index < text.Length)
            {
                if (text[index] == '\n')
                {
                    currentLine++;
                }

                index++;
            }

            // Add position within the target line (minus 1 because LinePosition is 1-based)
            return index + (linePosition - 1);
        }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

        protected static TextFormat? DecodeTextProcessingMode(string? value)
        {
            if (value is null)
                return null;

            // Remove a prefix if someone has put it there
            if (value.StartsWith("TextFormat.", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(12);

            // Now try to convert the value into the enum
            if (Enum.TryParse(value, ignoreCase: true, out TextFormat result))
                return result;
            else
                return null;
        }
    }
}
