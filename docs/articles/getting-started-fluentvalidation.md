# Getting Started

## Integration with FluentValidation

Tlumach localizes the messages of [FluentValidation](https://docs.fluentvalidation.net): the built-in messages of its validators (such as `'{PropertyName}' must not be empty.`), the messages of
error codes, the messages and the display names of individual rules, and the display names of properties. All of them come from an ordinary Tlumach translation, in the language of each validation.
This article sets the integration up step by step, using the sample `samples/Tlumach.Sample.FluentValidation` as the example. The details of every feature are in
[Localization of FluentValidation](fluent-validation.md).

The integration is in the `Tlumach.FluentValidation` assembly (.NET 9 and .NET 10), namespace `Tlumach.FluentValidation`, which is shipped as the separate package `AlliedBits.Tlumach.FluentValidation`.
The package requires FluentValidation 12 and brings `FluentValidation` and the core `AlliedBits.Tlumach` package with it.

### 1. Add Tlumach to your project

a) via NuGet

Add a package reference to "AlliedBits.Tlumach.FluentValidation" to your project:

```cmd
dotnet add package AlliedBits.Tlumach.FluentValidation
```

or, in the project file:

```xml
<ItemGroup>
    <PackageReference Include="AlliedBits.Tlumach.FluentValidation" Version="2.*" />
</ItemGroup>
```

b) with Source Code

Add the _Tlumach.Base_, _Tlumach_, and _Tlumach.FluentValidation_ projects to your solution and reference them from your project, and reference _Tlumach.Generator_ as an analyzer, as shown in
[Generator](generator.md).

### 2. Translations

Create a translation project with Generator as described in [Generator](generator.md), or keep the translations in the application project itself, as the sample does: the package passes
Generator on to the project that references it. The configuration file, here "Strings.cfg":

```ini
defaultFile=Strings.json
defaultLocale=en
generatedNamespace=MyApp.Translations
generatedClass=Strings
textProcessingMode=None

[translations]
de=Strings_de.json
```

* `textProcessingMode=None` keeps the placeholders of FluentValidation, such as `{PropertyName}`, away from Generator, which would otherwise treat them as its own (see
  [The Message Template](fluent-validation.md#the-message-template)).
* Set `defaultLocale` to the language of the default file. Without it, the English messages built into FluentValidation win over the texts of an English default file (see
  [How the Message Is Found](fluent-validation.md#how-the-message-is-found)).

Add the files to the project:

```xml
<ItemGroup>
    <AdditionalFiles Include="Strings.cfg" />
    <EmbeddedResource Include="Strings.json" />
    <EmbeddedResource Include="Strings_de.json" />
</ItemGroup>
```

When the translation files are embedded resources, they are looked up as `<AssemblyName>.<path>`. A project whose `RootNamespace` differs from its assembly name gives the resources different names,
and the files are not found; leave `RootNamespace` unset or set it to the name of the assembly.

"Strings.json" contains the texts in three groups: `FluentValidation` for the built-in messages (a key is the name of a validator, such as `NotEmptyValidator`), `DisplayNames` for the names of the
properties, and the groups of the application for the messages and names of individual rules:

```json
{
    "FluentValidation": {
        "NotEmptyValidator": "Please enter '{PropertyName}'."
    },
    "DisplayNames": {
        "Customer": {
            "Name": "Name"
        }
    },
    "Messages": {
        "NicknameTooLong": "'{PropertyName}' can have at most {MaxLength} characters."
    },
    "Names": {
        "Nickname": "Nickname"
    }
}
```

"Strings_de.json" has the same keys with German texts, e.g. `"NotEmptyValidator": "Bitte geben Sie '{PropertyName}' ein."`. The texts keep the placeholders of FluentValidation, which
FluentValidation fills itself. A translation needs only the messages that the application wants to change; every other message comes from FluentValidation.

### 3. Install the language manager and the display-name resolver

An application without dependency injection installs both once, while it starts:

```csharp
using FluentValidation;
using MyApp.Translations;
using Tlumach.FluentValidation;

ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(Strings.TranslationManager);
ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(Strings.TranslationManager).Resolve;
```

An application with dependency injection, e.g. an ASP.NET Core application, calls `AddTlumachFluentValidation` instead, which does the same and registers the language manager as a service:

```csharp
using Tlumach.FluentValidation;

builder.Services.AddTlumachFluentValidation(options =>
{
    options.TranslationManager = Strings.TranslationManager;
    options.UseDisplayNameResolver = true;
});
```

The method changes the global `ValidatorOptions` at the moment it is called. Its options are described in [Dependency Injection](fluent-validation.md#dependency-injection).

### 4. A validator

A validator of a class `Customer` with the `string` properties `Name` and `Nickname`:

```csharp
using FluentValidation;
using MyApp.Translations;
using Tlumach.FluentValidation;

public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        // The message comes from FluentValidation.NotEmptyValidator, the name from DisplayNames.Customer.Name.
        RuleFor(c => c.Name).NotEmpty();

        // The message and the name come from translation units created by Generator.
        RuleFor(c => c.Nickname).MaximumLength(10)
            .WithMessage(Strings.Messages.NicknameTooLong)
            .WithName(Strings.Names.Nickname);
    }
}
```

The first rule needs no code of its own: the language manager provides the message, and the display-name resolver provides the name. The second rule takes its message and name from translation
units. `WithMessage` and `WithName` read the text when the rule is validated, so a validator that lives for the whole process follows the language of each validation.

Import the namespace `Tlumach.FluentValidation` in every file that calls `WithMessage` or `WithName` with a translation unit. Without it, the call still compiles, but binds to the `string` overload of
FluentValidation and keeps the text of the moment when the rule is created (see the warning in [Rule-Level Messages and Names](fluent-validation.md#rule-level-messages-and-names)).

### 5. Validate

The messages are in the language of `CultureInfo.CurrentUICulture`, as with FluentValidation itself. The request localization of ASP.NET Core sets it for each request; a console application sets it
directly:

```csharp
CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture;

var customer = new Customer { Name = string.Empty, Nickname = "AVeryLongNickname" };
foreach (var error in new CustomerValidator().Validate(customer).Errors)
    Console.WriteLine(error.ErrorMessage);
```

The output is:

```
Bitte geben Sie 'Name' ein.
'Spitzname' darf höchstens 10 Zeichen lang sein.
```

Set `CurrentCulture` as well: FluentValidation formats the values in the messages with it. A desktop application that switches the language through the translation manager can make the messages
follow <xref:Tlumach.TranslationManager.CurrentCulture> instead; see [Culture](fluent-validation.md#culture).

### Where next

* [Localization of FluentValidation](fluent-validation.md): the message template, the choice of the culture, the order in which a message is found, error codes, rule-level messages with
  placeholders of their own, display names, and dependency injection.
* [Localization of Data Annotations](data-annotations.md) for applications that also use validation attributes.
* The sample `samples/Tlumach.Sample.FluentValidation` validates one object in English, German, and Ukrainian.
