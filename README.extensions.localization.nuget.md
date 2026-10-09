AlliedBits.Tlumach.Extensions.Localization makes the translations of [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) available through [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization.Abstractions).

* `AddTlumachLocalization` registers `IStringLocalizer`, `IStringLocalizer<T>`, and `IStringLocalizerFactory` implementations that take the texts from Tlumach translation managers.
* Works in any application that uses dependency injection: console applications, services, desktop and mobile applications, and ASP.NET Core.
* It is used by the `AlliedBits.Tlumach.Blazor` and `AlliedBits.Tlumach.AspNetCore` packages, which bring it in as a dependency.

Requires .NET 9 or later. See the "Using Tlumach via Dependency Injection" topic in the [documentation](https://alliedbits.com/tlumach).
