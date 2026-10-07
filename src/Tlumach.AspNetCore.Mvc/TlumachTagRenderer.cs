// <copyright file="TlumachTagRenderer.cs" company="Allied Bits Ltd.">
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
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

using Tlumach.Templating;

namespace Tlumach.AspNetCore.Mvc;

/// <summary>
/// The rendering shared by the text tag helpers and the HTML helper: trusted translation HTML with encoded values.
/// </summary>
internal static class TlumachTagRenderer
{
    internal static TlumachHtmlLocalizerFactory GetFactory(IHtmlLocalizerFactory factory)
        => factory as TlumachHtmlLocalizerFactory
            ?? throw new InvalidOperationException("The Tlumach tag helpers require the Tlumach HTML localizer factory. Call AddTlumachViewLocalization() on the MVC builder.");

    internal static string Render(
        TlumachHtmlLocalizerFactory factory,
        ViewContext viewContext,
        string? key,
        BaseTranslationUnit? unit,
        IEnumerable<KeyValuePair<string, object?>>? named,
        IEnumerable<object?>? positional,
        string? culture)
    {
        if ((key is null) == (unit is null))
            throw new InvalidOperationException("Set either a key or a translation unit (tlumach-key or tlumach-unit, key or unit), but not both.");

        CultureInfo resolved = TemplateTranslator.ToCulture(culture) ?? CultureInfo.CurrentCulture;
        TemplateArguments arguments = HtmlTranslation.ToArguments(named, positional, factory.Encoder);
        if (unit is not null)
            return RenderUnit(unit, arguments, resolved, factory.Encoder);

        string? path = viewContext.ExecutingFilePath;
        if (string.IsNullOrEmpty(path))
            path = viewContext.View?.Path;

        return factory.GetViewLookup(path).RenderOrKey(key!, arguments, resolved);
    }

    internal static string RenderUnit(BaseTranslationUnit unit, TemplateArguments arguments, CultureInfo culture, HtmlEncoder encoder)
    {
        Func<string, string> encode = encoder.Encode;
        TemplateTranslator translator = new(unit.TranslationManager);
        return translator.TryTranslateMarkup(unit, arguments, culture, encode, out string html) ? html : encode(unit.Key);
    }
}
