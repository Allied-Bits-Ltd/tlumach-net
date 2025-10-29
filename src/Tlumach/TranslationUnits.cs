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

using System.Globalization;
using System.Reflection;

using Tlumach.Base;

namespace Tlumach;

public class BaseTranslationUnit
{
    private readonly TranslationConfiguration _config;

    private readonly TranslationManager _translationManager;
    protected TranslationManager TranslationManager { get { return _translationManager; } }

    public string Key { get; internal set; }

    public BaseTranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
    {
        _translationManager = translationManager;
        _config = translationConfiguration;
        Key = key;
    }

    protected TranslationEntry? InternalGetValue(CultureInfo cultureInfo)
    {
        return _translationManager.GetValue(_config, cultureInfo, Key);
    }

    protected string InternalGetValueAsText(CultureInfo cultureInfo)
    {
        return _translationManager.GetValue(_config, cultureInfo, Key)?.Text ?? string.Empty;
    }
}

public class TranslationUnit : BaseTranslationUnit
{
    public string CurrentValue => InternalGetValueAsText(TranslationManager.CurrentCulture);

    public TranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
        : base(translationManager, translationConfiguration, key)
    {
    }

    public string GetValue(CultureInfo cultureInfo)
    {
        return InternalGetValueAsText(cultureInfo);
    }
}

public class TemplatedTranslationUnit : BaseTranslationUnit
{
    public TemplatedTranslationUnit(TranslationManager translationManager, TranslationConfiguration translationConfiguration, string key)
        : base(translationManager, translationConfiguration, key)
    {
    }

    public string GetValueAsTemplate(CultureInfo cultureInfo)
    {
        return InternalGetValueAsText(cultureInfo);
    }

    public string GetValue(IDictionary<string, object> parameters)
    {
        return GetValue(TranslationManager.CurrentCulture, parameters);
    }

    public string GetValue(CultureInfo cultureInfo, IDictionary<string, object> parameters)
    {
        TranslationEntry? result = InternalGetValue(cultureInfo);

        // If a value was obtained, it contains a template that we need to fill with values
        if (result is not null)
        {
            return ProcessTemplatedValue(result, (key) =>
            {
                string keyUpper = key.ToUpperInvariant();
                return parameters.FirstOrDefault(e => e.Key.Equals(keyUpper, StringComparison.OrdinalIgnoreCase));
            });
        }

        return string.Empty;
    }

    public string GetValue(object parameters)
    {
        return GetValue(TranslationManager.CurrentCulture, parameters);
    }

    public string GetValue(CultureInfo cultureInfo, object parameters)
    {
        TranslationEntry? result = InternalGetValue(cultureInfo);

        // If a value was obtained, it contains a template that we need to fill with values
        if (result is not null)
        {
            return ProcessTemplatedValue(result, (key) =>
            {
                if (ReflectionUtils.TryGetPropertyValue(parameters, key, out object? value))
                    return value;
                else
                    return null;
            });
        }

        return string.Empty;
    }

    private string ProcessTemplatedValue(TranslationEntry entry, Func<string, object?> getParamValueFunc)
    {
        // todo: implement
        return string.Empty;
    }

}
