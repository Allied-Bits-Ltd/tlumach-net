// <copyright file="FileContentNeededEventArgs.cs" company="Allied Bits Ltd.">
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

namespace Tlumach
{
    /// <summary>
    /// Contains the arguments of the FileContentsNeeded event.
    /// </summary>
    public class FileContentNeededEventArgs : EventArgs
    {
        // An optional assembly from which the file is supposed to be loaded as known to the translation unit.
        public Assembly? Assembly { get; }

        /// <summary>
        /// The culture, for which the file is needed.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// The name of the default file as determined from the configuration and known to the translation unit.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The placeholder for the content of the file. If it is set to any non-empty value, no attempt to load a file by other means will be done.
        /// </summary>
        public string Content { get; set; }

        public FileContentNeededEventArgs(Assembly? assembly, string fileName, CultureInfo culture)
        {
            Assembly = assembly;
            FileName = fileName;
            Culture = culture;
            Content = string.Empty;
        }
    }
}
