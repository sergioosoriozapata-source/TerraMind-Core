namespace TerraMind.State;

public enum Archetype
{
    Unknown,
    Melee,   // Guerrero: defensa alta, distancia <120px
    Ranged,  // Arquero: gestiona municion, rango 300-450px
    Mage,    // Mago: sostenido vs burst por mana
    Summoner // Invocador: minions + latigo
}

public sealed record ClassProfile(
    Archetype Archetype,
    float IdealRangeMin,
    float IdealRangeMax,
    string Notes);

public static class ClassDetector
{
    // Heuristica v1 Vanilla. Se refina con item IDs reales de Fase 1.
    // itemIds: hotbar activo + armadura. manaMax/def ayudan a desambiguar.
    public static ClassProfile Detect(int activeItemId, int maxMana, float defense, int maxMinions, bool hasWhipBuff)
    {
        // IDs placeholder: en Fase 1 se sustituyen por IDs reales (Space Gun=434, etc.)
        // Regla simple y portable a tModLoader.
        if (maxMinions > 1 || hasWhipBuff)
            return new ClassProfile(Archetype.Summoner, 250, 400, "Mantener buff latigo, no re-summon en fight");

        if (maxMana > 100 && defense < 40)
            return new ClassProfile(Archetype.Mage, 280, 450, "Si Mana<30% cambiar a arma sostenida");

        if (defense >= 40)
            return new ClassProfile(Archetype.Melee, 60, 120, "Melee prioriza contacto");

        // Por defecto ranged: la mayoria de sets early usan arcos/armas.
        return new ClassProfile(Archetype.Ranged, 300, 450, "Kite circular");
    }
}
