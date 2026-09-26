namespace TerraMind.State;

// Catalogo: que item invoca a quien, cuando/donde. El bot lo usa para invocar solo.
// "Sin item: ..." = se activa en el mundo (anciano, bulbo, altar, sectarios): el bot espera y pelea.
// Orden completo: FASE 1 pre-hardmode (7) + FASE 2 hardmode (10). Ver PhasePlan para conteo.
public sealed record BossSummon(
    string Boss,
    GameStage UnlocksAfter,   // etapa minima para intentarlo
    string ItemName,
    string When,              // "noche" / "dia" / "siempre"
    string Where,             // "superficie" / "dungeon" / "inframundo" / "jungla" / ...
    int HotbarSlot1Based,     // configurable por ti
    int MinLife, int MinDef)
{
    public bool NeedsWorldTrigger => ItemName.StartsWith("Sin item", StringComparison.OrdinalIgnoreCase);
    public bool CanAttempt(GameStage stage, WorldTime clock) =>
        stage >= UnlocksAfter &&
        (When == "siempre" || (When == "noche" && !clock.IsDay) || (When == "dia" && clock.IsDay));
}

public static class BossCatalog
{
    // Orden para pasarse el juego. Ajusta HotbarSlot a tus invocadores reales.
    public static readonly BossSummon[] Order = new[]
    {
        // FASE 1 pre-hardmode
        new BossSummon("KingSlime", GameStage.Start, "Corona de slime", "siempre", "superficie", 4, 100, 5),
        new BossSummon("EyeOfCthulhu", GameStage.Start, "Ojo sospechoso", "noche", "superficie", 4, 200, 10),
        new BossSummon("EaterOfWorlds", GameStage.EyeOfCthulhu_Done, "Cebo de gusanos", "siempre", "corrupcion", 5, 200, 12),
        new BossSummon("QueenBee", GameStage.EyeOfCthulhu_Done, "Abeemination", "siempre", "jungla", 5, 200, 12),
        new BossSummon("Skeletron", GameStage.EvilBoss_Done, "Sin item: habla con el anciano del dungeon", "noche", "dungeon", 6, 300, 15),
        new BossSummon("Deerclops", GameStage.EvilBoss_Done, "Deer Thing", "noche", "nieve", 5, 300, 15),
        new BossSummon("WallOfFlesh", GameStage.Skeletron_Done, "Muneco vudu del guia", "siempre", "inframundo", 7, 400, 20),
        // FASE 2 hardmode
        new BossSummon("QueenSlime", GameStage.WallOfFlesh_Done, "Cristal de gelatina", "siempre", "hallow", 4, 400, 25),
        new BossSummon("TheDestroyer", GameStage.WallOfFlesh_Done, "Gusano mecanico", "noche", "superficie", 4, 400, 30),
        new BossSummon("TheTwins", GameStage.WallOfFlesh_Done, "Ojo mecanico", "noche", "superficie", 5, 400, 30),
        new BossSummon("SkeletronPrime", GameStage.WallOfFlesh_Done, "Calavera mecanica", "noche", "superficie", 6, 400, 30),
        new BossSummon("Plantera", GameStage.Mech_Done, "Sin item: pica el bulbo en la jungla", "siempre", "jungla", 5, 500, 40),
        new BossSummon("Golem", GameStage.Plantera_Done, "Celula lizarhard (altar)", "siempre", "templo", 6, 500, 50),
        new BossSummon("DukeFishron", GameStage.Golem_Done, "Gusano trufa (pesca)", "siempre", "oceano", 5, 500, 50),
        new BossSummon("EmpressOfLight", GameStage.Golem_Done, "Crisopa prismatica (liberala DE DIA = furiosa)", "dia", "hallow superficie", 5, 500, 55),
        new BossSummon("LunaticCultist", GameStage.Golem_Done, "Sin item: mata a los sectarios del dungeon", "siempre", "dungeon", 6, 500, 55),
        new BossSummon("MoonLord", GameStage.Lunatic_Done, "Senal celestial", "siempre", "superficie", 4, 500, 60),
    };

    public static BossSummon? NextFor(GameStage s) => Order.FirstOrDefault(b => b.UnlocksAfter <= s && StageOf(b.Boss) > s);

    private static GameStage StageOf(string boss) => boss switch
    {
        "KingSlime" => GameStage.EyeOfCthulhu_Done,
        "EyeOfCthulhu" => GameStage.EyeOfCthulhu_Done,
        "EaterOfWorlds" => GameStage.EvilBoss_Done,
        "QueenBee" => GameStage.EvilBoss_Done,
        "Skeletron" => GameStage.Skeletron_Done,
        "Deerclops" => GameStage.Skeletron_Done,
        "WallOfFlesh" => GameStage.WallOfFlesh_Done,
        "QueenSlime" => GameStage.Mech_Done,
        "TheDestroyer" => GameStage.Mech_Done,
        "TheTwins" => GameStage.Mech_Done,
        "SkeletronPrime" => GameStage.Mech_Done,
        "Plantera" => GameStage.Plantera_Done,
        "Golem" => GameStage.Golem_Done,
        "DukeFishron" => GameStage.Lunatic_Done,
        "EmpressOfLight" => GameStage.Lunatic_Done,
        "LunaticCultist" => GameStage.Lunatic_Done,
        "MoonLord" => GameStage.MoonLord_Done,
        _ => GameStage.Start
    };
}
