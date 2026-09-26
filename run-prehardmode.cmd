@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind PRE-HARDMODE (fase 1: 7 jefes hasta el Muro) ===
echo 1/2 Estrategia (solo lee, no mueve)...
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --scan --slots 1,4
echo.
echo 2/2 Farmeo + jefes 10 min (mundo abierto, sin pausa, Ctrl+C para parar)...
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --farm --minutes 10
echo exit:%ERRORLEVEL%
pause
