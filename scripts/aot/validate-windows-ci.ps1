param(
    [ValidateSet('net8.0','net10.0')][string]$Framework,
    [ValidateSet('Aot','AotWeb')][string]$App
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
New-Item -ItemType Directory -Force artifacts/server | Out-Null
Invoke-WebRequest https://github.com/alibaba/nacos/releases/download/3.2.4/nacos-server-3.2.4.zip -OutFile artifacts/server/nacos.zip
Expand-Archive artifacts/server/nacos.zip artifacts/server
$nacosDirectory = (Resolve-Path artifacts/server/nacos).Path
$token = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
Add-Content "$nacosDirectory/conf/application.properties" @"
nacos.core.auth.enabled=true
nacos.core.auth.admin.enabled=true
nacos.core.auth.console.enabled=true
nacos.core.auth.plugin.nacos.token.secret.key=$token
nacos.core.auth.server.identity.key=ci
nacos.core.auth.server.identity.value=$([Guid]::NewGuid().ToString('N'))
"@
$serverProcess = Start-Process java -WindowStyle Hidden -PassThru -WorkingDirectory $nacosDirectory -ArgumentList @(
    '--add-opens=java.base/java.lang=ALL-UNNAMED',
    '--add-opens=java.base/java.lang.reflect=ALL-UNNAMED',
    '--add-opens=java.base/java.util=ALL-UNNAMED',
    '-Xms512m', '-Xmx1g', '-Dnacos.standalone=true', "-Dnacos.home=$nacosDirectory",
    '-Dnacos.deployment.type=merged',
    "-Dloader.path=$nacosDirectory/plugins,$nacosDirectory/plugins/health,$nacosDirectory/plugins/cmdb,$nacosDirectory/plugins/selector",
    '-jar', 'target/nacos-server.jar', '--spring.config.additional-location=file:./conf/',
    "--logging.config=$nacosDirectory/conf/nacos-logback.xml"
) -RedirectStandardOutput "$nacosDirectory/stdout.log" -RedirectStandardError "$nacosDirectory/stderr.log"
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 90; $attempt++) {
        if ($serverProcess.HasExited) { throw "Nacos exited before readiness: $($serverProcess.ExitCode)" }
        try { Invoke-WebRequest http://localhost:8080/v3/console/health/readiness -TimeoutSec 2 | Out-Null; $ready = $true; break }
        catch { Start-Sleep -Seconds 2 }
    }
    if (!$ready) { throw 'Nacos readiness timeout' }
    Invoke-RestMethod -Method Post http://localhost:8848/nacos/v3/auth/user/admin -Body @{password='nacos'} | Out-Null
    # Keep the server and both consumers in one runner shell lifetime.
    & "$PSScriptRoot/validate.ps1" -Framework $Framework -Rid win-x64 -App $App
    foreach ($project in Get-ChildItem src/*/*.csproj) {
        dotnet pack $project.FullName -c Release -o artifacts/packages
        if ($LASTEXITCODE) { throw 'Pack failed' }
    }
    python scripts/verify-packages.py artifacts/packages --version 2.2.0
    if ($LASTEXITCODE) { throw 'Invalid packages' }
    & "$PSScriptRoot/validate.ps1" -Framework $Framework -Rid win-x64 -App $App -PackageDirectory artifacts/packages -Version 2.2.0
} catch {
    Get-ChildItem $nacosDirectory -Recurse -Filter '*.log' | ForEach-Object { Get-Content $_.FullName -Tail 40 }
    throw
} finally {
    if (!$serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id }
}
