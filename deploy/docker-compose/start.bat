@echo off
REM Nacos Docker Compose 启动脚本 (Windows)
REM Nacos 版本: 3.1.1
REM 使用方式: start.bat [standalone|mysql|cluster]

setlocal enabledelayedexpansion

set MODE=%1
if "%MODE%"=="" set MODE=standalone

cd /d "%~dp0"

echo ===================================
echo   Nacos Docker Compose 部署脚本
echo   版本: 3.1.1
echo ===================================
echo.

REM 检查 Docker 是否运行
docker info >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker 未运行，请先启动 Docker Desktop
    exit /b 1
)

REM 创建必要的目录
if not exist "mysql\data" mkdir "mysql\data"
if not exist "mysql\init" mkdir "mysql\init"
if not exist "nacos\logs" mkdir "nacos\logs"
if not exist "nacos\data" mkdir "nacos\data"

REM 复制环境变量文件
if not exist ".env" (
    if exist ".env.example" (
        echo [INFO] 复制 .env.example 到 .env
        copy ".env.example" ".env" >nul
    )
)

if "%MODE%"=="cluster" (
    echo [INFO] 启动模式: 集群模式 ^(3 节点 + MySQL^)
    echo.
    
    if not exist "nacos\logs\nacos1" mkdir "nacos\logs\nacos1"
    if not exist "nacos\logs\nacos2" mkdir "nacos\logs\nacos2"
    if not exist "nacos\logs\nacos3" mkdir "nacos\logs\nacos3"
    if not exist "nacos\data\nacos1" mkdir "nacos\data\nacos1"
    if not exist "nacos\data\nacos2" mkdir "nacos\data\nacos2"
    if not exist "nacos\data\nacos3" mkdir "nacos\data\nacos3"
    
    docker-compose -f docker-compose.cluster.yml up -d
    
    echo.
    echo [OK] 集群模式启动完成!
    echo.
    echo 访问地址:
    echo   - Nginx 负载均衡: http://localhost
    echo   - Nacos 节点 1:   http://localhost:8080
    echo   - Nacos 节点 2:   http://localhost:8081
    echo   - Nacos 节点 3:   http://localhost:8082
) else if "%MODE%"=="mysql" (
    echo [INFO] 启动模式: 单机模式 + MySQL
    echo.
    
    docker-compose -f docker-compose.mysql.yml up -d
    
    echo.
    echo [OK] 单机模式 ^(MySQL^) 启动完成!
    echo.
    echo 访问地址:
    echo   - Nacos 控制台: http://localhost:8080
    echo   - HTTP API:     http://localhost:8848
) else (
    echo [INFO] 启动模式: 单机模式 ^(内嵌存储^)
    echo.
    
    docker-compose up -d
    
    echo.
    echo [OK] 单机模式启动完成!
    echo.
    echo 访问地址:
    echo   - Nacos 控制台: http://localhost:8080
    echo   - HTTP API:     http://localhost:8848
)

echo.
echo [INFO] 首次访问需要初始化 nacos 用户密码
echo.
echo 常用命令:
echo   查看日志: docker-compose logs -f nacos
echo   停止服务: stop.bat %MODE%
echo.

endlocal
