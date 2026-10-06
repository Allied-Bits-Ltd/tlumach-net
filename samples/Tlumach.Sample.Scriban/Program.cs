// <copyright file="Program.cs" company="Allied Bits Ltd.">
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
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

using Scriban;
using Scriban.Runtime;

using Tlumach.Sample.Emails;
using Tlumach.Scriban;

Console.OutputEncoding = Encoding.UTF8;

// `t` returns a translation by its key, or for a translation unit; `t_html` returns a translation that contains trusted HTML, with its values HTML-encoded.
Template subjectTemplate = Template.Parse("""{{ t "Email.Subject" orderId: order.id }}""");
Template bodyTemplate = Template.Parse("""
    <p>{{ t "Email.Greeting" name: customer.name }}</p>
    <p>{{ t "Email.Body" count: order.count }}</p>
    <p>{{ t_html "Email.Track" url: order.tracking_url carrier: order.carrier }}</p>
    <p>{{ t signature }}</p>
    """);

// The subject is plain text. The body is HTML, so the output of `t` is encoded there; the encoder keeps non-Latin letters as they are.
ScriptObject subjectFunctions = new ScriptObject(StringComparer.Ordinal).ImportTlumach(Strings.TranslationManager);
ScriptObject bodyFunctions = new ScriptObject(StringComparer.Ordinal).ImportTlumach(
    Strings.TranslationManager,
    new TlumachScribanOptions { HtmlEncode = true, HtmlEncoder = HtmlEncoder.Create(UnicodeRanges.All) });

#pragma warning disable S1075 // The tracking URL is sample data, not a path or an address that the program depends on.
var order = new Order("42", 3, new Uri("https://track.example.com/?id=42&lang=en"), "Example Post");
#pragma warning restore S1075

Customer[] customers =
[
    new("Zoë <Admin>", "en"),
    new("Jürgen", "de"),
    new("Олена", "uk"),
];

// The emails are rendered concurrently, each in the language of its customer. Nothing global is changed: the culture is pushed to the context of each render.
string[] emails = new string[customers.Length];
Parallel.For(0, customers.Length, i => emails[i] = RenderEmail(customers[i]));

foreach (string email in emails)
    Console.WriteLine(email);

string RenderEmail(Customer customer)
{
    CultureInfo culture = CultureInfo.GetCultureInfo(customer.Culture);
    var model = new ScriptObject(StringComparer.Ordinal)
    {
        ["customer"] = new ScriptObject(StringComparer.Ordinal) { ["name"] = customer.Name },
        ["order"] = new ScriptObject(StringComparer.Ordinal) { ["id"] = order.Id, ["count"] = order.Count, ["tracking_url"] = order.TrackingUrl.AbsoluteUri, ["carrier"] = order.Carrier },
        ["signature"] = Strings.Email.Signature,
    };

    string subject = Render(subjectTemplate, subjectFunctions, model, culture);
    string body = Render(bodyTemplate, bodyFunctions, model, culture);
    return $"--- {culture.EnglishName} ---{Environment.NewLine}Subject: {subject}{Environment.NewLine}{body}{Environment.NewLine}";
}

static string Render(Template template, ScriptObject functions, ScriptObject model, CultureInfo culture)
{
    var context = new TemplateContext(StringComparer.Ordinal);
    context.PushGlobal(functions);
    context.PushGlobal(model);

    // Tlumach picks the translation for this culture, and Scriban formats numbers and dates with it.
    context.PushCulture(culture);
    return template.Render(context);
}
