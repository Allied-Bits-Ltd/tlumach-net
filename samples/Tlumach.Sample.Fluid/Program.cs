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

using Fluid;
using Fluid.Values;

using Tlumach.Fluid;
using Tlumach.Sample.Emails;

Console.OutputEncoding = Encoding.UTF8;

// The encoder of the HTML body. It keeps non-Latin letters as they are; the values of `t_html` are encoded with the same encoder.
HtmlEncoder htmlEncoder = HtmlEncoder.Create(UnicodeRanges.All);

var options = new TemplateOptions();
options.MemberAccessStrategy.Register<Customer>();
options.MemberAccessStrategy.Register<Order>();
options.AddTlumach(Strings.TranslationManager, new TlumachFluidOptions { HtmlEncoder = htmlEncoder });

// `t` returns a translation by its key, or for a translation unit; `t_html` returns a translation that contains trusted HTML, with its values HTML-encoded.
IFluidTemplate subjectTemplate = Parse("""{{ "Email.Subject" | t: orderId: order.Id }}""");
IFluidTemplate bodyTemplate = Parse("""
    <p>{{ "Email.Greeting" | t: name: customer.Name }}</p>
    <p>{{ "Email.Body" | t: count: order.Count }}</p>
    <p>{{ "Email.Track" | t_html: url: order.TrackingUrl, carrier: order.Carrier }}</p>
    <p>{{ signature | t }}</p>
    """);

#pragma warning disable S1075 // The tracking URL is sample data, not a path or an address that the program depends on.
var order = new Order("42", 3, new Uri("https://track.example.com/?id=42&lang=en"), "Example Post");
#pragma warning restore S1075

Customer[] customers =
[
    new("Zoë <Admin>", "en"),
    new("Jürgen", "de"),
    new("Олена", "uk"),
];

// The emails are rendered concurrently, each in the language of its customer. Nothing global is changed: the culture is set on the context of each render.
string[] emails = new string[customers.Length];
Parallel.For(0, customers.Length, i => emails[i] = RenderEmail(customers[i]));

foreach (string email in emails)
    Console.WriteLine(email);

string RenderEmail(Customer customer)
{
    CultureInfo culture = CultureInfo.GetCultureInfo(customer.Culture);

    // The subject is plain text and is not encoded; the body is HTML, so the output of `t` is encoded by the encoder of the render.
    string subject = Render(subjectTemplate, customer, culture, NullEncoder.Default);
    string body = Render(bodyTemplate, customer, culture, htmlEncoder);
    return $"--- {culture.EnglishName} ---{Environment.NewLine}Subject: {subject}{Environment.NewLine}{body}{Environment.NewLine}";
}

string Render(IFluidTemplate template, Customer customer, CultureInfo culture, TextEncoder encoder)
{
    var context = new TemplateContext(options);
    context.SetValue("customer", customer);
    context.SetValue("order", order);

    // The unit is wrapped in an ObjectValue: TranslationUnit converts implicitly to string, so SetValue(name, unit) would bind to the string overload and pass the text in the current culture instead of the unit.
    context.SetValue("signature", new ObjectValue(Strings.Email.Signature));

    // Tlumach picks the translation for this culture, and Fluid formats numbers and dates with it.
    context.CultureInfo = culture;
    return template.Render(context, encoder);
}

static IFluidTemplate Parse(string text)
{
    var parser = new FluidParser();
    return parser.TryParse(text, out var template, out var error) ? template : throw new InvalidOperationException(error);
}
