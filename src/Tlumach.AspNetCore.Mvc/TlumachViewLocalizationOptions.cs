// <copyright file="TlumachViewLocalizationOptions.cs" company="Allied Bits Ltd.">
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

using Microsoft.AspNetCore.Mvc.Razor;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// Options of the Tlumach view localization, set with <see cref="TlumachMvcBuilderExtensions.AddTlumachViewLocalization"/>.
/// <para>The options are read when a view is first localized; change them only while the application starts.</para>
/// </summary>
public sealed class TlumachViewLocalizationOptions
{
    /// <summary>
    /// Gets or sets the function that returns the key prefix of a view from its path (e.g. "/Views/Home/Index.cshtml" → "Views.Home.Index.").
    /// A key of the view is looked up with the prefix first and then without it. Returning <see langword="null"/> or an empty string disables the prefix.
    /// <para>The prefix without its trailing dot is also the context, for which the options of <c>AddTlumachLocalization</c> are requested, so a view can use its own translation file.</para>
    /// </summary>
    public Func<string, string?> ViewKeyPrefix { get; set; } = DefaultViewKeyPrefix;

    /// <summary>
    /// Gets or sets the format of the culture-specific view files that <see cref="LanguageViewLocationExpander"/> finds (e.g. "Index.de.cshtml"),
    /// or <see langword="null"/> to not add the expander. The default is <see cref="LanguageViewLocationExpanderFormat.Suffix"/>.
    /// </summary>
    public LanguageViewLocationExpanderFormat? ViewLocationExpanderFormat { get; set; } = LanguageViewLocationExpanderFormat.Suffix;

    /// <summary>
    /// Returns the default key prefix of a view: the path without the leading slash and the extension, with slashes replaced by dots, followed by a dot.
    /// </summary>
    /// <param name="viewPath">The application-relative path of the view, e.g. "/Pages/Privacy.cshtml".</param>
    /// <returns>The prefix, e.g. "Pages.Privacy.", or <see langword="null"/> for an empty path.</returns>
    public static string? DefaultViewKeyPrefix(string viewPath)
    {
        ArgumentNullException.ThrowIfNull(viewPath);

        int start = 0;
        while (start < viewPath.Length && (viewPath[start] == '/' || viewPath[start] == '\\'))
            start++;

        int end = viewPath.Length - Path.GetExtension(viewPath).Length;
        if (end <= start)
            return null;

        return viewPath[start..end].Replace('/', '.').Replace('\\', '.') + ".";
    }
}
