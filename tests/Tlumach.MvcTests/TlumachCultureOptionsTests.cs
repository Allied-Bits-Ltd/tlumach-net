// <copyright file="TlumachCultureOptionsTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.Web;

namespace Tlumach.MvcTests;

public class TlumachCultureOptionsTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void FindSupportedCulture_ExactMatch_ReturnsSupportedInstance()
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(new CultureInfo("de-DE")));
    }

    [Fact]
    public void FindSupportedCulture_ParentCulture_IsUsed()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de");
        TlumachCultureOptions options = new() { SupportedCultures = [En, german] };

        Assert.Same(german, options.FindSupportedCulture(CultureInfo.GetCultureInfo("de-AT")));
    }

    [Fact]
    public void FindSupportedCulture_SameLanguage_IsUsed()
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(CultureInfo.GetCultureInfo("de-AT")));
    }

    [Fact]
    public void FindSupportedCulture_Unsupported_ReturnsNull()
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Null(options.FindSupportedCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void FindSupportedCulture_NoSupportedCultures_ReturnsRequestedCulture()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Same(french, new TlumachCultureOptions().FindSupportedCulture(french));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("DE-de")]
    public void FindSupportedCultureByName_ExactMatch_ReturnsSupportedInstance(string name)
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_ParentPrefix_IsUsed()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de");
        TlumachCultureOptions options = new() { SupportedCultures = [En, german] };

        Assert.Same(german, options.FindSupportedCulture("de-AT"));
    }

    [Fact]
    public void FindSupportedCultureByName_ParentPrefix_StripsOneSubtagAtATime()
    {
        CultureInfo hant = CultureInfo.GetCultureInfo("zh-Hant");
        TlumachCultureOptions options = new() { SupportedCultures = [CultureInfo.GetCultureInfo("zh"), hant] };

        Assert.Same(hant, options.FindSupportedCulture("zh-Hant-TW"));
    }

    [Fact]
    public void FindSupportedCultureByName_SameLanguage_IsUsed()
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture("de-AT"));
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("qq-ZZ")]
    [InlineData("not a culture")]
    [InlineData("")]
    public void FindSupportedCultureByName_Unsupported_ReturnsNull(string name)
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Null(options.FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_NoSupportedCultures_CreatesRequestedCulture()
    {
        Assert.Equal("fr-FR", new TlumachCultureOptions().FindSupportedCulture("fr-FR")?.Name);
    }

    [Theory]
    [InlineData("not a culture")]
    [InlineData("qq-ZZ")]
    public void FindSupportedCultureByName_NoSupportedCultures_NotPredefinedName_ReturnsNull(string name)
    {
        Assert.Null(new TlumachCultureOptions().FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_NoSupportedCultures_PredefinedNameInAnyCase_IsAccepted()
    {
        Assert.Equal("fr-FR", new TlumachCultureOptions().FindSupportedCulture("FR-fr")?.Name);
    }

    [Theory]
    [InlineData("zh-Hant-TW")]
    [InlineData("ZH-hant-tw")]
    [InlineData("zh-Hans-SG")]
    [InlineData("de-AT")]
    [InlineData("es-MX")]
    [InlineData("en-GB")]
    [InlineData("fr-FR")]
    public void FindSupportedCultureByName_PredefinedName_AgreesWithCultureOverload(string name)
    {
        TlumachCultureOptions options = new()
        {
            SupportedCultures =
            [
                CultureInfo.GetCultureInfo("zh-Hans"),
                CultureInfo.GetCultureInfo("zh-Hant"),
                CultureInfo.GetCultureInfo("de"),
                CultureInfo.GetCultureInfo("es-419"),
                En,
            ],
        };

        Assert.Same(options.FindSupportedCulture(CultureInfo.GetCultureInfo(name)), options.FindSupportedCulture(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FindSupportedCultureByName_EmptyName_ReturnsNull(string name)
    {
        Assert.Null(new TlumachCultureOptions { SupportedCultures = [En, De] }.FindSupportedCulture(name));
        Assert.Null(new TlumachCultureOptions().FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TlumachCultureOptions().FindSupportedCulture((string)null!));
    }

    [Fact]
    public void ResolveInitialCulture_Unsupported_UsesDefaultCulture()
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De], DefaultCulture = De };

        Assert.Same(De, options.ResolveInitialCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void ResolveInitialCulture_NoDefault_UsesFirstSupported()
    {
        TlumachCultureOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(En, options.ResolveInitialCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void Defaults_Endpoint()
    {
        TlumachCultureOptions options = new();

        Assert.Equal("/tlumach/culture", options.CultureEndpoint);
    }
}
