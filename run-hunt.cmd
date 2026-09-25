@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind-Core HUNT Skeletron (invoca/pelea, Ctrl+C para parar) ===
echo Requisito: de NOCHE, pj en la dungeon. Habla con el anciano y dale Maldicion; el bot pelea.
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --hunt Skeletron --minutes 15
echo exit:%ERRORLEVEL%
pause
