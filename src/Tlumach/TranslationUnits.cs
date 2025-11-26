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

using Tlumach.Base;

namespace Tlumach;

/// <summary>
/// The base class for TranslationUnit and TemplatedTranslationUnit classes.
/// </summary>
public class BaseTranslationUnit
{
    private readonly TranslationConfiguration _translationConfiguration;

    public TranslationManager TranslationManager { get; }

    public string Key { get; internal set; }

    protected TranslationConfiguration TranslationConfiguration => _translationConfiguration;

    public BaseTranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
    {
        TranslationManager = translationManager;
        _translationConfiguration = translationConfiguration;
        Key = key;
    }

    protected TranslationEntry? InternalGetValue(CultureInfo cultureInfo)
    {
        return TranslationManager.GetValue(TranslationConfiguration, Key, cultureInfo);
    }

    protected string InternalGetValueAsText(CultureInfo cultureInfo)
    {
        return TranslationManager.GetValue(TranslationConfiguration, Key, cultureInfo)?.Text ?? string.Empty;
    }
}

/// <summary>
/// <para>Represents a unit of translation - a unit of text (a word, a phrase, a sentence, etc.) in a translation accessible using a unique key - that does not contain parameters ('format items' in .NET terms) and can be used without further processing.</para>
/// <para>This class is used in generated code except when using Avalonia, WinUI or UWP (those have own TranslationUnit classes in the corresponding assemblies).</para>
/// </summary>
public class TranslationUnit : BaseTranslationUnit
{
    public string CurrentValue => InternalGetValueAsText(TranslationManager.CurrentCulture);

    public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
        : base(translationManager, translationConfiguration, key)
    {
    }

    /// <summary>
    /// Returns the value of the translation text for the specified culture/locale.
    /// </summary>
    /// <param name="cultureInfo">The culture/locale for which the text is needed.</param>
    /// <returns>The requested text or an empty string.</returns>
    public string GetValue(CultureInfo cultureInfo)
    {
        return InternalGetValueAsText(cultureInfo);
    }
}

/// <summary>
/// <para>Represents a unit of translation - a unit of text (a word, a phrase, a sentence, etc.) in a translation accessible using a unique key - that contains parameters ('format items' in .NET terms).</para>
/// <para>This class enables an application to relay processing of templates on the library.</para>
/// </summary>
public class TemplatedTranslationUnit : BaseTranslationUnit
{
    public TemplatedTranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
        : base(translationManager, translationConfiguration, key)
    {
    }

    /// <summary>
    /// Returns the text of the template translation entry without processing the template. This may be useful when template processing is handled by the caller (e.g., when strings use .NET template format which is handled using the <see cref="string.Format"/> method).
    /// </summary>
    /// <param name="cultureInfo">The culture/locale for which the text is needed.</param>
    /// <returns>The requested text or an empty string.</returns>
    public string GetValueAsTemplate(CultureInfo cultureInfo)
    {
        return InternalGetValueAsText(cultureInfo);
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/> is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named parameters. It will work for indexed parameters if the parameters in the `parameters` dictionary use indexes for keys.</para>
    /// </summary>
    /// <param name="parameters">A dictionary that contains parameter names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(IDictionary<string, object?> parameters)
    {
        return GetValue(TranslationManager.CurrentCulture, parameters);
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named parameters. It will work for indexed parameters if the parameters in the `parameters` dictionary use indexes for keys.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="parameters">A dictionary that contains parameter names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, IDictionary<string, object?> parameters)
    {
        return InternalGetValue(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode, parameters) ?? string.Empty;
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for both named and indexed parameters.</para>
    /// </summary>
    /// <param name="parameters">A dictionary that contains parameter names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(OrderedDictionary parameters)
    {
        return GetValue(TranslationManager.CurrentCulture, parameters);
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for both named and indexed parameters.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="parameters">a dictionary that contains parameter names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, OrderedDictionary parameters)
    {
        return InternalGetValue(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode, parameters) ?? string.Empty;
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for indexed parameters but not for named ones.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="parameters">a dictionary that contains parameter names as keys and actual values to substitute as values.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, params object[] parameters)
    {
        return InternalGetValue(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode, parameters) ?? string.Empty;
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named parameters but not for indexed ones.</para>
    /// </summary>
    /// <param name="parameters">An object, whose properties are used to provide values for parameters in the template. The names of the template's parameters are matched with the object property names in a case-insensitive manner.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">is thrown if processing of the template fails.</exception>
    public string GetValue(object parameters)
    {
        return GetValue(TranslationManager.CurrentCulture, parameters);
    }

    /// <summary>
    /// Processes the template translation entry by substituting the parameters with actual values and returns the final text.
    /// <para>If <see cref="TranslationConfiguration.TextProcessingMode"/>  is <seealso cref="TextFormat.DotNet"/>, <seealso cref="TextFormat.Arb"/>, or <seealso cref="TextFormat.ArbNoEscaping"/>, this overload will work for named parameters but not for indexed ones.</para>
    /// </summary>
    /// <param name="culture">The culture/locale for which the text is needed.</param>
    /// <param name="parameters">An object, whose properties are used to provide values for parameters in the template. The names of the template's parameters are matched with the object property names in a case-insensitive manner.</param>
    /// <returns>The requested text or an empty string.</returns>
    /// <exception cref="TemplateProcessingException">is thrown if processing of the template fails.</exception>
    public string GetValue(CultureInfo culture, object parameters)
    {
        return InternalGetValue(culture)?.ProcessTemplatedValue(culture, TranslationConfiguration.TextProcessingMode, parameters) ?? string.Empty;
    }
}
