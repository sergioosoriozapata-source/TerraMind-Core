using TerraMind.Brain;
using TerraMind.State;

namespace TerraMind.TModPort;

// Fase 5: port a tModLoader. NO reescribir Brain.
// Este proveedor sustituye a VanillaMemoryProvider y se rellena con API directa:
//   Player.statLife, Player.statMana, Player.wingTime, Player.inventory,
//   Main.npc[i].ai[], Main.projectile[i].velocity
// Pasos:
//  1. Crear mod tModLoader que referencie TerraMind.State + TerraMind.Brain.
//  2. En ModPlayer.PostUpdate: copiar Main.LocalPlayer a PlayerState.
//  3. En GlobalNPC + GlobalProjectile: copiar ai/velocity a BossState/ProjectileState.
//  4. Llamar AdaptiveBrain.Tick() y aplicar con Player.controlLeft/Right/Jump.
public sealed class TModProvider : IGameStateProvider
{
    private PlayerState _last = new() { Life = 100, MaxLife = 100, Mana = 20, MaxMana = 20 };

    public string Name => "TModLoader";
    public bool IsAttached => true;

    public bool Attach() => true;

    public PlayerState GetPlayer() => _last;

    public IReadOnlyList<BossState> GetBosses() => Array.Empty<BossState>();

    public IReadOnlyList<ProjectileState> GetHostileProjectiles() => Array.Empty<ProjectileState>();

    public void Dispose() { }
}
