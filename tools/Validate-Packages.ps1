#Requires -Version 7
<#
.SYNOPSIS
    Validates the NuGet packages of Tlumach.NET produced by build-tlumach-net.cmd.

.DESCRIPTION
    Checks that the package directory contains exactly one package for every nuspec in the repository root and nothing else, and that every package
    - has the expected version, a commit SHA in its repository metadata, a readme, and an icon;
    - contains exactly the expected assemblies in the expected lib folders, each with its XML documentation;
    - shares no assembly with another package (only AlliedBits.Tlumach contains the generator, with System.Text.Json and its dependencies, in analyzers/dotnet/cs);
    - depends on exactly the expected AlliedBits.Tlumach.* packages of the same version (an exact [version] range) in every dependency group,
      without excluding the analyzers of AlliedBits.Tlumach, so that the generator reaches the consumers of every package;
    - gets every Tlumach assembly referenced by its assemblies from itself or from the packages it depends on;
    - depends on exactly the expected other packages with the expected versions or ranges, and has the expected framework references;
    - contains assemblies of the package version (assembly and file version), built in the Release configuration, for the target framework of their folder,
      in exactly the folders that correspond to the target frameworks of their projects;
    - (AlliedBits.Tlumach) contains a generator built against a compiler no newer than the oldest supported one (Microsoft.CodeAnalysis 4.8), whose references
      are either next to it or provided by the compiler.

    The expected layout below is the reference for the package structure: change it together with the nuspec files.

.PARAMETER PackageDirectory
    The directory with the .nupkg files.

.PARAMETER Version
    The version that all packages must have.

.PARAMETER Commit
    The commit that the repository metadata of every package must name, e.g. the output of git rev-parse HEAD. When omitted, any commit SHA is accepted.

.EXAMPLE
    pwsh -File tools\Validate-Packages.ps1 -PackageDirectory ..\Redist\nuget\2.0.0 -Version 2.0.0
#>
param(
    [Parameter(Mandatory)] [string] $PackageDirectory,
    [Parameter(Mandatory)] [string] $Version,
    [string] $Commit
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
    'AlliedBits.Tlumach.MAUI'                    = @{ Deps = @($Core); Lib = 'Tlumach.MAUI'; Tfms = @($Net +
            @(foreach ($n in $Net) { foreach ($p in 'android21.0', 'ios15.0', 'maccatalyst15.0', 'windows10.0.19041.0') { "$n-$p" } })) }
    'AlliedBits.Tlumach.Avalonia'                = @{ Deps = @($Core); Lib = 'Tlumach.Avalonia' }
    'AlliedBits.Tlumach.UWP'                     = @{ Deps = @($Core); Lib = 'Tlumach.UWP'; Tfms = @('net9.0-windows10.0.17763.0', 'net10.0-windows10.0.17763.0') }
    'AlliedBits.Tlumach.Writers'                 = @{ Deps = @($Core); Lib = 'Tlumach.Writers'; Tfms = @('netstandard2.0', 'net9.0', 'net10.0') }
    'AlliedBits.Tlumach.FluentValidation'        = @{ Deps = @($Core); Lib = 'Tlumach.FluentValidation' }
    'AlliedBits.Tlumach.Scriban'                 = @{ Deps = @($Core); Lib = 'Tlumach.Scriban' }
    'AlliedBits.Tlumach.Fluid'                   = @{ Deps = @($Core); Lib = 'Tlumach.Fluid' }
    'AlliedBits.Tlumach.HandlebarsNet'           = @{ Deps = @($Core); Lib = 'Tlumach.HandlebarsNet' }
    'AlliedBits.Tlumach.MudBlazor'               = @{ Deps = @($Core, 'AlliedBits.Tlumach.Blazor'); Lib = 'Tlumach.MudBlazor' }
    'AlliedBits.Tlumach.Syncfusion.Blazor'       = @{ Deps = @($Core, 'AlliedBits.Tlumach.Blazor'); Lib = 'Tlumach.Syncfusion.Blazor' }
}

# The files of the generator in AlliedBits.Tlumach: the generator itself, and System.Text.Json with the dependencies that the compiler does not provide
$GeneratorFiles = @('Tlumach.Generator', 'System.Text.Json', 'System.Text.Encodings.Web', 'System.IO.Pipelines', 'Microsoft.Bcl.AsyncInterfaces', 'System.Memory',
    'System.Buffers', 'System.Numerics.Vectors', 'System.Runtime.CompilerServices.Unsafe', 'System.Threading.Tasks.Extensions') | ForEach-Object { "analyzers/dotnet/cs/$_.dll" }

# The dependencies on other packages than those of Tlumach: package ID -> (pattern of the dependency group framework -> 'ID version'); a group that matches no pattern has none
$NetStandardDeps = @('System.Memory 4.6.3', 'System.Text.Json 10.0.0')
$ExternalDeps = @{
    'AlliedBits.Tlumach'                         = @{ '.NETStandard2.0' = $NetStandardDeps }
    'AlliedBits.Tlumach.Writers'                 = @{ '.NETStandard2.0' = $NetStandardDeps }
    'AlliedBits.Tlumach.Extensions.Localization' = @{
        'net9.0'  = @('Microsoft.Extensions.DependencyInjection.Abstractions 9.0.0', 'Microsoft.Extensions.Localization.Abstractions 9.0.0')
        'net10.0' = @('Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0', 'Microsoft.Extensions.Localization.Abstractions 10.0.0') }
    'AlliedBits.Tlumach.Web'                     = @{ 'net9.0' = @('Microsoft.Extensions.DependencyInjection.Abstractions 9.0.0'); 'net10.0' = @('Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0') }
    'AlliedBits.Tlumach.FluentValidation'        = @{
        'net9.0'  = @('FluentValidation [12.0.0, 13.0.0)', 'Microsoft.Extensions.DependencyInjection.Abstractions 9.0.0')
        'net10.0' = @('FluentValidation [12.0.0, 13.0.0)', 'Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0') }
    'AlliedBits.Tlumach.Blazor'                  = @{ 'net9.0' = @('Microsoft.AspNetCore.Components.Web 9.0.0'); 'net10.0' = @('Microsoft.AspNetCore.Components.Web 10.0.0') }
    'AlliedBits.Tlumach.MAUI'                    = @{ 'net9.0*' = @('Microsoft.Maui.Controls 9.0.0'); 'net10.0*' = @('Microsoft.Maui.Controls 10.0.0') }
    'AlliedBits.Tlumach.Avalonia'                = @{ 'net*' = @('Avalonia [11.0.0, 13.0.0)') }
    'AlliedBits.Tlumach.Scriban'                 = @{ 'net*' = @('Scriban [7.2.2, 8.0.0)') }
    'AlliedBits.Tlumach.Fluid'                   = @{ 'net*' = @('Fluid.Core [2.40.0, 3.0.0)') }
    'AlliedBits.Tlumach.HandlebarsNet'           = @{ 'net*' = @('Handlebars.Net [2.1.0, 3.0.0)') }
    'AlliedBits.Tlumach.MudBlazor'               = @{ 'net*' = @('MudBlazor [9.0.0, 10.0.0)') }
    'AlliedBits.Tlumach.Syncfusion.Blazor'       = @{ 'net*' = @('Syncfusion.Blazor.Core 29.1.33') }
}

# The framework references (in every group of the package); the other packages have none
$FrameworkRefs = @{ 'AlliedBits.Tlumach.AspNetCore' = @('Microsoft.AspNetCore.App') }

# The assemblies that the compiler provides to the generator (and the framework assemblies), which therefore need not be next to it
$CompilerProvided = @('netstandard', 'mscorlib', 'Microsoft.CodeAnalysis', 'Microsoft.CodeAnalysis.CSharp', 'System.Collections.Immutable', 'System.Reflection.Metadata')
# The newest compiler version that the generator may be built against: the generator does not load in an older compiler (CS9057)
$MaxCompilerVersion = [version] '4.8.0.0'

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

        # Assembly references and attributes of every DLL under lib/ and analyzers/, read from the metadata
        $refs = @{}
        $infos = @{}
        foreach ($entry in $zip.Entries | Where-Object { $_.FullName -like 'lib/*.dll' -or $_.FullName -like 'analyzers/*.dll' }) {
            $ms = [System.IO.MemoryStream]::new()
            $s = $entry.Open(); try { $s.CopyTo($ms) } finally { $s.Dispose() }
            $ms.Position = 0
            $pe = [System.Reflection.PortableExecutable.PEReader]::new($ms)
            try {
                $md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
                $name = [Uri]::UnescapeDataString($entry.FullName)
                $references = @{}
                foreach ($h in $md.AssemblyReferences) { $r = $md.GetAssemblyReference($h); $references[$md.GetString($r.Name)] = $r.Version }
                if ($name -like 'lib/*') { $refs[$name] = @($references.Keys) }

                $definition = $md.GetAssemblyDefinition()
                $attributes = @{}
                foreach ($h in $definition.GetCustomAttributes()) {
                    $attribute = $md.GetCustomAttribute($h)
                    if ($attribute.Constructor.Kind -ne [System.Reflection.Metadata.HandleKind]::MemberReference) { continue }
                    $member = $md.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle] $attribute.Constructor)
                    if ($member.Parent.Kind -ne [System.Reflection.Metadata.HandleKind]::TypeReference) { continue }
                    $typeName = $md.GetString($md.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle] $member.Parent).Name)
                    if ($typeName -in 'AssemblyFileVersionAttribute', 'AssemblyConfigurationAttribute', 'TargetFrameworkAttribute') {
                        $blob = $md.GetBlobReader($attribute.Value)
                        $null = $blob.ReadUInt16() # prolog
                        $attributes[$typeName] = $blob.ReadSerializedString()
                    }
                }
                $infos[$name] = @{ Version = $definition.Version; Attributes = $attributes; References = $references }
            }
            finally { $pe.Dispose() }
        }
    }
    finally { $zip.Dispose() }

    $packages[$id] = @{ Entries = $entries; Nuspec = $nuspec; Refs = $refs; Infos = $infos }
}

# The target frameworks of a project, as its project file lists them (the conditional additions included); an assembly is expected in exactly these lib folders
function Get-ProjectFrameworks([string] $assembly) {
    $project = Join-Path $repoRoot "src/$assembly/$assembly.csproj"
    if (-not (Test-Path -LiteralPath $project)) { return $null }
    $text = [regex]::Replace((Get-Content -LiteralPath $project -Raw), '<!--.*?-->', '', 'Singleline')
    $result = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($text, '<TargetFrameworks?[^>]*>([^<]*)</TargetFrameworks?>')) {
        foreach ($t in $m.Groups[1].Value.Split(';')) { if ($t.Trim() -and $t -notmatch '\$\(') { $null = $result.Add($t.Trim()) } }
    }
    return $result
}

# A lib folder in the form of a project target framework: NuGet adds the platform version that the SDK implies (windows7.0, android21.0, ios15.0, maccatalyst15.0)
function ConvertTo-ProjectFramework([string] $folder) {
    return $folder -replace '-windows7\.0$', '-windows' -replace '-(android|ios|maccatalyst)[0-9.]+$', '-$1'
}

# The value of TargetFrameworkAttribute that an assembly in the given lib folder must have
function Get-ExpectedFrameworkAttribute([string] $folder) {
    if ($folder -eq 'netstandard2.0') { return '.NETStandard,Version=v2.0' }
    if ($folder -match '^net4(\d)(\d)$') { return ".NETFramework,Version=v4.$($Matches[1]).$($Matches[2])" }
    if ($folder -match '^net(\d+\.\d+)') { return ".NETCoreApp,Version=v$($Matches[1])" }
    return $null
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
    elseif ($Commit -and $meta.repository.commit -ne $Commit) { Fail "${id}: repository commit is '$($meta.repository.commit)', expected '$Commit'." }
    if (-not $meta.readme -or $pkg.Entries -notcontains $meta.readme) { Fail "${id}: the readme is missing." }
    if (-not $meta.icon -or $pkg.Entries -notcontains $meta.icon) { Fail "${id}: the icon is missing." }

    # Generator
    $analyzers = @($pkg.Entries | Where-Object { $_ -like 'analyzers/*' })
    if ($id -eq $Core) {
        if (Compare-Object @($GeneratorFiles | Sort-Object) @($analyzers | Sort-Object)) { Fail "${id}: analyzers must contain exactly [$($GeneratorFiles -join ', ')], found: [$($analyzers -join ', ')]." }

        # The generator must load in the oldest supported compiler, and everything it uses must be next to it or come from the compiler
        $bundled = @($analyzers | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) })
        foreach ($file in $analyzers | Where-Object { $pkg.Infos.ContainsKey($_) }) {
            foreach ($reference in $pkg.Infos[$file].References.GetEnumerator()) {
                if ($reference.Key -like 'Microsoft.CodeAnalysis*' -and $reference.Value -gt $MaxCompilerVersion) {
                    Fail "${id}: $file references $($reference.Key) $($reference.Value), so it does not load in compilers older than that; the oldest supported one is $MaxCompilerVersion."
                }
                if ($bundled -notcontains $reference.Key -and $CompilerProvided -notcontains $reference.Key -and $reference.Key -notlike 'System.Runtime*') {
                    Fail "${id}: $file references $($reference.Key), which is neither next to it nor provided by the compiler."
                }
            }
        }
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

            # The assembly must be of this version, built in Release, for the target framework of its folder (a leftover of an earlier build fails here)
            $info = $pkg.Infos["lib/$tfm/$dll"]
            if ($info.Version.ToString(3) -ne $Version -or $info.Version.Revision -ne 0) { Fail "${id}: lib/$tfm/$dll has assembly version $($info.Version), expected $Version.0." }
            if ($info.Attributes['AssemblyFileVersionAttribute'] -ne $Version) { Fail "${id}: lib/$tfm/$dll has file version '$($info.Attributes['AssemblyFileVersionAttribute'])', expected '$Version'." }
            if ($info.Attributes['AssemblyConfigurationAttribute'] -ne 'Release') { Fail "${id}: lib/$tfm/$dll was built in the '$($info.Attributes['AssemblyConfigurationAttribute'])' configuration, expected 'Release'." }
            $expectedFramework = Get-ExpectedFrameworkAttribute $tfm
            if ($expectedFramework -and $info.Attributes['TargetFrameworkAttribute'] -ne $expectedFramework) { Fail "${id}: lib/$tfm/$dll targets '$($info.Attributes['TargetFrameworkAttribute'])', expected '$expectedFramework'." }
        }
    }

    # Every assembly must be in exactly the lib folders that correspond to the target frameworks of its project
    foreach ($assembly in @($libFolders.Values | ForEach-Object { $_ } | Where-Object { $_ -like '*.dll' } | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) } | Sort-Object -Unique)) {
        $projectFrameworks = Get-ProjectFrameworks $assembly
        if ($null -eq $projectFrameworks) { Fail "${id}: there is no project src/$assembly/$assembly.csproj for $assembly.dll."; continue }
        $packaged = @($actualTfms | Where-Object { $libFolders[$_] -contains "$assembly.dll" } | ForEach-Object { ConvertTo-ProjectFramework $_ } | Sort-Object)
        if (Compare-Object @($projectFrameworks | Sort-Object) $packaged) {
            Fail "${id}: $assembly.dll is in the lib folders for [$($packaged -join ', ')], but its project targets [$(@($projectFrameworks | Sort-Object) -join ', ')]."
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
                if ($excludesAnalyzers) {
                    Fail "${id} ($($g.targetFramework)): the dependency on $Core excludes analyzers; it must not, so that the generator reaches the consumers of every package."
                }
            }
        }
        foreach ($d in @($g.ChildNodes | Where-Object { $_.LocalName -eq 'dependency' })) {
            if ($d.version -match '\$') { Fail "${id} ($($g.targetFramework)): the dependency on $($d.id) has an unreplaced token in its version '$($d.version)'." }
        }

        # Dependencies on other packages: exactly the expected IDs with the expected versions or ranges
        $expectedExternal = @()
        if ($ExternalDeps.ContainsKey($id)) {
            $pattern = @($ExternalDeps[$id].Keys | Where-Object { $g.targetFramework -like $_ } | Sort-Object Length -Descending | Select-Object -First 1)
            if ($pattern.Count) { $expectedExternal = @($ExternalDeps[$id][$pattern[0]]) }
        }
        $actualExternal = @($g.ChildNodes | Where-Object { $_.LocalName -eq 'dependency' -and $_.id -notlike 'AlliedBits.*' } | ForEach-Object { "$($_.id) $($_.version)" } | Sort-Object)
        if (Compare-Object @($expectedExternal | Sort-Object) $actualExternal) {
            Fail "${id} ($($g.targetFramework)): depends on [$($actualExternal -join ', ')] besides the packages of Tlumach, expected [$(@($expectedExternal | Sort-Object) -join ', ')]."
        }
    }

    # Framework references
    $expectedFrameworkRefs = @(if ($FrameworkRefs.ContainsKey($id)) { $FrameworkRefs[$id] })
    $frameworkGroups = @($meta.SelectNodes("*[local-name()='frameworkReferences']/*[local-name()='group']"))
    if ($expectedFrameworkRefs.Count -and $frameworkGroups.Count -eq 0) { Fail "${id}: no framework references, expected [$($expectedFrameworkRefs -join ', ')]." }
    foreach ($fg in $frameworkGroups) {
        $names = @($fg.ChildNodes | Where-Object { $_.LocalName -eq 'frameworkReference' } | ForEach-Object { $_.name } | Sort-Object)
        if (Compare-Object @($expectedFrameworkRefs | Sort-Object) $names) { Fail "${id} ($($fg.targetFramework)): framework references are [$($names -join ', ')], expected [$($expectedFrameworkRefs -join ', ')]." }
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
