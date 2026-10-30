@echo off
setlocal
cd /d "%~dp0"

echo [Refresh Taskbar] Compiling Native C# Build...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"refresh.ico" /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll /out:"RefreshTaskbar.exe" "RefreshTaskbar.cs"

if %errorlevel% neq 0 (
    echo [Refresh Taskbar] Compilation FAILED.
    exit /b %errorlevel%
)

echo [Refresh Taskbar] Compilation SUCCESSFUL: RefreshTaskbar.exe
