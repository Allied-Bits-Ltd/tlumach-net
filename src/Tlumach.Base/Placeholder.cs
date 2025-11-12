// <copyright file="Placeholder.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Base
{
    /// <summary>
    /// Represents a single placeholder within a templated entry.
    /// </summary>
    public class Placeholder
    {
        /// <summary>
        /// Gets or sets the name of the placeholder.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the type of the placeholder as specified in the translation file.
        /// </summary>
        public string? Type { get; protected set; }

        /// <summary>
        /// Gets or sets the format of the placeholder as specified in the translation file.
        /// </summary>
        public string? Format { get; protected set; }

        /// <summary>
        /// Gets or sets the example if one was provided in the translation file.
        /// </summary>
        public string? Example { get; protected set; }

        /// <summary>
        /// Contains additional (unrecognized) properties provided for the placeholder in the translation file.
        /// </summary>
        public Dictionary<string, string> Properties { get; } = [];

        /// <summary>
        /// Contains optional parameters as provided in the "optionalParameters" object for the placeholder in the translation file.
        /// </summary>
        public Dictionary<string, string> OptionalParameters { get; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="Placeholder"/> class.
        /// </summary>
        /// <param name="name">The name of the placeholder.</param>
        public Placeholder(string name) => Name = name;
    }
}
