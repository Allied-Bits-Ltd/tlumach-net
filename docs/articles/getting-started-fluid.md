# Getting Started

## Integration with Fluid

Tlumach provides translations to [Fluid](https://github.com/sebastienros/fluid) (Liquid) templates, e.g. for localized emails, reports, and documents. A template passes the key of a translation
to the filter `t`, together with the values of its placeholders, and gets the text in the language of the render. Renders in different languages can run at the same time. This article sets the
integration up step by step, using the sample `samples/Tlumach.Sample.Fluid` as the example. The details are in [Template Engines](template-engines.md), which also covers Scriban and Handlebars.Net.

The integration is in the `Tlumach.Fluid` assembly (.NET 9 and .NET 10), namespace `Tlumach.Fluid`, which is shipped as the separate package `AlliedBits.Tlumach.Fluid`. The package requires
Fluid 2 (`Fluid.Core` 2.40.0 or later within 2.x; Fluid 3 is not supported yet) and brings `Fluid.Core` and the core `AlliedBits.Tlumach` package with it.

### 1. Add Tlumach to your project

a) via NuGet

Add a package reference to "AlliedBits.Tlumach.Fluid" to your project:

```cmd
dotnet add package AlliedBits.Tlumach.Fluid
```

or, in the project file:

```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.Fluid" Version="2.*" />
</ItemGroup>
```

b) with Source Code

Add the _Tlumach.Base_, _Tlumach_, and _Tlumach.Fluid_ projects to your solution and reference them from your project, and reference _Tlumach.Generator_ as an analyzer, as shown in
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

### 3. Register the filters and render a template

`AddTlumach` adds the filters `t` and `t_html` to the `TemplateOptions`. The culture of a render is set on its `TemplateContext`:

```csharp
using System.Globalization;
using Fluid;
using MyApp.Translations;
using Tlumach.Fluid;

var options = new TemplateOptions();
options.MemberAccessStrategy.Register<Customer>();
options.MemberAccessStrategy.Register<Order>();
options.AddTlumach(Strings.TranslationManager);

var parser = new FluidParser();
IFluidTemplate template = parser.Parse("""
    {{ "Email.Subject" | t: orderId: order.Id }}
    {{ "Email.Greeting" | t: name: customer.Name }} {{ "Email.Body" | t: count: order.Count }}
    """);

var context = new TemplateContext(options);
context.SetValue("customer", new Customer("Jürgen"));
context.SetValue("order", new Order("42", 3));
context.CultureInfo = CultureInfo.GetCultureInfo("de");

Console.WriteLine(template.Render(context));

public record Customer(string Name);

public record Order(string Id, int Count);
```

The output is:

```
Ihre Bestellung 42 wurde versandt
Hallo, Jürgen! Wir haben 3 Artikel an Sie versandt.
```

Fluid reads only the members of the types registered in `MemberAccessStrategy`; without the registration, `customer.Name` is empty. Named values (`name: value`) fill the named placeholders of the
translation, and the ICU `plural` of `Email.Body` is evaluated for the culture of the render. Without `CultureInfo`, the translation manager's <xref:Tlumach.TranslationManager.CurrentCulture> is
used; a single call can also choose its culture with `culture: "uk"`.

### 4. HTML output

`template.Render(context)` uses the `NullEncoder` of Fluid, which leaves the text as it is. For an HTML template, render with an HTML encoder, e.g. `template.Render(context, HtmlEncoder.Default)`,
and Fluid encodes the output of `t`. Use `t_html` for translations that contain trusted HTML markup, and set <xref:Tlumach.Fluid.TlumachFluidOptions.HtmlEncoder> to the encoder of the render. See
[HTML Encoding](template-engines.md#html-encoding) for the rules.

### Where next

* [Template Engines](template-engines.md): keys and translation units (in Fluid, a unit is passed as an `ObjectValue`), placeholder values, the choice of the culture, HTML encoding, missing keys,
  and the options and call syntax of Fluid.
* The sample `samples/Tlumach.Sample.Fluid` renders an email concurrently in English, German, and Ukrainian, with a plain-text subject and an HTML body.
