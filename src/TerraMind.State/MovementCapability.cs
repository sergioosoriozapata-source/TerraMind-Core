namespace TerraMind.State;

// Capacidades fisicas calibradas en Fase 2.
// Ej: Fishron WingTimeMax=180 ticks, Terraspark MaxRunSpeed~11.5 tiles/s.
public sealed record MovementCapability
{
    public int WingTimeMax { get; init; }
    public float MaxRunSpeed { get; init; }
    public float RunAccelerationTimeMs { get; init; }
    public bool CanDash { get; init; }
    public bool CanFly { get; init; }

    // Reserva 25% de alas siempre para esquiva de emergencia.
    public bool ShouldReserveWings(float wingPct) => wingPct < 0.25f;

    public static MovementCapability Default() => new()
    {
        WingTimeMax = 180,
        MaxRunSpeed = 11.5f,
        RunAccelerationTimeMs = 600,
        CanDash = false,
        CanFly = true
    };
}
