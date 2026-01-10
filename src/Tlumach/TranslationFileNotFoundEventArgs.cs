// <copyright file="TranslationManager.cs" company="Allied Bits Ltd.">
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

using Tlumach.Base;

namespace Tlumach
{
    public class TranslationFileNotFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a reference to the culture, for which the text is needed.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets or sets a reference to the translation that should be used in place of the missing translation file.
        /// </summary>
        public Translation? Translation { get; set; }

        public TranslationFileNotFoundEventArgs(CultureInfo culture)
        {
            Culture = culture;
        }
    }
}
