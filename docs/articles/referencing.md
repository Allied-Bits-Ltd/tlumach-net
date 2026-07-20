# Referencing Only What You Need

`AlliedBits.Tlumach` ships the core library together with **every** platform integration assembly (`Tlumach.WPF.dll`, `Tlumach.WinUI.dll`, `Tlumach.MAUI.dll`, `Tlumach.Avalonia.dll`, `Tlumach.UWP.dll`, `Tlumach.Extensions.Localization.dll`) inside a single NuGet package. Depending on the target framework your project uses, you may notice that assemblies you never reference — for example `Tlumach.MAUI.dll` in an Avalonia-only application — end up copied into your build or publish output anyway.

## Why this happens

Several platform integrations legitimately share the same target framework moniker (for example, WPF, WinUI, and MAUI-on-Windows all build for a `net9.0-windows...` framework). NuGet's package layout groups assemblies by target framework into a single `lib\<TFM>` folder, and when your project references the package via `PackageReference`, NuGet copies **every** assembly in the matched folder to your compile, runtime, and publish output — it has no way to know which of those assemblies your code actually uses. This is a limitation of how NuGet resolves package assets, not a bug in the referenced types themselves.

## The fix: trim your project's output

Add the following to your application project's `.csproj` (the project that produces your `.exe` or app package — class library projects don't copy dependency assemblies to their own output by default, so this only matters for executable/publishable projects). Adjust the list of filenames to match the assemblies your project doesn't use, using the table below.

```xml
<Target Name="RemoveUnusedTlumachAssemblies"
        AfterTargets="ResolveAssemblyReferences">
  <ItemGroup>
    <ReferenceCopyLocalPaths Remove="@(ReferenceCopyLocalPaths)"
      Condition="'%(ReferenceCopyLocalPaths.Filename)' == 'Tlumach.WinUI'
              Or '%(ReferenceCopyLocalPaths.Filename)' == 'Tlumach.MAUI'
              Or '%(ReferenceCopyLocalPaths.Filename)' == 'Tlumach.UWP'
              Or '%(ReferenceCopyLocalPaths.Filename)' == 'Tlumach.Extensions.Localization'" />
  </ItemGroup>
</Target>

<Target Name="RemoveUnusedTlumachAssembliesFromPublish"
        AfterTargets="ComputeResolvedFilesToPublishList">
  <ItemGroup>
    <ResolvedFileToPublish Remove="@(ResolvedFileToPublish)"
      Condition="'%(ResolvedFileToPublish.Filename)' == 'Tlumach.WinUI'
              Or '%(ResolvedFileToPublish.Filename)' == 'Tlumach.MAUI'
              Or '%(ResolvedFileToPublish.Filename)' == 'Tlumach.UWP'
              Or '%(ResolvedFileToPublish.Filename)' == 'Tlumach.Extensions.Localization'" />
  </ItemGroup>
</Target>
```

The first target removes the unwanted assemblies from your regular build output (`bin\<Configuration>\<TFM>\`); the second removes them from `dotnet publish` output (including self-contained and single-file publishes, which MAUI and Avalonia apps commonly use). Both are needed — one does not imply the other.

The example above is written for an Avalonia-only application: it keeps `Tlumach.Avalonia.dll` and the core assemblies (`Tlumach.dll`, `Tlumach.Base.dll`, always required) while dropping WinUI, MAUI, UWP, and the `Microsoft.Extensions.Localization` adapter.

## Which assemblies to keep, per application type

| Your app | Typical `TargetFramework` | Keep | Safe to exclude if unused |
|---|---|---|---|
| WPF | `net9.0-windows` / `net10.0-windows` (or a `-windows10.0.19041.0` variant) | `Tlumach.WPF` | `Tlumach.WinUI`, `Tlumach.MAUI`, `Tlumach.UWP`, `Tlumach.Avalonia`, `Tlumach.Extensions.Localization`* |
| WinUI | `net9.0-windows10.0.19041.0` / `net10.0-windows10.0.19041.0` | `Tlumach.WinUI` | `Tlumach.WPF`, `Tlumach.MAUI`, `Tlumach.Avalonia`, `Tlumach.Extensions.Localization`* |
| MAUI | `net9.0-android21.0`, `net9.0-ios15.0`, `net9.0-maccatalyst15.0`, `net9.0-windows10.0.19041.0` (+ `net10.0` equivalents) | `Tlumach.MAUI` | `Tlumach.WPF` / `Tlumach.WinUI` (Windows target only), `Tlumach.Avalonia`, `Tlumach.Extensions.Localization`* |
| UWP | `net9.0-windows10.0.26100.0` / `net10.0-windows10.0.26100.0` | `Tlumach.UWP` | `Tlumach.WPF`, `Tlumach.WinUI`, `Tlumach.MAUI`, `Tlumach.Avalonia`, `Tlumach.Extensions.Localization`* |
| Avalonia | `net9.0` / `net10.0` (or a Windows/mobile-specific TFM, if you target one) | `Tlumach.Avalonia` | `Tlumach.Extensions.Localization`* always; also `Tlumach.WPF` / `Tlumach.WinUI` / `Tlumach.MAUI` / `Tlumach.UWP` if you target a Windows or mobile TFM |
| Console / server / DI-only (no XAML framework) | `net9.0` / `net10.0` / `netstandard2.0` | core only | `Tlumach.Avalonia` (present even on plain `net9.0`/`net10.0`), `Tlumach.Extensions.Localization`* |

\* Keep `Tlumach.Extensions.Localization` if your app wires up `Microsoft.Extensions.Localization`'s `IStringLocalizer`/DI integration — see [Dependency Injection](di.md). Otherwise it can be excluded too.

## Verifying the fix

After adding the target and rebuilding or republishing, check that only the assemblies you expect are present:

```cmd
dir bin\Release\net9.0\Tlumach*.dll
dir bin\Release\net9.0\publish\Tlumach*.dll
```

You should see only `Tlumach.dll`, `Tlumach.Base.dll`, and the single platform assembly your project uses.

## A note on this workaround

This is a project-file workaround for a NuGet packaging limitation — the assemblies you exclude are still part of the `AlliedBits.Tlumach` package and are still resolved at compile time, they are simply no longer copied to your build or publish output. If you maintain multiple application projects that need the same exclusions, consider moving the targets above into a shared `Directory.Build.targets` file instead of repeating them per project.
