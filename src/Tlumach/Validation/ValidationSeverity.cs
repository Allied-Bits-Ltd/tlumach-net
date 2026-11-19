// <copyright file="ValidationSeverity.cs" company="Allied Bits Ltd.">
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
/// The severity of a <see cref="ValidationMessage"/>.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>A note that does not indicate a problem.</summary>
    Info = 0,

    /// <summary>A problem that does not stop the file from loading, but most likely is a mistake.</summary>
    Warning = 1,

    /// <summary>A problem that makes the file, or a part of it, unusable at run time.</summary>
    Error = 2,
}
