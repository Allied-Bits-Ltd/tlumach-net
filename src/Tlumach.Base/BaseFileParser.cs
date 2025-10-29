// <copyright file="BaseFileParser.cs" company="Allied Bits Ltd.">
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

using System.Text;

namespace Tlumach.Base
{
    public abstract class BaseFileParser
    {
        /// <summary>
        /// Loads the keys from the default translation file and builds a tree of keys.
        /// </summary>
        /// <param name="fileName">the configuration file to read.</param>
        /// <param name="configuration">the loaded configuration or <see langword="null"/> if the method does not succeed.</param>
        /// <returns>The constructed <seealso cref="TranslationTree"/> upon success or <see langword="null"/> otherwise. </returns>
        /// <exception cref="ParserLoadException">Gets thrown when loading of a configuration file or a default translation file fails.</exception>
        /// <exception cref="TextFileParseException">Gets thrown when parsing of a default translation file fails.</exception>
        public TranslationTree? LoadTranslationStructure(string fileName, out TranslationConfiguration? configuration)
        {
            // First, load the configuration
            string? configContent = null;
            try
            {
                configContent = File.ReadAllText(fileName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new ParserLoadException(fileName, $"Loading of the configuration file '{fileName}' has failed", ex);
            }

            try
            {
                configuration = ParseConfiguration(configContent);
            }
            catch (GenericParserException ex)
            {
                throw new ParserFileException(fileName, $"Parsing of the configuration file '{fileName}' has failed with an error: {ex.Message}", ex.InnerException);
            }

            if (configuration is null)
                return null;

            if (string.IsNullOrEmpty(configuration.DefaultFile))
                throw new ParserConfigException($"Configuration file '{fileName}' does not contain a reference to a default translation file. The reference must be specified as a '{TranslationConfiguration.KEY_DEFAULT_FILE}' setting.");

            // Retrieve the name of the default translation file
            string defaultFile = configuration.DefaultFile;
            if (!Path.IsPathRooted(defaultFile))
            {
                defaultFile = Path.Combine(Path.GetDirectoryName(fileName), defaultFile);
            }

            string fileExt = Path.GetFileNameWithoutExtension(defaultFile)?.ToLowerInvariant() ?? string.Empty;

            BaseFileParser? parser;
            if (CanHandleExtension(fileExt))
                parser = this;
            else
                parser = FileFormats.GetParser(fileExt);

            if (parser is null)
                throw new ParserLoadException(fileName, $"No parser found for the {fileExt} file extension that the default translation file '{defaultFile}' has");

            // Read the default translation file
            string? defaultContent = null;
            try
            {
                defaultContent = File.ReadAllText(defaultFile);
            }
            catch (Exception ex)
            {
                throw new ParserLoadException(fileName, $"Loading of the default translation file '{defaultFile}' has failed", ex);
            }

            if (string.IsNullOrEmpty(defaultContent))
                throw new ParserLoadException(fileName, $"Default translation file '{defaultFile}' is empty");

            // Parse the default translation file and return the result
            try
            {
                return parser.InternalLoadTranslationStructure(defaultContent);
            }
            catch (TextParseException ex)
            {
                throw new TextFileParseException(defaultFile, ex.Message, ex.StartPosition, ex.EndPosition, ex.LineNumber, ex.ColumnNumber, ex.InnerException);
            }
        }

        /// <summary>
        /// Checks whether this parser can handle a translation file with the given extension.
        /// </summary>
        /// <param name="fileExtension">the extension to check.</param>
        /// <returns><see langword="true"/> if the extension is supported and <see langword="false"/> otherwise</returns>
        public abstract bool CanHandleExtension(string fileExtension);

        /*/// <summary>
        /// Checks whether the specified file is a configuration file of the given format.
        /// </summary>
        /// <param name="fileContent">the content of the file.</param>
        /// <param name="configuration">the loaded configuration.</param>
        /// <returns><see langword="true"/> if the config file is recognized and <see langword="false"/> otherwise</returns>
        public abstract bool IsValidConfigFile(string fileContent, out TranslationConfiguration? configuration);
*/

        public abstract TranslationConfiguration? ParseConfiguration(string fileContent);

        /// <summary>
        /// Loads the translation information from the file and returns a translation.
        /// </summary>
        /// <param name="translationText">The text of the file to load.</param>
        /// <returns>The loaded translation or <see langword="null"/> if loading failed.</returns>
        public abstract Translation? LoadTranslation(string translationText);

        /// <summary>
        /// Loads the keys from the default translation file and builds a tree of keys.
        /// </summary>
        /// <param name="content">the content to parse.</param>
        /// <returns>The constructed <seealso cref="TranslationTree"/> upon success or <see langword="null"/> otherwise. </returns>
        /// <exception cref="TextParseException">Gets thrown when parsing of a default translation file fails.</exception>
        protected abstract TranslationTree? InternalLoadTranslationStructure(string content);
    }
}
