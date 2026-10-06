AlliedBits.Tlumach.FluentValidation localizes [FluentValidation](https://www.nuget.org/packages/FluentValidation) through [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach).

* `TlumachLanguageManager` takes the built-in messages of FluentValidation (`NotEmptyValidator`, `LengthValidator`, ...) and the messages of error codes from a group of a Tlumach translation, `FluentValidation` by default. For a culture without a Tlumach text, the message built into FluentValidation is used.
* `WithMessage(Strings.SomeUnit)` and `WithName(Strings.SomeUnit)` take the message and the display name of a rule from a translation unit created by Tlumach Generator. The text is read when the rule is validated, so it follows the current language.
* `TlumachDisplayNameResolver` provides the display names of properties from a translation group.
* `AddTlumachFluentValidation` sets everything up in an application that uses dependency injection.

The placeholders of FluentValidation, such as `{PropertyName}`, are kept as they are in the translations and filled by FluentValidation.

Requires FluentValidation 12 and .NET 9 or later. See the "Localization of FluentValidation" topic in the [documentation](https://alliedbits.com/tlumach).
