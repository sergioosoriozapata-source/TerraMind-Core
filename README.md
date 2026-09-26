# TerraMind-Core — Bot adaptativo local Terraria (Vanilla-first, sin externos)

Solo single-player offline. Sin Cheat Engine: lee el heap .NET por nombres (ClrMD) + saves, calibra por intentos.

## Estructura
- `src/TerraMind.State/` — `LiveSnapshot`, `PhasePlan` (FASE 1 pre 7 jefes / FASE 2 hard 10), `BossCatalog` (17 jefes), `ProgressionState`, FSM states
- `src/TerraMind.Memory/` — `ClrMdGameReader` (stats, inventario con nombres reales, buffs, downed*, NPC `ai[]`, proyectiles hostiles) + `TerrariaSaveScanner` + `SimulatedWorld`
- `src/TerraMind.Brain/` — `AdaptiveBrain` (Ofensivo/Defensivo/Curacion/EsquivaAerea por proyectiles cercanos), `AutoCalibrator`, `SummonerManager`, `StrategyBuilder` (clase por inventario real), `GameBeater`
- `src/TerraMind.Input/` — `SendInput` x86 40B (A/D, Espacio, H, slots 1-0, click/hold), `WindowFocus` auto-foco
- `src/TerraMind.App/` — modos `--scan`, `--farm`, `--hunt`, `--summoner-test`, `--beat-game`
- `mods/TerraMind.TModPort/` — mismo Brain con API `Main.*` directa

## Uso (App es x86: ejecuta el `.exe`, no `dotnet dll`)
1. Steam Offline, backup `Documents/My Games/Terraria/`.
2. `dotnet build TerraMind-Core.sln -c Release`
3. Ver estado + estrategia (no toca el juego salvo leer): `run-scan.cmd`
   (`TerraMind.App.exe --scan --slots 1,4`)
4. Farmear solo 5 min: `run-farm.cmd` (`--farm --minutes 5`)
5. Cazar Skeletron (noche + dungeon, tú activas al anciano): `run-hunt.cmd` (`--hunt Skeletron --minutes 15`)
6. Invocar súbditos: `run-summoner.cmd`. Mover: `run-demo.cmd`. Live libre: `run-live.cmd`.

## Seguridad
Solo lectura (`PROCESS_VM_READ` + ClrMD attach). Si corrupción: Steam > Terraria > Propiedades > Archivos instalados > Verificar integridad.
