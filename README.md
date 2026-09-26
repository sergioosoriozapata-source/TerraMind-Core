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
3. Solo 3 runs:
   * `run-prehardmode.cmd` → estrategia + farmeo/jefes FASE 1 (7 jefes hasta el Muro), 10 min
   * `run-hardmode.cmd` → estrategia + farmeo/jefes FASE 2 (10 jefes hasta Moon Lord), 15 min
   * `run-empress.cmd` → caza Emperatriz DIURNA (furiosa one-shot, solo esquiva + súbditos), 15 min
   Mundo abierto, sin pausa. `Ctrl+C` para parar.

## Seguridad
Solo lectura (`PROCESS_VM_READ` + ClrMD attach). Si corrupción: Steam > Terraria > Propiedades > Archivos instalados > Verificar integridad.
