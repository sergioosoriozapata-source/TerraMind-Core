namespace TerraMind.State;

// Estado normalizado del jugador. Lo rellena VanillaMemoryProvider (Fase 1)
// y luego TModProvider sin cambiar Brain.
public sealed record PlayerState
{
    public int Life { get; init; }
    public int MaxLife { get; init; }
    public int Mana { get; init; }
    public int MaxMana { get; init; }
    public float Defense { get; init; }

    // Hotbar 0-9 (item IDs de Terraria), inventario 0-49 para municion/material.
    public int[] HotbarIds { get; init; } = new int[10];
    public int ActiveSlot { get; init; }

    // Buffs activos (buffType[22] en Terraria).
    public int[] Buffs { get; init; } = Array.Empty<int>();

    // Recursos con cooldown.
    public bool PotionSickness { get; init; } // no puede curarse
    public bool ManaSickness { get; init; }

    // Movimiento (Fase 2).
    public int WingTime { get; init; }
    public int WingTimeMax { get; init; }
    public float MaxRunSpeed { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float VelocityX { get; init; }
    public float VelocityY { get; init; }

    public float LifePct => MaxLife <= 0 ? 0 : (float)Life / MaxLife;
    public float ManaPct => MaxMana <= 0 ? 0 : (float)Mana / MaxMana;
    public float WingPct => WingTimeMax <= 0 ? 0 : (float)WingTime / WingTimeMax;
}
