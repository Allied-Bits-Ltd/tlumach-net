// <copyright file="CultureNameMatchEventArgs.cs" company="Allied Bits Ltd.">
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

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
#pragma warning disable CA1510 // Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance

    public class CultureNameMatchEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the value to match.
        /// </summary>
        public string Candidate { get; }

        /// <summary>
        /// Gets the culture to match the Candidate against.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets or sets the flag that says whether the value in Candidate corresponds to the culture.
        /// </summary>
        public bool Match { get; set;  }

        internal CultureNameMatchEventArgs(string candidate, CultureInfo culture)
        {
            Candidate = candidate;
            Culture = culture;
            Match = false;
        }
    }
}
