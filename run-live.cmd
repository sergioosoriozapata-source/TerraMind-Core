@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind-Core LIVE (auto-foco + input real, Ctrl+C para parar) ===
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --live --ticks 0
echo exit:%ERRORLEVEL%
pause
