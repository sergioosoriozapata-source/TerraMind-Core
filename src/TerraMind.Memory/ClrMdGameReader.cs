using System.Diagnostics;
using Microsoft.Diagnostics.Runtime;
using TerraMind.State;

namespace TerraMind.Memory;

// Lee TODO el estado real del juego sin Cheat Engine ni offsets pegados:
// ClrMD abre el heap .NET y lee campos por NOMBRE ("statLife", "inventory"...).
// TryRead*: si un campo no existe en tu version, warning + sigue (confianza parcial).
public sealed class ClrMdGameReader : IDisposable
{
    private DataTarget? _dt;
    private ClrRuntime? _rt;
    private ClrAppDomain? _domain;
    public string LastError { get; private set; } = "";
    public Dictionary<int, string> ItemNames { get; } = new();
    public Dictionary<int, string> NpcNames { get; } = new();
    public Dictionary<int, string> BuffNames { get; } = new();

    private static readonly string[] DownedFlags = new[]
    {
        "downedBoss1", "downedBoss2", "downedBoss3",
        "downedQueenBee", "downedSlimeKing", "downedDeerclops", "hardMode",
        "downedQueenSlime",
        "downedMechBoss1", "downedMechBoss2", "downedMechBoss3", "downedMechBossAny",
        "downedPlantBoss", "downedGolemBoss", "downedFishron",
        "downedEmpress", "downedHallowBoss",
        "downedHalloweenKing", "downedHalloweenTree", "downedChristmasIceQueen",
        "downedChristmasSantank", "downedChristmasTree", "downedMartians",
        "downedAncientCultist", "downedLunatic", "downedMoonlord", "downedTowerSolar",
        "downedTowerVortex", "downedTowerNebula", "downedTowerStardust"
    };

    public bool Attach()
    {
        DisposeRuntime();
        LastError = "";
        var proc = Process.GetProcessesByName("Terraria").FirstOrDefault();
        if (proc is null) { LastError = "no hay proceso Terraria"; return false; }
        try
        {
            try { _dt = DataTarget.AttachToProcess(proc.Id, false); }
            catch (Exception ex1)
            {
                try { _dt = DataTarget.CreateSnapshotAndAttach(proc.Id); }
                catch (Exception ex2) { LastError = $"attach: {ex1.GetType().Name}: {ex1.Message} | snapshot: {ex2.GetType().Name}: {ex2.Message}"; DisposeRuntime(); return false; }
            }
            var vers = _dt.ClrVersions.ToList();
            if (vers.Count == 0) { LastError = "sin CLR versions (no es .NET o sin acceso)"; DisposeRuntime(); return false; }
            var ver = vers[0];
            LastError = $"CLR detectado: {ver.Version} flavor={ver.Flavor}";
            _rt = ver.CreateRuntime();
            _domain = _rt.AppDomains.FirstOrDefault();
            if (_domain is null) { LastError += " | sin AppDomains"; DisposeRuntime(); return false; }
            LoadItemNames();
            return true;
        }
        catch (Exception ex) { LastError = $"runtime: {ex.GetType().Name}: {ex.Message}"; DisposeRuntime(); return false; }
    }

    // Tabla ID->nombre leida DEL PROPIO JUEGO (IdDictionary._idToName).
    // Sin hardcodear nada: sirve para cualquier version.
    private void LoadItemNames()
    {
        ItemNames.Clear();
        NpcNames.Clear();
        BuffNames.Clear();
        try
        {
            foreach (var (tn, dict) in new[] { ("Terraria.ID.ItemID", ItemNames), ("Terraria.ID.NPCID", NpcNames), ("Terraria.ID.BuffID", BuffNames) })
            {
                try
                {
                    var t = _rt!.Heap.GetTypeByName(tn);
                    var sf = t?.GetStaticFieldByName("Search");
                    if (sf is null) continue;
                    var so = sf.ReadObject(_domain!);
                    if (so.IsNull || !so.TryReadObjectField("_idToName", out ClrObject map) || map.IsNull) { LastError += $" | {tn}:sin _idToName"; continue; }
                    // Dictionary<int,string> (.NET Framework: entries/count SIN guion; Core: CON guion).
                    ClrObject eo = default;
                    bool hasEntries = map.TryReadObjectField("_entries", out eo) || map.TryReadObjectField("entries", out eo);
                    if (!hasEntries || !eo.IsArray) { LastError += $" | {tn}:sin entries"; continue; }
                    int count = 20000;
                    if (!map.TryReadField("_count", out count)) map.TryReadField("count", out count);
                    var entries = eo.AsArray();
                    var et = entries.Type?.ComponentType;
                    int okE = 0, posK = 0, okS = 0;
                    string etFields = et is null ? "?" : string.Join("/", et.Fields.Select(f => f.Name));
                    for (int i = 0; i < Math.Min(count * 2 + 10, entries.Length); i++)
                    {
                        try
                        {
                            var e = entries.GetStructValue(i);
                            okE++;
                            int key = e.ReadField<int>("key");
                            if (key <= 0 || dict.ContainsKey(key)) continue;
                            posK++;
                            var vs = e.ReadObjectField("value");
                            if (vs.IsNull) continue;
                            string name = vs.AsString(128);
                            if (!string.IsNullOrEmpty(name)) { dict[key] = name; okS++; }
                        }
                        catch { }
                    }
                    LastError += $" | {tn}:entries={entries.Length},etype=[{etFields}],structOk={okE},posKey={posK},strOk={okS}";
                }
                catch { }
            }
            LastError += $" | names:item={ItemNames.Count},npc={NpcNames.Count},buff={BuffNames.Count}";
        }
        catch (Exception ex) { LastError += $" | names fallo: {ex.Message}"; }
    }

    public string ItemName(int type) => ItemNames.TryGetValue(type, out var n) ? n : $"item:{type}";
    public string NpcName(int type) => NpcNames.TryGetValue(type, out var n) ? n : $"NPC:{type}";
    public string BuffName(int type) => BuffNames.TryGetValue(type, out var n) ? n : $"buff:{type}";

    // Sonda diagnostica: lista campos statics/instancia disponibles en esta version.
    // Sirve para encontrar donde estan nombres de items y selectedItem sin adivinar.
    public string Probe()
    {
        var sb = new System.Text.StringBuilder();
        if (_rt is null) return "sin runtime";
        var heap = _rt.Heap;
        foreach (var tn in new[] { "Terraria.Main", "Terraria.Lang", "Terraria.ID.ItemID", "Terraria.ID.NPCID", "Terraria.ID.BuffID" })
        {
            try
            {
                var t = heap.GetTypeByName(tn);
                if (t is null) { sb.AppendLine($"{tn}: NO EXISTE"); continue; }
                var names = t.StaticFields.Select(f => f.Name).ToList();
                sb.AppendLine($"{tn}: {names.Count} statics: {string.Join(",", names.Take(60))}");
            }
            catch (Exception ex) { sb.AppendLine($"{tn}: {ex.Message}"); }
        }
        try
        {
            var p = heap.GetTypeByName("Terraria.Player");
            if (p is not null)
            {
                var inst = p.Fields.Select(f => f.Name).Where(n =>
                    n.Contains("elect", StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("slot", StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("ing", StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("inion", StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("buff", StringComparison.OrdinalIgnoreCase)).ToList();
                sb.AppendLine($"Player matching: {string.Join(",", inst)}");
            }
        }
        catch (Exception ex) { sb.AppendLine("Player: " + ex.Message); }
        try
        {
            // Estructura interna del buscador ID<->nombre (para resolver nombres sin hardcodear).
            var iid = heap.GetTypeByName("Terraria.ID.ItemID");
            var dom = _domain!;
            var sf = iid?.GetStaticFieldByName("Search");
            if (sf is not null)
            {
                var so = sf.ReadObject(dom);
                sb.AppendLine($"ItemID.Search type={so.Type?.Name} addr={so.Address}");
                foreach (var f in so.Type?.Fields ?? Enumerable.Empty<ClrInstanceField>())
                    sb.AppendLine($"  field {f.Name} : {f.Type?.Name}");
            }
            var sets = heap.GetTypeByName("Terraria.ID.ItemID+Sets");
            if (sets is not null)
            {
                var names = sets.StaticFields.Select(f => f.Name).Where(n =>
                    n.Contains("Whip", StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("Summon", StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("Staff", StringComparison.OrdinalIgnoreCase)).ToList();
                sb.AppendLine($"ItemID.Sets whip/summon/staff: {string.Join(",", names)}");
            }
            else sb.AppendLine("ItemID.Sets: NO EXISTE");
            var item = heap.GetTypeByName("Terraria.Item");
            if (item is not null)
            {
                var names = item.Fields.Select(f => f.Name).Where(n =>
                    n is "mana" or "damage" or "buffType" or "consumable" or "shoot" or "summon" or "DamageType" or "healLife" or "maxStack").ToList();
                sb.AppendLine($"Item fields clave: {string.Join(",", names)}");
            }
        }
        catch (Exception ex) { sb.AppendLine("Search: " + ex.Message); }
        return sb.ToString();
    }

    public LiveSnapshot Scan()
    {
        var warnings = new List<string>();
        var downed = new Dictionary<string, bool>();
        int ok = 0, total = 0;
        var emptyBoss = Array.Empty<BossState>();

        if (_rt is null || _domain is null)
            return new LiveSnapshot(false, "simulado", 0, DateTime.Now,
                Life: null, MaxLife: null, Mana: null, MaxMana: null, Defense: null,
                PosX: null, PosY: null, VelX: null, VelY: null,
                WingTime: null, WingTimeMax: null, NumMinions: null, MaxMinions: null, ActiveSlot: null,
                Hotbar: Array.Empty<ItemSlot>(), Inventory: Array.Empty<ItemSlot>(), Buffs: Array.Empty<int>(),
                IsDay: null, WorldTime: null, WorldName: null, PlayerName: null,
                Downed: downed, ActiveBosses: emptyBoss, LiveProjectiles: Array.Empty<ProjectileState>(), Warnings: warnings);

        try
        {
            var heap = _rt.Heap;
            var main = heap.GetTypeByName("Terraria.Main");
            total++;
            if (main is null) { warnings.Add("Tipo Terraria.Main no encontrado"); return Fail(warnings, downed); }
            ok++;

            bool? isDay = SBool(main, "dayTime", warnings, ref ok, ref total);
            double? time = SDouble(main, "time", warnings, ref ok, ref total);
            int myPlayer = SInt(main, "myPlayer", warnings, ref ok, ref total) ?? 0;

            ClrObject player = default;
            bool hasPlayer = false;
            try
            {
                var arr = SObj(main, "player");
                if (arr.IsArray)
                {
                    var a = arr.AsArray();
                    if (myPlayer >= 0 && myPlayer < a.Length)
                    {
                        player = a.GetObjectValue(myPlayer);
                        hasPlayer = !player.IsNull;
                    }
                }
                total++; if (hasPlayer) ok++; else warnings.Add("player[] vacio o indice fuera");
            }
            catch (Exception ex) { total++; warnings.Add("player: " + ex.Message); }

            int? life = FInt(player, hasPlayer, "statLife", warnings, ref ok, ref total);
            int? maxLife = FInt(player, hasPlayer, "statLifeMax", warnings, ref ok, ref total)
                        ?? FInt(player, hasPlayer, "statLifeMax2", warnings, ref ok, ref total);
            int? mana = FInt(player, hasPlayer, "statMana", warnings, ref ok, ref total);
            int? maxMana = FInt(player, hasPlayer, "statManaMax", warnings, ref ok, ref total)
                       ?? FInt(player, hasPlayer, "statManaMax2", warnings, ref ok, ref total);
            int? def = FInt(player, hasPlayer, "statDefense", warnings, ref ok, ref total);
            int? wing = FInt(player, hasPlayer, "wingTime", warnings, ref ok, ref total);
            int? wingMax = FInt(player, hasPlayer, "wingTimeMax", warnings, ref ok, ref total);
            int? numMin = FInt(player, hasPlayer, "numMinions", warnings, ref ok, ref total);
            int? maxMin = FInt(player, hasPlayer, "maxMinions", warnings, ref ok, ref total);
            int? slot = FInt(player, hasPlayer, "selectedItem", warnings, ref ok, ref total);
            var (px, py) = FVec(player, hasPlayer, "position", warnings, ref ok, ref total);
            var (vx, vy) = FVec(player, hasPlayer, "velocity", warnings, ref ok, ref total);

            var hotbar = FItems(player, hasPlayer, "inventory", 0, 10, warnings, ref ok, ref total);
            var rest = FItems(player, hasPlayer, "inventory", 10, 49, warnings, ref ok, ref total);
            var buffs = FInts(player, hasPlayer, "buffType", 22, warnings, ref ok, ref total);

            var npcType = heap.GetTypeByName("Terraria.NPC");
            foreach (var flag in DownedFlags)
            {
                try
                {
                    var f = npcType?.GetStaticFieldByName(flag) ?? main.GetStaticFieldByName(flag);
                    if (f is null) continue;
                    downed[flag] = f.Read<bool>(_domain);
                }
                catch { }
            }

            var bosses = FBosses(main, warnings);
            var projs = FProjectiles(main, warnings);
            double conf = total == 0 ? 0 : Math.Round((double)ok / total, 2);
            return new LiveSnapshot(true, "clrmd-live", conf, DateTime.Now,
                life, maxLife, mana, maxMana, def, px, py, vx, vy, wing, wingMax,
                numMin, maxMin, slot, hotbar, rest, buffs,
                isDay, time, null, null, downed, bosses.ToArray(), projs.ToArray(), warnings);
        }
        catch (Exception ex)
        {
            warnings.Add("Scan fallo: " + ex.Message);
            return Fail(warnings, downed);
        }
    }

    private static LiveSnapshot Fail(List<string> w, Dictionary<string, bool> d) =>
        new(false, "clrmd-live", 0, DateTime.Now,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            Array.Empty<ItemSlot>(), Array.Empty<ItemSlot>(), Array.Empty<int>(),
            null, null, null, null, d, Array.Empty<BossState>(), Array.Empty<ProjectileState>(), w);

    // ---- primitivas por nombre ----
    private int? FInt(ClrObject o, bool has, string f, List<string> w, ref int ok, ref int total)
    {
        total++;
        if (!has) { w.Add($"Sin jugador para {f}"); return null; }
        try { if (o.TryReadField(f, out int v)) { ok++; return v; } w.Add($"Player.{f} no existe"); return null; }
        catch (Exception ex) { w.Add($"Player.{f}: {ex.Message}"); return null; }
    }

    private (float?, float?) FVec(ClrObject o, bool has, string f, List<string> w, ref int ok, ref int total)
    {
        total++;
        if (!has) { w.Add($"Sin jugador para {f}"); return (null, null); }
        try
        {
            if (o.TryReadValueTypeField(f, out ClrValueType v))
            {
                try { float x = v.ReadField<float>("X"); float y = v.ReadField<float>("Y"); ok++; return (x, y); }
                catch { }
            }
            w.Add($"Player.{f} no existe"); return (null, null);
        }
        catch (Exception ex) { w.Add($"Player.{f}: {ex.Message}"); return (null, null); }
    }

    private ItemSlot[] FItems(ClrObject o, bool has, string arr, int from, int count, List<string> w, ref int ok, ref int total)
    {
        var outp = new List<ItemSlot>();
        for (int i = 0; i < count; i++) outp.Add(ItemSlot.Empty(from + i));
        total++;
        if (!has) { w.Add($"Sin jugador para {arr}"); return outp.ToArray(); }
        try
        {
            if (!o.TryReadObjectField(arr, out ClrObject ao) || !ao.IsArray) { w.Add($"Player.{arr} no es array"); return outp.ToArray(); }
            var a = ao.AsArray();
            for (int i = from; i < from + count && i < a.Length; i++)
            {
                try
                {
                    var item = a.GetObjectValue(i);
                    if (item.IsNull) continue;
                    int type = item.TryReadField("type", out int t) ? t : 0;
                    int stack = item.TryReadField("stack", out int s) ? s : 0;
                    if (type > 0 && stack > 0)
                    {
                        int dmg = item.TryReadField("damage", out int d) ? d : -1;
                        int mana = item.TryReadField("mana", out int m) ? m : -1;
                        int bt = item.TryReadField("buffType", out int b) ? b : -1;
                        bool cons = item.TryReadField("consumable", out bool c) && c;
                        bool summ = item.TryReadField("summon", out bool sm) && sm;
                        outp[i - from] = new ItemSlot(i, type, stack, RoleOf(type, stack, dmg, mana, bt, cons, summ));
                    }
                }
                catch { }
            }
            ok++;
            return outp.ToArray();
        }
        catch (Exception ex) { w.Add($"{arr}: {ex.Message}"); return outp.ToArray(); }
    }

    private int[] FInts(ClrObject o, bool has, string arr, int max, List<string> w, ref int ok, ref int total)
    {
        total++;
        if (!has) return Array.Empty<int>();
        try
        {
            if (!o.TryReadObjectField(arr, out ClrObject ao) || !ao.IsArray) { w.Add($"Player.{arr} no es array"); return Array.Empty<int>(); }
            var a = ao.AsArray();
            var list = new List<int>();
            for (int i = 0; i < Math.Min(max, a.Length); i++)
                try { int v = a.GetValue<int>(i); if (v != 0) list.Add(v); } catch { }
            ok++;
            return list.ToArray();
        }
        catch (Exception ex) { w.Add($"{arr}: {ex.Message}"); return Array.Empty<int>(); }
    }

    private int? SInt(ClrType main, string f, List<string> w, ref int ok, ref int total)
    {
        total++;
        try { var sf = main.GetStaticFieldByName(f); if (sf is null) { w.Add($"Main.{f} no existe"); return null; } int v = sf.Read<int>(_domain!); ok++; return v; }
        catch (Exception ex) { w.Add($"Main.{f}: {ex.Message}"); return null; }
    }

    private bool? SBool(ClrType main, string f, List<string> w, ref int ok, ref int total)
    {
        total++;
        try { var sf = main.GetStaticFieldByName(f); if (sf is null) { w.Add($"Main.{f} no existe"); return null; } bool v = sf.Read<bool>(_domain!); ok++; return v; }
        catch (Exception ex) { w.Add($"Main.{f}: {ex.Message}"); return null; }
    }

    private double? SDouble(ClrType main, string f, List<string> w, ref int ok, ref int total)
    {
        total++;
        try { var sf = main.GetStaticFieldByName(f); if (sf is null) { w.Add($"Main.{f} no existe"); return null; } double v = sf.Read<double>(_domain!); ok++; return v; }
        catch (Exception ex) { w.Add($"Main.{f}: {ex.Message}"); return null; }
    }

    private ClrObject SObj(ClrType main, string f)
    {
        var sf = main.GetStaticFieldByName(f);
        if (sf is null) return default;
        return sf.ReadObject(_domain!);
    }

    private List<BossState> FBosses(ClrType main, List<string> w)
    {
        var list = new List<BossState>();
        try
        {
            ClrObject ao;
            try { ao = SObj(main, "npc"); } catch (Exception ex) { w.Add("Main.npc: " + ex.Message); return list; }
            if (!ao.IsArray) return list;
            var a = ao.AsArray();
            for (int i = 0; i < Math.Min(200, a.Length); i++)
            {
                try
                {
                    var npc = a.GetObjectValue(i);
                    if (npc.IsNull || !npc.TryReadField("active", out bool act) || !act) continue;
                    if (!npc.TryReadField("type", out int type) || type < 10) continue;
                    npc.TryReadField("life", out int life);
                    npc.TryReadField("lifeMax", out int maxLife);
                    npc.TryReadField("damage", out int dmg);
                    float px = 0, py = 0, vx = 0, vy = 0;
                    try
                    {
                        if (npc.TryReadValueTypeField("position", out ClrValueType p))
                        { px = p.ReadField<float>("X"); py = p.ReadField<float>("Y"); }
                    }
                    catch { }
                    try
                    {
                        if (npc.TryReadValueTypeField("velocity", out ClrValueType v))
                        { vx = v.ReadField<float>("X"); vy = v.ReadField<float>("Y"); }
                    }
                    catch { }
                    var ai = new float[4];
                    try
                    {
                        if (npc.TryReadObjectField("ai", out ClrObject aio) && aio.IsArray)
                        {
                            var aa = aio.AsArray();
                            for (int k = 0; k < 4 && k < aa.Length; k++) ai[k] = aa.GetValue<float>(k);
                        }
                    }
                    catch { }
                    if (life > 500 || maxLife > 1000 || dmg > 20)
                        list.Add(new BossState
                        {
                            Type = type, Name = NpcName(type), Life = life, MaxLife = maxLife,
                            PositionX = px, PositionY = py, VelocityX = vx, VelocityY = vy,
                            Ai = ai, Damage = dmg, Active = true
                        });
                }
                catch { }
            }
        }
        catch (Exception ex) { w.Add("npc: " + ex.Message); }
        return list;
    }

    // Proyectiles hostiles en vivo (Main.projectile[1000]): para esquivar dashes y tiros.
    // Solo hostiles con dano>0, tope 100. Todo TryRead: si tu version cambia nombres, lista vacia.
    private List<ProjectileState> FProjectiles(ClrType main, List<string> w)
    {
        var list = new List<ProjectileState>();
        try
        {
            ClrObject ao;
            try { ao = SObj(main, "projectile"); } catch (Exception ex) { w.Add("Main.projectile: " + ex.Message); return list; }
            if (!ao.IsArray) return list;
            var a = ao.AsArray();
            for (int i = 0; i < Math.Min(1000, a.Length) && list.Count < 100; i++)
            {
                try
                {
                    var p = a.GetObjectValue(i);
                    if (p.IsNull || !p.TryReadField("active", out bool act) || !act) continue;
                    if (!p.TryReadField("hostile", out bool hos) || !hos) continue;
                    p.TryReadField("type", out int type);
                    p.TryReadField("damage", out int dmg);
                    if (dmg <= 0) continue;
                    float px = 0, py = 0, vx = 0, vy = 0;
                    try
                    {
                        if (p.TryReadValueTypeField("position", out ClrValueType pp))
                        { px = pp.ReadField<float>("X"); py = pp.ReadField<float>("Y"); }
                    }
                    catch { }
                    try
                    {
                        if (p.TryReadValueTypeField("velocity", out ClrValueType vv))
                        { vx = vv.ReadField<float>("X"); vy = vv.ReadField<float>("Y"); }
                    }
                    catch { }
                    list.Add(new ProjectileState
                    {
                        Type = type, PositionX = px, PositionY = py,
                        VelocityX = vx, VelocityY = vy, Damage = dmg,
                        Hostile = true, Active = true,
                    });
                }
                catch { }
            }
        }
        catch (Exception ex) { w.Add("projectile: " + ex.Message); }
        return list;
    }

    // Rol real: nombre interno DEL JUEGO + campos del Item (-1 = campo ilegible).
    // BACULO = summon+buff | LATIGO = *Whip o summon sin buff | INVOCA-JEFE = invocadores conocidos o consumible plano.
    private string RoleOf(int type, int stack, int dmg, int mana, int buffType, bool consumable, bool summon)
    {
        string n = ItemName(type);
        bool staff = n.EndsWith("Staff", StringComparison.OrdinalIgnoreCase) && mana > 0;
        bool whip = n.EndsWith("Whip", StringComparison.OrdinalIgnoreCase);
        string tag = "mat";
        if ((summon && buffType > 0) || (dmg > 0 && mana > 0 && buffType > 0) || staff) tag = "BACULO";
        else if (whip || (summon && dmg > 0 && buffType <= 0)) tag = "LATIGO";
        else if (IsBossSummonName(n) || (consumable && dmg == 0 && mana == 0 && buffType == 0)) tag = "INVOCA-JEFE";
        else if (dmg > 0 && mana > 0) tag = "magia";
        else if (dmg > 0) tag = "arma";
        return $"{n}x{stack}[{tag}](d{dmg},m{mana},b{buffType}{(consumable ? ",cons" : "")}{(summon ? ",summ" : "")})";
    }

    private static bool IsBossSummonName(string n) =>
        n is "SuspiciousLookingEye" or "WormFood" or "BloodySpine" or "Abeemination"
            or "DeerThing" or "QueenSlimeCrystal" or "GelatinCrystal"
            or "GuideVoodooDoll" or "ClothierVoodooDoll" or "LizardPowerCell"
            or "NaughtyPresent" or "PumpkinMoonMedallion" or "SolarTablet" or "CelestialSigil"
            or "MechanicalEye" or "MechanicalWorm" or "MechanicalSkull"
        || n.Contains("VoodooDoll", StringComparison.OrdinalIgnoreCase);

    private void DisposeRuntime()
    {
        try { _rt?.Dispose(); } catch { }
        try { _dt?.Dispose(); } catch { }
        _rt = null; _dt = null; _domain = null;
    }

    public void Dispose() { DisposeRuntime(); }
}
