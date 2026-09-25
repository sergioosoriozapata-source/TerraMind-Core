@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind-Core FARM (farmea solo 5 min, Ctrl+C para parar) ===
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --farm --minutes 5
echo exit:%ERRORLEVEL%
pause
