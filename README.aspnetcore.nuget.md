AlliedBits.Tlumach.AspNetCore integrates [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) with ASP.NET Core, MVC, and Razor Pages.

* `UseTlumachRequestLocalization` sets the culture of each request, and `MapTlumachCultureEndpoint` stores the culture chosen by a user in a cookie (also for Blazor applications).
* For MVC and Razor Pages: the `IHtmlLocalizer` and `IViewLocalizer` implementations that take the texts of views and pages from Tlumach, the `tlumach-key` and `<tlumach-text>` tag helpers, the `Html.Tlumach` helper, the `<tlumach-culture-selector>` tag helper, and the localization of model binding messages and display names of models.
* The MVC and Razor Pages part does not depend on Blazor.

Requires .NET 9 or later. See the Getting Started section of the [documentation](https://alliedbits.com/tlumach).
