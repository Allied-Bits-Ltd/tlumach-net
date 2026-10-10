AlliedBits.Tlumach.Syncfusion.Blazor localizes the texts built into [Syncfusion Blazor](https://www.nuget.org/packages/Syncfusion.Blazor.Core) components through [Tlumach.NET](https://www.nuget.org/packages/AlliedBits.Tlumach).

* `AddTlumachSyncfusionBlazor` registers a Tlumach-backed `ISyncfusionStringLocalizer`, so Syncfusion takes its texts (grid pager, filter menus, and search box, date picker, numeric text box, ...) from Tlumach. It can be called before or after `AddSyncfusionBlazor`.
* The texts follow the language of each user of Tlumach.Blazor (`TlumachCultureState`): in Blazor Server, every circuit has its own language; Blazor WebAssembly and static rendering work, too.
* The keys of Syncfusion, such as `Grid_EmptyRecord`, are looked up in the `Syncfusion` group of the application's translations by default. The official `SfResources.*.resx` files of Syncfusion can be added as a second source through the ResX parser of Tlumach.
* A key that Tlumach does not have is shown in the English text built into Syncfusion.

Requires Syncfusion.Blazor.Core 29.1.33 or later and .NET 9 or later. Syncfusion components need a Syncfusion license; this package does not. See the "Localization of Syncfusion Blazor" topic in the [documentation](https://alliedbits.com/tlumach).
