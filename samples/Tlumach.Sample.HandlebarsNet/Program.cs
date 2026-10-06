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

using HandlebarsDotNet;

using Tlumach.HandlebarsNet;
using Tlumach.Sample.Emails;

Console.OutputEncoding = Encoding.UTF8;

IHandlebars handlebars = Handlebars.Create();
handlebars.RegisterTlumach(Strings.TranslationManager);

// `t` returns a translation by its key, or for a translation unit; Handlebars escapes it in {{ }} and not in {{{ }}}.
// `t_html` returns a translation that contains trusted HTML, with its values HTML-encoded.
// The subject is plain text, so it uses {{{ }}}.
HandlebarsTemplate<object, object> subjectTemplate = handlebars.Compile("""{{{t "Email.Subject" orderId=order.Id}}}""");
HandlebarsTemplate<object, object> bodyTemplate = handlebars.Compile("""
    <p>{{t "Email.Greeting" name=customer.Name}}</p>
    <p>{{t "Email.Body" count=order.Count}}</p>
    <p>{{t_html "Email.Track" url=order.TrackingUrl carrier=order.Carrier}}</p>
    <p>{{t signature}}</p>
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

// The emails are rendered concurrently, each in the language of its customer. Nothing global is changed: the culture is passed as @culture data of each render.
string[] emails = new string[customers.Length];
Parallel.For(0, customers.Length, i => emails[i] = RenderEmail(customers[i]));

foreach (string email in emails)
    Console.WriteLine(email);

string RenderEmail(Customer customer)
{
    CultureInfo culture = CultureInfo.GetCultureInfo(customer.Culture);

    var model = new
    {
        customer,
        order,
        signature = Strings.Email.Signature,
    };
    var data = new { culture };

    string subject = subjectTemplate(model, data);
    string body = bodyTemplate(model, data);
    return $"--- {culture.EnglishName} ---{Environment.NewLine}Subject: {subject}{Environment.NewLine}{body}{Environment.NewLine}";
}
