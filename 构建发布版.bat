@echo off
chcp 936 >nul
setlocal enabledelayedexpansion

echo ==========================================
echo      构建 Smart Text Editor 发布版
echo ==========================================
echo.

REM 检查.NET环境
echo 检查.NET环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ? 请先安装.NET 6 SDK
    echo 请查看 安装环境.bat 查看安装指南
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set "dotnet_version=%%i"
echo ? .NET SDK !dotnet_version! 可用
echo.

REM 显示项目信息
echo 项目信息:
echo   项目名称: Smart Text Editor
echo   目标平台: Windows x64
echo   运行时: .NET 6
echo   发布类型: 自包含多文件
echo.

REM 清理之前的构建
echo 清理之前的构建...
if exist "bin" (
    rmdir /s /q bin
    echo   ? 清理 bin 目录
)
if exist "obj" (
    rmdir /s /q obj
    echo   ? 清理 obj 目录
)
if exist "publish" (
    rmdir /s /q publish
    echo   ? 清理 publish 目录
)
echo.

REM 恢复包
echo 恢复NuGet包...
dotnet restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ? 包恢复失败
    pause
    exit /b 1
)
echo ? 包恢复成功
echo.

REM 构建项目
echo 构建项目...
dotnet build -c Release --no-restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ? 项目构建失败
    pause
    exit /b 1
)
echo ? 项目构建成功
echo.

REM 发布应用 (多文件, 自包含)
echo 发布应用程序...
echo   配置: Release
echo   平台: win-x64
echo   模式: 自包含多文件
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:ReadyToRun=true -o publish --verbosity quiet

if %errorlevel% equ 0 (
    echo.
    echo ?? 发布成功！
    echo.
    echo ?? 发布信息:
    echo   输出目录: .\publish\
    echo   主执行文件: SmartTextEditor.exe
    echo.
    
    REM 显示文件信息
    if exist "publish\SmartTextEditor.exe" (
        for %%f in (publish\SmartTextEditor.exe) do (
            set size=%%~zf
            set /a sizeMB=!size! / 1024 / 1024
            set /a sizeKB=!size! / 1024
            echo   ?? 文件大小: !sizeMB! MB (!sizeKB! KB)
            echo   ?? 生成时间: %%~tf
        )
    )
    
    REM 列出发布文件
    echo.
    echo ?? 发布目录内容:
    dir publish /b | find /v "SmartTextEditor.pdb"
    
    echo.
    echo ? 构建完成，应用程序已准备好部署！
    echo.
    
    set /p choice="是否运行测试程序(y/n): "
    if /i "!choice!"=="y" (
        echo ?? 启动应用...
        start publish\SmartTextEditor.exe
    )
    
    echo.
    set /p choice2="是否打开发布目录？(y/n): "
    if /i "!choice2!"=="y" (
        explorer publish
    )
) else (
    echo ? 发布失败
    echo 请检查上面的错误信息并重试
)

echo.
echo 按任意键退出...
pause >nul