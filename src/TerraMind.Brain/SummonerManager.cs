using TerraMind.State;

namespace TerraMind.Brain;

// Cerebro invocador: mira hora, mira que baculos hay, los invoca solo.
// Estrategia por intentos (sin Cheat Engine):
//  1. Mira hora (dia/noche) para decidir si es momento seguro.
//  2. Prueba slots 0-9: selecciona + click, mide si ActiveMinions sube.
//     Los que suben se guardan como SummonHotbarSlots.
//  3. Si NeedsSummon: rota por esos slots invocando hasta llenar MaxMinions.
//  4. Si NeedsWhipRefresh: cambia a slot de latigo y pega 1 click.
// En simulado los conteos se fingen; con juego real usa buffs/maxMinions leidos.
public sealed class SummonerManager
{
    public SummonerState State { get; private set; } = SummonerState.Empty();
    public WorldTime Clock { get; private set; } = WorldTime.Unknown();
    public List<string> Log { get; } = new();

    public void UpdateClock(WorldTime t) { Clock = t; Log.Add($"Hora: {t.Label} (dia={t.IsDay})"); }

    public void SetState(SummonerState s) => State = s;

    // Descubre baculos probando. getMinions: lee minions actuales. trySummon(slot): selecciona+click.
    public int[] DiscoverStaves(Func<int> getMinions, Func<int, bool> trySummon, Action<string>? log = null)
    {
        var found = new List<int>();
        int before = getMinions();
        for (int slot = 0; slot < 10; slot++)
        {
            bool ok = trySummon(slot);
            int after = getMinions();
            // En juego real: si after > before es baculo. En simulado el llamador lo finge.
            if (ok && after > before) { found.Add(slot); before = after; log?.Invoke($"[Summon] slot {slot + 1} parece baculo (minions={after})"); }
            else log?.Invoke($"[Summon] slot {slot + 1} nada (minions={after})");
            if (found.Count >= 3) break; // no perder toda la partida probando
        }
        State = State with { SummonHotbarSlots = found.ToArray() };
        return found.ToArray();
    }

    // Invoca hasta llenar. Devuelve invocaciones hechas.
    public int EnsureFullArmy(Func<int> getMinions, Func<int, bool> trySummon, int maxTries = 6)
    {
        int made = 0;
        var slots = State.SummonHotbarSlots.Length > 0 ? State.SummonHotbarSlots : new[] { 0, 1, 2 };
        for (int i = 0; i < maxTries && getMinions() < State.MaxMinions; i++)
        {
            int slot = slots[i % slots.Length];
            if (trySummon(slot)) made++;
            Thread.Sleep(350); // Terraria necesita tiempo entre invocaciones
        }
        return made;
    }
}
