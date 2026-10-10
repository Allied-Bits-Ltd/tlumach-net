# Localization of FluentValidation

## Overview

[FluentValidation](https://docs.fluentvalidation.net) produces four kinds of texts that an application may want to localize: the built-in messages of its validators, such as
`'{PropertyName}' must not be empty.`, the messages of error codes set with `WithErrorCode`, the messages set on individual rules with `WithMessage`, and the display names of properties, which become
`{PropertyName}` in a message. Tlumach can provide all four from an ordinary translation.

The integration is shipped as the separate package `AlliedBits.Tlumach.FluentValidation`, so that FluentValidation is not forced on applications that do not use it. The package requires
FluentValidation 12 and .NET 9 or later. To set the integration up, see [Getting Started for integration with FluentValidation](getting-started-fluentvalidation.md).

There are three routes. They are complementary and are usually combined.

| Route | What it localizes | How it is set up |
|---|---|---|
| <xref:Tlumach.FluentValidation.TlumachLanguageManager> | The built-in messages of FluentValidation and the messages of error codes | Assigned to `ValidatorOptions.Global.LanguageManager`, or installed with `AddTlumachFluentValidation` |
| `WithMessage` and `WithName` of <xref:Tlumach.FluentValidation.TlumachRuleBuilderExtensions> | The message and the display name of an individual rule | Called in the validator, with a translation unit or a key |
| <xref:Tlumach.FluentValidation.TlumachDisplayNameResolver> | The display names of properties, found by convention | Assigned to `ValidatorOptions.Global.DisplayNameResolver`, or installed with `AddTlumachFluentValidation` |

## The Language Manager

FluentValidation reads every built-in message through a language manager, and it reads the language manager only from the process-wide `ValidatorOptions.Global.LanguageManager`.
<xref:Tlumach.FluentValidation.TlumachLanguageManager> derives from the language manager of FluentValidation, takes the messages from a Tlumach translation, and falls back to the messages built into
FluentValidation. An application without dependency injection installs it once, while it starts, by assigning `new TlumachLanguageManager(Strings.TranslationManager)` to
`ValidatorOptions.Global.LanguageManager`. An application with dependency injection calls
<xref:Tlumach.FluentValidation.TlumachFluentValidationServiceCollectionExtensions.AddTlumachFluentValidation*> instead, which does the same and also registers the language manager as a service (see
[Dependency Injection](#dependency-injection) below). Both ways are shown in [Getting Started for integration with FluentValidation](getting-started-fluentvalidation.md).

The keys of FluentValidation are looked up in the group set by <xref:Tlumach.FluentValidation.TlumachLanguageManagerOptions.FluentValidationGroup>, which is `FluentValidation` by default. A key is the
name of a validator, such as `NotEmptyValidator` or `LengthValidator`, or an error code. With a `null` or empty group, the keys are looked up at the root of the translation. In a JSON translation file, the
group is an object at the root:

```json
{
    "FluentValidation": {
        "NotEmptyValidator": "Please enter '{PropertyName}'."
    }
}
```

Only the messages that the translation contains are taken from it; every other message comes from FluentValidation, in the language of the culture when FluentValidation has it. A translation therefore
needs no more than the messages that the application wants to change.

The translation is an ordinary Tlumach translation, typically with a class created by [Generator](generator.md), and the `FluentValidation` group becomes the nested class `Strings.FluentValidation`.

### The Message Template

**A message keeps the placeholders of FluentValidation**, such as `{PropertyName}`, `{PropertyValue}`, `{MinLength}`, `{ComparisonValue}`, and the formatted form `{Name:format}`. The text is passed to
FluentValidation, which fills the placeholders, and the placeholder engine of Tlumach is bypassed. As a consequence, the named placeholders of Tlumach are not resolved in such a text, and
<xref:Tlumach.BaseTranslationUnit.OnPlaceholderValueNeeded> does not fire.

The text is returned as the parser loaded it. Backslash escapes are processed in the `DotNet` and `BackslashEscaping` modes, while the apostrophe quoting of ICU is **not** applied: in an ARB file,
`'{PropertyName}' must not be blank.` keeps its apostrophes, exactly as the message of FluentValidation does.

For a translation file that holds only validation texts, `textProcessingMode=None` is recommended (see [Configuration Files](config-file.md)). In the `DotNet`, `Arb` and `ArbNoEscaping` modes, which
treat text in braces as placeholders, Generator treats the placeholders of FluentValidation as its own and creates methods for them that the validation never uses. The `None` and `BackslashEscaping`
modes do not treat braces as placeholders.

### Culture

The culture of a message is chosen in this order:

1. The culture passed to `GetString` explicitly, when the caller passes one.
2. The `Culture` property of the language manager, when it is set.
3. The culture selected by <xref:Tlumach.FluentValidation.TlumachLanguageManagerOptions.CultureSource>, a <xref:Tlumach.FluentValidation.MessageCultureSource> value.

The default culture source is `CurrentUICulture`, which is what the built-in language manager of FluentValidation uses. The value `TranslationManager` uses
<xref:Tlumach.TranslationManager.CurrentCulture> of the translation manager instead. That is meant for desktop applications that switch the language through the translation manager:

```csharp
ValidatorOptions.Global.LanguageManager = new TlumachLanguageManager(
    Strings.TranslationManager,
    new TlumachLanguageManagerOptions { CultureSource = MessageCultureSource.TranslationManager });
```

A web application keeps the default. The request localization of ASP.NET Core sets `CurrentUICulture` for each request, and <xref:Tlumach.Blazor.TlumachCultureState> sets it for a Blazor
application. On Blazor Server, the same limitation applies as to the messages of data annotations: the messages follow the culture of the circuit, which a live switch of the language changes only on the
next page load. See "Switching the language" in [Getting Started with Blazor](getting-started-blazor.md).

An application that also uses the attributes of [Data Annotations](data-annotations.md) should note that their `TranslationCultureSource` defaults to the culture of the translation manager (its `Ambient`
value uses `CultureInfo.CurrentCulture`), whereas `MessageCultureSource` defaults to `CurrentUICulture`; align the two so that both kinds of messages are in one language.

The culture only selects the text. FluentValidation formats a value in `{Name:format}` with `CultureInfo.CurrentCulture`, which may differ from the culture of the message. An application that sets
`CurrentUICulture` alone gets texts in one language and numbers in the format of another; set `CurrentCulture` as well.

### How the Message Is Found

A message is chosen in four steps; the first one that has a text wins:

1. **The Tlumach text for the culture.** This is a text found for the culture or for its basic culture, for example the text for `de-DE` when `de-AT` is requested, or a text supplied by a handler of
   <xref:Tlumach.TranslationManager.OnTranslationValueNeeded>. The text of the default file also counts here, but only when the default file is written in the language of the culture.
2. **The message built into FluentValidation for the culture.** The built-in message counts as the text of the culture when it is not empty and either the culture is English, or one of the cultures of
   English, or the message differs from the English message.
3. **The text of the default file of Tlumach.** This is the text that step 1 did not accept because the default file is written in another language. A text supplied by a handler of
   <xref:Tlumach.TranslationManager.OnTranslationValueNotFound> is used here as well.
4. **The English message of FluentValidation.** For a key that neither Tlumach nor FluentValidation knows, this is an empty string, as in FluentValidation.

Whether the default file is written in the language of a culture is decided by the neutral languages of the two cultures. With an English default file, its text is the text of `en`, `en-GB` and
`en-US`, and it wins over the English message of FluentValidation for all three; for `fr`, the French message of FluentValidation wins over it. The language of the default file is known only from the
`defaultLocale` setting of the configuration (see [Configuration Files](config-file.md)), exposed as <xref:Tlumach.Base.TranslationConfiguration.DefaultFileLocale>. **Set it.** Without it, the default
file never counts as the text of a culture, and the English messages of FluentValidation win over the texts of an English default file.

FluentValidation does not tell whether it has a translation of a message for a culture: it falls back to English silently. Step 2 therefore compares the message for the culture with the English message,
and a message equal to the English one counts as missing, unless the culture is English. The edge case is a translation of FluentValidation that is identical to the English text. For such a message, the
text of the default file of Tlumach, when it has one, wins over the built-in translation. The translations added to the language manager with `AddTranslation` take part in step 2 like the built-in ones.

When `Enabled` is set to `false`, the culture is ignored, as in FluentValidation. The language manager then returns the text of the default file of Tlumach, looked up for the locale of the default file
(or for the invariant culture when that locale is unknown), and otherwise the English message of FluentValidation. `Enabled` affects only the language manager: the rule-level messages and names and the
display names described below keep following the culture of the messages.

The text is read each time a message is needed, and the language manager keeps no copy of the Tlumach texts, so a change of the culture and a reload of a translation take effect at once. The language manager holds no
mutable state apart from `Culture` and `Enabled`, and it can be used from many threads at once.

Like the display-name resolver, the language manager looks up every key in the translation, so a key that the translation does not contain, for example a key of a built-in validator whose message
the application does not override, makes the translation manager fire <xref:Tlumach.TranslationManager.OnTranslationValueNotFound> on each lookup. A handler of that event should expect these lookups.

### Error Codes

A rule with an error code, set with `WithErrorCode("X")`, makes FluentValidation ask the language manager for the key `X` first. The language manager looks it up as `{FluentValidationGroup}.X`
(`FluentValidation.X` by default, `X` with a `null` or empty group), through the four steps above. When the result is empty, FluentValidation uses the message of the validator instead. An application can therefore translate its error codes without any code of its own:

```json
{
    "FluentValidation": {
        "CustomerNameTaken": "The name '{PropertyValue}' is already taken."
    }
}
```

```csharp
RuleFor(c => c.Name).Must(BeUnique).WithErrorCode("CustomerNameTaken");
```

In a culture without a text for `FluentValidation.CustomerNameTaken`, and with no default file that has one, the message is the one of `PredicateValidator`, the validator of `Must`.

### Trimming and NativeAOT

`Tlumach.FluentValidation` uses no reflection: translation units and translation managers are passed to it directly, and keys are looked up by name. The library is marked as compatible with trimming and
NativeAOT.

FluentValidation 12 itself is not marked as trimmable or AOT-compatible. A trimmed or NativeAOT application that uses it should be tested in its trimmed form.

## Rule-Level Messages and Names

<xref:Tlumach.FluentValidation.TlumachRuleBuilderExtensions> takes the message and the display name of a rule from a translation. Every method reads the text **when the rule is validated** rather than
when the validator is created, so a validator that lives for the whole process follows the culture of each validation.

| Method | What it sets |
|---|---|
| `WithMessage(unit)` | The message, from a translation unit |
| `WithMessage(unit, placeholders)` | The message, from a translation unit, with placeholders of its own filled by a callback |
| `WithMessage(manager, key)` | The message, from a translation manager by the key of the translation entry |
| `WithMessage(manager, key, placeholders)` | The message, from a translation manager by key, with placeholders of its own filled by a callback |
| `WithName(unit)` | The display name, which becomes `{PropertyName}`, from a translation unit |
| `WithName(manager, key)` | The display name, from a translation manager by key |

The forms with a key take the full key, including its groups, for example `Messages.NicknameTooLong`. They are the route for a class generated with `onlyDeclareKeys`, which has no translation units to pass.

The text is read for the same culture as the built-in messages: through the rules of the installed <xref:Tlumach.FluentValidation.TlumachLanguageManager>, or, when another language manager is installed,
from its `Culture` property and then `CurrentUICulture`. The messages of one validation are therefore in one language. The text is read as a template, like the texts of the language manager, and
FluentValidation then fills its placeholders.

The validator of the sample `samples/Tlumach.Sample.FluentValidation` combines the built-in messages with rule-level ones:

```csharp
using FluentValidation;
using Tlumach.FluentValidation;

internal sealed class CustomerValidator : AbstractValidator<Customer>
{
    public const decimal MaxDiscount = 1000m;

    public CustomerValidator()
    {
        // The message comes from FluentValidation.NotEmptyValidator in the translation, and the name from DisplayNames.Customer.Name.
        RuleFor(c => c.Name).NotEmpty();

        // No Tlumach text: the message built into FluentValidation is used, in the current language.
        RuleFor(c => c.Email).EmailAddress();

        // The message and the name of the rule come from translation units created by Generator.
        RuleFor(c => c.Nickname).MaximumLength(10).WithMessage(Strings.Messages.NicknameTooLong).WithName(Strings.Names.Nickname);

        // A placeholder of our own, {Limit:N0}, is filled here; FluentValidation fills {PropertyName}.
        RuleFor(c => c.Discount).LessThanOrEqualTo(MaxDiscount)
            .WithMessage(Strings.Messages.DiscountLimit, (customer, discount, formatter) => formatter.AppendArgument("Limit", MaxDiscount))
            .WithName(Strings.Names.Discount);
    }
}
```

The texts of the translation use the placeholders of FluentValidation together with placeholders of their own:

```json
{
    "Messages": {
        "NicknameTooLong": "'{PropertyName}' can have at most {MaxLength} characters; you entered {TotalLength}.",
        "DiscountLimit": "'{PropertyName}' must not exceed {Limit:N0}."
    },
    "Names": {
        "Nickname": "Nickname",
        "Discount": "Discount"
    }
}
```

The `placeholders` callback receives the validated object, the value of the property, and a `MessageFormatter`, and adds values with `AppendArgument`. A format specifier, as in `{Limit:N0}`, works as
in FluentValidation and is applied with `CultureInfo.CurrentCulture`, which may differ from the culture of the message. The values of the callback are inserted before FluentValidation fills its own
placeholders, so a value that itself contains a placeholder of FluentValidation, such as `{PropertyName}`, has that placeholder replaced as well.

> [!WARNING]
> Import the namespace `Tlumach.FluentValidation` in every file that calls these methods. A translation unit converts implicitly to `string`, so without `using Tlumach.FluentValidation;`, the call
> `WithMessage(Strings.X)` still compiles, but it binds to `WithMessage(string)` of FluentValidation. The text is then read once, while the rule is created, and it stays in the language of that moment.

`WithMessage(unit)` and `WithName(unit)` are generic in the class of the unit, which is constrained to <xref:Tlumach.BaseTranslationUnit>. Every unit class, including <xref:Tlumach.TranslationUnit> and
the unit classes of Avalonia, WinUI and UWP, which declare implicit conversions to `string` of their own, therefore binds to the Tlumach method without ambiguity, while a `string` argument still binds to
the method of FluentValidation.

A unit or a key that has no text for the culture raises an `InvalidOperationException` that names the key and the culture. Like the localized attributes of
[Data Annotations](data-annotations.md), it is raised during validation, when the message or the name is read, rather than when the rule is created.

## Display Names

<xref:Tlumach.FluentValidation.TlumachDisplayNameResolver> provides the display names of properties by convention, so that a validator needs no `WithName` call for them. Its
<xref:Tlumach.FluentValidation.TlumachDisplayNameResolver.Resolve(System.Type,System.Reflection.MemberInfo,System.Linq.Expressions.LambdaExpression)> method matches the delegate of
`ValidatorOptions.Global.DisplayNameResolver`:

```csharp
ValidatorOptions.Global.DisplayNameResolver = new TlumachDisplayNameResolver(Strings.TranslationManager).Resolve;
```

For the property `Name` of the class `Customer`, the resolver looks up two keys: the type-qualified key `{group}.Customer.Name` and the member key `{group}.Name`, where the group is the
`displayNamesGroup` argument of the constructor, `DisplayNames` by default. With a `null` or empty group, the group part is omitted. The class is the type of the object that FluentValidation validates,
that is, the root object passed to the validator, and it is identified by its name without the namespace, so a shared name such as `{group}.Name` serves every class that does not have a name of its own.

A name in the language of the message is preferred to a name in another language. The resolver returns the first of these:

1. The text of the type-qualified key, found for the culture or for its basic culture.
2. The text of the member key, found for the culture or for its basic culture.
3. The text of the type-qualified key from the default file, or a text supplied by a handler of <xref:Tlumach.TranslationManager.OnTranslationValueNotFound>.
4. The text of the member key from the default file, or a text supplied by such a handler.

For example, when the English default file has `DisplayNames.Customer.Name` and the German file has only `DisplayNames.Name`, a German message uses the German member name rather than the English
type-qualified one. When neither key has text, the resolver returns `null`, and FluentValidation uses its default name, which is the name of the property split into words.

```json
{
    "DisplayNames": {
        "Customer": {
            "Name": "Name",
            "Email": "E-mail address"
        }
    }
}
```

A rule that calls `WithName` is not affected by the resolver: the name of the rule wins, as in FluentValidation.

The resolver has no culture option of its own. It reads the names for the same culture as the rule-level messages, that is, through the rules of the installed
<xref:Tlumach.FluentValidation.TlumachLanguageManager>, or otherwise from `LanguageManager.Culture` and then `CurrentUICulture`. A name and its message are therefore always in one language. The resolver
looks up both keys on each call, and every key that it cannot find makes the translation manager fire <xref:Tlumach.TranslationManager.OnTranslationValueNotFound>, once per missing key on each
validation; a handler of that event should expect these lookups.

## Dependency Injection

<xref:Tlumach.FluentValidation.TlumachFluentValidationServiceCollectionExtensions.AddTlumachFluentValidation*> sets the integration up in an application that uses dependency injection. It creates a
<xref:Tlumach.FluentValidation.TlumachLanguageManager>, installs it as `ValidatorOptions.Global.LanguageManager`, and registers it as a singleton for both `TlumachLanguageManager` and `ILanguageManager`.
With `UseDisplayNameResolver`, it also creates a <xref:Tlumach.FluentValidation.TlumachDisplayNameResolver>, installs it as `ValidatorOptions.Global.DisplayNameResolver`, and registers it as a singleton.

The options are the properties of <xref:Tlumach.FluentValidation.TlumachFluentValidationOptions>:

| Option | Default | Meaning |
|---|---|---|
| `TranslationManager` | none, required | The translation manager that provides the texts. An `ArgumentException` is raised when the callback leaves it unset |
| `FluentValidationGroup` | `"FluentValidation"` | The group of the keys of FluentValidation. `null` or empty for root keys |
| `CultureSource` | `MessageCultureSource.CurrentUICulture` | The culture used when neither the caller nor `Culture` specifies one |
| `UseDisplayNameResolver` | `false` | Whether a `TlumachDisplayNameResolver` is installed |
| `DisplayNamesGroup` | `"DisplayNames"` | The group of the display names, used with `UseDisplayNameResolver` |

FluentValidation offers no way to supply a language manager through dependency injection, so **the method changes the global `ValidatorOptions` at the moment it is called**, not when the service
provider is built. The language manager is shared by every validator in the process.

The method can be combined with `AddTlumachLocalization` (see [Dependency Injection](di.md)) and with the registration of validators, in any order:

```csharp
using FluentValidation;
using Tlumach.Extensions.Localization;
using Tlumach.FluentValidation;

builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddTlumachFluentValidation(options =>
{
    options.TranslationManager = Strings.TranslationManager;
    options.UseDisplayNameResolver = true;
});
builder.Services.AddValidatorsFromAssemblyContaining<CustomerValidator>();
```

`AddValidatorsFromAssemblyContaining` is provided by the `FluentValidation.DependencyInjectionExtensions` package, which is not a dependency of `AlliedBits.Tlumach.FluentValidation`.
