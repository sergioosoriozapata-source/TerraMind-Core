@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo === TerraMind EMPERATRIZ DIURNA (furiosa one-shot) ===
echo Requisitos: despues de Plantera, DE DIA (4:30-19:30), en el HALLOW superficie.
echo Suelta una Crisopa prismatica de dia para invocarla. El bot SOLO esquiva + subditos.
echo DPS: subditos al max. Sin latigazos (moririas). Ctrl+C para parar.
"src\TerraMind.App\bin\Release\net8.0\TerraMind.App.exe" --hunt EmpressOfLight --minutes 15
echo exit:%ERRORLEVEL%
pause
