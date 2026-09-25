namespace TerraMind.State;

// Hora del mundo (Main.time + dayTime en Terraria).
// Sin lectura real aun: se estima por hora sistema + se corrige cuando haya attach.
// Importa a invocador: de noche hay mas spawns, ciertos latigos/minions rinden distinto,
// y no invocar a ciegas en pleno boss.
public sealed record WorldTime(
    bool IsDay,
    double TimeValue,   // 0..54000 dia, 0..32400 noche (formato Terraria)
    string Label)
{
    public static WorldTime Unknown() => new(true, 27000, "dia?");
    public static WorldTime FromSystem()
    {
        var h = DateTime.Now.Hour;
        bool day = h >= 6 && h < 19;
        return new(day, day ? 27000 : 16200, (day ? "dia~" : "noche~") + $"{h:00}:00");
    }
}

// Estado invocador normalizado.
public sealed record SummonerState(
    int ActiveMinions,     // Player.numMinions aprox (buffs de summon contados)
    int MaxMinions,        // Player.maxMinions
    int WhipBuffTimeLeft,  // ticks restantes del buff de latigo, 0 = sin buff
    int[] SummonHotbarSlots, // slots 0-9 que parecen invocadores (por intentos)
    int ActiveSlot)
{
    public bool NeedsSummon => ActiveMinions < MaxMinions;
    public bool NeedsWhipRefresh => WhipBuffTimeLeft <= 60;
    public static SummonerState Empty(int maxMinions = 3) => new(0, maxMinions, 0, Array.Empty<int>(), 0);
}
