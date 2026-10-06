# Template Engines

## Overview

Server applications often build texts from templates: emails, reports, documents, notifications. Tlumach provides translations to three template engines for .NET through separate packages, so that
an engine is not forced on applications that do not use it:

| Package | Engine | Registration | Syntax |
|---|---|---|---|
| `AlliedBits.Tlumach.Scriban` | [Scriban](https://github.com/scriban/scriban) 7 | `scriptObject.ImportTlumach(manager)` | `{{ t "key" name: value }}` |
| `AlliedBits.Tlumach.Fluid` | [Fluid](https://github.com/sebastienros/fluid) 2 (Liquid) | `templateOptions.AddTlumach(manager)` | `{{ "key" \| t: name: value }}` |
| `AlliedBits.Tlumach.HandlebarsNet` | [Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net) 2 | `handlebars.RegisterTlumach(manager)` | `{{t "key" name=value}}` |

The packages require .NET 9 or later, and a version of the engine within the supported range: Scriban 7.2.2 or later within 7.x (earlier 7.x releases carry NuGet vulnerability advisories), Fluid.Core 2.40.0 or later
within 2.x, and Handlebars.Net 2.1.0 or later within 2.x. Each package adds two functions (filters in Fluid, helpers in Handlebars): `t`, which returns a translation as text, and `t_html`, which returns a translation that
contains trusted HTML. All three behave the same way, because they share <xref:Tlumach.Templating.TemplateTranslator> from the main Tlumach package. That class can also be used to integrate another template engine.

## Common Concepts

### Keys and Translation Units

The first argument of `t` is the key of a translation, with the names of [groups](strings.md) separated by dots, e.g. `"Email.Subject"`. Instead of a key, a template can use a translation unit
created by [Generator](generator.md) that the application passes in the model, e.g. `signature` for `Strings.Email.Signature`.

<xref:Tlumach.Templating.TemplateTranslationOptions.KeyPrefix> is prepended to every key, so a template for emails can use `"Subject"` for `"Email.Subject"`. To use several prefixes in one template, register
the functions twice with different names and prefixes. Keys are looked up in the default configuration of the translation manager or in the one set by
<xref:Tlumach.Templating.TemplateTranslationOptions.Configuration>; units use their own configuration.

### Placeholder Values

The arguments after the key fill the [placeholders](placeholders.md) of the translation:

* Named values (`name: value` in Scriban and Fluid, `name=value` in Handlebars) fill named placeholders, e.g. `{name}`. Names are case-insensitive.
* Positional values fill indexed placeholders, `{0}`, `{1}`, and so on. A named placeholder without a named value takes the positional value at its position. Indexed placeholders require the
  `DotNet` text processing mode of the configuration, because the `Arb` mode rejects placeholder names that start with a digit.
* ICU placeholders, `plural` and `select`, take their values in the same way, e.g. `{{ t "Email.Body" count: order.count }}` for `{count, plural, one{# item} other{# items}}`. ICU placeholders
  are evaluated only when the text processing mode of the configuration is `Arb`; note that in this mode an apostrophe quotes the following characters. Since `Arb` rejects `{0}`-style names and `DotNet`
  does not evaluate ICU, one configuration can use either indexed placeholders or ICU `plural` and `select`, not both.
* For a translation unit, a placeholder without a value takes the value cached in the unit or provided by its `OnPlaceholderValueNeeded` event.
* A value that is `null` (nil) is rendered as an empty string. A placeholder without a value is left as Tlumach leaves it.

The argument named `culture` is not a placeholder value; see the next section. Its name is set by <xref:Tlumach.Templating.TemplateTranslationOptions.CultureArgumentName>.

### Culture

The culture of a call is resolved in this order:

1. The `culture` argument of the call: a `CultureInfo` or a culture name.
2. The culture of the render, as the engine expresses it: `TemplateContext.PushCulture` in Scriban, `TemplateContext.CultureInfo` in Fluid, and the `@culture` data of the render in Handlebars.
   The invariant culture, which Scriban and Fluid use by default, means "not set".
3. <xref:Tlumach.TranslationManager.CurrentCulture> of the translation manager, which is `CultureInfo.CurrentCulture` when <xref:Tlumach.TranslationManager.UseContextCulture> is set.

The culture selects the translation and formats the numbers and dates in the placeholders. Set the culture of each render rather than a global culture: then a batch job can render emails for many
users concurrently, each in the language of its user, as the samples do.

### HTML Encoding

`t` returns text: the translation and the values are data. Whether the text is HTML-encoded depends on the engine and on how the template is rendered, as with any other value:

| Engine | `t` in HTML output | `t` in text output |
|---|---|---|
| Scriban | Set `HtmlEncode` in the options, or pipe to `html.escape` | Default |
| Fluid | Render with an HTML encoder: `template.Render(context, encoder)` | Default (`NullEncoder`) |
| Handlebars | `{{t ...}}` | `{{{t ...}}}`, or `NoEscape` in the configuration |

`t_html` is for translations that contain HTML markup, such as `Track your parcel at <a href="{url}">{carrier}</a>.` The translation is trusted and inserted as it is, while the values are encoded:
numbers (`sbyte` to `decimal`, `float`, and `double`) and dates and times (`DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, and `TimeOnly`) are formatted for the culture and inserted without encoding; strings and
all other values, including other `IFormattable` types such as `Uri`, are converted to text and HTML-encoded; and a value that is already HTML (a <xref:Tlumach.Templating.TemplateMarkup>, or in Fluid a
string passed through `raw`) is inserted as it is. Use `t_html` only for translations that come from a trusted source.

The adapters read translations directly and do not use <xref:Tlumach.TranslationManager.WebEncodeValues>, so a translation manager that encodes values for Razor pages can be shared with templates
without encoding anything twice.

`HtmlEncoder.Default` of .NET, which Scriban and Fluid use unless told otherwise, encodes all characters outside Basic Latin, e.g. `ü` as `&#xFC;`. The result is valid HTML, but to keep the letters as
they are, use `HtmlEncoder.Create(UnicodeRanges.All)` both for rendering and in the options of the package.

### Missing Keys

When no translation contains a key, <xref:Tlumach.Templating.TemplateTranslationOptions.MissingKey> decides what `t` returns:

* <xref:Tlumach.Templating.MissingKeyBehavior.ReturnKey> (the default) returns the key, with the prefix, which makes a misspelled key visible without stopping a batch;
* <xref:Tlumach.Templating.MissingKeyBehavior.Empty> returns an empty string;
* <xref:Tlumach.Templating.MissingKeyBehavior.Throw> throws <xref:Tlumach.Templating.TemplateKeyNotFoundException>, which the engine may wrap in its own exception.

<xref:Tlumach.Templating.TemplateTranslationOptions.OnMissingKey> is called with the key and the culture first; a text that it returns is used instead, and the function may also log the key.
In `t_html`, the returned key or the text returned by `OnMissingKey` is HTML-encoded: it is treated as text, not as markup.

## Scriban

Install the `AlliedBits.Tlumach.Scriban` package and add the functions to a `ScriptObject`, which can be shared by all renders:

```csharp
using Scriban;
using Scriban.Runtime;
using Tlumach.Scriban;

ScriptObject functions = new ScriptObject().ImportTlumach(Strings.TranslationManager);

var context = new TemplateContext();
context.PushGlobal(functions);
context.PushGlobal(model);
context.PushCulture(CultureInfo.GetCultureInfo("de"));
string text = Template.Parse("""{{ t "Email.Greeting" name: customer.name }}""").Render(context);
```

Calls look like `{{ t "Email.Subject" orderId: order.id }}`, `{{ t "Pair" first second }}`, `{{ t "Welcome" culture: "uk" }}`, `{{ "Welcome" | t }}`, and `{{ t signature }}` for a unit.
Scriban does not encode output, so `t` returns text unless <xref:Tlumach.Scriban.TlumachScribanOptions.HtmlEncode> is set; <xref:Tlumach.Scriban.TlumachScribanOptions.HtmlEncoder> sets the encoder for it and for the
values of `t_html`. The names of the functions are set by <xref:Tlumach.Scriban.TlumachScribanOptions.FunctionName> and <xref:Tlumach.Scriban.TlumachScribanOptions.MarkupFunctionName>; a named
value cannot be called `size`, which Scriban reserves.

The functions use no reflection, so the package is suitable for trimmed and NativeAOT applications (pass the model as `ScriptObject`s, as reflection-based `Import` of Scriban is not). See
`samples/Tlumach.Sample.Scriban`.

## Fluid

Install the `AlliedBits.Tlumach.Fluid` package and add the filters to the `TemplateOptions`:

```csharp
using Fluid;
using Tlumach.Fluid;

var options = new TemplateOptions();
options.AddTlumach(Strings.TranslationManager);

var context = new TemplateContext(options);
context.SetValue("customer", customer);
context.CultureInfo = CultureInfo.GetCultureInfo("de");
string html = template.Render(context, HtmlEncoder.Default);
```

A translation unit is passed to the template with `new ObjectValue(unit)` (namespace `Fluid.Values`) or a cast to `object`, e.g. `context.SetValue("signature", new ObjectValue(Strings.Email.Signature));`.
A translation unit converts implicitly to `string`, so `context.SetValue("signature", unit)` binds to the `SetValue(string, string)` overload and passes the text of the current culture instead of the unit.

Calls look like `{{ "Email.Subject" | t: orderId: order.Id }}`, `{{ "Pair" | t: first, second }}`, `{{ "Welcome" | t: culture: "uk" }}`, and `{{ signature | t }}` for a unit. Put named
arguments after positional ones: Fluid passes both in one list, and the filter tells them apart by their order.

`t` returns a string that Fluid encodes when the template is rendered with an HTML encoder and leaves as it is with the default `NullEncoder`. `t_html` encodes the values with
<xref:Tlumach.Fluid.TlumachFluidOptions.HtmlEncoder>, which should be the encoder that the template is rendered with, and returns HTML that Fluid does not encode again. The names of the filters are set
by <xref:Tlumach.Fluid.TlumachFluidOptions.FilterName> and <xref:Tlumach.Fluid.TlumachFluidOptions.MarkupFilterName>. Liquid numbers are decimals; the filter passes whole numbers as integers.

The package supports Fluid 2 (2.40.0 or later); Fluid 3 will be supported once it is released. See `samples/Tlumach.Sample.Fluid`.

## Handlebars.Net

Install the `AlliedBits.Tlumach.HandlebarsNet` package and register the helpers in a Handlebars environment:

```csharp
using HandlebarsDotNet;
using Tlumach.HandlebarsNet;

IHandlebars handlebars = Handlebars.Create();
handlebars.RegisterTlumach(Strings.TranslationManager);

var template = handlebars.Compile("""<p>{{t "Email.Greeting" name=customer.Name}}</p>""");
string html = template(model, new { culture = CultureInfo.GetCultureInfo("de") });
```

Calls look like `{{t "Email.Subject" orderId=order.Id}}`, `{{t "Pair" first second}}`, `{{t "Welcome" culture="uk"}}`, `{{t signature}}` for a unit, and `(t "Welcome")` as a
subexpression. The culture of a render is passed as data; the name of the data variable is set by <xref:Tlumach.HandlebarsNet.TlumachHandlebarsOptions.CultureDataName>.

`t` is escaped in `{{ }}` and written as it is in `{{{ }}}`, as any Handlebars value. `t_html` encodes the values with the encoder of the Handlebars configuration and writes the result without escaping
it; in `{{{ }}}` or with `NoEscape`, nothing is encoded. The default encoder of Handlebars.Net writes every non-ASCII character in `{{ }}` as a numeric entity (e.g. `J&#252;rgen`), which is valid HTML; if
readable output matters, set a custom `ITextEncoder` in `HandlebarsConfiguration.TextEncoder`. The names of the helpers are set by <xref:Tlumach.HandlebarsNet.TlumachHandlebarsOptions.HelperName> and
<xref:Tlumach.HandlebarsNet.TlumachHandlebarsOptions.MarkupHelperName>.

Handlebars.Net compiles templates at run time and reads models through reflection, so it is not suitable for NativeAOT applications. See `samples/Tlumach.Sample.HandlebarsNet`.

## Without an Integration Package

A template engine can also read translations without these packages: the application passes the generated class, its units, or the translation manager in the model, and the template reads e.g.
`strings.greeting` (each engine has to be allowed to access these members). This works for simple cases but has limits:

* The text of a unit is taken for <xref:Tlumach.TranslationManager.CurrentCulture>, which is shared by the whole application, so renders in different languages cannot run concurrently, and changing the culture
  affects every other user of the translation manager.
* Placeholders are not filled from the template; the application must fill them before rendering, e.g. by passing the results of `GetValue(culture, values)`.
* Escaping is up to the template: text from a translation manager with <xref:Tlumach.TranslationManager.WebEncodeValues> set must be written raw, and other text must be escaped.
* Missing keys are not reported.

The integration packages solve all four, at the cost of one package reference and one line of setup.
