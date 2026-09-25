namespace TerraMind.State;

// Catalogo: que item invoca a quien, cuando/donde. El bot lo usa para invocar solo.
// slotHint: hotbar 1-10 donde TU pones el invocador (luego lo aprende).
public sealed record BossSummon(
    string Boss,
    GameStage UnlocksAfter,   // etapa minima para intentarlo
    string ItemName,
    string When,              // "noche" / "dia" / "siempre"
    string Where,             // "superficie" / "dungeon" / "inframundo" / "jungla"
    int HotbarSlot1Based,     // configurable por ti
    int MinLife, int MinDef)
{
    public bool CanAttempt(GameStage stage, WorldTime clock) =>
        stage >= UnlocksAfter &&
        (When == "siempre" || (When == "noche" && !clock.IsDay) || (When == "dia" && clock.IsDay));
}

public static class BossCatalog
{
    // Orden para pasarse el juego. Ajusta HotbarSlot a tus invocadores reales.
    public static readonly BossSummon[] Order = new[]
    {
        new BossSummon("EyeOfCthulhu", GameStage.Start, "Ojo sospechoso", "noche", "superficie", 4, 200, 10),
        new BossSummon("EaterOfWorlds", GameStage.EyeOfCthulhu_Done, "Cebo de gusanos", "siempre", "corrupcion", 5, 200, 12),
        new BossSummon("Skeletron", GameStage.EvilBoss_Done, "Hablar anciano dungeon", "noche", "dungeon", 6, 300, 15),
        new BossSummon("WallOfFlesh", GameStage.Skeletron_Done, "Muneco vudu del guia", "siempre", "inframundo", 7, 400, 20),
        new BossSummon("TheDestroyer", GameStage.WallOfFlesh_Done, "Gusano mecanico", "noche", "superficie", 4, 400, 30),
        new BossSummon("Plantera", GameStage.Mech_Done, "Bulbo (picar en jungla)", "siempre", "jungla", 5, 500, 40),
        new BossSummon("Golem", GameStage.Plantera_Done, "Celula lizarhard (altar)", "siempre", "templo", 6, 500, 50),
        new BossSummon("MoonLord", GameStage.Lunatic_Done, "Senal celestial", "siempre", "superficie", 4, 500, 60),
    };

    public static BossSummon? NextFor(GameStage s) => Order.FirstOrDefault(b => b.UnlocksAfter <= s && StageOf(b.Boss) > s);

    private static GameStage StageOf(string boss) => boss switch
    {
        "EyeOfCthulhu" => GameStage.EyeOfCthulhu_Done,
        "EaterOfWorlds" => GameStage.EvilBoss_Done,
        "Skeletron" => GameStage.Skeletron_Done,
        "WallOfFlesh" => GameStage.WallOfFlesh_Done,
        "TheDestroyer" => GameStage.Mech_Done,
        "Plantera" => GameStage.Plantera_Done,
        "Golem" => GameStage.Golem_Done,
        "MoonLord" => GameStage.MoonLord_Done,
        _ => GameStage.Start
    };
}
