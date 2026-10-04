// <copyright file="TlumachCultureSelector.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tlumach.Blazor;

/// <summary>
/// Lets the user choose one of <see cref="TlumachBlazorOptions.SupportedCultures"/>.
/// <para>In an interactive component, it is a <c>select</c> element that switches the language when the selection changes. In static server-side rendering,
/// it is a form that sends the choice to the culture endpoint (<c>MapTlumachCultureEndpoint</c>), which stores it in a cookie and reloads the page.</para>
/// </summary>
public sealed class TlumachCultureSelector : ComponentBase
{
    /// <summary>
    /// Gets or sets a value indicating whether the page is reloaded after a switch so that all formatting uses the new culture.
    /// </summary>
    [Parameter]
    public bool ForceReload { get; set; }

    /// <summary>
    /// Gets or sets the function that returns the label of a culture. By default, the native name of the culture is used.
    /// </summary>
    [Parameter]
    public Func<CultureInfo, string>? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the text of the submit button of the form rendered in static server-side rendering.
    /// </summary>
    [Parameter]
    public string SubmitText { get; set; } = "OK";

    /// <summary>
    /// Gets or sets the attributes applied to the <c>select</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private TlumachCulture? Culture { get; set; }

    [Inject]
    private TlumachCultureState State { get; set; } = default!;

    [Inject]
    private TlumachBlazorOptions Options { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        CultureInfo current = (Culture ?? State.Current).Culture;

        if (RendererInfo.IsInteractive)
        {
            builder.OpenRegion(0);
            BuildSelect(builder, current, interactive: true);
            builder.CloseRegion();
            return;
        }

        builder.OpenElement(1, "form");
        builder.AddAttribute(2, "method", "get");
        builder.AddAttribute(3, "action", Navigation.BaseUri + Options.CultureEndpoint.TrimStart('/'));
        builder.OpenElement(4, "input");
        builder.AddAttribute(5, "type", "hidden");
        builder.AddAttribute(6, "name", "redirectUri");
        builder.AddAttribute(7, "value", new Uri(Navigation.Uri).PathAndQuery);
        builder.CloseElement();
        builder.OpenRegion(8);
        BuildSelect(builder, current, interactive: false);
        builder.CloseRegion();
        builder.OpenElement(9, "button");
        builder.AddAttribute(10, "type", "submit");
        builder.AddContent(11, SubmitText);
        builder.CloseElement();
        builder.CloseElement();
    }

    private void BuildSelect(RenderTreeBuilder builder, CultureInfo current, bool interactive)
    {
        IReadOnlyList<CultureInfo> cultures = State.SupportedCultures.Count > 0 ? State.SupportedCultures : [current];

        builder.OpenElement(0, "select");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "name", "culture");
        if (interactive)
            builder.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, OnChangeAsync));

        foreach (CultureInfo culture in cultures)
        {
            builder.OpenElement(4, "option");
            builder.AddAttribute(5, "value", culture.Name);
            builder.AddAttribute(6, "selected", culture.Name.Equals(current.Name, StringComparison.OrdinalIgnoreCase));
            builder.AddContent(7, DisplayName?.Invoke(culture) ?? culture.NativeName);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        if (e.Value is string name && name.Length > 0)
            await State.SetCultureAsync(CultureInfo.GetCultureInfo(name), ForceReload).ConfigureAwait(true);
    }
}
