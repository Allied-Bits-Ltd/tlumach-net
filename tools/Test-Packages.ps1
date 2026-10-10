#Requires -Version 7
<#
.SYNOPSIS
    Installs the NuGet packages of Tlumach.NET into new consumer projects and builds and runs them.

.DESCRIPTION
    For every case below, the script creates a small project in a work directory outside the repository (so that nothing of the repository, such as
    Directory.Build.props, applies to it), which references only the given packages: the packages of Tlumach from the package directory and any other package
    from nuget.org, with a fresh global packages folder. The project contains a translation set (strings.cfg and strings.json), so a project that compiles has
    got the class created by the generator from the packages it references. Applications are run, and they fail unless the generated class returns the text.

    The cases cover:
    - every package, referenced alone, so that the generator must reach the project through the package;
    - the generator with the .NET 8, .NET 9, and .NET 10 SDKs, with the compiler that runs on .NET Framework (a project in the old format built by the MSBuild
      of Visual Studio), and with C# 7.3 (the default language version of .NET Framework and .NET Standard 2.0); the warnings that a generator did not load
      (CS9057, CS8784, CS8785) are errors;
    - a .NET 9 application that references version 9 of Microsoft.Extensions.* (no package downgrade);
    - Avalonia 12 and the plain net10.0 target of the MAUI package;
    - the upgrade from version 1.12.0: an application that used an integration assembly from the single package fails to compile with the core package of
      this version alone and compiles with the integration package added.

    A case whose prerequisite is missing (Windows, an SDK version, the MSBuild of Visual Studio) is skipped, unless -RequireAll is given.

.PARAMETER PackageDirectory
    The directory with the .nupkg files, e.g. the output of build-tlumach-net.cmd.

.PARAMETER Version
    The version of the packages.

.PARAMETER WorkDirectory
    The directory in which the consumer projects are created. The default is a new directory in the temporary directory.

.PARAMETER Filter
    A wildcard pattern for the names of the cases to run.

.PARAMETER RequireAll
    Fail instead of skipping the cases whose prerequisites are missing.

.EXAMPLE
    pwsh -File tools\Test-Packages.ps1 -PackageDirectory ..\Redist\nuget\2.0.0 -Version 2.0.0
#>
param(
    [Parameter(Mandatory)] [string] $PackageDirectory,
    [Parameter(Mandatory)] [string] $Version,
    [string] $WorkDirectory,
    [string] $Filter = '*',
    [switch] $RequireAll
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PackageDirectory = (Resolve-Path -LiteralPath $PackageDirectory).Path
if (-not $WorkDirectory) { $WorkDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("TlumachPackageTests-" + [Guid]::NewGuid().ToString('N').Substring(0, 8)) }
New-Item -ItemType Directory -Force -Path $WorkDirectory | Out-Null
$WorkDirectory = (Resolve-Path -LiteralPath $WorkDirectory).Path
$PackagesFolder = Join-Path $WorkDirectory '.packages'

$IsWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

# The cases. Packages: 'Id' (the version under test) or 'Id@version'. Kind: Exe (built and run), Library (built), Restore (restored), Legacy (a .NET Framework
# project in the old format, built by the MSBuild of Visual Studio and run). Expect: an error code that the build must report instead of succeeding.
$Cases = @(
    @{ Name = 'Core.Net10'; Packages = @('AlliedBits.Tlumach'); Tfm = 'net10.0'; Type = 'Tlumach.TranslationManager' }
    @{ Name = 'Core.Net9'; Packages = @('AlliedBits.Tlumach'); Tfm = 'net9.0'; Type = 'Tlumach.TranslationManager' }
    @{ Name = 'Core.NetStandard20.CSharp73'; Packages = @('AlliedBits.Tlumach'); Tfm = 'netstandard2.0'; Kind = 'Library'; Type = 'Tlumach.TranslationManager' }
    @{ Name = 'Core.Sdk8'; Packages = @('AlliedBits.Tlumach'); Tfm = 'net8.0'; Type = 'Tlumach.TranslationManager'; SdkVersion = '8.0' }
    @{ Name = 'Core.Sdk9'; Packages = @('AlliedBits.Tlumach'); Tfm = 'net9.0'; Type = 'Tlumach.TranslationManager'; SdkVersion = '9.0' }
    @{ Name = 'Core.Net48.CSharp73'; Packages = @('AlliedBits.Tlumach'); Tfm = 'net48'; Type = 'Tlumach.TranslationManager'; WindowsOnly = $true }
    @{ Name = 'Core.Legacy.Net472'; Packages = @('AlliedBits.Tlumach.WinForms'); Kind = 'Legacy'; Type = 'Tlumach.WinForms.TranslationProvider'; WindowsOnly = $true }
    @{ Name = 'Extensions.Localization'; Packages = @('AlliedBits.Tlumach.Extensions.Localization'); Tfm = 'net10.0'; Type = 'Tlumach.Extensions.Localization.TlumachStringLocalizer' }
    @{ Name = 'Web'; Packages = @('AlliedBits.Tlumach.Web'); Tfm = 'net10.0'; Type = 'Tlumach.Web.TlumachCultureOptions' }
    @{ Name = 'Blazor'; Packages = @('AlliedBits.Tlumach.Blazor'); Tfm = 'net10.0'; Type = 'Tlumach.Blazor.TlumachCultureState'; Sdk = 'Microsoft.NET.Sdk.Razor' }
    @{ Name = 'AspNetCore'; Packages = @('AlliedBits.Tlumach.AspNetCore'); Tfm = 'net10.0'; Type = 'Tlumach.AspNetCore.Mvc.TlumachHtmlLocalizerFactory'; Sdk = 'Microsoft.NET.Sdk.Web' }
    @{ Name = 'AspNetCore.Net9.Extensions9'; Packages = @('AlliedBits.Tlumach.AspNetCore', 'Microsoft.Extensions.Localization.Abstractions@9.0.0', 'Microsoft.Extensions.DependencyInjection.Abstractions@9.0.0'); Tfm = 'net9.0'; Type = 'Tlumach.AspNetCore.Mvc.TlumachHtmlLocalizerFactory'; Sdk = 'Microsoft.NET.Sdk.Web' }
    @{ Name = 'MudBlazor'; Packages = @('AlliedBits.Tlumach.MudBlazor'); Tfm = 'net10.0'; Type = 'Tlumach.MudBlazor.TlumachMudLocalizer'; Sdk = 'Microsoft.NET.Sdk.Razor' }
    @{ Name = 'Syncfusion.Blazor'; Packages = @('AlliedBits.Tlumach.Syncfusion.Blazor'); Tfm = 'net10.0'; Type = 'Tlumach.Syncfusion.Blazor.TlumachSyncfusionLocalizer'; Sdk = 'Microsoft.NET.Sdk.Razor' }
    @{ Name = 'FluentValidation'; Packages = @('AlliedBits.Tlumach.FluentValidation'); Tfm = 'net10.0'; Type = 'Tlumach.FluentValidation.TlumachLanguageManager' }
    @{ Name = 'Scriban'; Packages = @('AlliedBits.Tlumach.Scriban'); Tfm = 'net10.0'; Type = 'Tlumach.Scriban.TlumachScriptObjectExtensions' }
    @{ Name = 'Fluid'; Packages = @('AlliedBits.Tlumach.Fluid'); Tfm = 'net10.0'; Type = 'Tlumach.Fluid.TlumachTemplateOptionsExtensions' }
    @{ Name = 'HandlebarsNet'; Packages = @('AlliedBits.Tlumach.HandlebarsNet'); Tfm = 'net10.0'; Type = 'Tlumach.HandlebarsNet.TlumachHandlebarsExtensions' }
    @{ Name = 'Writers'; Packages = @('AlliedBits.Tlumach.Writers'); Tfm = 'net10.0'; Type = 'Tlumach.Writers.JsonWriter' }
    @{ Name = 'Writers.NetStandard20'; Packages = @('AlliedBits.Tlumach.Writers'); Tfm = 'netstandard2.0'; Kind = 'Library'; Type = 'Tlumach.Writers.JsonWriter' }
    @{ Name = 'Avalonia11'; Packages = @('AlliedBits.Tlumach.Avalonia'); Tfm = 'net10.0'; Type = 'Tlumach.Avalonia.TranslationUnit'; Code = 'System.Console.WriteLine("Binding: " + new Tlumach.Avalonia.Translate(new Tlumach.Avalonia.TranslationUnit(Consumer.Tr.Strings.TranslationManager, Consumer.Tr.Strings.Configuration, "Hello", false)).ProvideValue(null).GetType().Name);' }
    @{ Name = 'Avalonia12'; Packages = @('AlliedBits.Tlumach.Avalonia', 'Avalonia@12.*'); Tfm = 'net10.0'; Type = 'Tlumach.Avalonia.TranslationUnit'; Code = 'System.Console.WriteLine("Binding: " + new Tlumach.Avalonia.Translate(new Tlumach.Avalonia.TranslationUnit(Consumer.Tr.Strings.TranslationManager, Consumer.Tr.Strings.Configuration, "Hello", false)).ProvideValue(null).GetType().Name);' }
    @{ Name = 'WinUI.Plain'; Packages = @('AlliedBits.Tlumach.WinUI'); Tfm = 'net10.0'; Type = 'Tlumach.WinUI.TranslationUnit' }
    @{ Name = 'MAUI.Plain'; Packages = @('AlliedBits.Tlumach.MAUI'); Tfm = 'net10.0'; Kind = 'Restore' }
    @{ Name = 'WPF'; Packages = @('AlliedBits.Tlumach.WPF'); Tfm = 'net10.0-windows'; Type = 'Tlumach.WPF.TranslateExtension'; Properties = '<UseWPF>true</UseWPF>'; WindowsOnly = $true }
    @{ Name = 'WinForms.Net472'; Packages = @('AlliedBits.Tlumach.WinForms'); Tfm = 'net472'; Type = 'Tlumach.WinForms.TranslationProvider'; Properties = '<UseWindowsForms>true</UseWindowsForms>'; WindowsOnly = $true }
    @{ Name = 'WinForms.Net10'; Packages = @('AlliedBits.Tlumach.WinForms'); Tfm = 'net10.0-windows'; Type = 'Tlumach.WinForms.TranslationProvider'; Properties = '<UseWindowsForms>true</UseWindowsForms>'; WindowsOnly = $true }
    @{ Name = 'WinUI.Windows'; Packages = @('AlliedBits.Tlumach.WinUI'); Tfm = 'net10.0-windows10.0.19041.0'; Type = 'Tlumach.WinUI.TranslationUnit'; WindowsOnly = $true }
    @{ Name = 'UWP'; Packages = @('AlliedBits.Tlumach.UWP'); Tfm = 'net10.0-windows10.0.19041.0'; Type = 'Tlumach.UWP.TranslationUnit'; WindowsOnly = $true }
    # The single package of 1.12.0 declared no dependencies, so its users referenced Microsoft.Extensions.Localization.Abstractions themselves
    @{ Name = 'Upgrade.From1_12'; Packages = @('AlliedBits.Tlumach@1.12.0', 'Microsoft.Extensions.Localization.Abstractions@10.0.0'); Tfm = 'net10.0'; Type = 'Tlumach.Extensions.Localization.TlumachStringLocalizer'; FromNuGetOrg = $true }
    @{ Name = 'Upgrade.CoreOnly'; Packages = @('AlliedBits.Tlumach'); Tfm = 'net10.0'; Type = 'Tlumach.Extensions.Localization.TlumachStringLocalizer'; Expect = 'CS0234' }
    @{ Name = 'Upgrade.WithIntegration'; Packages = @('AlliedBits.Tlumach', 'AlliedBits.Tlumach.Extensions.Localization'); Tfm = 'net10.0'; Type = 'Tlumach.Extensions.Localization.TlumachStringLocalizer' }
)

$NoLoadWarnings = 'CS9057;CS8784;CS8785'

function Get-InstalledSdkMajors {
    $result = @()
    foreach ($line in (& dotnet --list-sdks)) { if ($line -match '^(\d+)\.(\d+)\.') { $result += "$($Matches[1]).$($Matches[2])" } }
    return $result | Sort-Object -Unique
}

function Find-VisualStudioMSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) { return $null }
    $path = & $vswhere -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    return $path
}

function Write-NuGetConfig([string] $directory, [bool] $fromNuGetOrg) {
    # The packages of Tlumach come only from the package directory, so that a published package of the same version cannot be used instead
    # (unless the case takes an earlier version from nuget.org)
    $localMapping = if ($fromNuGetOrg) { '' } else { @'
    <packageSource key="local">
      <package pattern="AlliedBits.Tlumach" />
      <package pattern="AlliedBits.Tlumach.*" />
    </packageSource>
'@ }
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$PackageDirectory" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
$localMapping
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
  <config>
    <add key="globalPackagesFolder" value="$PackagesFolder" />
  </config>
</configuration>
"@ | Set-Content -LiteralPath (Join-Path $directory 'nuget.config') -Encoding utf8
}

function Get-PackageReferences($case) {
    foreach ($p in $case.Packages) {
        $id, $ver = $p.Split('@')
        if (-not $ver) { $ver = $Version }
        "    <PackageReference Include=""$id"" Version=""$ver"" />"
    }
}

function Write-Sources([string] $directory, $case, [string] $kind) {
    "defaultFile=strings.json`ngeneratedNamespace=Consumer.Tr`ngeneratedClass=Strings`n" | Set-Content -LiteralPath (Join-Path $directory 'strings.cfg') -Encoding utf8 -NoNewline
    '{ "Hello": "Hello" }' | Set-Content -LiteralPath (Join-Path $directory 'strings.json') -Encoding utf8
    $code = if ($case.ContainsKey('Code')) { $case.Code } else { '' }
    # C# 7.3 syntax only: some of the projects use that language version
    if ($kind -eq 'Library') {
        $source = @"
public static class Check
{
    public static string Run()
    {
        return typeof($($case.Type)).FullName + " " + Consumer.Tr.Strings.Hello.CurrentValue;
    }
}
"@
    }
    else {
        $source = @"
public static class Program
{
    public static int Main()
    {
        System.Console.WriteLine(typeof($($case.Type)).FullName);
        $code
        string text = Consumer.Tr.Strings.Hello.CurrentValue;
        System.Console.WriteLine("GEN: " + text);
        return text == "Hello" ? 0 : 1;
    }
}
"@
    }
    $source | Set-Content -LiteralPath (Join-Path $directory 'Program.cs') -Encoding utf8
}

function Write-SdkProject([string] $directory, [string] $name, $case, [string] $kind) {
    $sdk = if ($case.ContainsKey('Sdk')) { $case.Sdk } else { 'Microsoft.NET.Sdk' }
    $outputType = if ($kind -eq 'Exe') { '<OutputType>Exe</OutputType>' } else { '' }
    $properties = if ($case.ContainsKey('Properties')) { $case.Properties } else { '' }
    $items = if ($kind -eq 'Restore') { '' } else { @'
  <ItemGroup>
    <AdditionalFiles Include="strings.cfg" />
    <EmbeddedResource Include="strings.json" />
  </ItemGroup>
'@ }
    @"
<Project Sdk="$sdk">
  <PropertyGroup>
    $outputType
    <TargetFramework>$($case.Tfm)</TargetFramework>
    <AssemblyName>$name</AssemblyName>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <WarningsAsErrors>`$(WarningsAsErrors);$NoLoadWarnings</WarningsAsErrors>
    $properties
  </PropertyGroup>
  <ItemGroup>
$((Get-PackageReferences $case) -join "`n")
  </ItemGroup>
$items
</Project>
"@ | Set-Content -LiteralPath (Join-Path $directory "$name.csproj") -Encoding utf8
}

function Write-LegacyProject([string] $directory, [string] $name, $case) {
    @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="`$(MSBuildExtensionsPath)\`$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('`$(MSBuildExtensionsPath)\`$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '`$(Configuration)' == '' ">Release</Configuration>
    <Platform Condition=" '`$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{8C1D7C1E-1111-4C2B-9E7A-3B1A2C3D4E5F}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <RootNamespace>$name</RootNamespace>
    <AssemblyName>$name</AssemblyName>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <OutputPath>bin\`$(Configuration)\</OutputPath>
    <RestoreProjectStyle>PackageReference</RestoreProjectStyle>
    <WarningsAsErrors>`$(WarningsAsErrors);$NoLoadWarnings</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Windows.Forms" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
  </ItemGroup>
  <ItemGroup>
    <AdditionalFiles Include="strings.cfg" />
    <EmbeddedResource Include="strings.json" />
  </ItemGroup>
  <ItemGroup>
$((Get-PackageReferences $case) -join "`n")
  </ItemGroup>
  <Import Project="`$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
"@ | Set-Content -LiteralPath (Join-Path $directory "$name.csproj") -Encoding utf8
}

function Invoke-Logged([string] $logFile, [string] $exe, [string[]] $arguments, [string] $directory) {
    Push-Location -LiteralPath $directory
    try {
        $output = & $exe @arguments 2>&1 | Out-String
        $code = $LASTEXITCODE
    }
    finally { Pop-Location }
    Add-Content -LiteralPath $logFile -Value "> $exe $($arguments -join ' ')`n$output"
    return @{ Code = $code; Output = $output }
}

$sdkMajors = Get-InstalledSdkMajors
$vsMSBuild = if ($IsWindowsHost) { Find-VisualStudioMSBuild } else { $null }
$results = [System.Collections.Generic.List[object]]::new()

foreach ($case in $Cases | Where-Object { $_.Name -like $Filter }) {
    $name = 'Consumer_' + ($case.Name -replace '[^A-Za-z0-9]', '_')
    $kind = if ($case.ContainsKey('Kind')) { $case.Kind } else { 'Exe' }
    $directory = Join-Path $WorkDirectory $case.Name
    $logFile = Join-Path $WorkDirectory "$($case.Name).log"

    $missing = $null
    if ($case.ContainsKey('WindowsOnly') -and $case.WindowsOnly -and -not $IsWindowsHost) { $missing = 'Windows' }
    elseif ($case.ContainsKey('SdkVersion') -and $sdkMajors -notcontains $case.SdkVersion) { $missing = ".NET $($case.SdkVersion) SDK" }
    elseif ($kind -eq 'Legacy' -and -not $vsMSBuild) { $missing = 'MSBuild of Visual Studio' }
    if ($missing) {
        $results.Add([pscustomobject]@{ Case = $case.Name; Result = $(if ($RequireAll) { 'FAILED' } else { 'skipped' }); Details = "requires $missing" })
        continue
    }

    if (Test-Path -LiteralPath $directory) { Remove-Item -LiteralPath $directory -Recurse -Force }
    New-Item -ItemType Directory -Path $directory | Out-Null
    Set-Content -LiteralPath $logFile -Value "Case $($case.Name)"
    Write-NuGetConfig $directory ($case.ContainsKey('FromNuGetOrg') -and $case.FromNuGetOrg)
    if ($case.ContainsKey('SdkVersion')) {
        "{ ""sdk"": { ""version"": ""$($case.SdkVersion).100"", ""rollForward"": ""latestFeature"" } }" | Set-Content -LiteralPath (Join-Path $directory 'global.json') -Encoding utf8
    }
    if ($kind -ne 'Restore') { Write-Sources $directory $case $kind }
    if ($kind -eq 'Legacy') { Write-LegacyProject $directory $name $case } else { Write-SdkProject $directory $name $case $kind }

    Write-Host "Running $($case.Name)..."
    if ($kind -eq 'Restore') {
        $build = Invoke-Logged $logFile 'dotnet' @('restore') $directory
    }
    elseif ($kind -eq 'Legacy') {
        $build = Invoke-Logged $logFile $vsMSBuild @('-restore', '-p:Configuration=Release', '-nologo', '-v:m') $directory
    }
    else {
        $build = Invoke-Logged $logFile 'dotnet' @('build', '-c', 'Release', '-nologo') $directory
    }

    if ($case.ContainsKey('Expect')) {
        $ok = $build.Code -ne 0 -and $build.Output -match $case.Expect
        $details = if ($ok) { "failed with $($case.Expect) as expected" } else { "expected the build to fail with $($case.Expect)" }
        $results.Add([pscustomobject]@{ Case = $case.Name; Result = $(if ($ok) { 'passed' } else { 'FAILED' }); Details = $details })
        continue
    }

    if ($build.Code -ne 0) {
        $firstError = ($build.Output -split "`n" | Where-Object { $_ -match ' error |error [A-Z]+[0-9]+' } | Select-Object -First 1)
        $results.Add([pscustomobject]@{ Case = $case.Name; Result = 'FAILED'; Details = "build: $("$firstError".Trim())" })
        continue
    }

    if ($kind -in 'Exe', 'Legacy') {
        if ($kind -eq 'Legacy') { $program = Join-Path $directory "bin\Release\$name.exe"; $run = Invoke-Logged $logFile $program @() $directory }
        elseif ($case.Tfm -like 'net4*') { $program = Join-Path $directory "bin/Release/$($case.Tfm)/$name.exe"; $run = Invoke-Logged $logFile $program @() $directory }
        else { $run = Invoke-Logged $logFile 'dotnet' @("bin/Release/$($case.Tfm)/$name.dll") $directory }

        if ($run.Code -ne 0 -or $run.Output -notmatch 'GEN: Hello') {
            $results.Add([pscustomobject]@{ Case = $case.Name; Result = 'FAILED'; Details = "run: exit code $($run.Code), $("$($run.Output)".Trim() -replace '\s+', ' ')" })
            continue
        }
    }

    $results.Add([pscustomobject]@{ Case = $case.Name; Result = 'passed'; Details = '' })
}

$results | Format-Table -AutoSize | Out-String -Width 250 | Write-Host
Write-Host "Logs and projects: $WorkDirectory"

$failed = @($results | Where-Object { $_.Result -eq 'FAILED' })
if ($failed.Count) {
    Write-Host "$($failed.Count) case(s) failed." -ForegroundColor Red
    exit 1
}

Write-Host "All $(@($results | Where-Object { $_.Result -eq 'passed' }).Count) case(s) passed, $(@($results | Where-Object { $_.Result -eq 'skipped' }).Count) skipped." -ForegroundColor Green
exit 0
