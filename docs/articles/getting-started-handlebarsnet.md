# Getting Started

## Integration with Handlebars.Net

Tlumach provides translations to [Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net) templates, e.g. for localized emails, reports, and documents. A template calls the helper `t`
with the key of a translation and the values of its placeholders, and gets the text in the language of the render. Renders in different languages can run at the same time. This article sets the
integration up step by step, using the sample `samples/Tlumach.Sample.HandlebarsNet` as the example. The details are in [Template Engines](template-engines.md), which also covers Scriban and Fluid.

The integration is in the `Tlumach.HandlebarsNet` assembly (.NET 9 and .NET 10), namespace `Tlumach.HandlebarsNet`, which is shipped as the separate package `AlliedBits.Tlumach.HandlebarsNet`. The
package requires Handlebars.Net 2.1.0 or later within 2.x and brings `Handlebars.Net` and the core `AlliedBits.Tlumach` package with it. Handlebars.Net compiles templates at run time and reads models
through reflection, so it is not suitable for NativeAOT applications.

### 1. Add Tlumach to your project

a) via NuGet

Add a package reference to "AlliedBits.Tlumach.HandlebarsNet" to your project:

```cmd
dotnet add package AlliedBits.Tlumach.HandlebarsNet
```

or, in the project file:

```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.HandlebarsNet" Version="2.*" />
</ItemGroup>
```

b) with Source Code

Add the _Tlumach.Base_, _Tlumach_, and _Tlumach.HandlebarsNet_ projects to your solution and reference them from your project, and reference _Tlumach.Generator_ as an analyzer, as shown in
[Generator](generator.md).

### 2. Translations

Create a translation project with Generator as described in [Generator](generator.md), or keep the translations in the application project itself, as the sample does: the package passes
Generator on to the project that references it. The configuration file, here "Strings.cfg":

```ini
defaultFile=Strings.json
defaultLocale=en
generatedNamespace=MyApp.Translations
generatedClass=Strings
textProcessingMode=Arb

[translations]
de=Strings_de.json
```

The `Arb` text processing mode evaluates ICU placeholders such as `plural`. Use `DotNet` instead if the texts use indexed placeholders (`{0}`); one configuration cannot use both (see
[Placeholder Values](template-engines.md#placeholder-values)).

Add the files to the project:

```xml
<ItemGroup>
    <AdditionalFiles Include="Strings.cfg" />
    <EmbeddedResource Include="Strings.json" />
    <EmbeddedResource Include="Strings_de.json" />
</ItemGroup>
```

When the translation files are embedded resources, they are looked up as `<AssemblyName>.<path>`; leave `RootNamespace` unset or set it to the name of the assembly.

"Strings.json":

```json
{
    "Email": {
        "Subject": "Your order {orderId} has shipped",
        "Greeting": "Hello, {name}!",
        "Body": "We have shipped {count, plural, one{# item} other{# items}} to you."
    }
}
```

"Strings_de.json":

```json
{
    "Email": {
        "Subject": "Ihre Bestellung {orderId} wurde versandt",
        "Greeting": "Hallo, {name}!",
        "Body": "Wir haben {count, plural, one{# Artikel} other{# Artikel}} an Sie versandt."
    }
}
```

### 3. Register the helpers and render a template

`RegisterTlumach` registers the helpers `t` and `t_html` in a Handlebars environment. The culture of a render is passed as its `@culture` data:

```csharp
using System.Globalization;
using HandlebarsDotNet;
using MyApp.Translations;
using Tlumach.HandlebarsNet;

IHandlebars handlebars = Handlebars.Create();
handlebars.RegisterTlumach(Strings.TranslationManager);

// Plain text, so the output of `t` is not escaped: {{{ }}}.
HandlebarsTemplate<object, object> template = handlebars.Compile("""
    {{{t "Email.Subject" orderId=order.Id}}}
    {{{t "Email.Greeting" name=customer.Name}}} {{{t "Email.Body" count=order.Count}}}
    """);

var model = new { customer = new { Name = "Jürgen" }, order = new { Id = "42", Count = 3 } };
var data = new { culture = CultureInfo.GetCultureInfo("de") };

Console.WriteLine(template(model, data));
```

The output is:

```
Ihre Bestellung 42 wurde versandt
Hallo, Jürgen! Wir haben 3 Artikel an Sie versandt.
```

Hash arguments (`name=value`) fill the named placeholders of the translation, and the ICU `plural` of `Email.Body` is evaluated for the culture of the render. Without the `culture` data, the
translation manager's <xref:Tlumach.TranslationManager.CurrentCulture> is used; a single call can also choose its culture with `culture="uk"`.

### 4. HTML output

In an HTML template, use `{{t ...}}`: Handlebars escapes the output of `t` like any other value. Use `t_html` for translations that contain trusted HTML markup; it encodes the values that it
inserts. See [HTML Encoding](template-engines.md#html-encoding) for the rules.

### Where next

* [Template Engines](template-engines.md): keys and translation units, placeholder values, the choice of the culture, HTML encoding, missing keys, and the options and call syntax of Handlebars.Net.
* The sample `samples/Tlumach.Sample.HandlebarsNet` renders an email concurrently in English, German, and Ukrainian, with a plain-text subject and an HTML body.
