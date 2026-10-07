// <copyright file="KeyTestTypes.cs" company="Allied Bits Ltd.">
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

#pragma warning disable SA1403, CA1034, CA1515, MA0048 // Several small key-test types with several namespaces in one file (SA1403, MA0048); nesting is what the tests are about (CA1034); the types are public so that the MVC metadata provider and reflection see them as application types (CA1515).
namespace Tlumach.MvcTests
{
    public sealed class RootLevelModel
    {
        public string? Name { get; set; }
    }
}

namespace Tlumach.MvcTests.Pages.Movies
{
    public sealed class CreateModel
    {
        public sealed class InputModel
        {
            public string? Title { get; set; }
        }
    }
}

namespace Tlumach.MvcTests.Pages.Actors
{
    public sealed class CreateModel
    {
        public sealed class InputModel
        {
            public string? Title { get; set; }
        }
    }
}

namespace Tlumach.MvcTests.Models
{
    public sealed class Paged<T>
    {
        public T? Item { get; set; }
    }
}
#pragma warning restore SA1403, CA1034, CA1515, MA0048
