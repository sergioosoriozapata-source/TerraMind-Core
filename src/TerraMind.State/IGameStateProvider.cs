namespace TerraMind.State;

// Abstraccion clave: Brain nunca toca memoria.
// Vanilla la implementa con ReadProcessMemory, tModLoader con Main.* directo.
public interface IGameStateProvider : IDisposable
{
    string Name { get; }
    bool IsAttached { get; }
    bool Attach();
    PlayerState GetPlayer();
    IReadOnlyList<BossState> GetBosses();
    IReadOnlyList<ProjectileState> GetHostileProjectiles();
}
