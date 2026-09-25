namespace TerraMind.Memory;

// Offsets APRENDIDOS por el bot, no pegados desde Cheat Engine.
// Flujo nuevo (sin externos):
//  1. VanillaMemoryProvider.AutoDiscover() prueba firmas internas.
//  2. Si falla, entra en modo CalibracionPorIntentos (simulado + timing real).
//  3. AutoCalibrator mide wingTime, runSpeed haciendo intentos y lo guarda aqui.
//  4. Se persiste a learned_offsets.json para no recalibrar cada vez.
public sealed class LearnedOffsets
{
    public bool IsLearned { get; set; }
    public string Source { get; set; } = "none"; // "auto-aob" | "intentos" | "simulado"
    public double Confidence { get; set; }

    // Direcciones aprendidas (no constantes manuales).
    public IntPtr LocalPlayerPtr { get; set; } = IntPtr.Zero;

    // Capacidades aprendidas por intentos (sustituye a numeros manuales).
    public int LearnedWingTimeMax { get; set; } = 180;
    public float LearnedMaxRunSpeed { get; set; } = 11.5f;
    public int LearnedPotionCooldownMs { get; set; } = 3600;

    public string ToJson() =>
        $"{{\"isLearned\":{IsLearned.ToString().ToLower()},\"source\":\"{Source}\",\"confidence\":{Confidence},\"wingTimeMax\":{LearnedWingTimeMax},\"maxRunSpeed\":{LearnedMaxRunSpeed},\"potionCdMs\":{LearnedPotionCooldownMs}}}";
}
