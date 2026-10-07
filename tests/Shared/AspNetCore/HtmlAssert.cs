// <copyright file="HtmlAssert.cs" company="Allied Bits Ltd.">
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

using System.Text.RegularExpressions;

namespace Tlumach.AspNetCore.Testing;

/// <summary>
/// Extracts parts of rendered pages. The test views mark every checked element with a unique id.
/// </summary>
internal static class HtmlAssert
{
    public static string InnerHtml(string html, string id)
    {
        Match match = Regex.Match(html, "<(?<tag>[a-zA-Z0-9]+)[^>]*\\bid=\"" + Regex.Escape(id) + "\"[^>]*>(?<inner>.*?)</\\k<tag>>", RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Assert.True(match.Success, $"No element with id '{id}' in:{Environment.NewLine}{html}");
        return match.Groups["inner"].Value.Trim();
    }
}
