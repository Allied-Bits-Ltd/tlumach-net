// <copyright file="TlumachComponentBase.cs" company="Allied Bits Ltd.">
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

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

namespace Tlumach.Blazor;

/// <summary>
/// An optional base class for components that use translations in code or in attributes, where <see cref="TlumachText"/> cannot be placed.
/// <para>The component re-renders when the user switches the language, because it takes the cascading <see cref="TlumachCulture"/> value.
/// The class subscribes to no events, so it needs no disposal.</para>
/// </summary>
public abstract class TlumachComponentBase : ComponentBase
{
    private string? _lastCultureName;

    /// <summary>
    /// Gets or sets the cascaded culture snapshot.
    /// </summary>
    [CascadingParameter]
    protected TlumachCulture? CascadedCulture { get; set; }

    /// <summary>
    /// Gets or sets the culture state of the user, e.g. for switching the language.
    /// </summary>
    [Inject]
    protected TlumachCultureState CultureState { get; set; } = default!;

    /// <summary>
    /// Gets the culture snapshot used by the <c>T</c> methods.
    /// </summary>
    protected TlumachCulture Culture => CascadedCulture ?? CultureState.Current;

    /// <summary>
    /// Detects a change of the cascaded culture and, when it has changed, calls <see cref="OnCultureChangedAsync"/> before the parameters are applied.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns>A task that completes when the parameters have been set.</returns>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue(nameof(CascadedCulture), out TlumachCulture? incoming) && incoming is not null)
        {
            bool changed = _lastCultureName is not null && !incoming.Culture.Name.Equals(_lastCultureName, StringComparison.Ordinal);
            _lastCultureName = incoming.Culture.Name;
            if (changed)
                await OnCultureChangedAsync(incoming).ConfigureAwait(true);
        }

        await base.SetParametersAsync(parameters).ConfigureAwait(true);
    }

    /// <summary>
    /// Returns the text of the unit as plain text, suitable for attributes and code.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <returns>The text.</returns>
    protected string T(BaseTranslationUnit unit) => Culture.Get(unit);

    /// <summary>
    /// Returns the text of the unit with indexed placeholders replaced.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="values">The values of the placeholders.</param>
    /// <returns>The text.</returns>
    protected string T(BaseTranslationUnit unit, params object[] values) => Culture.Get(unit, values);

    /// <summary>
    /// Returns the text of the unit with named placeholders replaced.
    /// </summary>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The values of the placeholders, keyed by placeholder names.</param>
    /// <returns>The text.</returns>
    protected string T(BaseTranslationUnit unit, IDictionary<string, object?> args) => Culture.Get(unit, args);

    /// <summary>
    /// Returns the text of the unit with named placeholders replaced by the public properties of <paramref name="args"/>, e.g. an anonymous object.
    /// <para>The method has its own name: as an overload of <c>T</c>, it would win over the other overloads for arrays and dictionaries and silently change their meaning.</para>
    /// </summary>
    /// <typeparam name="TArgs">The type that supplies the values.</typeparam>
    /// <param name="unit">The translation unit.</param>
    /// <param name="args">The object that supplies the values.</param>
    /// <returns>The text.</returns>
    protected string TFrom<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(BaseTranslationUnit unit, TArgs args)
        => Culture.GetFrom(unit, args);

    /// <summary>
    /// Called before the component re-renders because the user has switched the language.
    /// </summary>
    /// <param name="culture">The new culture snapshot. <see cref="Culture"/> still returns the previous one during the call.</param>
    /// <returns>A task that completes when the component has reacted.</returns>
    protected virtual Task OnCultureChangedAsync(TlumachCulture culture) => Task.CompletedTask;
}
