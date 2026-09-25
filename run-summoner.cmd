@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind-Core INVOCADOR (slots 1,2,3 por defecto) ===
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --summoner-test --slots 1,2,3
echo exit:%ERRORLEVEL%
pause
