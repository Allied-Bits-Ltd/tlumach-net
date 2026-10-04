// <copyright file="TlumachBlazorOptionsTests.cs" company="Allied Bits Ltd.">
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

using Tlumach.Blazor;

namespace Tlumach.BlazorTests;

public class TlumachBlazorOptionsTests
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void FindSupportedCulture_ExactMatch_ReturnsSupportedInstance()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(new CultureInfo("de-DE")));
    }

    [Fact]
    public void FindSupportedCulture_ParentCulture_IsUsed()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de");
        TlumachBlazorOptions options = new() { SupportedCultures = [En, german] };

        Assert.Same(german, options.FindSupportedCulture(CultureInfo.GetCultureInfo("de-AT")));
    }

    [Fact]
    public void FindSupportedCulture_SameLanguage_IsUsed()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(CultureInfo.GetCultureInfo("de-AT")));
    }

    [Fact]
    public void FindSupportedCulture_Unsupported_ReturnsNull()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Null(options.FindSupportedCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void FindSupportedCulture_NoSupportedCultures_ReturnsRequestedCulture()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");

        Assert.Same(french, new TlumachBlazorOptions().FindSupportedCulture(french));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("DE-de")]
    public void FindSupportedCultureByName_ExactMatch_ReturnsSupportedInstance(string name)
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_ParentPrefix_IsUsed()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de");
        TlumachBlazorOptions options = new() { SupportedCultures = [En, german] };

        Assert.Same(german, options.FindSupportedCulture("de-AT"));
    }

    [Fact]
    public void FindSupportedCultureByName_ParentPrefix_StripsOneSubtagAtATime()
    {
        CultureInfo hant = CultureInfo.GetCultureInfo("zh-Hant");
        TlumachBlazorOptions options = new() { SupportedCultures = [CultureInfo.GetCultureInfo("zh"), hant] };

        Assert.Same(hant, options.FindSupportedCulture("zh-Hant-TW"));
    }

    [Fact]
    public void FindSupportedCultureByName_SameLanguage_IsUsed()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(De, options.FindSupportedCulture("de-AT"));
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("qq-ZZ")]
    [InlineData("not a culture")]
    [InlineData("")]
    public void FindSupportedCultureByName_Unsupported_ReturnsNull(string name)
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Null(options.FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_NoSupportedCultures_CreatesRequestedCulture()
    {
        Assert.Equal("fr-FR", new TlumachBlazorOptions().FindSupportedCulture("fr-FR")?.Name);
    }

    [Theory]
    [InlineData("not a culture")]
    [InlineData("qq-ZZ")]
    public void FindSupportedCultureByName_NoSupportedCultures_NotPredefinedName_ReturnsNull(string name)
    {
        Assert.Null(new TlumachBlazorOptions().FindSupportedCulture(name));
    }

    [Fact]
    public void FindSupportedCultureByName_NoSupportedCultures_PredefinedNameInAnyCase_IsAccepted()
    {
        Assert.Equal("fr-FR", new TlumachBlazorOptions().FindSupportedCulture("FR-fr")?.Name);
    }

    [Fact]
    public void FindSupportedCultureByName_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TlumachBlazorOptions().FindSupportedCulture((string)null!));
    }

    [Fact]
    public void ResolveInitialCulture_Unsupported_UsesDefaultCulture()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De], DefaultCulture = De };

        Assert.Same(De, options.ResolveInitialCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void ResolveInitialCulture_NoDefault_UsesFirstSupported()
    {
        TlumachBlazorOptions options = new() { SupportedCultures = [En, De] };

        Assert.Same(En, options.ResolveInitialCulture(CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void Defaults_EndpointAndStorageKey()
    {
        TlumachBlazorOptions options = new();

        Assert.Equal("/tlumach/culture", options.CultureEndpoint);
        Assert.Equal("tlumach.culture", options.LocalStorageKey);
        Assert.Equal(TlumachCulturePersistence.Cookie, options.EffectivePersistence);
        Assert.False(options.EffectiveApplyCultureGlobally);
    }
}
