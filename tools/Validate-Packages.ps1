#Requires -Version 7
<#
.SYNOPSIS
    Validates the NuGet packages of Tlumach.NET produced by build-tlumach-net.cmd.

.DESCRIPTION
    Checks that the package directory contains exactly one package for every nuspec in the repository root and nothing else, and that every package
    - has the expected version, a commit SHA in its repository metadata, a readme, and an icon;
    - contains exactly the expected assemblies in the expected lib folders, each with its XML documentation;
    - shares no assembly with another package (only AlliedBits.Tlumach contains the generator, in analyzers/dotnet);
    - depends on exactly the expected AlliedBits.Tlumach.* packages of the same version (an exact [version] range) in every dependency group;
    - gets every Tlumach assembly referenced by its assemblies from itself or from the packages it depends on.

    The expected layout below is the reference for the package structure: change it together with the nuspec files.

.PARAMETER PackageDirectory
    The directory with the .nupkg files.

.PARAMETER Version
    The version that all packages must have.

.EXAMPLE
    pwsh -File tools\Validate-Packages.ps1 -PackageDirectory ..\Redist\nuget\2.0.0 -Version 2.0.0
#>
param(
    [Parameter(Mandatory)] [string] $PackageDirectory,
    [Parameter(Mandatory)] [string] $Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$Net = @('net9.0', 'net10.0')
$Core = 'AlliedBits.Tlumach'

# Package ID -> lib folders with their assemblies, and the AlliedBits.Tlumach.* packages that the package depends on
$Expected = [ordered]@{
    'AlliedBits.Tlumach'                         = @{ Deps = @(); Lib = @{
            'netstandard2.0' = @('Tlumach.Base', 'Tlumach')
            'net9.0'         = @('Tlumach.Base', 'Tlumach', 'Tlumach.DataAnnotations')
            'net10.0'        = @('Tlumach.Base', 'Tlumach', 'Tlumach.DataAnnotations') } }
    'AlliedBits.Tlumach.Extensions.Localization' = @{ Deps = @($Core); Lib = 'Tlumach.Extensions.Localization' }
    'AlliedBits.Tlumach.Web'                     = @{ Deps = @($Core); Lib = 'Tlumach.Web' }
    'AlliedBits.Tlumach.Blazor'                  = @{ Deps = @($Core, 'AlliedBits.Tlumach.Web', 'AlliedBits.Tlumach.Extensions.Localization'); Lib = 'Tlumach.Blazor' }
    'AlliedBits.Tlumach.AspNetCore'              = @{ Deps = @($Core, 'AlliedBits.Tlumach.Web', 'AlliedBits.Tlumach.Extensions.Localization')
        Lib = @{ 'net9.0' = @('Tlumach.AspNetCore', 'Tlumach.AspNetCore.Mvc'); 'net10.0' = @('Tlumach.AspNetCore', 'Tlumach.AspNetCore.Mvc') } }
    'AlliedBits.Tlumach.WPF'                     = @{ Deps = @($Core); Lib = 'Tlumach.WPF'; Tfms = @('net9.0-windows7.0', 'net10.0-windows7.0') }
    'AlliedBits.Tlumach.WinForms'                = @{ Deps = @($Core); Lib = 'Tlumach.WinForms'; Tfms = @('net472', 'net9.0-windows7.0', 'net10.0-windows7.0') }
    'AlliedBits.Tlumach.WinUI'                   = @{ Deps = @($Core); Lib = 'Tlumach.WinUI'; Tfms = @('net9.0', 'net10.0', 'net9.0-windows10.0.19041.0', 'net10.0-windows10.0.19041.0') }
    'AlliedBits.Tlumach.MAUI'                    = @{ Deps = @($Core); Lib = 'Tlumach.MAUI'; Tfms = @(
            foreach ($n in $Net) { foreach ($p in 'android21.0', 'ios15.0', 'maccatalyst15.0', 'windows10.0.19041.0') { "$n-$p" } }) }
    'AlliedBits.Tlumach.Avalonia'                = @{ Deps = @($Core); Lib = 'Tlumach.Avalonia' }
    'AlliedBits.Tlumach.UWP'                     = @{ Deps = @($Core); Lib = 'Tlumach.UWP'; Tfms = @('net9.0-windows10.0.26100.0', 'net10.0-windows10.0.26100.0') }
    'AlliedBits.Tlumach.Writers'                 = @{ Deps = @($Core); Lib = 'Tlumach.Writers'; Tfms = @('netstandard2.0', 'net9.0', 'net10.0') }
    'AlliedBits.Tlumach.FluentValidation'        = @{ Deps = @($Core); Lib = 'Tlumach.FluentValidation' }
    'AlliedBits.Tlumach.Scriban'                 = @{ Deps = @($Core); Lib = 'Tlumach.Scriban' }
    'AlliedBits.Tlumach.Fluid'                   = @{ Deps = @($Core); Lib = 'Tlumach.Fluid' }
    'AlliedBits.Tlumach.HandlebarsNet'           = @{ Deps = @($Core); Lib = 'Tlumach.HandlebarsNet' }
    'AlliedBits.Tlumach.MudBlazor'               = @{ Deps = @($Core, 'AlliedBits.Tlumach.Blazor'); Lib = 'Tlumach.MudBlazor' }
    'AlliedBits.Tlumach.Syncfusion.Blazor'       = @{ Deps = @($Core, 'AlliedBits.Tlumach.Blazor'); Lib = 'Tlumach.Syncfusion.Blazor' }
}

# The packages whose dependency on AlliedBits.Tlumach passes the generator on to their consumers (the others exclude analyzers from it)
$GeneratorFlows = @('AlliedBits.Tlumach.Extensions.Localization', 'AlliedBits.Tlumach.Web', 'AlliedBits.Tlumach.Blazor', 'AlliedBits.Tlumach.AspNetCore',
    'AlliedBits.Tlumach.WPF', 'AlliedBits.Tlumach.WinForms', 'AlliedBits.Tlumach.WinUI', 'AlliedBits.Tlumach.MAUI', 'AlliedBits.Tlumach.Avalonia', 'AlliedBits.Tlumach.UWP')

# Expand the short forms: one assembly in the given (or the plain .NET 9 and .NET 10) lib folders
foreach ($id in @($Expected.Keys)) {
    $e = $Expected[$id]
    if ($e.Lib -is [string]) {
        $tfms = if ($e.ContainsKey('Tfms')) { $e.Tfms } else { $Net }
        $lib = @{}
        foreach ($t in $tfms) { $lib[$t] = @($e.Lib) }
        $e.Lib = $lib
    }
}

$errors = [System.Collections.Generic.List[string]]::new()
function Fail([string] $message) { $errors.Add($message) }

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
    throw "The package directory '$PackageDirectory' does not exist."
}

# The set of packages: one per nuspec in the repository root, and the nuspecs must match the expected layout
$nuspecIds = @(Get-ChildItem -LiteralPath $repoRoot -Filter '*.nuspec' | ForEach-Object { ([xml](Get-Content -LiteralPath $_.FullName -Raw)).package.metadata.id })
foreach ($id in $nuspecIds) { if (-not $Expected.Contains($id)) { Fail "The nuspec of $id is not described in the expected layout of $($MyInvocation.MyCommand.Name)." } }
foreach ($id in $Expected.Keys) { if ($nuspecIds -notcontains $id) { Fail "There is no nuspec for the expected package $id." } }

$files = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter '*.nupkg')
foreach ($f in $files) {
    if (-not ($Expected.Keys | Where-Object { $f.Name -eq "$_.$Version.nupkg" })) { Fail "Unexpected file $($f.Name) in $PackageDirectory." }
}

# Read the packages
$packages = @{}
foreach ($id in $Expected.Keys) {
    $path = Join-Path $PackageDirectory "$id.$Version.nupkg"
    if (-not (Test-Path -LiteralPath $path)) { Fail "Package $id.$Version.nupkg is missing."; continue }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
    try {
        $entries = @($zip.Entries | ForEach-Object { [Uri]::UnescapeDataString($_.FullName) })
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -notmatch '/' -and $_.FullName -like '*.nuspec' } | Select-Object -First 1
        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try { [xml] $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }

        # Assembly references of every DLL under lib/, read from the metadata
        $refs = @{}
        foreach ($entry in $zip.Entries | Where-Object { $_.FullName -like 'lib/*.dll' }) {
            $ms = [System.IO.MemoryStream]::new()
            $s = $entry.Open(); try { $s.CopyTo($ms) } finally { $s.Dispose() }
            $ms.Position = 0
            $pe = [System.Reflection.PortableExecutable.PEReader]::new($ms)
            try {
                $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
                $refs[[Uri]::UnescapeDataString($entry.FullName)] = @($md.AssemblyReferences | ForEach-Object { $md.GetString($md.GetAssemblyReference($_).Name) })
            }
            finally { $pe.Dispose() }
        }
    }
    finally { $zip.Dispose() }

    $packages[$id] = @{ Entries = $entries; Nuspec = $nuspec; Refs = $refs }
}

function Get-LibFolders($pkg) {
    $result = @{}
    foreach ($e in $pkg.Entries | Where-Object { $_ -like 'lib/*/*' }) {
        $parts = $e.Split('/')
        if (-not $result.ContainsKey($parts[1])) { $result[$parts[1]] = [System.Collections.Generic.List[string]]::new() }
        $result[$parts[1]].Add($parts[2])
    }
    return $result
}

# The lib folder of a package that NuGet would use for the given framework (a simplified version of the NuGet rules, sufficient for the frameworks used here)
function Find-CompatibleFolder($libFolders, [string] $tfm) {
    $candidates = [System.Collections.Generic.List[string]]::new()
    $candidates.Add($tfm)
    if ($tfm -match '^(net\d+\.\d+)-') { $candidates.Add($Matches[1]) }
    if ($tfm -match '^(net\d+\.\d+)-windows10') { $candidates.Add("$($Matches[1])-windows7.0") }
    $candidates.Add('netstandard2.0')
    foreach ($c in $candidates) { if ($libFolders.ContainsKey($c)) { return $c } }
    return $null
}

$assemblyOwner = @{}
foreach ($id in $Expected.Keys) {
    if (-not $packages.ContainsKey($id)) { continue }
    $pkg = $packages[$id]
    $meta = $pkg.Nuspec.package.metadata
    $exp = $Expected[$id]

    if ($meta.version -ne $Version) { Fail "${id}: version is '$($meta.version)', expected '$Version'." }
    if ($meta.repository.commit -notmatch '^[0-9a-f]{40}$') { Fail "${id}: repository commit '$($meta.repository.commit)' is not a commit SHA." }
    if (-not $meta.readme -or $pkg.Entries -notcontains $meta.readme) { Fail "${id}: the readme is missing." }
    if (-not $meta.icon -or $pkg.Entries -notcontains $meta.icon) { Fail "${id}: the icon is missing." }

    # Generator
    $analyzers = @($pkg.Entries | Where-Object { $_ -like 'analyzers/*' })
    if ($id -eq $Core) {
        if ($analyzers -notcontains 'analyzers/dotnet/Tlumach.Generator.dll' -or $analyzers.Count -ne 1) { Fail "${id}: analyzers must contain exactly analyzers/dotnet/Tlumach.Generator.dll, found: $($analyzers -join ', ')." }
    }
    elseif ($analyzers.Count) { Fail "${id}: unexpected analyzers: $($analyzers -join ', ')." }

    $unexpected = @($pkg.Entries | Where-Object { $_ -match '^(build|buildTransitive|content|contentFiles|tools|ref|runtimes)/' })
    if ($unexpected.Count) { Fail "${id}: unexpected files: $($unexpected -join ', ')." }

    # Lib folders and assemblies
    $libFolders = Get-LibFolders $pkg
    $expectedTfms = @($exp.Lib.Keys | Sort-Object)
    $actualTfms = @($libFolders.Keys | Sort-Object)
    if (Compare-Object $expectedTfms $actualTfms) { Fail "${id}: lib folders are [$($actualTfms -join ', ')], expected [$($expectedTfms -join ', ')]." }
    foreach ($tfm in $actualTfms) {
        $files = @($libFolders[$tfm])
        $expectedFiles = @($exp.Lib[$tfm] | ForEach-Object { "$_.dll"; "$_.xml" } | Sort-Object)
        if ($exp.Lib.ContainsKey($tfm) -and (Compare-Object $expectedFiles @($files | Sort-Object))) {
            Fail "${id}: lib/$tfm contains [$(($files | Sort-Object) -join ', ')], expected [$($expectedFiles -join ', ')]."
        }
        foreach ($dll in $files | Where-Object { $_ -like '*.dll' }) {
            if ($assemblyOwner.ContainsKey($dll) -and $assemblyOwner[$dll] -ne $id) { Fail "$dll is in both $($assemblyOwner[$dll]) and $id." }
            $assemblyOwner[$dll] = $id
        }
    }

    # Dependencies on the packages of Tlumach: the expected set with the exact version, in every group, and a group for every lib folder
    $groups = @($meta.dependencies.group)
    # NuGet writes the frameworks of the groups in a normalized form (e.g. net9.0-windows10.0.19041 for net9.0-windows10.0.19041.0)
    $groupTfms = @($groups | ForEach-Object { $_.targetFramework -replace '^(.*\d+\.\d+\.\d+)\.0$', '$1' })
    foreach ($g in $groups) {
        $deps = @($g.ChildNodes | Where-Object { $_.LocalName -eq 'dependency' -and $_.id -like 'AlliedBits.*' })
        $ids = @($deps | ForEach-Object { $_.id } | Sort-Object)
        if (Compare-Object @($exp.Deps | Sort-Object) $ids) { Fail "${id} ($($g.targetFramework)): depends on [$($ids -join ', ')], expected [$(($exp.Deps | Sort-Object) -join ', ')]." }
        foreach ($d in $deps) {
            if ($d.version -ne "[$Version]") { Fail "${id} ($($g.targetFramework)): the dependency on $($d.id) has version '$($d.version)', expected '[$Version]'." }
            if ($d.id -eq $Core) {
                $excludesAnalyzers = @(([string] $d.exclude).Split(',') | ForEach-Object { $_.Trim() }) -contains 'Analyzers'
                if (($GeneratorFlows -contains $id) -eq $excludesAnalyzers) {
                    Fail "${id} ($($g.targetFramework)): the dependency on $Core $(if ($excludesAnalyzers) { 'excludes' } else { 'does not exclude' }) analyzers, so the generator $(if ($excludesAnalyzers) { 'does not reach' } else { 'reaches' }) the consumers, contrary to the expected layout."
                }
            }
        }
        foreach ($d in @($g.ChildNodes | Where-Object { $_.LocalName -eq 'dependency' })) {
            if ($d.version -match '\$') { Fail "${id} ($($g.targetFramework)): the dependency on $($d.id) has an unreplaced token in its version '$($d.version)'." }
        }
    }
    foreach ($tfm in $actualTfms) {
        $normalized = if ($tfm -eq 'net472') { '.NETFramework4.7.2' } elseif ($tfm -eq 'netstandard2.0') { '.NETStandard2.0' } else { $tfm -replace '^(.*\d+\.\d+\.\d+)\.0$', '$1' }
        if ($groupTfms -notcontains $normalized) { Fail "${id}: no dependency group for lib/$tfm (groups: $($groupTfms -join ', '))." }
    }
}

# Every Tlumach assembly referenced by an assembly of a package must come from the package itself or from the packages it depends on (transitively)
function Get-DependencyClosure([string] $id) {
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    $queue = [System.Collections.Generic.Queue[string]]::new()
    $queue.Enqueue($id)
    while ($queue.Count) {
        $current = $queue.Dequeue()
        if ($seen.Add($current) -and $Expected.Contains($current)) { foreach ($d in $Expected[$current].Deps) { $queue.Enqueue($d) } }
    }
    return $seen
}

foreach ($id in $packages.Keys) {
    $closure = Get-DependencyClosure $id
    foreach ($dllPath in $packages[$id].Refs.Keys) {
        $tfm = $dllPath.Split('/')[1]
        foreach ($ref in $packages[$id].Refs[$dllPath] | Where-Object { $_ -like 'Tlumach*' }) {
            $found = $false
            foreach ($p in $closure) {
                if (-not $packages.ContainsKey($p)) { continue }
                $libFolders = Get-LibFolders $packages[$p]
                $folder = Find-CompatibleFolder $libFolders $tfm
                if ($folder -and $libFolders[$folder] -contains "$ref.dll") { $found = $true; break }
            }
            if (-not $found) { Fail "${id}: $dllPath references $ref, which neither the package nor its dependencies provide for $tfm." }
        }
    }
}

if ($errors.Count) {
    Write-Host "Package validation failed ($($errors.Count) problem(s)):" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Validated $($packages.Count) packages of version $Version in $PackageDirectory." -ForegroundColor Green
exit 0
