namespace TerraMind.Brain;

public enum BrainState
{
    Ofensivo,
    Defensivo,
    Curacion,
    EsquivaAerea
}

public sealed record BrainDecision(
    BrainState State,
    string Reason,
    float MoveX,   // -1 izquierda, +1 derecha
    bool Jump,
    bool Fly,
    bool UseHealPotion,
    bool SwitchToSustainedWeapon);
