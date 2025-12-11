// <copyright file="TranslationUnits.cs" company="Allied Bits Ltd.">
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

using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

using Tlumach.Base;

namespace Tlumach;

/// <summary>
/// The base class for TranslationUnit classes.
/// </summary>
public class BaseTranslationUnit
{
    protected readonly string? _constantValue;

    private readonly TranslationConfiguration _translationConfiguration;

    private Dictionary<string, object?>? _placeholderValueCache;

    protected TranslationEntry? _constantEntry;

    /// <summary>
    /// Gets a reference to the configuration used when creating a unit.
    /// </summary>
    protected TranslationConfiguration TranslationConfiguration => _translationConfiguration;

    /// <summary>
    /// Gets the reference to the translation manager, associated with this unit.
    /// </summary>
    public TranslationManager TranslationManager { get; }

    /// <summary>
    /// Gets the key of the translation unit, i.e., the identifier with which this unit appears in translation files.
    /// </summary>
    public string Key { get; internal set; }

    /// <summary>
    /// Gets the value indicating whether the text of the unit contains placeholders, and the application has to provide values via the <seealso cref="OnPlaceholderValueNeeded"/> event or using the <seealso cref="CachePlaceholderValue(string, object?)"/> method.
    /// </summary>
    public bool ContainsPlaceholders { get; internal set; }

    /// <summary>
    /// Fires when the value for a placeholder is needed during the retrieval of the text.
    /// </summary>
    public event EventHandler<PlaceholderValueNeededEventArgs>? OnPlaceholderValueNeeded;

    public BaseTranslationUnit(string constantValue, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
    {
        _constantValue = constantValue;
        TranslationManager = TranslationManager.Empty;
        _translationConfiguration = translationConfiguration;
        Key = string.Empty;
        ContainsPlaceholders = containsPlaceholders;
    }

    public BaseTranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key, bool containsPlaceholders)
    {
        TranslationManager = translationManager;
        _translationConfiguration = translationConfiguration;
        Key = key;
        ContainsPlaceholders = containsPlaceholders;
    }

    protected virtual TranslationEntry? InternalGetEntry(CultureInfo cultureInfo)
    {
        if (TranslationManager is not null)
            return TranslationManager.GetValue(TranslationConfiguration, Key, cultureInfo);

        if (_constantValue is not null)
            return _constantEntry ??= new TranslationEntry(string.Empty, _constantValue);

        return null;
    }

    protected virtual string InternalGetValueAsText(CultureInfo culture)
    {
        if (TranslationManager is not null)
            return TranslationManager.GetValue(TranslationConfiguration, Key, culture)?.Text ?? string.Empty;

        return _constantValue ?? string.Empty;
    }

    /// <summary>
    /// Returns the text of the template translation entry without processing the template. This may be useful when template processing is handled by the caller (e.g., when strings use .NET template format which is handled using the <see cref="string.Format(string, object[])"/> method or its overloads).
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <returns>The requested text or an empty string.</returns>
    public string GetValueAsTemplate(CultureInfo culture)
    {
        return InternalGetValueAsText(culture);
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>This overload will use the cached values if those are available, or will fire the <seealso cref="OnPlaceholderValueNeeded"/> event to obtain the values.</para>
    /// </summary>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue()
    {
        return GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>This overload will use the cached values if those are available, or will fire the <seealso cref="OnPlaceholderValueNeeded"/> event to obtain the values.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture)
    {
        if (!ContainsPlaceholders)
            return InternalGetValueAsText(culture);

        return InternalGetEntry(culture)?.ProcessTemplatedValue(
            culture,
            TranslationConfiguration.TextProcessingMode ?? TextFormat.None,
            (name, index) =>
                {
                    object? value = null;
                    if (_placeholderValueCache?.TryGetValue(name, out value) == true)
                        return value;

                    if (OnPlaceholderValueNeeded is not null)
                    {
                        PlaceholderValueNeededEventArgs args = new(name, index);
                        OnPlaceholderValueNeeded.Invoke(this, args);
                        value = args.Value;
                        if (args.CacheValue)
                        {
                            CachePlaceholderValue(name, value);
                        }
                    }

                    return value;
                }) ?? string.Empty;
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/> is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named placeholders. It will work for indexed placeholders if the values in the `placeholderValues` dictionary use indexes for keys.</para>
    /// </summary>
    /// <param name="placeholderValues">A dictionary that contains placeholder names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(IDictionary<string, object?> placeholderValues)
    {
        return GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture, placeholderValues);
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named placeholders. It will work for indexed placeholders if the values in the `placeholderValues` dictionary use indexes for keys.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="placeholderValues">A dictionary that contains placeholder names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, IDictionary<string, object?> placeholderValues)
    {
        if (!ContainsPlaceholders)
            return InternalGetValueAsText(culture);

        return InternalGetEntry(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode ?? TextFormat.None, placeholderValues) ?? string.Empty;
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for both named and indexed placeholders.</para>
    /// </summary>
    /// <param name="placeholderValues">A dictionary that contains placeholder names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(OrderedDictionary placeholderValues)
    {
        return GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture, placeholderValues);
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for both named and indexed placeholders.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="placeholderValues">a dictionary that contains placeholder names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, OrderedDictionary placeholderValues)
    {
        if (!ContainsPlaceholders)
            return InternalGetValueAsText(culture);

        return InternalGetEntry(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode ?? TextFormat.None, placeholderValues) ?? string.Empty;
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for indexed placeholders but not for named ones.</para>
    /// </summary>
    /// <param name="placeholderValues">a dictionary that contains placeholder names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(params object[] placeholderValues)
    {
        return GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture, placeholderValues);
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for indexed placeholders but not for named ones.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="placeholderValues">a dictionary that contains placeholder names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, params object[] placeholderValues)
    {
        if (!ContainsPlaceholders)
            return InternalGetValueAsText(culture);

        return InternalGetEntry(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode ?? TextFormat.None, placeholderValues) ?? string.Empty;
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named placeholders but not for indexed ones.</para>
    /// </summary>
    /// <param name="placeholderValues">An object, whose properties are used to provide values for placeholders in the template. The names of the template's placeholders are matched with the object property names in a case-insensitive manner.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">is thrown if processing of the template fails.</exception>
    public string GetValue(object placeholderValues)
    {
        return GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture, placeholderValues);
    }

    /// <summary>
    /// Processes the templated translation entry by substituting the placeholders with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named placeholders but not for indexed ones.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="placeholderValues">An object, whose properties are used to provide values for placeholders in the template. The names of the template's placeholders are matched with the object property names in a case-insensitive manner.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">is thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, object placeholderValues)
    {
        if (!ContainsPlaceholders)
            return InternalGetValueAsText(culture);

        return InternalGetEntry(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode ?? TextFormat.None, placeholderValues) ?? string.Empty;
    }

    /// <summary>
    /// Adds a value for the placeholder to the cache.
    /// <para>Call <seealso cref="NotifyPlaceholdersUpdated"/> to notify the bindings after the list of values is upated.</para>
    /// </summary>
    /// <param name="name">The name of the value to cache.</param>
    /// <param name="value">The value to cache.</param>
    public void CachePlaceholderValue(string name, object? value)
    {
        _placeholderValueCache ??= new(StringComparer.InvariantCulture);
        _placeholderValueCache[name] = value;
    }

    /// <summary>
    /// Removes the value from the cache.
    /// <para>Call <seealso cref="NotifyPlaceholdersUpdated"/> to notify the bindings after the list of values is upated.</para>
    /// </summary>
    /// <param name="name">The name of the placeholder, whose value is to be removed.</param>
    public void ForgetPlaceholderValue(string name)
    {
        _placeholderValueCache?.Remove(name);
    }

    /// <summary>
    /// Notifies XAML bindings that they need to request a new value and update the controls.
    /// <para>Use this method when underlying data changes regardless of whether you cache this data as values or pass it via the <seealso cref="OnPlaceholderValueNeeded"/> event.</para>
    /// </summary>
    public virtual void NotifyPlaceholdersUpdated()
    {
    }
}

/// <summary>
/// <para>Represents a unit of translation - a unit of text (a word, a phrase, a sentence, etc.) in a translation accessible using a unique key.</para>
/// <para>This class is used in generated code except when using Avalonia, WinUI or UWP (those have own TranslationUnit classes in the corresponding assemblies).</para>
/// </summary>
public class TranslationUnit : BaseTranslationUnit, IDisposable
{
    private string? _currentValue;

    private bool disposedValue;

    /// <summary>
    /// Gets or sets the indicator that tells the class that it may cache the string values without going for them to TranslationManager every time.
    /// </summary>
    public static bool CacheValues { get; set; } = true;

    /// <summary>
    /// Gets the value of the unit according to the current culture set in the associated translation manager.
    /// </summary>
    public virtual string CurrentValue
    {
        get
        {
            if (CacheValues)
            {
                if (_currentValue is null)
                {
                    if (_constantValue is not null)
                        _currentValue = _constantValue;
                    else
                        _currentValue = GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture);
                }

                return _currentValue;
            }
            else
            {
                return GetValue(TranslationManager?.CurrentCulture ?? CultureInfo.CurrentCulture);
            }
        }
    }

    /// <summary>
    /// Fires when the <see cref="NotifyPlaceholdersUpdated"/> method is called to notify listeners (mostly, XAML bindings) that they need to update.
    /// <para>Does not fire when the culture of the translation manager referenced by the <see cref="BaseTranslationUnit.TranslationManager"/> property changes.
    /// If an application needs, it can subscribe to the <see cref="TranslationManager.OnCultureChanged" /> event.</para>
    /// </summary>
    public event EventHandler<EventArgs>? OnChange;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationUnit"/> class.
    /// <para>This constructor is used to create a constant translation unit, i.e., the one whose text does not come from a translation file but is fixed.</para>
    /// </summary>
    /// <param name="constantValue">The string value to return.</param>
    /// <param name="translationConfiguration">A reference to an instance of <seealso cref="TranslationConfiguration"/>. If <paramref name="containsPlaceholders"/> is <see langword="true"/>, this configuration's TextProcessingMode is used to process the <paramref name="constantValue"/>.</param>
    /// <param name="containsPlaceholders">Specifies whether <paramref name="constantValue"/> contains placeholders and should be processed accordingly.</param>
    public TranslationUnit(string constantValue, TranslationConfiguration translationConfiguration, bool containsPlaceholders)
        : base(constantValue, translationConfiguration, containsPlaceholders)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationUnit"/> class.
    /// <para>For internal use. This constructor is used by the code generated by Generator.</para>
    /// </summary>
    /// <param name="translationManager">The translation manager to which the unit is bound.</param>
    /// <param name="translationConfiguration">The translation configuration used to create the unit.</param>
    /// <param name="key">The key of the unit.</param>
    /// <param name="containsPlaceholders">An indicator of whether the unit contains placeholders.</param>
    public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key, bool containsPlaceholders)
        : base(translationManager, translationConfiguration, key, containsPlaceholders)
    {
        if (translationManager is not null)
            translationManager.OnCultureChanged += TranslationManager_OnCultureChanged;
    }

    public override string ToString() => this.Key;

    public static implicit operator string(TranslationUnit unit)
        => unit?.ToString() ?? string.Empty;

    /// <summary>
    /// Notifies XAML bindings that they need to request a new value and update the controls.
    /// </summary>
    public override void NotifyPlaceholdersUpdated()
    {
        _currentValue = null;
        OnChange?.Invoke(this, EventArgs.Empty);
    }

    private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        _currentValue = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (TranslationManager is not null)
                    TranslationManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
