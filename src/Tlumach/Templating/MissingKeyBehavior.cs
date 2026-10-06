// <copyright file="MissingKeyBehavior.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Templating;

/// <summary>
/// Specifies what <see cref="TemplateTranslator"/> returns for a key that no translation contains.
/// </summary>
public enum MissingKeyBehavior
{
    /// <summary>
    /// The key itself, with the key prefix, is returned. This makes a missing or misspelled key visible in the rendered text without stopping the rendering.
    /// </summary>
    ReturnKey = 0,

    /// <summary>
    /// An empty string is returned.
    /// </summary>
    Empty = 1,

    /// <summary>
    /// A <see cref="TemplateKeyNotFoundException"/> is thrown.
    /// </summary>
    Throw = 2,
}
