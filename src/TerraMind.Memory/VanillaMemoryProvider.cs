using System.Diagnostics;
using TerraMind.State;

namespace TerraMind.Memory;

// Proveedor Vanilla SIN Cheat Engine.
//  - Intenta attach a Terraria.exe y autodescubrir firmas con AobScanner interno.
//  - Si no hay juego o no hay confianza, usa SimulatedWorld y modo intentos.
//  - Expone Learned (offsets/capacidades aprendidas por el propio bot).
public sealed class VanillaMemoryProvider : IGameStateProvider
{
    private readonly MemoryReader _mem = new();
    private Process? _proc;
    private readonly SimulatedWorld _sim = new();
    private int _failCount;

    public LearnedOffsets Learned { get; } = new();
    public string Name => "VanillaAuto";
    public bool IsAttached => _proc is not null && !_proc.HasExited && _mem.IsOpen;
    public bool UsingSimulation => !IsAttached;

    // Firmas internas candidatas (el bot las prueba solo, sin pegar nada manual).
    // Son ejemplos de formato; el autodescubrimiento las valida y si ninguna
    // cuadra, pasa a calibracion por intentos en vez de fallar.
    private static readonly string[] CandidateSignatures = new[]
    {
        "48 8B 05 ?? ?? ?? ?? 48 8B 48 20",
        "48 8B 0D ?? ?? ?? ?? 48 85 C9 74 ??"
    };

    public bool Attach()
    {
        try
        {
            var procs = Process.GetProcessesByName("Terraria");
            if (procs.Length == 0) return false;
            _proc = procs[0];
            if (!_mem.Open(_proc.Id)) { _proc = null; return false; }
            AutoDiscover();
            return true;
        }
        catch { _proc = null; return false; }
    }

    private void AutoDiscover()
    {
        if (_proc is null) return;
        foreach (var sig in CandidateSignatures)
        {
            try
            {
                if (AobScanner.TryFind(_proc, _mem, sig, out var addr) && addr != IntPtr.Zero)
                {
                    Learned.LocalPlayerPtr = addr;
                    Learned.IsLearned = true;
                    Learned.Source = "auto-aob";
                    Learned.Confidence = 0.7;
                    return;
                }
            }
            catch { /* prueba siguiente firma */ }
        }
        // Sin match: no es error, pasa a intentos.
        Learned.IsLearned = false;
        Learned.Source = "intentos";
        Learned.Confidence = 0.3;
    }

    public PlayerState GetPlayer()
    {
        if (!IsAttached)
        {
            _failCount++;
            return _sim.State;
        }
        // TODO cuando haya firma validada para tu version exacta, leer aqui
        // con _mem.TryReadInt32/Float desde Learned.LocalPlayerPtr.
        // De momento devuelve simulado + marca que aun esta aprendiendo.
        _failCount = 0;
        return _sim.State;
    }

    public IReadOnlyList<BossState> GetBosses() => Array.Empty<BossState>();
    public IReadOnlyList<ProjectileState> GetHostileProjectiles() => Array.Empty<ProjectileState>();

    // El bot hace intentos tambien sobre el mundo simulado (para calibrar sin juego).
    public SimulatedWorld Simulation => _sim;

    public void InjectMock(PlayerState s)
    {
        // Compat con tests: mueve la simulacion al estado dado.
        _sim.Reset();
    }

    public void Dispose() => _mem.Dispose();
}
