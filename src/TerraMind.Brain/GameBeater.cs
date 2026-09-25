using TerraMind.State;

namespace TerraMind.Brain;

// Farmeo autonomo por etapa: que matar, cuantas veces, cuando parar.
// Sin memoria real aun: cuenta por intentos/tiempo; con memoria contara drops/bosses.
public sealed class FarmManager
{
    public sealed record FarmJob(string Target, int KillsWanted, int KillsDone, string Reason)
    {
        public bool Done => KillsDone >= KillsWanted;
    }

    public FarmJob? Current { get; private set; }

    public FarmJob PlanFor(GameStage stage) => stage switch
    {
        GameStage.Start => new("EyeOfCthulhu", 3, 0, "oro + pico carmesi/sombra"),
        GameStage.EyeOfCthulhu_Done => new("EvilBiome", 5, 0, "escamas/muestras para armadura"),
        GameStage.EvilBoss_Done => new("Skeletron", 2, 0, "acceso dungeon + huesos"),
        GameStage.Skeletron_Done => new("Underworld", 8, 0, "piedra infernal antes del muro"),
        GameStage.WallOfFlesh_Done => new("Mimics/Hallowed", 10, 0, "daedalus + emblemas hardmode"),
        GameStage.Mech_Done => new("Jungle", 8, 0, "frutas de vida a 500 + bulbos"),
        GameStage.Plantera_Done => new("Golem", 3, 0, "pico + accesorios templo"),
        GameStage.Golem_Done => new("Pilares", 4, 0, "fragmentos para senal"),
        _ => new("MoonLord", 1, 0, "fin del juego")
    };

    public void Start(GameStage s) => Current = PlanFor(s);
    public void AddKill() { if (Current is not null) Current = Current with { KillsDone = Current.KillsDone + 1 }; }
}

// GameBeater: mira progresion -> farmea -> invoca jefe -> repite hasta Moon Lord.
// Estados: Farm -> Invocar -> Pelear -> VerificarMuerte -> Avanzar.
public sealed class GameBeater
{
    public enum BeaterState { Farm, Summon, Fight, CheckKill, Done }
    public BeaterState State { get; private set; } = BeaterState.Farm;
    public ProgressionState Progress { get; private set; } = ProgressionState.Fresh();
    public FarmManager Farm { get; } = new();
    public List<string> Log { get; } = new();

    public void Load(ProgressionState p) { Progress = p; Farm.Start(p.Stage); State = BeaterState.Farm; }

    public void ReportKill(string boss)
    {
        var next = Progress.Stage switch
        {
            GameStage.Start when boss.Contains("Eye") => GameStage.EyeOfCthulhu_Done,
            GameStage.EyeOfCthulhu_Done when boss.Contains("Eater") || boss.Contains("Brain") => GameStage.EvilBoss_Done,
            GameStage.EvilBoss_Done when boss.Contains("Skeletron") => GameStage.Skeletron_Done,
            GameStage.Skeletron_Done when boss.Contains("Wall") => GameStage.WallOfFlesh_Done,
            GameStage.WallOfFlesh_Done when boss.Contains("Destroyer") || boss.Contains("Twins") || boss.Contains("Prime") => GameStage.Mech_Done,
            GameStage.Mech_Done when boss.Contains("Plantera") => GameStage.Plantera_Done,
            GameStage.Plantera_Done when boss.Contains("Golem") => GameStage.Golem_Done,
            GameStage.Golem_Done when boss.Contains("Lunatic") => GameStage.Lunatic_Done,
            GameStage.Lunatic_Done when boss.Contains("Moon") => GameStage.MoonLord_Done,
            _ => Progress.Stage
        };
        Progress = Progress with { Stage = next, BossesKilledTotal = Progress.BossesKilledTotal + 1, LastBoss = boss, Updated = DateTime.Now, Hardmode = next >= GameStage.WallOfFlesh_Done };
        Farm.Start(next);
        State = next == GameStage.MoonLord_Done ? BeaterState.Done : BeaterState.Farm;
        Log.Add($"Kill {boss} -> {next}. Siguiente: {Progress.NextGoal}");
    }

    // Decide cada tick. trySummon: selecciona slot + click. farmTick: devuelve true si hubo kill.
    public string Tick(WorldTime clock, Func<BossSummon, bool> trySummon, Func<bool> farmTick)
    {
        if (Progress.GameBeaten) { State = BeaterState.Done; return "JUEGO PASADO"; }
        var next = BossCatalog.NextFor(Progress.Stage);

        switch (State)
        {
            case BeaterState.Farm:
                if (Farm.Current is null) Farm.Start(Progress.Stage);
                if (farmTick()) { Farm.AddKill(); Log.Add($"Farm {Farm.Current?.Target} {Farm.Current?.KillsDone}/{Farm.Current?.KillsWanted}"); }
                if (Farm.Current?.Done == true)
                {
                    State = BeaterState.Summon;
                    return $"Farm listo, a invocar {next?.Boss}";
                }
                return $"Farmeando {Farm.Current?.Target} {Farm.Current?.KillsDone}/{Farm.Current?.KillsWanted}";

            case BeaterState.Summon:
                if (next is null) return "Sin siguiente jefe";
                if (!next.CanAttempt(Progress.Stage, clock))
                    return $"Espera invocacion {next.Boss}: {next.When} en {next.Where} (ahora {clock.Label})";
                State = BeaterState.Fight;
                bool sent = trySummon(next);
                return sent ? $"Invocado {next.Boss} con {next.ItemName} (slot {next.HotbarSlot1Based})" : $"Fallo al invocar {next.Boss}";

            case BeaterState.Fight:
                // El AdaptiveBrain lleva la pelea; aqui solo espera kill o muerte.
                // En test, farmTick()==true simula kill.
                if (farmTick()) { State = BeaterState.CheckKill; return "Parece muerto, verificando..."; }
                return $"Peleando {next?.Boss} (esquiva + dps por clase)";

            case BeaterState.CheckKill:
                // Con memoria real: Main.npc muerto + mensaje. Ahora: lo confirma el llamador.
                return "Confirma kill con --report-kill Nombre";

            default: return "Fin";
        }
    }
}
