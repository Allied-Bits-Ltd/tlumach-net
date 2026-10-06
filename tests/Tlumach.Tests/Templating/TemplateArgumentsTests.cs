// <copyright file="TemplateArgumentsTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.Templating;

namespace Tlumach.Tests.Templating;

public class TemplateArgumentsTests
{
    [Fact]
    public void NamedValuesIgnoreCase()
    {
        var arguments = new TemplateArguments().AddNamed("Name", "Ann");

        Assert.Equal("Ann", arguments.Named["name"]);
    }

    [Fact]
    public void LaterNamedValueReplacesEarlierOne()
    {
        var arguments = new TemplateArguments().AddNamed("name", "Ann").AddNamed("NAME", "Bob");

        Assert.Equal("Bob", Assert.Single(arguments.Named).Value);
    }

    [Fact]
    public void KeepsPositionalOrder()
    {
        var arguments = new TemplateArguments().AddPositional("a").AddPositional(null).AddPositional(3);

        Assert.Equal(new object?[] { "a", null, 3 }, arguments.Positional);
    }

    [Fact]
    public void EmptyCannotBeChanged()
    {
        Assert.Throws<InvalidOperationException>(() => TemplateArguments.Empty.AddPositional(1));
        Assert.Throws<InvalidOperationException>(() => TemplateArguments.Empty.AddNamed("a", 1));
        Assert.Empty(TemplateArguments.Empty.Positional);
        Assert.Empty(TemplateArguments.Empty.Named);
    }
}
