// <copyright file="TranslationFileOrigin.cs" company="Allied Bits Ltd.">
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

#if GENERATOR
namespace Tlumach.Generator.Validation;
#else
namespace Tlumach.Validation;
#endif

/// <summary>
/// How <see cref="TranslationValidator"/> found a translation file.
/// </summary>
public enum TranslationFileOrigin
{
    /// <summary>The default file of the configuration.</summary>
    DefaultFile = 0,

    /// <summary>The file is named in the translations section of the configuration.</summary>
    Configuration = 1,

    /// <summary>The file is next to the default file and is named after it, as <c>TranslationManager</c> looks for translation files.</summary>
    NamingConvention = 2,

    /// <summary>The file is elsewhere on the disk, but the project links it into the directory of the default file under a name that follows the naming convention (the translation manager finds it among the embedded resources).</summary>
    LinkedItem = 3,
}
