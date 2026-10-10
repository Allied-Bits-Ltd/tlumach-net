AlliedBits.Tlumach.Web contains the parts of the web integrations of [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) that are shared by the Blazor and the ASP.NET Core packages.

* `AddTlumachCultures` and `TlumachCultureOptions` define the cultures supported by a web application (`SupportedCultures`), the default culture (`DefaultCulture`), and the path of the culture endpoint (`CultureEndpoint`). The endpoint itself, which stores the chosen culture in the `.AspNetCore.Culture` cookie, is mapped by `AlliedBits.Tlumach.AspNetCore`.
* The package does not depend on ASP.NET Core and is normally installed as a dependency of `AlliedBits.Tlumach.Blazor` or `AlliedBits.Tlumach.AspNetCore`.

Requires .NET 9 or later. See the Getting Started section of the [documentation](https://alliedbits.com/tlumach).
