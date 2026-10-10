# Getting Started

## Integration with Scriban

Tlumach provides translations to [Scriban](https://github.com/scriban/scriban) templates, e.g. for localized emails, reports, and documents. A template calls the function `t` with the key of a
translation and the values of its placeholders, and gets the text in the language of the render. Renders in different languages can run at the same time. This article sets the integration up step
by step, using the sample `samples/Tlumach.Sample.Scriban` as the example. The details are in [Template Engines](template-engines.md), which also covers Fluid and Handlebars.Net.

The integration is in the `Tlumach.Scriban` assembly (.NET 9 and .NET 10), namespace `Tlumach.Scriban`, which is shipped as the separate package `AlliedBits.Tlumach.Scriban`. The package requires
Scriban 7.2.2 or later within 7.x and brings `Scriban` and the core `AlliedBits.Tlumach` package with it.

### 1. Add Tlumach to your project

a) via NuGet

Add a package reference to "AlliedBits.Tlumach.Scriban" to your project:

```cmd
dotnet add package AlliedBits.Tlumach.Scriban
```

or, in the project file:

```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.Scriban" Version="2.*" />
</ItemGroup>
```

b) with Source Code

Add the _Tlumach.Base_, _Tlumach_, and _Tlumach.Scriban_ projects to your solution and reference them from your project, and reference _Tlumach.Generator_ as an analyzer, as shown in
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

### 3. Register the functions and render a template

`ImportTlumach` adds the functions `t` and `t_html` to a `ScriptObject`, which can be shared by all renders. The culture of a render is pushed to its `TemplateContext`:

```csharp
using System.Globalization;
using MyApp.Translations;
using Scriban;
using Scriban.Runtime;
using Tlumach.Scriban;

ScriptObject functions = new ScriptObject().ImportTlumach(Strings.TranslationManager);

Template template = Template.Parse("""
    {{ t "Email.Subject" orderId: order.id }}
    {{ t "Email.Greeting" name: customer.name }} {{ t "Email.Body" count: order.count }}
    """);

var model = new ScriptObject
{
    ["customer"] = new ScriptObject { ["name"] = "Jürgen" },
    ["order"] = new ScriptObject { ["id"] = "42", ["count"] = 3 },
};

var context = new TemplateContext();
context.PushGlobal(functions);
context.PushGlobal(model);
context.PushCulture(CultureInfo.GetCultureInfo("de"));

Console.WriteLine(template.Render(context));
```

The output is:

```
Ihre Bestellung 42 wurde versandt
Hallo, Jürgen! Wir haben 3 Artikel an Sie versandt.
```

Named values (`name: value`) fill the named placeholders of the translation, and the ICU `plural` of `Email.Body` is evaluated for the culture of the render. Without `PushCulture`, the translation
manager's <xref:Tlumach.TranslationManager.CurrentCulture> is used; a single call can also choose its culture with `culture: "uk"`.

### 4. HTML output

Scriban does not encode output, so `t` returns plain text. For an HTML template, set <xref:Tlumach.Scriban.TlumachScribanOptions.HtmlEncode> in the options of `ImportTlumach`, and use `t_html` for
translations that contain trusted HTML markup:

```csharp
ScriptObject htmlFunctions = new ScriptObject().ImportTlumach(Strings.TranslationManager, new TlumachScribanOptions { HtmlEncode = true });
```

See [HTML Encoding](template-engines.md#html-encoding) for the rules.

### Where next

* [Template Engines](template-engines.md): keys and translation units, placeholder values, the choice of the culture, HTML encoding, missing keys, and the options and call syntax of Scriban.
* The sample `samples/Tlumach.Sample.Scriban` renders an email concurrently in English, German, and Ukrainian, with a plain-text subject and an HTML body.
