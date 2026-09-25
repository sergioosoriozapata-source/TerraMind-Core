@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind-Core DEMO (no toca el juego) ===
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --ticks 90
echo exit:%ERRORLEVEL%
pause
