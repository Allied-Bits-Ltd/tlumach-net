# Localization of Data Annotations

## Overview

The validation attributes of `System.ComponentModel.DataAnnotations` localize their messages through a pair of properties: `ErrorMessageResourceType` names a type and `ErrorMessageResourceName` names a
member of it. The mechanism requires a **public static property of the type `string` declared on the named type**, which is what a `.resx` file produces. A class created by [Generator](generator.md)
exposes <xref:Tlumach.TranslationUnit> members instead, so it cannot be used as a resource type as it stands.

Tlumach offers three ways to close that gap. They are complementary, and which one fits depends on where the validation runs.

| Route | Use it when | What it needs |
|---|---|---|
| The localized attributes of Tlumach | Validation runs anywhere: WPF, MAUI, a console application, a background service, or `Validator.TryValidateObject` called directly | The `AlliedBits.Tlumach` package. Nothing else |
| The string accessors of Generator | Something insists on a resource type, above all the sealed `DisplayAttribute` | The `createStringAccessors` option of Generator |
| `IStringLocalizer` of ASP.NET | The application is an ASP.NET Core application with request localization | `AddTlumachLocalization` and `AddDataAnnotationsLocalization` |

## The Localized Attributes

`Tlumach.DataAnnotations` provides counterparts of the validation attributes that take the message from a translation. Every one of them derives from the attribute of the framework that it replaces, so
validation itself is unchanged; only the message differs.

```
ValidationAttribute
 ├─ TlumachValidationAttribute      derive from this one for an attribute of your own
 ├─ RequiredAttribute
 │   └─ TlumachRequiredAttribute
 ├─ StringLengthAttribute
 │   └─ TlumachStringLengthAttribute
 ├─ RangeAttribute
 │   └─ TlumachRangeAttribute
 ├─ RegularExpressionAttribute
 │   └─ TlumachRegularExpressionAttribute
 ├─ CompareAttribute
 │   └─ TlumachCompareAttribute
 └─ DataTypeAttribute
     ├─ TlumachEmailAddressAttribute
     └─ TlumachPhoneAttribute
```

Each attribute carries three properties:

* **TranslationClass** - the class created by Generator that provides the message, or a nested group class inside one.
* **TranslationKey** - the name of the translation unit member in that class, or the key of a translation entry.
* **CultureSource** - a <xref:Tlumach.DataAnnotations.TranslationCultureSource> value that selects the culture. The default defers to
  <xref:Tlumach.DataAnnotations.TlumachValidationDefaults.CultureSource>.

The first two are also available as constructor arguments, which is the shorter form and the one to prefer:

```csharp
using Tlumach.DataAnnotations;

public class RegisterModel
{
    [TlumachRequired(typeof(Strings), Strings.emailRequiredKey)]
    [TlumachEmailAddress(typeof(Strings), Strings.emailInvalidKey)]
    public string? Email { get; set; }

    [TlumachStringLength(100, typeof(Strings), Strings.passwordLengthKey, MinimumLength = 6)]
    public string? Password { get; set; }

    [TlumachCompare(nameof(Password), typeof(Strings), Strings.passwordMismatchKey)]
    public string? ConfirmPassword { get; set; }
}
```

Pass the generated `...Key` constant rather than a string literal, so that a typo becomes a compilation error. An attribute that carries no translation behaves exactly like the attribute it derives from,
which makes the replacement safe to do one property at a time.

### The Message Template

**The placeholders of a validation message are positional, as in `{0}` and `{1}`, and not the named identifiers that the rest of Tlumach accepts.** A message that contains a named placeholder such as
`{userName}` raises a `FormatException` when the message is formatted, whatever the `textProcessingMode` of the translation file is.

The reason is that the text is used as a template of `string.Format`, exactly as the message of the attribute of the framework is: `ValidationAttribute.FormatErrorMessage` is defined in those terms, and
the placeholder engine of Tlumach is deliberately bypassed here. It also keeps one translation key usable through all three routes described above, because in the other two the framework itself does the
formatting and only understands positional placeholders.

The display name of the member is always `{0}`; the remaining arguments depend on the attribute and follow the order that the attribute of the framework uses:

| Attribute | `{1}` | `{2}` |
|---|---|---|
| `TlumachRequired`, `TlumachEmailAddress`, `TlumachPhone` | - | - |
| `TlumachStringLength` | the **maximum** length | the **minimum** length |
| `TlumachRange` | the minimum | the maximum |
| `TlumachRegularExpression` | the pattern | - |
| `TlumachCompare` | the display name of the other property | - |

Mind the order of the two arguments of `TlumachStringLength`: the maximum comes first. That is the order of `StringLengthAttribute` itself, and it is the opposite of the order of `TlumachRangeAttribute`.

Because the placeholders match those of the framework, an existing message moves into a translation file unchanged:

```ini
passwordLength=The {0} must be at least {2} and at max {1} characters long.
```

An attribute of your own that derives from `TlumachValidationAttribute` passes its own arguments to <xref:Tlumach.DataAnnotations.TlumachMessageResolver.Format(System.String,System.Object[])>, and they
become `{1}`, `{2}` and so on in the same way. To use the named placeholders of Tlumach in a message, read the text through a translation unit yourself, with
<xref:Tlumach.BaseTranslationUnit.GetValue(System.Globalization.CultureInfo,System.Collections.Generic.IDictionary{System.String,System.Object})> or a generated `Filled()` method, and pass the result as
the error message.

### Culture

By default the message is read for `CurrentCulture` of the <xref:Tlumach.TranslationManager> that provides it, which is the same culture that
<xref:Tlumach.TranslationUnit.CurrentValue> uses. That is what a desktop application wants, because it switches the language through the manager.

A web application sets the culture per request instead, and the manager knows nothing about that. Such an application switches the default once, while it starts:

```csharp
TlumachValidationDefaults.CultureSource = TranslationCultureSource.Ambient;
```

A single attribute can also be switched over with `CultureSource = TranslationCultureSource.Ambient`. Note that the culture only selects the text; the numbers and the dates of the arguments are always
formatted with `CultureInfo.CurrentCulture`, exactly as the attributes of the framework format them.

### How the Message Is Found

The key is resolved once per annotation and then cached for the lifetime of the process, so repeated validation performs no reflection. The steps are:

1. A public static field or property of the given class whose name matches the key and whose type derives from <xref:Tlumach.BaseTranslationUnit>. This covers both the field that Generator emits by
   default and the property it emits with `delayedUnitsCreation`, and it works for a nested group class as well, for example `typeof(Strings.account)`.
2. Otherwise the <xref:Tlumach.TranslationManager> of the class, or of a class that declares it, together with the key of the translation entry. This is the route for a class generated with
   `onlyDeclareKeys`, where there are no translation units to name.
3. Otherwise an `InvalidOperationException`.

A cached binding holds the route and never the text, so a change of the culture and a reload of a translation take effect at once.

Configuration that cannot work is reported rather than ignored, and, like the attributes of the framework, it is reported when the message is first formatted rather than while the attribute is
constructed. An `InvalidOperationException` is raised when only one of `TranslationClass` and `TranslationKey` is set, when a translation key is combined with `ErrorMessage` or
`ErrorMessageResourceName`, when the named member is not a translation unit, when neither a member nor a translation manager can be found, when the key is in no translation, and when the key has no text
for the culture in question.

### Trimming and NativeAOT

`TranslationClass` is annotated with `DynamicallyAccessedMembers`, so naming a class in an annotation keeps its members. One case cannot be annotated: reaching the translation manager of the class that
declares a nested group class. An application that is trimmed and that refers to such a class by a translation key rather than by the name of a translation unit should register the manager explicitly:

```csharp
TlumachValidationDefaults.RegisterTranslationManager(typeof(Strings.account), Strings.TranslationManager);
```

Registrations are consulted before any reflection.

## The String Accessors of Generator

`DisplayAttribute` is sealed, and both `Validator` and the model metadata of ASP.NET read that exact type, so a display name can only be localized through `ResourceType` and `Name`. The
`createStringAccessors` option of [Generator](generator.md) emits, next to the translation units, a nested class of `public static string` properties that satisfies the requirement:

```csharp
public static class Texts
{
    public static string emailLabel => global::Sample.Translations.Strings.emailLabel.CurrentTemplate;
}
```

An accessor returns the text **unprocessed**, through <xref:Tlumach.BaseTranslationUnit.CurrentTemplate> or
<xref:Tlumach.BaseTranslationUnit.GetValueAsTemplate(System.Globalization.CultureInfo)>, and never through `CurrentValue`. The attribute that reads it passes the text to `String.Format` together with its own
arguments, so the positional placeholders of a validation message have to survive. Reading the processed value would strip them whenever `textProcessingMode` is `DotNet` or `Arb`, and
`The {0} field is required.` would arrive as `The  field is required.`. A text without placeholders is the same either way, which is the usual case for a display name.

The name of a property is the value of the matching `...Key` constant, so the constant can be used for `Name`:

```csharp
[Display(ResourceType = typeof(Strings.Texts), Name = Strings.emailLabelKey)]
[Required(ErrorMessageResourceType = typeof(Strings.Texts), ErrorMessageResourceName = Strings.emailRequiredKey)]
public string? Email { get; set; }
```

One class is generated per generated class and per group class, so a key of a group is reached through the group: `typeof(Strings.account.Texts)`.

The framework caches the property it resolved but not the value it read, so a change of the culture is picked up on the next call. Which culture that is depends on `stringAccessorsCulture`: the default
reads the culture of the translation manager, which is one value for the whole process, while "ambient" reads the culture of the thread and therefore follows the culture of a request.

## Localization Through IStringLocalizer

ASP.NET Core can localize the annotations of the framework itself, with no Tlumach attribute and no generated accessor involved. `AddDataAnnotationsLocalization` makes the model metadata resolve
`DisplayAttribute.Name` through `IStringLocalizer` whenever `ResourceType` is not set, and makes the validation adapters resolve `ErrorMessage` the same way. Combined with
[Dependency Injection](di.md), the text of an annotation becomes a translation key:

```csharp
builder.Services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager);
builder.Services.AddRazorPages().AddDataAnnotationsLocalization();
```

```csharp
[Required(ErrorMessage = Strings.emailRequiredKey)]
[Display(Name = Strings.emailLabelKey)]
public string? Email { get; set; }
```

This is the route with true per-request culture, because <xref:Tlumach.Extensions.Localization.TlumachStringLocalizer> reads `CultureInfo.CurrentCulture` at the moment of the call. It works only inside
ASP.NET, however: `Validator.TryValidateObject` called on its own knows nothing of `IStringLocalizer`, and that is where the localized attributes of Tlumach are needed.
