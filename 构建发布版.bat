@echo off
chcp 936 >nul
setlocal enabledelayedexpansion

echo ==========================================
echo      ���� Smart Text Editor ������
echo ==========================================
echo.

REM ���.NET����
echo ���.NET����...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ? ���Ȱ�װ.NET 6 SDK
    echo ��鿴 ��װ����.bat �鿴��װָ��
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set "dotnet_version=%%i"
echo ? .NET SDK !dotnet_version! ����
echo.

REM ��ʾ��Ŀ��Ϣ
echo ��Ŀ��Ϣ:
echo   ��Ŀ����: Smart Text Editor
echo   Ŀ��ƽ̨: Windows x64
echo   ����ʱ: .NET 6
echo   ��������: �԰������ļ�
echo.

REM ����֮ǰ�Ĺ���
echo ����֮ǰ�Ĺ���...
if exist "bin" (
    rmdir /s /q bin
    echo   ? ���� bin Ŀ¼
)
if exist "obj" (
    rmdir /s /q obj
    echo   ? ���� obj Ŀ¼
)
if exist "publish" (
    rmdir /s /q publish
    echo   ? ���� publish Ŀ¼
)
echo.

REM �ָ���
echo �ָ�NuGet��...
dotnet restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ? ���ָ�ʧ��
    pause
    exit /b 1
)
echo ? ���ָ��ɹ�
echo.

REM ������Ŀ
echo ������Ŀ...
dotnet build -c Release --no-restore --verbosity quiet
if %errorlevel% neq 0 (
    echo ? ��Ŀ����ʧ��
    pause
    exit /b 1
)
echo ? ��Ŀ�����ɹ�
echo.

REM ����Ӧ�� (���ļ�, �԰���)
echo ����Ӧ�ó���...
echo   ����: Release
echo   ƽ̨: win-x64
echo   ģʽ: �԰������ļ�
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:ReadyToRun=true -o publish --verbosity quiet

if %errorlevel% equ 0 (
    echo.
    echo ?? �����ɹ���
    echo.
    echo ?? ������Ϣ:
    echo   ���Ŀ¼: .\publish\
    echo   ��ִ���ļ�: SmartTextEditor.exe
    echo.
    
    REM ��ʾ�ļ���Ϣ
    if exist "publish\SmartTextEditor.exe" (
        for %%f in (publish\SmartTextEditor.exe) do (
            set size=%%~zf
            set /a sizeMB=!size! / 1024 / 1024
            set /a sizeKB=!size! / 1024
            echo   ?? �ļ���С: !sizeMB! MB (!sizeKB! KB)
            echo   ?? ����ʱ��: %%~tf
        )
    )
    
    REM �г������ļ�
    echo.
    echo ?? ����Ŀ¼����:
    dir publish /b | find /v "SmartTextEditor.pdb"
    
    echo.
    echo ? ������ɣ�Ӧ�ó�����׼���ò���
    echo.
    
    set /p choice="�Ƿ����в��Գ���(y/n): "
    if /i "!choice!"=="y" (
        echo ?? ����Ӧ��...
        start publish\SmartTextEditor.exe
    )
    
    echo.
    set /p choice2="�Ƿ�򿪷���Ŀ¼��(y/n): "
    if /i "!choice2!"=="y" (
        explorer publish
    )
) else (
    echo ? ����ʧ��
    echo ��������Ĵ�����Ϣ������
)

echo.
echo ��������˳�...
pause >nul