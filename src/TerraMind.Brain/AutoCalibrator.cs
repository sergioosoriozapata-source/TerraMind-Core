using TerraMind.State;

namespace TerraMind.Brain;

// Calibracion POR INTENTOS, sin Cheat Engine ni numeros manuales.
// Siempre sobre el mundo que se le pase (simulado). El llamador decide:
//  - sin juego: sim.Step
//  - con juego live: el bot ya calibrado ajusta por timing real en cada sesion.
public sealed class AutoCalibrator
{
    public sealed record CalibrationResult(
        int WingTimeMax,
        float MaxRunSpeed,
        int AccelTicks,
        int TicksMeasured,
        string Source);

    public CalibrationResult Calibrate(
        Func<PlayerState> get,
        Func<float, bool, bool, PlayerState> step,
        Action<string>? log = null)
    {
        // 1. Carrera: 120 ticks (~2s) a la derecha.
        for (int i = 0; i < 30; i++) step(0, false, false); // frenar / estabilizar
        var p0 = get();
        float x0 = p0.PositionX;
        PlayerState last = p0;
        for (int i = 0; i < 120; i++) last = step(1f, false, false);
        float dist = last.PositionX - x0;
        float vel = Math.Abs(last.VelocityX);
        // Usa velocidad si es fiable, si no distancia. Nunca baja de 5 para no romper Brain.
        float learnedSpeed = vel > 0.5f ? vel : Math.Max(5f, dist / 20f);
        log?.Invoke($"[Calib] Carrera dist={dist:F1} vel={last.VelocityX:F2} => speed~{learnedSpeed:F2}");

        // 2. Aceleracion: ticks hasta 90% de learnedSpeed.
        int accelTicks = 40;
        {
            for (int i = 0; i < 60; i++) last = step(0, false, false);
            for (int i = 0; i < 120; i++)
            {
                last = step(1f, false, false);
                if (Math.Abs(last.VelocityX) >= learnedSpeed * 0.9f) { accelTicks = i + 1; break; }
            }
            log?.Invoke($"[Calib] Aceleracion {accelTicks} ticks hasta 90%");
        }

        // 3. Vuelo: subir hasta agotar alas.
        int wingMax = get().WingTimeMax > 0 ? get().WingTimeMax : 180;
        {
            for (int i = 0; i < 120; i++) last = step(0, false, false); // recarga
            int startWing = get().WingTime;
            if (startWing <= 0) startWing = wingMax;
            int ticksFlying = 0;
            for (int i = 0; i < 2000; i++)
            {
                last = step(0, true, true);
                ticksFlying++;
                if (last.WingTime <= 0) break;
            }
            if (ticksFlying > 5 && ticksFlying < 2000) wingMax = ticksFlying;
            log?.Invoke($"[Calib] Vuelo startWing={startWing} ticks={ticksFlying} => wingMax={wingMax}");
        }

        return new CalibrationResult(wingMax, learnedSpeed, accelTicks, 120, "intentos");
    }
}
