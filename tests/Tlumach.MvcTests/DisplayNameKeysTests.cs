// <copyright file="DisplayNameKeysTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.AspNetCore.Mvc;
using Tlumach.MvcTests.Models;

namespace Tlumach.MvcTests;

public class DisplayNameKeysTests
{
    private const string Root = "Tlumach.MvcTests";

    [Theory]
    [InlineData(typeof(RegisterViewModel), TlumachDisplayNameKeyStyle.RelativeTypeName, "Models.RegisterViewModel")]
    [InlineData(typeof(RegisterViewModel), TlumachDisplayNameKeyStyle.TypeName, "RegisterViewModel")]
    [InlineData(typeof(RegisterViewModel), TlumachDisplayNameKeyStyle.FullTypeName, "Tlumach.MvcTests.Models.RegisterViewModel")]
    [InlineData(typeof(Pages.Movies.CreateModel.InputModel), TlumachDisplayNameKeyStyle.RelativeTypeName, "Pages.Movies.CreateModel.InputModel")]
    [InlineData(typeof(Pages.Actors.CreateModel.InputModel), TlumachDisplayNameKeyStyle.RelativeTypeName, "Pages.Actors.CreateModel.InputModel")]
    [InlineData(typeof(Pages.Movies.CreateModel.InputModel), TlumachDisplayNameKeyStyle.TypeName, "CreateModel.InputModel")]
    [InlineData(typeof(Paged<int>), TlumachDisplayNameKeyStyle.RelativeTypeName, "Models.Paged")]
    [InlineData(typeof(Paged<>), TlumachDisplayNameKeyStyle.TypeName, "Paged")]
    [InlineData(typeof(RootLevelModel), TlumachDisplayNameKeyStyle.RelativeTypeName, "RootLevelModel")]
    [InlineData(typeof(Version), TlumachDisplayNameKeyStyle.RelativeTypeName, "System.Version")]
    public void GetContainerKey_ProducesTheDocumentedKeys(Type type, TlumachDisplayNameKeyStyle style, string expected)
        => Assert.Equal(expected, TlumachDisplayNameKeys.GetContainerKey(type, style, Root));

    [Fact]
    public void RootNamespace_IsRemovedOnlyAtADotBoundary()
        => Assert.Equal("Tlumach.MvcTests.Models.RegisterViewModel", TlumachDisplayNameKeys.GetContainerKey(typeof(RegisterViewModel), TlumachDisplayNameKeyStyle.RelativeTypeName, "Tlumach.Mvc"));

    [Fact]
    public void NullRootNamespace_KeepsTheFullName()
        => Assert.Equal("Tlumach.MvcTests.Models.RegisterViewModel", TlumachDisplayNameKeys.GetContainerKey(typeof(RegisterViewModel), TlumachDisplayNameKeyStyle.RelativeTypeName, null));

    [Fact]
    public void DefaultRootNamespace_IsTheAssemblyName()
        => Assert.Equal("Tlumach.MvcTests", TlumachDisplayNameOptions.DefaultRootNamespace(typeof(RegisterViewModel).Assembly));
}
