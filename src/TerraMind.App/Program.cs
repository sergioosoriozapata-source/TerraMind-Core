using TerraMind.Brain;
using TerraMind.Input;
using TerraMind.Memory;
using TerraMind.State;

// TerraMind-Core v0.3 invocador — sin externos, calibracion por intentos.
// Uso:
//   sin args                 : demo simulada
//   --live --ticks N         : juega con movimiento
//   --test-move              : mantiene D 3s
//   --summoner-test [--slots 1,2,3] : mira hora, prueba baculos e invoca solo
//   --beat-game [--stage Start] [--dry] : farmea + invoca jefes en orden hasta Moon Lord
//   --report-kill Nombre : avanza progresion (temporal hasta lectura real de muertes)
//   --scan [--slots 1,2,3] : lee TODO lo real (saves + heap ClrMD) y propone estrategia
//   --farm [--minutes 5] [--dry] : farmea solo (ataque+pociones+upkeep invocador, resync real)
//   --hunt <jefe> [--boss-slot N] [--minutes 10] [--dry] : invoca al jefe, pelea y detecta la kill
bool live = args.Contains("--live");
bool testMove = args.Contains("--test-move");
bool summonerTest = args.Contains("--summoner-test");
bool beatGame = args.Contains("--beat-game");
bool dryBeat = args.Contains("--dry");
bool scan = args.Contains("--scan");
bool probe = args.Contains("--probe");
bool farm = args.Contains("--farm");
string huntName = "";
for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--hunt") huntName = args[i + 1];
bool hunt = huntName.Length > 0;
double minutes = 5;
for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--minutes" && double.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var m)) minutes = m;
int bossSlotOverride = 0;
for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--boss-slot" && int.TryParse(args[i + 1], out var b)) bossSlotOverride = b;
int ticks = 600;
for (int i = 0; i < args.Length - 1; i++) if (args[i] == "--ticks" && int.TryParse(args[i + 1], out var n)) ticks = n;
bool infinite = ticks <= 0;

var provider = new VanillaMemoryProvider();
var brain = new AdaptiveBrain();
var calibrator = new AutoCalibrator();
var input = new InputActuator { DryRun = !(live || testMove || summonerTest || (beatGame && !dryBeat) || ((farm || hunt) && !dryBeat)), Verbose = true };

Console.WriteLine("TerraMind-Core v0.4 scan+estrategia (sin Cheat Engine)");

// 0. SCAN: lee saves + heap real y propone estrategia. No calibra ni mueve nada.
if (scan)
{
    int[] userSlots = Array.Empty<int>();
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--slots")
            userSlots = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                .Where(n => n >= 1 && n <= 10).ToArray();

    var saves = TerrariaSaveScanner.Scan();
    Console.WriteLine($"Saves: jugador={saves.ActivePlayer} ({saves.PlayerBytes}B {saves.PlayerDate:HH:mm}) | mundo={saves.ActiveWorld} ({saves.WorldBytes / 1048576:F1}MB v{saves.WorldVersion} {saves.WorldDate:HH:mm}) | misma sesion={saves.SameSession}");

    using var clr = new ClrMdGameReader();
    bool okA = clr.Attach();
    Console.WriteLine($"Heap ClrMD attach: {okA} | {clr.LastError}");
    if (probe) { Console.WriteLine("--- PROBE ---"); Console.WriteLine(clr.Probe()); return; }
    var snap = clr.Scan();
    Console.WriteLine($"Snapshot: source={snap.Source} conf={snap.Confidence} {snap.Taken:HH:mm:ss}");
    Console.WriteLine($"  vida {snap.Life}/{snap.MaxLife} mana {snap.Mana}/{snap.MaxMana} def {snap.Defense} pos=({snap.PosX},{snap.PosY}) vel=({snap.VelX},{snap.VelY})");
    Console.WriteLine($"  alas {snap.WingTime}/{snap.WingTimeMax} minions {snap.NumMinions}/{snap.MaxMinions} slot={snap.ActiveSlot} buffs=[{string.Join(",", snap.Buffs.Select(b => clr.BuffName(b)))}]");
    Console.WriteLine($"  mundo dia={snap.IsDay} t={snap.WorldTime} | downed: {string.Join(",", snap.Downed.Where(kv => kv.Value).Select(kv => kv.Key))}");
    Console.WriteLine($"  hotbar: {string.Join(" | ", snap.Hotbar.Select(s => $"{s.Index + 1}:{s.Role}"))}");
    var inv = snap.Inventory.Where(s => !s.IsEmpty).Take(20).ToArray();
    Console.WriteLine($"  inv no vacios: {snap.Inventory.Count(s => !s.IsEmpty)} (top: {string.Join(" | ", inv.Select(s => $"{s.Index + 1}:{s.Role}"))})");
    Console.WriteLine($"  jefes activos: {(snap.ActiveBosses.Length == 0 ? "ninguno" : string.Join(";", snap.ActiveBosses.Select(b => $"{b.Name} {b.Life}/{b.MaxLife}")))}");
    Console.WriteLine($"  proyectiles hostiles: {snap.LiveProjectiles.Length}{(snap.LiveProjectiles.Length > 0 ? " (top: " + string.Join(";", snap.LiveProjectiles.Take(5).Select(p => $"t{p.Type} d{p.Damage} v({p.VelocityX:F1},{p.VelocityY:F1})")) + ")" : "")}");
    Console.WriteLine($"  {PhasePlan.Summary(snap.Downed)}");
    var opt = PhasePlan.PendingOptional(PhasePlan.PreHardmode.Concat(PhasePlan.Hardmode).ToArray(), snap.Downed);
    if (opt.Length > 0) Console.WriteLine($"  opcionales pendientes: {string.Join(",", opt)}");
    if (snap.Warnings.Count > 0) { Console.WriteLine($"  warnings({snap.Warnings.Count}):"); foreach (var x in snap.Warnings.Take(15)) Console.WriteLine($"    - {x}"); }

    var strat = StrategyBuilder.Build(snap, saves.ActivePlayer ?? "?", saves.ActiveWorld ?? "?", userSlots);
    Console.WriteLine($"--- ESTRATEGIA (conf {strat.Confidence}) ---");
    Console.WriteLine($"J: {strat.PlayerLine}");
    Console.WriteLine($"M: {strat.WorldLine}");
    Console.WriteLine($"Clase: {strat.SuggestedClass} ({strat.Reason}) | Etapa: {strat.Stage} | Siguiente: {strat.NextGoal}");
    Console.WriteLine($"Slots invocador: {string.Join(",", strat.SuggestedSummonSlots)} | Slots jefe: {string.Join(",", strat.SuggestedBossSlots)}");
    Console.WriteLine($"Farm: {string.Join(" / ", strat.FarmGoals)}");
    Console.WriteLine($"Crafteo: {string.Join(" / ", strat.CraftChecklist)}");
    var gear = SummonerGearPlan.Build(snap, clr.BuffNames);
    Console.WriteLine($"--- PLAN INVOCADOR PRE-HARDMODE ({gear.Pct}% | set {gear.ArmorSet}) ---");
    foreach (var n in gear.Needs)
        Console.WriteLine(n.Have is not null ? $"  [OK] {n.Slot}: {n.Have}" : $"  [..] {n.Slot}: FALTA {n.Missing} -> {n.FarmHint}{(n.Manual ? " [MANUAL]" : "")}");
    if (gear.EquipNow.Length > 0) Console.WriteLine($"  EQUIPA YA (en mochila): {string.Join(" | ", gear.EquipNow)}");
    var (ready, why) = SummonerGearPlan.WallReadiness(snap, gear);
    Console.WriteLine($"  MURO: {(ready ? "LISTO" : "NO")} - {why}");
    var eq = (snap.Equipped ?? Array.Empty<ItemSlot>()).Where(s => !s.IsEmpty).Take(12).ToArray();
    if (eq.Length > 0) Console.WriteLine($"  puesto: {string.Join(" | ", eq.Select(s => s.Role))}");
    return;
}

Console.WriteLine("TerraMind-Core v0.2 auto (sin Cheat Engine)");
bool attached = provider.Attach();
Console.WriteLine($"Attach Terraria.exe: {attached} | modo={(attached ? "juego detectado" : "simulado")} | input={(live ? "LIVE" : "DryRun")}");
Console.WriteLine($"Learned inicial: source={provider.Learned.Source} conf={provider.Learned.Confidence}");

// 1. AUTOCALIBRACION POR INTENTOS — siempre en simulacion, silenciosa, sin pulsar teclas.
// El bot prueba correr/volar en el mundo interno y mide. Con juego real reutiliza
// lo aprendido y sigue ajustando por timing en el loop (sin direcciones manuales).
Console.WriteLine("Calibrando por intentos (simulacion interna, sin tocar el juego)...");
var sim = provider.Simulation;
sim.Reset();
var cal = calibrator.Calibrate(
    get: () => sim.State,
    step: (mx, fly, jump) => sim.Step(mx, fly, jump),
    log: Console.WriteLine);
Console.WriteLine($"Calibrado: wingMax={cal.WingTimeMax} speed~{cal.MaxRunSpeed:F2} accelTicks={cal.AccelTicks} via {cal.Source}");
provider.Learned.LearnedWingTimeMax = cal.WingTimeMax;
provider.Learned.LearnedMaxRunSpeed = cal.MaxRunSpeed;
provider.Learned.IsLearned = true;
provider.Learned.Source = "intentos";
provider.Learned.Confidence = 0.85;

var cls = ClassDetector.Detect(activeItemId: 0, maxMana: 200, defense: 30, maxMinions: 1, hasWhipBuff: false);
Console.WriteLine($"Clase: {cls.Archetype} rango [{cls.IdealRangeMin}-{cls.IdealRangeMax}]");
if (live && attached) Console.WriteLine("MODO LIVE: auto-foco a Terraria + input real...");
if (live && attached)
{
    for (int i = 0; i < 6; i++)
    {
        bool ok = WindowFocus.FocusTerraria();
        Console.WriteLine($"Auto-foco intento {i + 1}: {ok}");
        if (ok) break;
        await Task.Delay(500);
    }
}
if (live && !attached) Console.WriteLine("AVISO --live sin Terraria: sigo en simulado.");

// 1b. TEST AISLADO: --test-move mantiene D 3s sin cerebro. Si esto no anda, es foco/pausa.
if (testMove)
{
    Console.WriteLine("TEST-MOVE: foco + HOLD D 3s...");
    for (int i = 0; i < 6; i++) { bool ok = WindowFocus.FocusTerraria(); Console.WriteLine($"Foco {i + 1}: {ok}"); if (ok) break; await Task.Delay(400); }
    await Task.Delay(1000); // tiempo para soltar el teclado y mirar el pj
    input.TestHoldD(3000);
    input.ReleaseAll();
    Console.WriteLine("TEST-MOVE fin. Si el pj no fue a la derecha 3s: juego en pausa/menu/sin foco.");
    provider.Dispose();
    return;
}

// 1c. INVOCADOR: --summoner-test [--slots 1,2,3] mira hora y auto-invoca.
// Como aun no lee inventario real, prueba los slots que le digas (1-10).
// Pon tus baculos en esos slots del hotbar antes de lanzar.
if (summonerTest)
{
    int[] slots = new[] { 0, 1, 2 };
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--slots")
            slots = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? Math.Clamp(n - 1, 0, 9) : -1)
                .Where(n => n >= 0).Distinct().ToArray();

    var sm = new SummonerManager();
    var clock = WorldTime.FromSystem(); // TODO: Main.time real cuando el scanner valide tu version
    sm.UpdateClock(clock);
    sm.SetState(SummonerState.Empty(maxMinions: 3));
    Console.WriteLine($"[Summoner] Hora: {clock.Label} dia={clock.IsDay} | slots a probar: {string.Join(",", slots.Select(s => s + 1))}");
    Console.WriteLine("[Summoner] Pon los baculos en esos slots, mundo abierto, sin pausa. Foco en 2s...");
    for (int i = 0; i < 6; i++) { bool ok = WindowFocus.FocusTerraria(); if (ok) break; await Task.Delay(400); }
    await Task.Delay(2000);

    int total = 0;
    foreach (var slot in slots)
    {
        Console.WriteLine($"[Summoner] slot {slot + 1}: selecciona + click x2...");
        input.SelectSlot(slot);
        await Task.Delay(250);
        input.LeftClick(); await Task.Delay(600);
        input.LeftClick(); await Task.Delay(600);
        total += 2;
        Console.WriteLine($"[Summoner] slot {slot + 1} invocado (mira si salio el minion, voy al siguiente)");
    }
    // Refresca latigo: ultimo slot como supuesto latigo, 1 latigazo.
    if (slots.Length > 0)
    {
        Console.WriteLine($"[Summoner] latigazo con slot {slots[^1] + 1}...");
        input.SelectSlot(slots[^1]); await Task.Delay(250);
        input.LeftClick(); await Task.Delay(400);
    }
    input.ReleaseAll();
    Console.WriteLine($"[Summoner] fin ({total} clicks). Si no salio nada: slots equivocados o sin mana. Repite con --slots 4,5,6 por ejemplo.");
    provider.Dispose();
    return;
}

// 1d. BEAT-GAME: mira progresion, farmea, invoca jefes en orden, hasta Moon Lord.
// Sin memoria real: --dry simula, live invoca de verdad con los slots del BossCatalog.
// Progresion real (muertes) entra con --report-kill hasta leer Main.npc.
if (beatGame)
{
    GameStage stage = GameStage.Start;
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--stage" && Enum.TryParse<GameStage>(args[i + 1], out var s)) stage = s;

    var beater = new GameBeater();
    beater.Load(new ProgressionState(stage, 0, stage >= GameStage.WallOfFlesh_Done, 400, "", DateTime.Now));
    var clock = WorldTime.FromSystem();
    Console.WriteLine($"[Beat] Hora: {clock.Label} | Etapa: {stage} | Siguiente: {beater.Progress.NextGoal} | modo={(dryBeat ? "DRY simulado" : "LIVE invoca de verdad")}");
    if (!dryBeat)
    {
        for (int i = 0; i < 6; i++) { if (WindowFocus.FocusTerraria()) break; await Task.Delay(400); }
        await Task.Delay(1500);
    }

    // Bucle corto: 12 decisiones (farm -> summon -> fight). En real seria infinito hasta MoonLord.
    for (int step = 0; step < 12 && !beater.Progress.GameBeaten; step++)
    {
        string msg = beater.Tick(clock,
            trySummon: (boss) =>
            {
                Console.WriteLine($"[Beat] Invocando {boss.Boss} con {boss.ItemName} slot {boss.HotbarSlot1Based} ({boss.When}/{boss.Where})...");
                if (dryBeat) return true;
                input.SelectSlot(boss.HotbarSlot1Based - 1); Task.Delay(300).Wait();
                input.LeftClick(); Task.Delay(800).Wait();
                return true;
            },
            farmTick: () => false); // false = aun farmeando; true lo marcas tu al matar
        Console.WriteLine($"[Beat {step}] {beater.State} | {msg} | farm:{beater.Farm.Current?.Target} {beater.Farm.Current?.KillsDone}/{beater.Farm.Current?.KillsWanted}");
        // Avanza farm simulado para que se vea el ciclo completo en --dry
        if (dryBeat && beater.State == GameBeater.BeaterState.Farm) beater.Farm.AddKill();
        if (dryBeat && beater.State == GameBeater.BeaterState.Fight) { beater.ReportKill(BossCatalog.NextFor(beater.Progress.Stage)?.Boss ?? "?"); }
        await Task.Delay(400);
    }
    Console.WriteLine($"[Beat] fin demo. Etapa final: {beater.Progress.Stage}. Para avanzar de verdad: mata al jefe y relanza con --stage {beater.Progress.Stage} (luego sera automatico).");
    Console.WriteLine("[Beat] Slots invocadores por defecto en BossCatalog.cs (4-7). Cambialos a tus slots reales.");
    provider.Dispose();
    return;
}

// 1e. FARM/HUNT: farmea de verdad (ataque+pociones+upkeep) y caza jefes con kill real.
// Lee snapshot ClrMD cada ~2s, pelea con la FSM, guarda progression.json al matar.
int[] userSlotsOf(string[] a)
{
    for (int i = 0; i < a.Length - 1; i++)
        if (a[i] == "--slots")
            return a[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                .Where(n => n >= 1 && n <= 10).ToArray();
    return Array.Empty<int>();
}
void EnsureMinions(InputActuator inp, int[] staffSlots, int max)
{
    foreach (var slot in staffSlots.Take(3))
    {
        inp.SelectSlot(slot); Task.Delay(250).Wait();
        inp.LeftClick(); Task.Delay(500).Wait();
        inp.LeftClick(); Task.Delay(500).Wait();
    }
}
bool HasBuff(LiveSnapshot s, ClrMdGameReader reader, string buffInternalName)
{
    var id = reader.BuffNames.FirstOrDefault(kv => kv.Value == buffInternalName).Key;
    return id != 0 && s.Buffs.Contains(id);
}
LiveSnapshot FakeFarmTick(SimulatedWorld sim, int tick)
{
    var st = sim.State;
    bool boss = tick is >= 100 and <= 160;
    var projs = boss
        ? new[]
        {
            new ProjectileState { Type = 1, PositionX = st.PositionX + 100, PositionY = st.PositionY, VelocityX = -6, VelocityY = 0, Damage = 20, Hostile = true, Active = true },
            new ProjectileState { Type = 2, PositionX = st.PositionX + 120, PositionY = st.PositionY - 30, VelocityX = -7, VelocityY = 1, Damage = 25, Hostile = true, Active = true },
        }
        : Array.Empty<ProjectileState>();
    return new LiveSnapshot(false, "simulado", 0, DateTime.Now, 400, 400, 200, 200,
        21, st.PositionX, st.PositionY, 0, 0, 180, 180, 1, 2, 0,
        new ItemSlot[] { new(0, 1, 1, "ThornWhipx1[LATIGO]"), new(3, 2, 1, "FlinxStaffx1[BACULO]") }, Array.Empty<ItemSlot>(), Array.Empty<int>(),
        true, 27000, "?", "?", new Dictionary<string, bool>(), boss ? new[] { new BossState { Type = 4, Name = "EyeofCthulhu", Life = 2000, MaxLife = 2800, Active = true } } : Array.Empty<BossState>(), projs, new List<string>());
}
LiveSnapshot FakeEmpressTick(SimulatedWorld sim, int tick)
{
    // Escenario guionizado FASE1->FASE2 diurna para probar cada patron de esquiva.
    var st = sim.State;
    float px = st.PositionX, py = st.PositionY;
    bool p2 = tick >= 165;
    var projs = new List<ProjectileState>();
    float bx = px, by = py, bvx = 0, bvy = 0;
    float ai0 = -1;
    string win = tick switch
    {
        < 40 => "bolts", < 70 => "windup", < 85 => "dash",
        < 125 => "ring", < 165 => "rainbow", _ => "waves",
    };
    if (!p2)
    {
        if (win == "bolts") for (int i = 0; i < 4; i++)
            projs.Add(new ProjectileState { Type = 10 + i, PositionX = px + 200 + i * 30, PositionY = py - 100 + i * 50, VelocityX = -5, VelocityY = 1, Damage = 100, Hostile = true, Active = true });
        if (win == "windup") { bx = px + 300; by = py; }
        if (win == "dash") { bx = px + 150; by = py; bvx = -25; }
        if (win == "ring") { ai0 = 4; for (int i = 0; i < 8; i++) { float a = i * MathF.PI / 4; projs.Add(new ProjectileState { Type = 20 + i, PositionX = px + MathF.Cos(a) * 350, PositionY = py + MathF.Sin(a) * 350, VelocityX = 0.5f, VelocityY = 0, Damage = 120, Hostile = true, Active = true }); } }
        if (win == "rainbow") { bx = px + 200; by = py - 200; for (int i = 0; i < 10; i++) { float a = i * MathF.PI * 2 / 10; projs.Add(new ProjectileState { Type = 30 + i, PositionX = bx + MathF.Cos(a) * 280, PositionY = by + MathF.Sin(a) * 280, VelocityX = MathF.Cos(a + 1) * 2, VelocityY = MathF.Sin(a + 1) * 2, Damage = 100, Hostile = true, Active = true }); } }
    }
    else
    {
        ai0 = 7; bx = px - 400; by = py - 100;
        for (int i = 0; i < 8; i++)
            projs.Add(new ProjectileState { Type = 40 + i, PositionX = px - 500, PositionY = py - 300 + i * 80, VelocityX = 14, VelocityY = 0, Damage = 130, Hostile = true, Active = true });
    }
    var boss = new BossState { Type = 636, Name = "EmpressofLight", Life = p2 ? 30000 : 70000, MaxLife = 70000, PositionX = bx, PositionY = by, VelocityX = bvx, VelocityY = bvy, Ai = new[] { ai0, 0, 0, 0 }, Damage = 150, Active = true };
    return new LiveSnapshot(false, "simulado", 0, DateTime.Now, 500, 500, 200, 200,
        21, px, py, 0, 0, 180, 180, 3, 4, 0,
        new ItemSlot[] { new(0, 1, 1, "ThornWhipx1[LATIGO]"), new(3, 2, 1, "StardustDragonStaffx1[BACULO]") }, Array.Empty<ItemSlot>(), Array.Empty<int>(),
        true, 15000, "?", "?", new Dictionary<string, bool>(), tick > 230 ? Array.Empty<BossState>() : new[] { boss }, projs.ToArray(), new List<string>());
}
if (farm || hunt)
{
    string progPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TerraMind-Core", "progression.json");
    Directory.CreateDirectory(Path.GetDirectoryName(progPath)!);
    GameStage savedStage = GameStage.Start;
    try
    {
        if (File.Exists(progPath))
        {
            var txt = File.ReadAllText(progPath);
            if (txt.Contains("MoonLord_Done")) savedStage = GameStage.MoonLord_Done;
            else foreach (GameStage g in Enum.GetValues<GameStage>())
                if (txt.Contains(g.ToString())) savedStage = (GameStage)Math.Max((int)savedStage, (int)g);
        }
    }
    catch { }

    bool isDry = dryBeat;
    Console.WriteLine($"[Farm] modo={(isDry ? "DRY (simulado, no toca el juego)" : "LIVE")} minutos={minutes} hunt={huntName} stage={savedStage}");
    using var clr2 = new ClrMdGameReader();
    bool okC = isDry ? false : clr2.Attach();
    Console.WriteLine($"[Farm] attach: {okC} | {clr2.LastError}");
    if (!isDry && !okC) { Console.WriteLine("[Farm] Sin juego. Usa --dry para simular."); return; }

    var snap0 = isDry ? new LiveSnapshot(false, "simulado", 0, DateTime.Now, 400, 400, 200, 200, 21, 0, 0, 0, 0, 180, 180, 0, 2, 0, new ItemSlot[] { new(0, 1, 1, "ThornWhipx1[LATIGO]"), new(3, 2, 1, "FlinxStaffx1[BACULO]") }, Array.Empty<ItemSlot>(), Array.Empty<int>(), true, 27000, "?", "?", new Dictionary<string, bool>(), Array.Empty<BossState>(), Array.Empty<ProjectileState>(), new List<string>())
        : clr2.Scan();
    var strat0 = StrategyBuilder.Build(snap0, "?", "?", userSlotsOf(args));
    var arche = strat0.SuggestedClass;
    var profile = arche switch
    {
        Archetype.Melee => new ClassProfile(arche, 60, 120, ""),
        Archetype.Mage => new ClassProfile(arche, 280, 450, ""),
        Archetype.Summoner => new ClassProfile(arche, 250, 400, ""),
        _ => new ClassProfile(Archetype.Ranged, 300, 450, ""),
    };
    Console.WriteLine($"[Farm] clase={arche} ({strat0.Reason}) | etapa={strat0.Stage} | siguiente={strat0.NextGoal}");
    Console.WriteLine($"[Farm] {PhasePlan.Summary(snap0.Downed)}");
    var gear0 = SummonerGearPlan.Build(snap0, clr2.BuffNames);
    var (ready0, why0) = SummonerGearPlan.WallReadiness(snap0, gear0);
    Console.WriteLine($"[Farm] gear invocador {gear0.Pct}% ({gear0.ArmorSet}) | MURO: {(ready0 ? "LISTO" : "NO")} - {why0}");
    int[] staffSlots = strat0.SuggestedSummonSlots.Select(s => s - 1).Where(s => s is >= 0 and <= 9).ToArray();
    int[] bossSlotsAuto = strat0.SuggestedBossSlots.Select(s => s - 1).Where(s => s is >= 0 and <= 9).ToArray();
    int weaponSlot = snap0.Hotbar.FirstOrDefault(s => s.Role.Contains("LATIGO")) is ItemSlot w1 && !w1.IsEmpty ? w1.Index
        : snap0.Hotbar.FirstOrDefault(s => s.Role.Contains("BACULO")) is ItemSlot w2 && !w2.IsEmpty ? w2.Index : 0;
    Console.WriteLine($"[Farm] staffs=[{string.Join(",", staffSlots.Select(s => s + 1))}] arma=slot {weaponSlot + 1} bossSlots=[{string.Join(",", bossSlotsAuto.Select(s => s + 1))}]");

    BossSummon? target = hunt ? BossCatalog.Order.FirstOrDefault(b => b.Boss.Contains(huntName, StringComparison.OrdinalIgnoreCase)) : null;
    if (hunt && target is null) { Console.WriteLine($"[Hunt] Jefe '{huntName}' no esta en el catalogo. Opciones: {string.Join(",", BossCatalog.Order.Select(b => b.Boss))}"); return; }
    int bossSlot = bossSlotOverride > 0 ? bossSlotOverride - 1 : (bossSlotsAuto.Length > 0 ? bossSlotsAuto[0] : 3);
    var startDowned = new HashSet<string>(snap0.Downed.Where(kv => kv.Value).Select(kv => kv.Key));
    bool bossSeen = false, killed = false, deadOnce = false;
    var beater2 = new GameBeater();
    beater2.Load(new ProgressionState((GameStage)Math.Max((int)savedStage, (int)strat0.Stage), 0, false, snap0.MaxLife ?? 400, "", DateTime.Now));

    if (!isDry)
    {
        Console.WriteLine("[Farm] Asegura minions + foco en 3s (mundo abierto, sin pausa)...");
        for (int i = 0; i < 6; i++) { if (WindowFocus.FocusTerraria()) break; await Task.Delay(400); }
        EnsureMinions(input, staffSlots, snap0.MaxMinions ?? 2);
        input.SelectSlot(weaponSlot); await Task.Delay(200);
        await Task.Delay(1500);
    }

    var deadline = DateTime.Now.AddMinutes(minutes);
    int tick = 0, lastUpkeep = 0;
    var empressDodge = new EmpressDodge();
    var sim2 = provider.Simulation; sim2.Reset();
    try
    {
        while (DateTime.Now < deadline && !killed)
        {
            bool dryEmpress = isDry && hunt && huntName.Contains("Empress", StringComparison.OrdinalIgnoreCase);
            LiveSnapshot s = isDry ? (dryEmpress ? FakeEmpressTick(sim2, tick) : FakeFarmTick(sim2, tick)) : clr2.Scan();
            bool potionSick = HasBuff(s, clr2, "PotionSickness");
            var p = new PlayerState
            {
                Life = s.Life ?? 100, MaxLife = s.MaxLife ?? 400, Mana = s.Mana ?? 20, MaxMana = s.MaxMana ?? 200,
                Defense = s.Defense ?? 10, HotbarIds = s.Hotbar.Select(h => h.Type).ToArray(),
                Buffs = s.Buffs, PotionSickness = potionSick,
                WingTime = s.WingTime ?? 0, WingTimeMax = s.WingTimeMax ?? 0,
                MaxRunSpeed = 11.5f, PositionX = s.PosX ?? 0, PositionY = s.PosY ?? 0,
                VelocityX = s.VelX ?? 0, VelocityY = s.VelY ?? 0,
            };
            var decision = brain.Tick(p, s.ActiveBosses, s.LiveProjectiles, profile);
            var act = GameAction.FromDecision(decision with { UseHealPotion = decision.UseHealPotion && !potionSick });

            // Emperatriz: modulo dedicado (diurna = furia one-shot, esquiva total, sin armas).
            var empress = s.ActiveBosses.FirstOrDefault(b => EmpressDodge.IsEmpress(b.Name));
            EmpressDodgeDecision? ed = null;
            if (empress is not null)
            {
                ed = empressDodge.Decide(p, empress, s.LiveProjectiles, s.IsDay ?? true);
                bool healOk = !ed.Enraged && decision.State == BrainState.Curacion && !potionSick;
                act = new GameAction(ed.MoveX, ed.Fly, ed.Fly, ed.Attack, healOk, false);
            }

            if (!isDry)
            {
                input.Execute(act);
                input.AttackHold(act.Attack);
                if (tick % 25 == 0 && act.Attack) input.AttackPulse(); // latigos
                if (tick % 120 == 0) WindowFocus.FocusTerraria();
            }
            else sim2.Step(act.MoveX, act.Fly, act.Jump);

            // Upkeep invocador cada ~15s.
            if (!isDry && tick - lastUpkeep > 900 && (s.NumMinions ?? 99) < (s.MaxMinions ?? 99))
            {
                Console.WriteLine($"[Farm] Upkeep: minions {s.NumMinions}/{s.MaxMinions}, re-invocando...");
                EnsureMinions(input, staffSlots, s.MaxMinions ?? 2);
                input.SelectSlot(weaponSlot);
                lastUpkeep = tick;
            }

            // Hunt: invocar cuando toca / detectar kill.
            if (hunt && target is not null)
            {
                var clock = new WorldTime(s.IsDay ?? true, s.WorldTime ?? 0, s.IsDay == true ? "dia" : "noche");
                if (s.ActiveBosses.Length > 0) bossSeen = true;
                if (!bossSeen && !isDry && target.CanAttempt(beater2.Progress.Stage, clock) && s.ActiveBosses.Length == 0)
                {
                    if (target.NeedsWorldTrigger)
                    {
                        if (tick % 300 == 0) Console.WriteLine($"[Hunt] {target.Boss}: {target.ItemName} ({target.Where}). Activalo tu y yo peleo. Esperando jefe...");
                    }
                    else if (tick == 1 || tick % 600 == 0)
                    {
                        Console.WriteLine($"[Hunt] Invocando {target.Boss} (slot {bossSlot + 1}, {target.When}/{target.Where})...");
                        input.SelectSlot(bossSlot); await Task.Delay(300);
                        input.LeftClick(); await Task.Delay(800);
                        input.SelectSlot(weaponSlot);
                    }
                }
                if (isDry && !dryEmpress && tick == 30) bossSeen = true; // simulacro generico
                if (dryEmpress && tick == 10) bossSeen = true;
                if (bossSeen && s.ActiveBosses.Length == 0 && (isDry ? (dryEmpress ? tick > 230 : tick > 60) : true))
                {
                    var newFlags = s.Downed.Where(kv => kv.Value).Select(kv => kv.Key).Except(startDowned).ToList();
                    if (isDry || newFlags.Count > 0 || (s.Life ?? 1) > 0 && tick > 100)
                    {
                        if (!isDry && newFlags.Count == 0) { /* puede ser wipe o boss despawn: sigue */ }
                        else
                        {
                            killed = true;
                            beater2.ReportKill(target.Boss);
                            if (!isDry) File.WriteAllText(progPath, $"{{\"stage\":\"{beater2.Progress.Stage}\",\"boss\":\"{target.Boss}\",\"at\":\"{DateTime.Now:O}\"}}");
                            Console.WriteLine($"[Hunt] KILL {target.Boss} -> etapa {beater2.Progress.Stage}.{(isDry ? " (dry: no guardado)" : " Guardado en progression.json")}");
                        }
                    }
                }
                if (!isDry && (s.Life ?? 1) <= 0 && !deadOnce) { deadOnce = true; Console.WriteLine("[Hunt] Mori. Reaparezco y sigo (jefe sigue con vida?)."); bossSeen = false; }
            }

            if (tick % (dryEmpress ? 20 : 120) == 0)
                Console.WriteLine($"[Farm t{tick}] {(ed is null ? $"{decision.State} | {decision.Reason}" : $"EMPERATRIZ {ed.Pattern}{(ed.Enraged ? "/FURIA" : "")}{(ed.Phase2 ? "/P2" : "")} | {ed.Reason}")} | vida {p.Life}/{p.MaxLife} | jefes:{s.ActiveBosses.Length} projs:{s.LiveProjectiles.Length} | dia={s.IsDay}");
            tick++;
            await Task.Delay(16);
        }
    }
    finally { input.ReleaseAll(); provider.Dispose(); }
    Console.WriteLine($"[Farm] fin. ticks={tick} kill={killed} etapa={beater2.Progress.Stage}");
    return;
}
sim.Reset();
int t = 0;
try
{
    while (infinite || t < ticks)
    {
        PlayerState p;
        if (provider.IsAttached && live)
        {
            // Con juego real aun sin lectura validada: usa lo calibrado + timing.
            // Cuando AobScanner valide tu version, aqui entraran lecturas reales.
            if (t % 60 == 0) WindowFocus.FocusTerraria(); // mantiene foco, si no SendInput va al terminal
            p = sim.State with { WingTimeMax = cal.WingTimeMax, MaxRunSpeed = cal.MaxRunSpeed };
            // Avanza sim en paralelo para no perder la referencia de intentos.
            var peek = brain.Tick(p, provider.GetBosses(), provider.GetHostileProjectiles(), cls);
            var actLive = GameAction.FromDecision(peek);
            input.Execute(actLive);
            sim.Step(actLive.MoveX, actLive.Fly, actLive.Jump);
        }
        else
        {
            p = sim.State with { WingTimeMax = cal.WingTimeMax, MaxRunSpeed = cal.MaxRunSpeed };
            var decision = brain.Tick(p, provider.GetBosses(), provider.GetHostileProjectiles(), cls);
            var action = GameAction.FromDecision(decision);
            input.Execute(action);
            sim.Step(action.MoveX, action.Fly, action.Jump);
            if (t % 30 == 0)
                Console.WriteLine($"[t{t}] {decision.State} | {decision.Reason} | pos=({p.PositionX:F1},{p.PositionY:F1}) wing={p.WingTime}/{p.WingTimeMax}");
        }

        if (live && attached && t % 30 == 0)
        {
            var q = sim.State;
            Console.WriteLine($"[live t{t}] input enviado a Terraria (foco requerido) sim=({q.PositionX:F1},{q.PositionY:F1})");
        }

        await Task.Delay(16);
        t++;
    }
}
finally { input.ReleaseAll(); provider.Dispose(); }

Console.WriteLine($"Demo terminada ({t} ticks). Compilacion OK, sin externos.");
Console.WriteLine(live ? "Si Terraria no se movio: dale foco a la ventana durante el loop y usa --ticks 0 para infinito."
    : "Para mover Terraria de verdad: abre mundo single-player offline, dale foco y: dotnet run --project src/TerraMind.App -c Release -- --live --ticks 0");
