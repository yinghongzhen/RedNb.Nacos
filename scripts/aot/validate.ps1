param(
    [ValidateSet('net8.0','net10.0')][string]$Framework = 'net10.0',
    [ValidateSet('win-x64','linux-x64')][string]$Rid = 'win-x64',
    [ValidateSet('Aot','AotWeb')][string]$App = 'Aot',
    [switch]$Managed,
    [string]$PackageDirectory,
    [string]$OutputRoot,
    [string]$Version = '2.2.0'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $root "samples/RedNb.Nacos.Sample.$App/RedNb.Nacos.Sample.$App.csproj"
$mode = if ($Managed -and $PackageDirectory) { 'package-strict' } elseif ($Managed) { 'strict' } elseif ($PackageDirectory) { 'package-native' } else { 'native' }
if (!$OutputRoot) { $OutputRoot = Join-Path $root 'artifacts/aot-validation' }
$output = Join-Path $OutputRoot "$mode/$Rid/$Framework/$App"
New-Item -ItemType Directory -Force $output | Out-Null
$properties = @('-p:JsonSerializerIsReflectionEnabledByDefault=false', "-p:TargetFrameworks=$Framework")
if ($PackageDirectory) {
    $properties += @('-p:UseNacosPackages=true', "-p:NacosPackageVersion=$Version", "-p:RestoreAdditionalProjectSources=$((Resolve-Path $PackageDirectory).Path)")
}
if ($Managed) {
    dotnet publish $project -f $Framework -c Release -o $output -p:PublishAot=false @properties 2>&1 | Tee-Object "$output/publish.log"
} else {
    dotnet publish $project -f $Framework -c Release -r $Rid --self-contained -o $output @properties `
        -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=true -p:IlcTreatWarningsAsErrors=true 2>&1 | Tee-Object "$output/publish.log"
}
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
if (Select-String "$output/publish.log" -Pattern 'warning IL\d+') { throw 'Unresolved AOT/trim warnings' }
if ($PackageDirectory) {
    $assets = Get-Content (Join-Path (Split-Path $project) 'obj/project.assets.json') -Raw | ConvertFrom-Json
    $sdkLibraries = @($assets.libraries.PSObject.Properties | Where-Object Name -Like 'RedNb.Nacos*/*')
    if ($sdkLibraries.Count -lt 4) { throw 'Expected SDK package graph was not restored' }
    foreach ($library in $sdkLibraries) {
        if ($library.Value.type -ne 'package') { throw "Source reference leaked into package consumer: $($library.Name)" }
        $id = $library.Name.Split('/')[0]
        $installed = $null
        foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
            $candidate = Join-Path $folder "$($library.Value.path)/$($id.ToLowerInvariant()).$Version.nupkg"
            if (Test-Path $candidate) { $installed = $candidate; break }
        }
        if (!$installed) { throw "Installed package not found: $id" }
        $expected = Join-Path $PackageDirectory "$id.$Version.nupkg"
        if ((Get-FileHash $installed).Hash -ne (Get-FileHash $expected).Hash) {
            throw "Stale package cache for $id. Use a fresh NUGET_PACKAGES directory before retrying."
        }
    }
    Write-Output "PACKAGE GRAPH: $($sdkLibraries.Count) packages, exact local hashes verified"
}
[string[]]$arguments = if ($App -eq 'Aot') { @('--live') } else { @() }
if ($Managed) { dotnet "$output/RedNb.Nacos.Sample.$App.dll" @arguments 2>&1 | Tee-Object "$output/run.log" }
else {
    $extension = if ($Rid -eq 'win-x64') { '.exe' } else { '' }
    & "$output/RedNb.Nacos.Sample.$App$extension" @arguments 2>&1 | Tee-Object "$output/run.log"
}
if ($LASTEXITCODE -ne 0) { throw 'Execution failed' }
$expectedMode = if ($Managed) { 'MODE dynamic=True reflection=False' } else { 'MODE dynamic=False reflection=False' }
if (!(Select-String -Path "$output/run.log" -SimpleMatch -Pattern $expectedMode)) { throw 'Wrong execution mode' }
$marker = if ($App -eq 'Aot') { 'PASS live SDK contracts' } else { 'PASS native-web' }
if (!(Select-String -Path "$output/run.log" -SimpleMatch -Pattern $marker)) { throw 'No successful live validation detected' }
