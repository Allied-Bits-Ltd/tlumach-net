AlliedBits.Tlumach.MudBlazor localizes the texts built into [MudBlazor](https://www.nuget.org/packages/MudBlazor) components through [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach).

* `AddTlumachMudBlazor` makes MudBlazor take its texts (data grid filters and pager, table pager, date picker, dialogs, validation messages, ...) from Tlumach. It can be called before or after `AddMudServices`.
* The texts follow the language of each user of Tlumach.Blazor (`TlumachCultureState`): in Blazor Server, every circuit has its own language, also after a live switch; Blazor WebAssembly and static rendering work, too.
* The keys of MudBlazor, such as `MudDataGrid_Contains`, are looked up in the `MudBlazor` group of the application's translations by default, or at the root of a dedicated translation set. The .resx files of the community project MudBlazor.Translations can be used as such a set with the ResX parser of Tlumach.
* A key that Tlumach does not have is shown in the English text built into MudBlazor.

Requires MudBlazor 9 and .NET 9 or later. See the "Localization of MudBlazor" topic in the [documentation](https://alliedbits.com/tlumach).
