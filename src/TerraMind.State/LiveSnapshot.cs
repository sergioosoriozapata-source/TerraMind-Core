namespace TerraMind.State;

// Foto real del juego leida sin Cheat Engine ni offsets manuales:
// ClrMdGameReader localiza objetos por NOMBRE de tipo/campo (metadata .NET)
// y lee primitivas con ReadProcessMemory. Todo campo es opcional:
// si un nombre cambia de version, ese campo queda null y baja la confianza.
public sealed record ItemSlot(int Index, int Type, int Stack, string Role)
{
    public bool IsEmpty => Type <= 0 || Stack <= 0;
    public static ItemSlot Empty(int i) => new(i, 0, 0, "vacio");
}

public sealed record LiveSnapshot(
    bool Attached,
    string Source,          // "clrmd-live" | "saves" | "simulado"
    double Confidence,      // 0..1 segun campos leidos OK
    DateTime Taken,
    // Jugador
    int? Life, int? MaxLife, int? Mana, int? MaxMana, int? Defense,
    float? PosX, float? PosY, float? VelX, float? VelY,
    int? WingTime, int? WingTimeMax,
    int? NumMinions, int? MaxMinions,
    int? ActiveSlot,
    ItemSlot[] Hotbar,      // 10
    ItemSlot[] Inventory,   // resto
    int[] Buffs,
    // Mundo/tiempo
    bool? IsDay, double? WorldTime,
    string? WorldName, string? PlayerName,
    // Progresion (flags downed*)
    Dictionary<string, bool> Downed,
    // Jefes activos ahora
    BossState[] ActiveBosses,
    // Proyectiles hostiles en vivo (para esquivar dashes y tiros)
    ProjectileState[] LiveProjectiles,
    List<string> Warnings)
{
    public GameStage GuessStage()
    {
        bool D(string k) => Downed.TryGetValue(k, out var v) && v;
        if (D("downedMoonlord")) return GameStage.MoonLord_Done;
        if (D("downedLunatic") || D("downedAncientCultist")) return GameStage.Lunatic_Done;
        if (D("downedGolemBoss")) return GameStage.Golem_Done;
        if (D("downedPlantBoss")) return GameStage.Plantera_Done;
        if (D("downedMechBoss1") && D("downedMechBoss2") && D("downedMechBoss3")) return GameStage.Mech_Done;
        if (D("downedMechBossAny") || D("hardMode") && (D("downedMechBoss1") || D("downedMechBoss2") || D("downedMechBoss3"))) return GameStage.WallOfFlesh_Done;
        if (D("hardMode")) return GameStage.WallOfFlesh_Done;
        if (D("downedBoss3")) return GameStage.Skeletron_Done;
        if (D("downedBoss2")) return GameStage.EvilBoss_Done;
        if (D("downedBoss1")) return GameStage.EyeOfCthulhu_Done;
        return GameStage.Start;
    }
}
