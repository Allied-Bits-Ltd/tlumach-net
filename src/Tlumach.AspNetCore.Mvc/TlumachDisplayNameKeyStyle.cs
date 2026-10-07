// <copyright file="TlumachDisplayNameKeyStyle.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// How the key of the container type of a property is formed; see <see cref="TlumachDisplayNameKeys.GetContainerKey"/>.
/// </summary>
public enum TlumachDisplayNameKeyStyle
{
    /// <summary>The namespace without the root namespace of the application, then the declaring types: "Pages.Movies.CreateModel.InputModel". Unique within an application.</summary>
    RelativeTypeName,

    /// <summary>The declaring types only: "CreateModel.InputModel". Short, but types with the same name in different namespaces share keys.</summary>
    TypeName,

    /// <summary>The full namespace and the declaring types: "MyApp.Pages.Movies.CreateModel.InputModel". Changes when the root namespace changes.</summary>
    FullTypeName,
}
