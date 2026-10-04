// <copyright file="TlumachText.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tlumach.Blazor;

/// <summary>
/// Renders the text of a translation unit or of a key in the culture of the user and re-renders when the user switches the language.
/// <para>The text is HTML-encoded like any other Razor output. If the translation manager encodes values itself (<see cref="TranslationManager.WebEncodeValues"/>),
/// the already encoded text is not encoded again. Set <see cref="AsMarkup"/> only for translations that contain trusted HTML.</para>
/// </summary>
public sealed class TlumachText : ComponentBase
{
    /// <summary>
    /// Gets or sets the translation unit, usually a member of a generated class, e.g. <c>Strings.Hello</c>.
    /// </summary>
    [Parameter]
    public BaseTranslationUnit? Unit { get; set; }

    /// <summary>
    /// Gets or sets the key of the text, used when <see cref="Unit"/> is not set.
    /// </summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the translation manager that resolves <see cref="Key"/>. When not set, <see cref="TlumachBlazorOptions.DefaultManager"/> is used.
    /// </summary>
    [Parameter]
    public TranslationManager? Manager { get; set; }

    /// <summary>
    /// Gets or sets the values of named placeholders, keyed by placeholder names.
    /// </summary>
    [Parameter]
    public IDictionary<string, object?>? Args { get; set; }

    /// <summary>
    /// Gets or sets the values of indexed placeholders, used when <see cref="Args"/> is not set.
    /// </summary>
    [Parameter]
    public IReadOnlyList<object>? Values { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the text is rendered as markup. Use it only for translations that contain trusted HTML.
    /// </summary>
    [Parameter]
    public bool AsMarkup { get; set; }

    [CascadingParameter]
    private TlumachCulture? Culture { get; set; }

    [Inject]
    private TlumachCultureState State { get; set; } = default!;

    [Inject]
    private TlumachBlazorOptions Options { get; set; } = default!;

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        if (Unit is null && string.IsNullOrEmpty(Key))
            throw new InvalidOperationException($"{nameof(TlumachText)} requires either the {nameof(Unit)} or the {nameof(Key)} parameter.");

        if (Unit is null && (Manager ?? Options.DefaultManager) is null)
            throw new InvalidOperationException($"{nameof(TlumachText)} with the {nameof(Key)} parameter requires the {nameof(Manager)} parameter or {nameof(TlumachBlazorOptions)}.{nameof(TlumachBlazorOptions.DefaultManager)}.");
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        TlumachCulture culture = Culture ?? State.Current;
        object[]? values = Values is null ? null : Values as object[] ?? [.. Values];

        string text;
        bool markup;
        if (Unit is not null)
        {
            text = culture.GetRaw(Unit, Args, values);

            // With WebEncodeValues, the unit returns encoded text; adding it as markup keeps it from being encoded twice.
            markup = AsMarkup || Unit.TranslationManager.WebEncodeValues;
        }
        else
        {
            text = culture.GetByKey((Manager ?? Options.DefaultManager)!, Key!, Args, values);
            markup = AsMarkup;
        }

        if (markup)
            builder.AddMarkupContent(0, text);
        else
            builder.AddContent(1, text);
    }
}
