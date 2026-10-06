// <copyright file="ValidatorOptionsScope.cs" company="Allied Bits Ltd.">
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
using System.Linq.Expressions;
using System.Reflection;

using FluentValidation;
using FluentValidation.Resources;

namespace Tlumach.FluentValidationTests;

/// <summary>
/// Restores the global options of FluentValidation and the cultures of the current thread that a test changes.
/// </summary>
internal sealed class ValidatorOptionsScope : IDisposable
{
    private readonly ILanguageManager _languageManager = ValidatorOptions.Global.LanguageManager;
    private readonly Func<Type, MemberInfo, LambdaExpression, string> _displayNameResolver = ValidatorOptions.Global.DisplayNameResolver;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public void Dispose()
    {
        ValidatorOptions.Global.LanguageManager = _languageManager;
        ValidatorOptions.Global.DisplayNameResolver = _displayNameResolver;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
