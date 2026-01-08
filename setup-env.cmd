@echo off
echo ========================================
echo 设置环境变量以解决 Grpc.Tools 中文路径问题
echo ========================================
echo.

REM 检查是否以管理员权限运行
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [错误] 需要管理员权限来设置系统环境变量
    echo 请右键点击此文件，选择"以管理员身份运行"
    echo.
    pause
    exit /b 1
)

REM 创建临时目录
if not exist "D:\temp" (
    mkdir "D:\temp"
    echo [成功] 创建目录: D:\temp
) else (
    echo [信息] 目录已存在: D:\temp
)

echo.
echo 设置系统环境变量...
echo TEMP=D:\temp
echo TMP=D:\temp
echo.

REM 设置系统环境变量（对所有用户生效）
setx TEMP "D:\temp" /M
setx TMP "D:\temp" /M

echo.
echo [成功] 环境变量已设置
echo.
echo 重要提示:
echo 1. 请重启 Visual Studio 以使环境变量生效
echo 2. 如果仍有问题，请重启计算机
echo 3. 或者使用 build.ps1 / build.cmd 脚本进行构建
echo.
pause
