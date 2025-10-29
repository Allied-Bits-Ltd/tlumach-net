// <copyright file="TranslationConfiguration.cs" company="Allied Bits Ltd.">
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

using System.Reflection;

namespace Tlumach.Base
{
    /// <summary>
    /// Contains the configuration built when parsing source files and passed to the translation manager by the translation unit.
    /// </summary>
    public class TranslationConfiguration
    {

        /// <summary>
        /// The name of the default file (the one that will be loaded by default).
        /// </summary>
        public const string KEY_DEFAULT_FILE = "default_file";

        /// <summary>
        /// The name of the locale of the text in the default file.
        /// </summary>
        public const string KEY_DEFAULT_LOCALE = "default_locale";

        /// <summary>
        /// The name of the namespace that the generator puts to the generated source code.
        /// </summary>
        public const string KEY_GENERATED_NAMESPACE = "generated_namespace";
        /// <summary>
        /// The name of the class that the generator puts to the generated source code.
        /// </summary>
        public const string KEY_GENERATED_CLASS = "generated_class";

        /// <summary>
        /// The name of the translations section in the configuration file.
        /// </summary>
        public const string KEY_SECTION_TRANSLATIONS = "translations";

        public const string KEY_TRANSLATION_ASTERISK = "*";

        public const string KEY_TRANSLATION_DEFAULT = "default";

        public static TranslationConfiguration Empty { get; }

        /// <summary>
        /// A reference to the assembly, in which the generated file resides.
        /// </summary>
        public Assembly? Assembly { get; }

        /// <summary>
        /// A reference to the default file. The value may be a filename with or without a path, but it must include an extension, so that the proper parser can be selected.
        /// </summary>
        public string DefaultFile { get; }

        public string? Namespace { get; }

        public string? ClassName { get; }

        public string? DefaultFileLocale { get; }

        /// <summary>
        /// Contains the list of individual translation items covered by the configuration.
        /// This list may be empty or incomplete, in which case, the library will use heuristics to determine the filename to load the translation from.
        /// </summary>
        public Dictionary<string, string> Translations { get; }

        static TranslationConfiguration()
        {
            Empty = new();
        }

        private TranslationConfiguration()
        {}

        public TranslationConfiguration(Assembly assembly, string defaultFile, string? defaultFileLocale)
        {
            Assembly = assembly;
            DefaultFile = defaultFile;
            DefaultFileLocale = defaultFileLocale;
            Translations = [];
        }

        /// <summary>
        /// Used by configuration parsers.
        /// </summary>
        public TranslationConfiguration(string? defaultFile, string? @namespace, string? className, string? defaultFileLocale)
        {
            DefaultFile = defaultFile;
            DefaultFileLocale = defaultFileLocale;
            Namespace = @namespace;
            ClassName = className;
            Translations = [];
        }
    }
}
