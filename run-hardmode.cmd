@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind HARDMODE (fase 2: 10 jefes hasta Moon Lord) ===
echo 1/2 Estrategia (solo lee, no mueve)...
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --scan --slots 1,4
echo.
echo 2/2 Farmeo + jefes 15 min (mundo abierto, sin pausa, Ctrl+C para parar)...
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --farm --minutes 15
echo exit:%ERRORLEVEL%
pause
