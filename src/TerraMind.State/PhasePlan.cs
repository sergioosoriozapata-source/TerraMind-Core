namespace TerraMind.State;

// Dos fases, cada una separada por jefes con conteo:
// FASE 1 pre-hardmode (7): KingSlime*, EoC, Evil(EoW/BoC), QueenBee*, Skeletron, Deerclops*, WoF (*=opcional)
// FASE 2 hardmode (10): QueenSlime*, Destroyer, Twins, Prime, Plantera, Golem, Fishron*, Empress*, Cultist, MoonLord
// Los flags se leen del juego (NPC.downed*). Si un flag no existe en tu version, cuenta como pendiente.
public enum Phase { PreHardmode, Hardmode }

public sealed record BossCheck(string Name, string[] Flags, bool Optional, string SummonHint)
{
    public bool Done(IReadOnlyDictionary<string, bool> downed) =>
        Flags.Any(f => downed.TryGetValue(f, out var v) && v);
}

public static class PhasePlan
{
    public static readonly BossCheck[] PreHardmode = new[]
    {
        new BossCheck("KingSlime", new[] { "downedSlimeKing" }, true, "Corona de slime, superficie"),
        new BossCheck("EyeOfCthulhu", new[] { "downedBoss1" }, false, "Ojo sospechoso, noche"),
        new BossCheck("Evil", new[] { "downedBoss2" }, false, "EoW/BoC en corrupcion/carmesi"),
        new BossCheck("QueenBee", new[] { "downedQueenBee" }, true, "Abeemination, jungla"),
        new BossCheck("Skeletron", new[] { "downedBoss3" }, false, "Anciano dungeon, noche"),
        new BossCheck("Deerclops", new[] { "downedDeerclops" }, true, "Deer Thing, nieve/noche"),
        new BossCheck("WallOfFlesh", new[] { "hardMode" }, false, "Muneco vudu, inframundo"),
    };

    public static readonly BossCheck[] Hardmode = new[]
    {
        new BossCheck("QueenSlime", new[] { "downedQueenSlime" }, true, "Cristal gelatina, hallow"),
        new BossCheck("TheDestroyer", new[] { "downedMechBoss1" }, false, "Gusano mecanico, noche"),
        new BossCheck("TheTwins", new[] { "downedMechBoss2" }, false, "Ojo mecanico, noche"),
        new BossCheck("SkeletronPrime", new[] { "downedMechBoss3" }, false, "Calavera mecanica, noche"),
        new BossCheck("Plantera", new[] { "downedPlantBoss" }, false, "Bulbo, jungla"),
        new BossCheck("Golem", new[] { "downedGolemBoss" }, false, "Celula, templo"),
        new BossCheck("DukeFishron", new[] { "downedFishron" }, true, "Gusano trufa, oceano"),
        new BossCheck("EmpressOfLight", new[] { "downedEmpress", "downedHallowBoss" }, true, "Crisopa, hallow/noche"),
        new BossCheck("LunaticCultist", new[] { "downedAncientCultist" }, false, "Sectarios, dungeon"),
        new BossCheck("MoonLord", new[] { "downedMoonlord" }, false, "Senal celestial"),
    };

    public static Phase Current(IReadOnlyDictionary<string, bool> downed) =>
        downed.TryGetValue("hardMode", out var h) && h ? Phase.Hardmode : Phase.PreHardmode;

    public static (int done, int total) Count(BossCheck[] list, IReadOnlyDictionary<string, bool> downed) =>
        (list.Count(b => b.Done(downed)), list.Length);

    public static BossCheck? NextRequired(BossCheck[] list, IReadOnlyDictionary<string, bool> downed) =>
        list.FirstOrDefault(b => !b.Optional && !b.Done(downed));

    public static string[] PendingOptional(BossCheck[] list, IReadOnlyDictionary<string, bool> downed) =>
        list.Where(b => b.Optional && !b.Done(downed)).Select(b => b.Name).ToArray();

    public static string Summary(IReadOnlyDictionary<string, bool> downed)
    {
        var (pd, pt) = Count(PreHardmode, downed);
        var (hd, ht) = Count(Hardmode, downed);
        var phase = Current(downed);
        var next = phase == Phase.PreHardmode ? NextRequired(PreHardmode, downed) : NextRequired(Hardmode, downed);
        return $"FASE {(phase == Phase.PreHardmode ? "1 pre-hardmode" : "2 hardmode")} | pre {pd}/{pt} | hard {hd}/{ht} | siguiente: {(next is null ? (phase == Phase.PreHardmode ? "MURO (pasa a hardmode)" : "JUEGO PASADO") : $"{next.Name} ({next.SummonHint})")}";
    }
}
