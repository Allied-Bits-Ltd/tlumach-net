# Packages

Since version 2.0, Tlumach is distributed as a core package and a set of integration packages. Reference the integration package of the framework that your application uses: it depends on the core package `AlliedBits.Tlumach` (of exactly the same version), so NuGet adds the core package, too. Only the assemblies of the packages that you reference end up in your build and publish output.

## The core package

`AlliedBits.Tlumach` contains everything that does not depend on an application framework:

| Assembly | Contents |
|---|---|
| `Tlumach.Base` | The parsers of translation files, <xref:Tlumach.Base.TranslationConfiguration>, and the ICU and placeholder engine. |
| `Tlumach` | <xref:Tlumach.TranslationManager>, <xref:Tlumach.TranslationUnit>, and the engine-neutral support of template engines. |
| `Tlumach.DataAnnotations` | The localized validation attributes (.NET 9 and .NET 10 only), see [Localization of Data Annotations](data-annotations.md). |
| `Tlumach.Generator` | [Generator](generator.md), included as a source generator (analyzer). |

The core package supports .NET 10, .NET 9, and .NET Standard 2.0 (and so .NET Framework 4.7.2 and later).

The project with translations, which is processed by Generator, references the core package. The integration packages listed below pass Generator on to the projects that reference them, so an application that keeps its translations in its own project needs only the integration package.

## The integration packages

| Package | Use it in | Assemblies | Depends on |
|---|---|---|---|
| `AlliedBits.Tlumach.WPF` | WPF applications (.NET 9 and .NET 10) | `Tlumach.WPF` | |
| `AlliedBits.Tlumach.WinForms` | Windows Forms applications (.NET Framework 4.7.2, .NET 9, and .NET 10) | `Tlumach.WinForms` | |
| `AlliedBits.Tlumach.WinUI` | WinUI 3 and Uno Platform applications | `Tlumach.WinUI` | |
| `AlliedBits.Tlumach.UWP` | UWP applications on .NET 9 and .NET 10 | `Tlumach.UWP` | |
| `AlliedBits.Tlumach.MAUI` | .NET MAUI applications | `Tlumach.MAUI` | `Microsoft.Maui.Controls` |
| `AlliedBits.Tlumach.Avalonia` | Avalonia 11 applications | `Tlumach.Avalonia` | `Avalonia` |
| `AlliedBits.Tlumach.Blazor` | Blazor applications and their WebAssembly clients | `Tlumach.Blazor` | `AlliedBits.Tlumach.Web`, `AlliedBits.Tlumach.Extensions.Localization`, `Microsoft.AspNetCore.Components.Web` |
| `AlliedBits.Tlumach.AspNetCore` | ASP.NET Core, MVC, and Razor Pages applications, and the server projects of Blazor applications | `Tlumach.AspNetCore`, `Tlumach.AspNetCore.Mvc` | `AlliedBits.Tlumach.Web`, `AlliedBits.Tlumach.Extensions.Localization`, the ASP.NET Core shared framework |
| `AlliedBits.Tlumach.Web` | Normally added as a dependency of the Blazor and ASP.NET Core packages | `Tlumach.Web` | |
| `AlliedBits.Tlumach.Extensions.Localization` | Applications that use `IStringLocalizer` and dependency injection, see [Dependency Injection](di.md) | `Tlumach.Extensions.Localization` | `Microsoft.Extensions.Localization.Abstractions` |
| `AlliedBits.Tlumach.MudBlazor` | Blazor applications with MudBlazor, see [Localization of MudBlazor](component-suites-mudblazor.md) | `Tlumach.MudBlazor` | `AlliedBits.Tlumach.Blazor`, `MudBlazor` |
| `AlliedBits.Tlumach.FluentValidation` | Applications with FluentValidation, see [Localization of FluentValidation](fluent-validation.md) | `Tlumach.FluentValidation` | `FluentValidation` |
| `AlliedBits.Tlumach.Scriban`, `AlliedBits.Tlumach.Fluid`, `AlliedBits.Tlumach.HandlebarsNet` | Template engines, see [Template Engines](template-engines.md) | `Tlumach.Scriban`, `Tlumach.Fluid`, `Tlumach.HandlebarsNet` | the template engine |
| `AlliedBits.Tlumach.Writers` | Export and conversion of translations, see [Writers](writers.md) | `Tlumach.Writers` | |

Every integration package depends on `AlliedBits.Tlumach` in addition to the packages listed. The packages of Tlumach depend on each other with an exact version, so all packages of Tlumach used by an application must have the same version. When you update one, update all of them.

The packages of WPF, Windows Forms, WinUI, and UWP do not depend on a framework package: WPF and Windows Forms are parts of the Windows desktop shared framework, and a WinUI or UWP application chooses its own version of Windows App SDK or Uno Platform.

An application can reference several integration packages, e.g. `AlliedBits.Tlumach.WPF` together with `AlliedBits.Tlumach.Blazor` in a Blazor Hybrid application, or `AlliedBits.Tlumach.Blazor` together with `AlliedBits.Tlumach.AspNetCore` in the server project of a Blazor Web App.

## Upgrading from version 1.x

Up to version 1.12, the `AlliedBits.Tlumach` package contained all integration assemblies. When you upgrade to version 2.0:

1. Keep the reference to `AlliedBits.Tlumach` in the projects with translations.
2. In the application projects, add the integration packages that correspond to the `Tlumach.*` namespaces that the projects use (e.g. `AlliedBits.Tlumach.WPF` for `Tlumach.WPF`, or `AlliedBits.Tlumach.AspNetCore` for `Tlumach.AspNetCore.Mvc`). A reference to `AlliedBits.Tlumach` in these projects can then be removed.
3. Remove the targets that excluded unused Tlumach assemblies from the build output, if you added them, as they are no longer needed.
