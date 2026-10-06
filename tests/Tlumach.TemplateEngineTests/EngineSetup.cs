// <copyright file="EngineSetup.cs" company="Allied Bits Ltd.">
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

using Tlumach.Templating;

namespace Tlumach.TemplateEngineTests;

/// <summary>
/// The engine-neutral configuration of one render in the shared scenarios. Each engine maps it to its own options.
/// </summary>
#pragma warning disable CA1515 // The record is part of the protected signatures of the public test classes, which xUnit requires to be public.
public sealed record EngineSetup
#pragma warning restore CA1515
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EngineSetup"/> class.
    /// </summary>
    /// <param name="manager">The translation manager.</param>
    public EngineSetup(TranslationManager manager)
    {
        Manager = manager;
    }

    public TranslationManager Manager { get; }

    public string? KeyPrefix { get; init; }

    public MissingKeyBehavior MissingKey { get; init; } = MissingKeyBehavior.ReturnKey;

    public Func<string, CultureInfo, string?>? OnMissingKey { get; init; }

    public string FunctionName { get; init; } = "t";

    public string? MarkupFunctionName { get; init; } = "t_html";

    /// <summary>
    /// Gets a value indicating whether the output is HTML: Scriban encodes the output of <c>t</c> (<c>HtmlEncode</c>), Fluid renders with <c>HtmlEncoder.Default</c>,
    /// and Handlebars escapes <c>{{ }}</c> (otherwise <c>NoEscape</c> is set).
    /// </summary>
    public bool HtmlOutput { get; init; }
}
