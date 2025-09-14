@echo off
chcp 936 >nul
setlocal enabledelayedexpansion

echo ==========================================
echo    Smart Text Editor C# 文本编辑器
echo ==========================================
echo.

REM 检查.NET环境
echo [1/3] 检查.NET环境...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ? .NET SDK 未安装
    echo.
    echo 请先安装.NET 6+ SDK:
    echo 下载: https://dotnet.microsoft.com/download
    echo.
    echo 安装完成后重新运行此脚本
    pause
    exit /b 1
)

REM 获取.NET版本信息
for /f "tokens=*" %%a in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%a
echo ? .NET SDK 已安装 (版本: !DOTNET_VERSION!)

REM 检查项目文件
echo [2/3] 检查项目结构...
if not exist "SmartTextEditor.csproj" (
    echo ? 项目文件未找到
    echo 请确保在正确的项目目录下运行此脚本
    pause
    exit /b 1
)
echo ? 项目结构正确

REM 检查重要文件
echo 检查关键文件...
if exist "MainWindow.xaml" (
    echo   ? 主窗口文件存在
)
if exist "App.xaml" (
    echo   ? 应用配置文件存在
)
if exist "Windows\FileCompareWindow.xaml" (
    echo   ? 文件对比窗口存在
)

echo.
echo ==========================================
echo           准备启动应用
echo ==========================================

REM 恢复NuGet包
echo [3/3] 正在恢复NuGet包...
dotnet restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ? 包恢复失败
    echo 请检查网络连接并重试
    pause
    exit /b 1
)
echo ? 包恢复成功

REM 构建项目
echo 正在构建项目...
dotnet build --configuration Release --verbosity quiet
if %errorlevel% neq 0 (
    echo ? 项目构建失败
    echo 请检查代码错误并修复
    pause
    exit /b 1
)
echo ? 项目构建成功

REM 显示启动信息
echo.
echo ?? 启动 Smart Text Editor...
echo ??  如需关闭应用，请直接关闭窗口
echo ??  或在此终端中按 Ctrl+C 终止
echo.

REM 运行应用
dotnet run --configuration Release

REM 应用退出后的处理
echo.
echo ? Smart Text Editor 已关闭
echo.
echo 按任意键退出...
pause >nul