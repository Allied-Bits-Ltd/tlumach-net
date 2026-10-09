AlliedBits.Tlumach.Blazor integrates [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach) with Blazor (Server, WebAssembly, static SSR, and Hybrid).

* The `TlumachText` component and the `TlumachComponentBase` helpers show translations in the language of each user and update them when the language is switched.
* `TlumachCultureState` keeps the language of each user (every circuit of Blazor Server has its own), and the `TlumachCultureSelector` component switches it live.
* To store the chosen culture in a cookie on the server, add the `AlliedBits.Tlumach.AspNetCore` package. The texts built into MudBlazor components are localized by the `AlliedBits.Tlumach.MudBlazor` package.
* The package ships no JavaScript or CSS files.

Requires .NET 9 or later. See the Getting Started section of the [documentation](https://alliedbits.com/tlumach).
