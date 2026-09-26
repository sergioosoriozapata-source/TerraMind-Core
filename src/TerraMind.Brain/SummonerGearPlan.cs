using TerraMind.State;

namespace TerraMind.Brain;

// Plan BiS Invocador PRE-HARDMODE con lo que el jugador TIENE (nombres reales del juego).
// No hardcodea IDs: todo Contains sobre inventory + equipped + buffs.
// Cada faltante trae DONDE farmearlo (guia del usuario) y si es MANUAL/ASISTIDO.
public sealed record GearNeed(string Slot, string? Have, string Missing, string FarmHint, bool Manual);

public sealed record GearPlan(
    GearNeed[] Needs,
    string[] EquipNow,      // en mochila -> ponlo ya
    string ArmorSet,        // "Bee x3" / "Obsidian x2" / "Flinx x1" / "mixto/nada"
    int Score, int MaxScore)
{
    public double Pct => MaxScore == 0 ? 0 : Math.Round(100.0 * Score / MaxScore);
}

public static class SummonerGearPlan
{
    public static GearPlan Build(LiveSnapshot snap, Dictionary<int, string> buffNames)
    {
        var inv = snap.Hotbar.Concat(snap.Inventory).Where(s => !s.IsEmpty).ToList();
        var eq = (snap.Equipped ?? Array.Empty<ItemSlot>()).Where(s => !s.IsEmpty).ToList();
        string? Get(IEnumerable<ItemSlot> l, string pat) =>
            l.FirstOrDefault(s => NameOf(s).Contains(pat, StringComparison.OrdinalIgnoreCase))?.Role;
        static string NameOf(ItemSlot s) => s.Role.Split('x')[0];

        var needs = new List<GearNeed>();
        var equip = new List<string>();
        int score = 0, max = 0;
        void Need(string slot, string[] pats, string missing, string farm, bool manual = false)
        {
            max++;
            var haveEq = pats.Select(p => Get(eq, p)).FirstOrDefault(x => x is not null);
            var haveInv = pats.Select(p => Get(inv, p)).FirstOrDefault(x => x is not null);
            if (haveEq is not null) { score++; needs.Add(new GearNeed(slot, haveEq, "", "", false)); }
            else if (haveInv is not null) { needs.Add(new GearNeed(slot, null, missing, "lo TIENES en mochila -> equipalo", false)); equip.Add(haveInv); }
            else needs.Add(new GearNeed(slot, null, missing, farm, manual));
        }

        // ARMADURA (set cuenta en equipped)
        int bee = eq.Count(s => NameOf(s).Contains("Bee", StringComparison.OrdinalIgnoreCase) && !NameOf(s).Contains("Knees", StringComparison.OrdinalIgnoreCase));
        int obs = eq.Count(s => NameOf(s).Contains("Obsidian", StringComparison.OrdinalIgnoreCase));
        int flx = eq.Count(s => NameOf(s).Contains("Flinx", StringComparison.OrdinalIgnoreCase));
        string set = bee >= 2 ? $"Bee x{bee}" : obs >= 2 ? $"Obsidian x{obs}" : flx >= 1 ? $"Flinx x{flx}+mix" : "mixto/nada";
        max++;
        if (bee >= 2 || obs >= 2) { score++; needs.Add(new GearNeed("Armadura", set, "", "", false)); }
        else needs.Add(new GearNeed("Armadura", bee + obs + flx > 0 ? set : null, "Bee x2+ o Obsidian x2+", "Abeja Reina (cera, colmenas jungla) o Obsidiana (lava+agua) + Seda (telaranas) + Escama/Sangre (evil YA caido)", false));

        // BACULOS (hotbar/inv)
        Need("Baculo(top)", new[] { "ImpStaff", "HornetStaff", "VampireFrog" }, "ImpStaff/HornetStaff", "Imp: piedra infernal (inframundo). Hornet: Abeja Reina");
        Need("Baculo(ya)", new[] { "FlinxStaff", "SlimeStaff" }, "FlinxStaff", "Flinx: bioma nieve/tundra (la tienes? mira inv)");

        // LATIGOS
        Need("Latigo(top)", new[] { "SpinalTap", "Snapthorn" }, "SpinalTap/Snapthorn", "SpinalTap: huesos+telarana (dungeon). Snapthorn: selva (vinas/aguijones/esporas)");
        Need("Latigo(ya)", new[] { "LeatherWhip", "ThornWhip", "Whip" }, "cualquier latigo", "Zoologa (cuero) o selva");

        // ACCESORIOS (equipped o mochila->equipar)
        Need("Movilidad", new[] { "HermesBoots", "LightningBoots", "Terraspark", "SpectreBoots", "Dunerider" }, "HermesBoots+", "Cofres superficie (si esta en mochila: equipalo)");
        Need("Dash/def", new[] { "ShieldofCthulhu", "EoCShield", "WormScarf", "BrainofConfusion" }, "Escudo Cthulhu / Bufanda", "EoC (ya caido) / Evil (ya caido)");
        Need("Salto", new[] { "CloudinaBottle", "BlizzardinaBottle", "Horseshoe", "Balloon" }, "Cloud in a Bottle+", "Cofres / pesca (la tienes? mira inv)");
        Need("Espejo", new[] { "MagicMirror", "IceMirror", "RecallPotion" }, "Espejo/Recall", "Cofres (IceMirror en inv?) o pociones recall");

        // BUFFS (nombres reales de buffs activos)
        var activeBuffs = snap.Buffs.Select(b => buffNames.TryGetValue(b, out var n) ? n : "").ToList();
        bool HasB(string pat) => activeBuffs.Any(b => b.Contains(pat, StringComparison.OrdinalIgnoreCase));
        max++;
        if (HasB("Summon")) { score++; needs.Add(new GearNeed("Pocion invocador", "Summoning activa", "", "", false)); }
        else needs.Add(new GearNeed("Pocion invocador", null, "Summoning Potion (+1 subdito)", "Pesca (MANUAL: el bot aun no pesca)", true));
        max++;
        if (HasB("Bewitch")) { score++; needs.Add(new GearNeed("Mesa", "Bewitched activa", "", "", false)); }
        else needs.Add(new GearNeed("Mesa", null, "Bewitching Table (+1 gratis)", "Dungeon: click tu una vez (ASISTIDO)", true));
        max++;
        if (HasB("WellFed") || HasB("Stuffed") || HasB("Exquisitely")) { score++; needs.Add(new GearNeed("Comida", "WellFed activa", "", "", false)); }
        else needs.Add(new GearNeed("Comida", null, "Comida tier medio/alto", "Cocina/ollas: lleva siempre", false));

        return new GearPlan(needs.ToArray(), equip.Distinct().ToArray(), set, score, max);
    }

    // Gate Muro de Carne (invocador, con Sergio): armadura 2+ piezas, baculo tier, latigo, movilidad, pociones.
    public static (bool Ready, string Why) WallReadiness(LiveSnapshot snap, GearPlan plan)
    {
        var inv = snap.Hotbar.Concat(snap.Inventory).Where(s => !s.IsEmpty).ToList();
        int pots = inv.Where(s => s.Role.StartsWith("HealingPotion") || s.Role.StartsWith("LesserHealing")).Sum(s => s.Stack);
        var missing = plan.Needs.Where(n => n.Have is null && n.Missing.Length > 0 && !n.Manual).Select(n => n.Slot).ToArray();
        var must = new List<string>();
        if (plan.ArmorSet is "mixto/nada") must.Add("armadura Bee/Obsidian");
        if (!plan.Needs.Any(n => n.Slot.StartsWith("Baculo") && n.Have is not null)) must.Add("baculo decente");
        if (!plan.Needs.Any(n => n.Slot.StartsWith("Latigo") && n.Have is not null)) must.Add("latigo");
        if (pots < 10) must.Add($"pociones ({pots}/10+)");
        if ((snap.MaxLife ?? 0) < 400) must.Add($"vida {(snap.MaxLife ?? 0)}/400");
        if (must.Count == 0) return (true, $"LISTO: {plan.ArmorSet}, latigo+baculo, {pots} pociones, {snap.MaxLife} vida. Puente en inframundo + a por el Muro.");
        return (false, $"FALTA: {string.Join(", ", must)}. {plan.Pct}% del plan.");
    }
}
