# Autocalibracion por intentos (sin Cheat Engine ni externos)

El bot NO necesita pegar direcciones ni firmas manuales.

1. Al iniciar, `VanillaMemoryProvider.Attach()` busca `Terraria.exe`.
   `AobScanner` interno prueba firmas candidatas el solo.
2. Si no hay juego o no hay confianza, pasa a `SimulatedWorld`.
3. `AutoCalibrator.Calibrate()` hace intentos reales:
   - Corre 120 ticks con MoveX=1, mide distancia/velocidad => MaxRunSpeed.
   - Mide ticks hasta 90% velocidad => aceleracion.
   - Vuela con Fly=true hasta WingTime=0 => WingTimeMax.
4. Guarda en `LearnedOffsets` (`source=intentos`, `confidence=0.85`).
   En juego real el mismo codigo mide con lecturas reales + timing.
5. `AdaptiveBrain` usa lo aprendido, no numeros manuales.

Para jugar real: abre Terraria offline single-player, dale foco y lanza con `--live`.
El bot seguira intentando y ajustando en cada sesion.
