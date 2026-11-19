// <copyright file="ValidationCodes.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Validation;

/// <summary>
/// The codes of the checks made by <see cref="TranslationValidator"/>. The codes are stable: the documentation and users' notes refer to them.
/// </summary>
public static class ValidationCodes
{
    /// <summary>The configuration file cannot be read or parsed.</summary>
    public const string ConfigurationUnreadable = "TLUMACHVAL001";

    /// <summary>The default translation file is missing, empty, or cannot be parsed.</summary>
    public const string DefaultFileUnusable = "TLUMACHVAL002";

    /// <summary>A configuration processed by the generator does not name the namespace or the class to generate.</summary>
    public const string GeneratorSettingMissing = "TLUMACHVAL003";

    /// <summary>A file named in the translations section of the configuration does not exist.</summary>
    public const string ReferencedFileMissing = "TLUMACHVAL004";

    /// <summary>A translation file cannot be parsed.</summary>
    public const string TranslationUnparsable = "TLUMACHVAL005";

    /// <summary>The placeholder template of an entry cannot be processed.</summary>
    public const string TemplateInvalid = "TLUMACHVAL006";

    /// <summary>A translation file contains a key that the default file does not contain.</summary>
    public const string KeyNotInDefault = "TLUMACHVAL007";

    /// <summary>A translation file is empty or yields no translation for its locale.</summary>
    public const string TranslationEmpty = "TLUMACHVAL008";

    /// <summary>A locale name in the translations section of the configuration is not a known culture.</summary>
    public const string UnknownLocale = "TLUMACHVAL009";

    /// <summary>A translation file is never loaded because another file takes precedence for its locale.</summary>
    public const string FileShadowed = "TLUMACHVAL010";

    /// <summary>No parser is available for the extension of a file named in the configuration.</summary>
    public const string NoParser = "TLUMACHVAL011";

    /// <summary>A translation lacks keys of the default file; the translation manager shows the texts of the basic culture or of the default file for them.</summary>
    public const string KeyMissingFromTranslation = "TLUMACHVAL012";

    /// <summary>A ResX translation file is embedded in a way that makes it unreadable for the translation manager (compiled into binary resources, moved into a satellite assembly, or embedded without its extension).</summary>
    public const string ResxNotEmbeddedAsText = "TLUMACHVAL013";
}
