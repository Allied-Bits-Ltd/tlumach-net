// <copyright file="UnitMessageBinding.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.DataAnnotations
{
    /// <summary>
    /// A validation message read from a translation unit of a class created by Tlumach Generator.
    /// </summary>
    internal sealed class UnitMessageBinding : ITlumachMessageBinding
    {
        private readonly BaseTranslationUnit _unit;

        internal UnitMessageBinding(BaseTranslationUnit unit)
        {
            _unit = unit;
        }

        public CultureInfo GetCulture(TranslationCultureSource cultureSource)
            => cultureSource == TranslationCultureSource.Ambient ? CultureInfo.CurrentCulture : _unit.TranslationManager.CurrentCulture;

        public string GetTemplate(TranslationCultureSource cultureSource)
            => _unit.GetValueAsTemplate(GetCulture(cultureSource));
    }
}
