@echo off
REM Nacos Docker Compose 停止脚本 (Windows)
REM 使用方式: stop.bat [standalone|mysql|cluster] [--clean]

setlocal enabledelayedexpansion

set MODE=%1
set CLEAN=%2
if "%MODE%"=="" set MODE=standalone

cd /d "%~dp0"

echo ===================================
echo   Nacos Docker Compose 停止脚本
echo ===================================
echo.

if "%MODE%"=="cluster" (
    echo [INFO] 停止模式: 集群模式
    
    if "%CLEAN%"=="--clean" (
        echo [INFO] 清理模式: 删除所有数据
        docker-compose -f docker-compose.cluster.yml down -v
        if exist "mysql\data" rd /s /q "mysql\data" && mkdir "mysql\data"
        if exist "nacos\logs" rd /s /q "nacos\logs" && mkdir "nacos\logs"
        if exist "nacos\data" rd /s /q "nacos\data" && mkdir "nacos\data"
    ) else (
        docker-compose -f docker-compose.cluster.yml down
    )
) else if "%MODE%"=="mysql" (
    echo [INFO] 停止模式: 单机模式 + MySQL
    
    if "%CLEAN%"=="--clean" (
        echo [INFO] 清理模式: 删除所有数据
        docker-compose -f docker-compose.mysql.yml down -v
        if exist "mysql\data" rd /s /q "mysql\data" && mkdir "mysql\data"
        if exist "nacos\logs" rd /s /q "nacos\logs" && mkdir "nacos\logs"
        if exist "nacos\data" rd /s /q "nacos\data" && mkdir "nacos\data"
    ) else (
        docker-compose -f docker-compose.mysql.yml down
    )
) else (
    echo [INFO] 停止模式: 单机模式
    
    if "%CLEAN%"=="--clean" (
        echo [INFO] 清理模式: 删除所有数据
        docker-compose down -v
        if exist "nacos\logs" rd /s /q "nacos\logs" && mkdir "nacos\logs"
        if exist "nacos\data" rd /s /q "nacos\data" && mkdir "nacos\data"
    ) else (
        docker-compose down
    )
)

echo.
echo [OK] 服务已停止!
echo.

endlocal
