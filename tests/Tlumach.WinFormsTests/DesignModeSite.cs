// <copyright file="DesignModeSite.cs" company="Allied Bits Ltd.">
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

using System.ComponentModel;

namespace Tlumach.WinFormsTests
{
    /// <summary>
    /// A site that reports design mode, as the Visual Studio Designer does for the components on the designed form.
    /// </summary>
    internal sealed class DesignModeSite : ISite
    {
        public DesignModeSite(IComponent component) => Component = component;

        public IComponent Component { get; }

        public IContainer? Container => null;

        public bool DesignMode => true;

        public string? Name { get; set; } = "translationProvider1";

        public object? GetService(Type serviceType) => null;
    }
}
