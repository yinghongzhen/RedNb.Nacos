#!/usr/bin/env pwsh
# PowerShell 构建脚本，解决 Grpc.Tools 中文路径问题

# 创建临时目录
$tempDir = "D:\temp"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
}

# 设置环境变量
$env:TEMP = $tempDir
$env:TMP = $tempDir

Write-Host "================================================" -ForegroundColor Green
Write-Host "正在使用临时目录: $tempDir" -ForegroundColor Green
Write-Host "这可以解决用户名包含中文字符时的 Grpc.Tools 编译问题" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# 执行构建
if ($args.Count -eq 0) {
    Write-Host "执行: dotnet build" -ForegroundColor Cyan
    dotnet build
} else {
    $command = "dotnet " + ($args -join " ")
    Write-Host "执行: $command" -ForegroundColor Cyan
    & dotnet @args
}
