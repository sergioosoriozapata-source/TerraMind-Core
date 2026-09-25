# TerraMind-Core — Bot adaptativo local Terraria (Vanilla-first, sin externos)

Solo single-player offline. Sin Cheat Engine, sin ReClass, sin vision. El propio bot escanea y calibra por intentos.

## Estructura
- `src/TerraMind.State/` — structs + `IGameStateProvider`
- `src/TerraMind.Memory/` — `VanillaMemoryProvider` auto + `AobScanner` interno + `LearnedOffsets` + `SimulatedWorld`
- `src/TerraMind.Brain/` — `AdaptiveBrain` + `AutoCalibrator` (corre/vuela y mide)
- `src/TerraMind.Input/` — `SendInput` real (A/D, Espacio, H)
- `src/TerraMind.App/` — autocalibracion + loop demo limitado
- `mods/TerraMind.TModPort/` — mismo Brain con API `Main.*` directa
- `tools/AOB/AUTOCALIBRACION.md` — como aprende solo
- `docs/diagrams/` — FSM y arquitectura

## Uso
1. Steam Offline, backup `Documents/My Games/Terraria/`.
2. `dotnet build TerraMind-Core.sln -c Release`
3. Prueba sin juego (simulado, no pulsa teclas):
   `dotnet run --project src/TerraMind.App -c Release`
4. Juego real (Terraria abierto, mundo cargado, ventana con foco):
   `dotnet run --project src/TerraMind.App -c Release -- --live`
   Para demo infinita: `-- --live --ticks 0`

## Seguridad
Solo `PROCESS_VM_READ`. Si corrupcion: Steam > Terraria > Propiedades > Archivos instalados > Verificar integridad.
