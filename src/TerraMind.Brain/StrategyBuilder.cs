using TerraMind.State;

namespace TerraMind.Brain;

// Estrategia a partir de lo REAL: snapshot ClrMD (vida, mana, inventario, buffs,
// jefes caidos, hora) + saves (nombres). No inventa IDs: clasifica por lo observado
// y propone slots concretos; el usuario confirma con --slots.
public sealed record GameStrategy(
    string PlayerLine,
    string WorldLine,
    Archetype SuggestedClass,
    string Reason,
    GameStage Stage,
    string NextGoal,
    int[] SuggestedSummonSlots, // 1-based hotbar
    int[] SuggestedBossSlots,
    string[] FarmGoals,
    string[] CraftChecklist,
    double Confidence);

public static class StrategyBuilder
{
    public static GameStrategy Build(LiveSnapshot snap, string playerName, string worldName, int[] userSlots)
    {
        var stage = snap.GuessStage();
        var next = new ProgressionState(stage, 0, stage >= GameStage.WallOfFlesh_Done, snap.MaxLife ?? 100, "", DateTime.Now).NextGoal;

        // Clase por recursos REALES: roles del hotbar mandan (nombres del propio juego).
        var nonEmpty = snap.Hotbar.Concat(snap.Inventory).Where(s => !s.IsEmpty).ToArray();
        var hotRoles = snap.Hotbar.Select(s => s.Role).ToArray();
        int staves = hotRoles.Count(r => r.Contains("BACULO"));
        int whips = hotRoles.Count(r => r.Contains("LATIGO"));
        var staffNames = snap.Hotbar.Where(s => s.Role.Contains("BACULO") || s.Role.Contains("LATIGO")).Select(s => s.Role).ToArray();
        Archetype cls;
        string reason;
        if (staves + whips >= 2 || (snap.NumMinions ?? 0) > 0 && (staves + whips) >= 1)
        { cls = Archetype.Summoner; reason = $"invocador: {string.Join(", ", staffNames)} + minions {snap.NumMinions}/{snap.MaxMinions}"; }
        else if ((snap.MaxMana ?? 20) >= 140 && (snap.Defense ?? 99) < 40) { cls = Archetype.Mage; reason = $"mana max {snap.MaxMana} + def {snap.Defense}"; }
        else if ((snap.Defense ?? 0) >= 40) { cls = Archetype.Melee; reason = $"defensa {snap.Defense}"; }
        else { cls = Archetype.Ranged; reason = "equipo inicial/neutro: arco + kite"; }

        // Slots: del INVENTARIO REAL (baculos / invoca-jefes), el usuario confirma con --slots.
        int[] foundStaves = snap.Hotbar.Where(s => s.Role.Contains("BACULO")).Select(s => s.Index + 1).ToArray();
        int[] foundWhips = snap.Hotbar.Where(s => s.Role.Contains("LATIGO")).Select(s => s.Index + 1).ToArray();
        int[] foundBoss = snap.Hotbar.Concat(snap.Inventory).Where(s => s.Role.Contains("INVOCA-JEFE")).Select(s => s.Index <= 9 ? s.Index + 1 : -1).Where(i => i > 0).Distinct().ToArray();
        int[] summonSlots = userSlots.Length > 0 ? userSlots : foundStaves.Concat(foundWhips).Distinct().ToArray();
        if (summonSlots.Length == 0) summonSlots = new[] { 1, 2, 3 };
        int[] bossSlots = foundBoss.Length > 0 ? foundBoss : new[] { 4 };

        var farm = new List<string>();
        var craft = new List<string>();
        int life = snap.MaxLife ?? 100;
        if ((snap.WingTimeMax ?? 0) <= 0) { craft.Add("ALAS o globo+herradura: sin alas no hay esquiva aerea"); }
        if ((snap.MaxMinions ?? 1) <= 2 && cls == Archetype.Summoner) { craft.Add("Mesa de invocador + armadura telarana/tiki para +minions"); }
        if (life < 400 && stage < GameStage.WallOfFlesh_Done) { farm.Add("Corazones de cristal hasta 400 vida"); craft.Add("Arena larga + fogatas + pociones piel de hierro"); }
        if ((snap.MaxMana ?? 20) < 200 && cls == Archetype.Mage) { farm.Add("Estrellas caidas (mana 200)"); }
        if ((snap.Defense ?? 0) < 15 && stage == GameStage.Start) { farm.Add("Ojo x3: oro + pico carmesi/sombra"); craft.Add("Armadura oro/platino + arco platino + botas"); }
        if (stage == GameStage.WallOfFlesh_Done) { farm.Add("Mimics sagrados + emblemas"); craft.Add("Alas + botas relampago + emblema de clase"); }
        if (stage >= GameStage.Mech_Done && life < 500) { farm.Add("Frutas de vida jungla hasta 500"); }
        farm.Add($"Jefe: {next}");

        double conf = Math.Round(snap.Confidence * 0.8 + (nonEmpty.Length > 0 ? 0.2 : 0), 2);
        return new GameStrategy(
            $"Jugador {(playerName ?? "?")} vida {snap.Life}/{snap.MaxLife} mana {snap.Mana}/{snap.MaxMana} def {snap.Defense} minions {snap.NumMinions}/{snap.MaxMinions} buffs[{string.Join(",", snap.Buffs)}] items!=0:{nonEmpty.Length}",
            $"Mundo {(worldName ?? "?")} hora {(snap.IsDay == true ? "dia" : snap.IsDay == false ? "noche" : "?")} t={snap.WorldTime}",
            cls, reason, stage, next, summonSlots, bossSlots,
            farm.ToArray(), craft.ToArray(), conf);
    }
}
