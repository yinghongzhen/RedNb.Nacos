@echo off
REM 设置临时目录到不包含中文字符的路径，解决 Grpc.Tools 编译问题
setlocal

REM 创建临时目录
if not exist "D:\temp" mkdir "D:\temp"

REM 设置环境变量
set TEMP=D:\temp
set TMP=D:\temp

echo ================================================
echo 正在使用临时目录: %TEMP%
echo 这可以解决用户名包含中文字符时的 Grpc.Tools 编译问题
echo ================================================
echo.

REM 执行构建
if "%1"=="" (
    echo 执行: dotnet build
    dotnet build
) else (
    echo 执行: dotnet %*
    dotnet %*
)

endlocal
