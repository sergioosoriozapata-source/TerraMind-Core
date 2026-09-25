using TerraMind.Brain;

namespace TerraMind.Input;

// Accion normalizada que Brain produce y el actuador ejecuta.
// En Vanilla se traduce a SendInput, en tModLoader a Player.control*.
public sealed record GameAction(
    float MoveX,
    bool Jump,
    bool Fly,
    bool Attack,
    bool UseHealPotion,
    bool SwitchWeapon)
{
    public static GameAction FromDecision(BrainDecision d) => new(
        d.MoveX, d.Jump, d.Fly,
        Attack: d.State == BrainState.Ofensivo,
        d.UseHealPotion,
        d.SwitchToSustainedWeapon);
}
