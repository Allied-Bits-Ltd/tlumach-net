// <copyright file="ManagerMessageBinding.cs" company="Allied Bits Ltd.">
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
    /// A validation message read through the translation manager of a class created by Tlumach Generator, by the key of the translation entry.
    /// <para>This is the route used when the annotation refers to a key rather than to a translation unit member, which is what a configuration with <c>onlyDeclareKeys</c> produces.</para>
    /// </summary>
    internal sealed class ManagerMessageBinding : ITlumachMessageBinding
    {
        private readonly TranslationManager _manager;

        private readonly string _key;

        internal ManagerMessageBinding(TranslationManager manager, string key)
        {
            _manager = manager;
            _key = key;
        }

        public CultureInfo GetCulture(TranslationCultureSource cultureSource)
            => cultureSource == TranslationCultureSource.Ambient ? CultureInfo.CurrentCulture : _manager.CurrentCulture;

        public string GetTemplate(TranslationCultureSource cultureSource)
            => _manager.GetValue(_key, GetCulture(cultureSource)).Text ?? string.Empty;
    }
}
