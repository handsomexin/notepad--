@echo off
chcp 936 >nul
setlocal enabledelayedexpansion

echo ==========================================
echo    Smart Text Editor C# �ı��༭��
echo ==========================================
echo.

REM ���.NET����
echo [1/3] ���.NET����...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ? .NET SDK δ��װ
    echo.
    echo ���Ȱ�װ.NET 6+ SDK:
    echo ����: https://dotnet.microsoft.com/download
    echo.
    echo ��װ��ɺ��������д˽ű�
    pause
    exit /b 1
)

REM ��ȡ.NET�汾��Ϣ
for /f "tokens=*" %%a in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%a
echo ? .NET SDK �Ѱ�װ (�汾: !DOTNET_VERSION!)

REM �����Ŀ�ļ�
echo [2/3] �����Ŀ�ṹ...
if not exist "SmartTextEditor.csproj" (
    echo ? ��Ŀ�ļ�δ�ҵ�
    echo ��ȷ������ȷ����ĿĿ¼�����д˽ű�
    pause
    exit /b 1
)
echo ? ��Ŀ�ṹ��ȷ

REM �����Ҫ�ļ�
echo ���ؼ��ļ�...
if exist "MainWindow.xaml" (
    echo   ? �������ļ�����
)
if exist "App.xaml" (
    echo   ? Ӧ�������ļ�����
)
if exist "Windows\FileCompareWindow.xaml" (
    echo   ? �ļ��Աȴ��ڴ���
)

echo.
echo ==========================================
echo           ׼������Ӧ��
echo ==========================================

REM �ָ�NuGet��
echo [3/3] ���ڻָ�NuGet��...
dotnet restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ? ���ָ�ʧ��
    echo �����������Ӳ�����
    pause
    exit /b 1
)
echo ? ���ָ��ɹ�

REM ������Ŀ
echo ���ڹ�����Ŀ...
dotnet build --configuration Release --verbosity quiet
if %errorlevel% neq 0 (
    echo ? ��Ŀ����ʧ��
    echo �����������޸�
    pause
    exit /b 1
)
echo ? ��Ŀ�����ɹ�

REM ��ʾ������Ϣ
echo.
echo ?? ���� Smart Text Editor...
echo ??  ����ر�Ӧ�ã���ֱ�ӹرմ���
echo ??  ���ڴ��ն��а� Ctrl+C ��ֹ
echo.

REM ����Ӧ��
dotnet run --configuration Release

REM Ӧ���˳���Ĵ���
echo.
echo ? Smart Text Editor �ѹر�
echo.
echo ��������˳�...
pause >nul