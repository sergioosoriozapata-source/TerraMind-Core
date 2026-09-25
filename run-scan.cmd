@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind-Core SCAN (lee saves + juego, no mueve nada) ===
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --scan --slots 1,2,3
echo exit:%ERRORLEVEL%
pause
