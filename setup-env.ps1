#!/usr/bin/env pwsh
#Requires -RunAsAdministrator

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "设置环境变量以解决 Grpc.Tools 中文路径问题" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 创建临时目录
$tempDir = "D:\temp"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    Write-Host "[成功] 创建目录: $tempDir" -ForegroundColor Green
} else {
    Write-Host "[信息] 目录已存在: $tempDir" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "设置系统环境变量..." -ForegroundColor Cyan
Write-Host "TEMP=$tempDir" -ForegroundColor Gray
Write-Host "TMP=$tempDir" -ForegroundColor Gray
Write-Host ""

try {
    # 设置系统环境变量（需要管理员权限）
    [System.Environment]::SetEnvironmentVariable("TEMP", $tempDir, [System.EnvironmentVariableTarget]::Machine)
    [System.Environment]::SetEnvironmentVariable("TMP", $tempDir, [System.EnvironmentVariableTarget]::Machine)
    
    Write-Host "[成功] 环境变量已设置" -ForegroundColor Green
    Write-Host ""
    Write-Host "重要提示:" -ForegroundColor Yellow
    Write-Host "1. 请重启 Visual Studio 以使环境变量生效" -ForegroundColor White
    Write-Host "2. 如果仍有问题，请重启计算机" -ForegroundColor White
    Write-Host "3. 或者使用 build.ps1 / build.cmd 脚本进行构建" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "[错误] 设置环境变量失败: $_" -ForegroundColor Red
    Write-Host "请确保以管理员身份运行此脚本" -ForegroundColor Red
    exit 1
}
