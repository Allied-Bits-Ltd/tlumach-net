// <copyright file="TranslationValueEventArgs.cs" company="Allied Bits Ltd.">
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
    /// <summary>
    /// Contains the arguments of the ReferenceNotResolved event.
    /// </summary>
    public class ReferenceNotResolvedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a reference to the culture, for which the text is needed.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets the key of the requested translation entry.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the reference that has not been resolved.
        /// </summary>
        public string Reference { get; }

        /// <summary>
        /// Should be set to the text value that corresponds to the specified Key and Culture.
        /// </summary>
        public string? Text { get; set; }

        public ReferenceNotResolvedEventArgs(CultureInfo culture, string key, string reference)
        {
            Culture = culture;
            Key = key;
            Reference = reference;
            Text = '@' + reference;
        }
    }
}
