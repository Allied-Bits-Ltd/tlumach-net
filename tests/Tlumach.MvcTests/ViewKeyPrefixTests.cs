// <copyright file="ViewKeyPrefixTests.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.MvcTests;

public class ViewKeyPrefixTests
{
    [Theory]
    [InlineData("/Views/Home/Index.cshtml", "Views.Home.Index.")]
    [InlineData("/Pages/Privacy.cshtml", "Pages.Privacy.")]
    [InlineData("/Areas/Admin/Views/Users/List.cshtml", "Areas.Admin.Views.Users.List.")]
    [InlineData("/Views/Shared/_Layout.cshtml", "Views.Shared._Layout.")]
    [InlineData("Views\\Home\\Index.cshtml", "Views.Home.Index.")]
    [InlineData("/Pages/Movies/Create.cshtml", "Pages.Movies.Create.")]
    public void DefaultViewKeyPrefix_MapsPathsToDottedPrefixes(string path, string expected)
        => Assert.Equal(expected, TlumachViewLocalizationOptions.DefaultViewKeyPrefix(path));

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/.cshtml")]
    public void DefaultViewKeyPrefix_EmptyPath_ReturnsNull(string path)
        => Assert.Null(TlumachViewLocalizationOptions.DefaultViewKeyPrefix(path));
}
