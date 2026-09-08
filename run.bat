@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo   KFC Queue Simulation Launcher
echo ===================================================

cd /d "%~dp0"

REM If executable already exists, run it directly!
if exist "KfcQueueSim.exe" (
    echo Launching simulation...
    echo.
    KfcQueueSim.exe
    goto finished
)

REM If not built yet, compile with Windows built-in C# compiler
echo Compiling source code using Windows C# compiler...
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /t:exe /out:KfcQueueSim.exe Program.cs DataLoader.cs RandomGen.cs SimulationEngine.cs Models\CustomerRecord.cs Models\CustomerSimStat.cs Models\SimulationResult.cs Models\ValidationRow.cs Models\SimEvent.cs
) else (
    C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /nologo /t:exe /out:KfcQueueSim.exe Program.cs DataLoader.cs RandomGen.cs SimulationEngine.cs Models\CustomerRecord.cs Models\CustomerSimStat.cs Models\SimulationResult.cs Models\ValidationRow.cs Models\SimEvent.cs
)

if exist "KfcQueueSim.exe" (
    echo Compilation successful!
    echo.
    KfcQueueSim.exe
) else (
    echo [ERROR] Could not build executable.
)

:finished
echo.
pause
