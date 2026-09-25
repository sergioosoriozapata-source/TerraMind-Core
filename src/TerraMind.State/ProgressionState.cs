namespace TerraMind.State;

// Progresion para pasarse el juego solo. Orden Vanilla:
// Slime King(opt) -> EoC -> EoW/BoC -> Skeletron -> WoF [muro] -> Hardmode ->
// Destroyer/Twins/Prime -> Plantera -> Golem -> Lunatic -> Moon Lord [fin].
public enum GameStage
{
    Start,
    EyeOfCthulhu_Done,
    EvilBoss_Done,      // EoW o BoC
    Skeletron_Done,
    WallOfFlesh_Done,   // entra Hardmode
    Mech_Done,          // los 3 mecanicos
    Plantera_Done,
    Golem_Done,
    Lunatic_Done,
    MoonLord_Done       // juego pasado
}

public sealed record ProgressionState(
    GameStage Stage,
    int BossesKilledTotal,
    bool Hardmode,
    int MaxLifeSeen,    // 400 pre-WoF, 500 hardmode
    string LastBoss,
    DateTime Updated)
{
    public bool GameBeaten => Stage == GameStage.MoonLord_Done;
    public string NextGoal => Stage switch
    {
        GameStage.Start => "Eye of Cthulhu (ojo sospechoso, de noche)",
        GameStage.EyeOfCthulhu_Done => "Eater/Brain (gusano/vertebra en corrupcion/carmesi)",
        GameStage.EvilBoss_Done => "Skeletron (anciano dungeon, de noche)",
        GameStage.Skeletron_Done => "Muro carnoso (muneco vudu en inframundo)",
        GameStage.WallOfFlesh_Done => "Mecanicos (items mecanicos, de noche)",
        GameStage.Mech_Done => "Plantera (bulbo en jungla)",
        GameStage.Plantera_Done => "Golem (celula en templo)",
        GameStage.Golem_Done => "Lunatist (altar, despues pilares)",
        GameStage.Lunatic_Done => "Moon Lord (senal celestial)",
        GameStage.MoonLord_Done => "JUEGO PASADO",
        _ => "?"
    };

    public static ProgressionState Fresh() => new(GameStage.Start, 0, false, 100, "", DateTime.Now);
}
