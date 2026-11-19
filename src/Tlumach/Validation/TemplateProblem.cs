// <copyright file="TemplateProblem.cs" company="Allied Bits Ltd.">
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
/// A problem with a template, found by <see cref="TemplateChecker"/>.
/// </summary>
internal sealed class TemplateProblem
{
    public TemplateProblem(string message, bool definite)
    {
        Message = message;
        Definite = definite;
    }

    /// <summary>Gets the description of the problem.</summary>
    public string Message { get; }

    /// <summary>Gets a value indicating whether formatting the text fails whatever values are passed (an error); otherwise it fails only with values of some types (a warning).</summary>
    public bool Definite { get; }
}
