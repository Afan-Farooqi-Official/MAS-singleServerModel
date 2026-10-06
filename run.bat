@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo   KFC Tandem Queue Simulation Launcher
echo ===================================================

cd /d "%~dp0"

REM If executable already exists, run it directly!
if exist "TandemSim.exe" (
    echo Launching simulation...
    echo.
    TandemSim.exe
    goto finished
)

REM If not built yet, compile with Windows built-in C# compiler
echo Compiling source code using Windows C# compiler...
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /t:exe /out:TandemSim.exe TandemSim.cs
) else (
    C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /t:exe /out:TandemSim.exe TandemSim.cs
)

if exist "TandemSim.exe" (
    echo Compilation successful!
    echo.
    TandemSim.exe
) else (
    echo [ERROR] Could not build executable.
)

:finished
echo.
pause
