namespace TerraMind.State;

public sealed record BossState
{
    public int Type { get; init; }
    public string Name { get; init; } = "";
    public int Life { get; init; }
    public int MaxLife { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float VelocityX { get; init; }
    public float VelocityY { get; init; }

    // NPC.ai[0..3] en Terraria. Clave para anticipar embestidas.
    // Ej: EoC ai[0]==3 => dash inminente.
    public float[] Ai { get; init; } = new float[4];
    public int Damage { get; init; }
    public bool Active { get; init; }

    public float Speed => MathF.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);
}

public sealed record ProjectileState
{
    public int Type { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float VelocityX { get; init; }
    public float VelocityY { get; init; }
    public int Damage { get; init; }
    public bool Hostile { get; init; }
    public bool Active { get; init; }

    public float Speed => MathF.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);
}
